using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Configuration;
using System.IO;
using MongoDB;
using Utility;

namespace MySQL2MongoDB
{
    class Program
    {
        public static double tasksize = 0;
        public static int FileSize= Convert.ToInt32(ConfigurationManager.AppSettings["FileSize"]);
        public static int ThreadCount = Convert.ToInt32(ConfigurationManager.AppSettings["ThreadCount"]);
        static void Main(string[] args)
        {

            Console.WriteLine("程序功能：");
            Console.WriteLine("1-zone group insert;");
            Console.WriteLine("2-域名注册信息和解析状态查询并更新状态");
            Console.WriteLine("3-域名解析状态查询并更新状态");
            Console.Write("请输入对应的数字：");
            int input = Console.Read();
            string basepath = AppDomain.CurrentDomain.BaseDirectory;
            //Console.WriteLine("你输入的是：" + input.ToString());
            switch (input) {
                case 49:
                    break;
                case 50:
                    break;
                case 51:
                    string path = basepath + "Log\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                    if (File.Exists(path))
                        File.Delete(path);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    QueryDNSAnalysis();
                    break;
                case 52:

                    MongoInsertFromDnsrecords();
                    break;
                default:
                    break;
            }
            


                Thread[] tlist = new Thread[ThreadCount];
                double totalcount = 0;
                DataSet ds = new DataSet();

                //MongoInsertOne2();
                //MongoInsertFromAuthorities();
                //WhoisInfoDeal();
                #region function0
                //ds = MySQLHelper.Query("SELECT count(1) FROM dnsrecordstemp ");
                //totalcount = Convert.ToInt32(ds.Tables[0].Rows[0][0]);
                //tasksize = Math.Ceiling(totalcount / ThreadCount);
                //Console.WriteLine("TotalCount " + totalcount.ToString() + " PageSize " + tasksize.ToString() + " ");

                //for (int i = 0; i < ThreadCount; i++)
                //{
                //    tlist[i] = new Thread(new ParameterizedThreadStart(MongoInsertOne));
                //    tlist[i].Start(i);
                //}
                #endregion
                #region function1
                //DataSet ds = MySQLHelper.Query("SELECT count(DISTINCT zone) FROM dnsrecordstemp");
                // totalcount = Convert.ToInt32(ds.Tables[0].Rows[0][0]);
                //tasksize = Math.Ceiling(totalcount / ThreadCount);
                //Console.WriteLine("");
                //Console.WriteLine("TotalCount "+ totalcount.ToString()+" PageSize " + tasksize.ToString() + " ");

                //for (int i = 0; i < ThreadCount; i++) {
                //    tlist[i]= new Thread(new ParameterizedThreadStart(MongoInsertAll));
                //    tlist[i].Start(i);
                //} 
                #endregion
                #region function2
                //ds = MySQLHelper.Query("SELECT count(1) FROM  authorities WHERE Zone IN (SELECT Zone FROM zones WHERE NSState in (1,6,7)) ");
                //totalcount = Convert.ToInt32(ds.Tables[0].Rows[0][0]);
                //tasksize = Math.Ceiling(totalcount / ThreadCount);
                //Console.WriteLine("TotalCount " + totalcount.ToString() + " PageSize " + tasksize.ToString() + " ");

                //for (int i = 0; i < ThreadCount; i++)
                //{
                //    tlist[i] = new Thread(new ParameterizedThreadStart(MongoInsertFromAuthorities));
                //    tlist[i].Start(i);
                //}
                #endregion
                Console.ReadKey();
        }
        public static void MongoInsertAll(object ThreadIndex)
        {
            int startindex = Convert.ToInt32(ThreadIndex) * Convert.ToInt32(tasksize);

            DataSet ds = MySQLHelper.Query("SELECT DISTINCT zone FROM dnsrecordstemp limit " + startindex.ToString() + "," + Convert.ToInt32(tasksize).ToString());
            int RowCount = ds.Tables[0].Rows.Count;
            Console.WriteLine("Collection Query Success. Total " + RowCount.ToString() + " Row");
            DataTable dt = ds.Tables[0];
            long c = 0, dc = 0;
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时     
            foreach (DataRow dr in dt.Rows)
            {
                List<Domain> dl = new List<Domain>();
                string collection = dr[0].ToString().Trim();
                DataSet ds2 = MySQLHelper.Query("select zone,host,type,data,ttl,mx_priority,view from dnsrecordstemp where zone='" + collection + "'");
                if (ds2.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow dr2 in ds2.Tables[0].Rows)
                    {
                        Domain d = new Domain();
                        d.Name = dr2[1].ToString();
                        d.Type = dr2[2].ToString();
                        d.Rdata = dr2[3].ToString();
                        d.Ttl = int.Parse(dr2[4].ToString());
                        if (dr2[5] != DBNull.Value) d.Mx_priority = int.Parse(dr2[5].ToString());
                        d.View = dr2[6].ToString();
                        dl.Add(d);
                        dc++;
                    }
                    MongoHelper.MongoHelper2.InsertAll<Domain>(collection, dl);
                }
                if (c % 200 == 0 && c > 0)
                {
                    long t2 = watch.ElapsedMilliseconds;
                    Console.WriteLine("进程" + ThreadIndex.ToString() + " 添加 " + c.ToString() + " collection total " + dc.ToString() + " Rows. Use Time " + t2.ToString());
                }
                c++;
            }
            Console.WriteLine("进程" + ThreadIndex.ToString() + " 添加 " + RowCount + " collection total " + dc.ToString() + " Rows. Use Time " + watch.ElapsedMilliseconds.ToString());
            watch.Stop();
        }
        public static void MongoInsertOne(object ThreadIndex)
        {
            int startindex = Convert.ToInt32(ThreadIndex) * Convert.ToInt32(tasksize);
            long c = 0;
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时     
            DataSet ds = MySQLHelper.Query("select zone,host,type,data,ttl,mx_priority,view from dnsrecordstemp  ORDER BY Zone limit " + startindex.ToString() + "," + Convert.ToInt32(tasksize).ToString());
            long q = watch.ElapsedMilliseconds;
            DataTable dt = ds.Tables[0];
            long count = dt.Rows.Count;
            int idx = 0;
            Console.WriteLine("thread " + ThreadIndex.ToString() + ",query use time：" + q.ToString() + ";" + count + " rows data");
            while (idx < count)
            {
                List<Domain> dl = new List<Domain>();

                string zone = dt.Rows[idx][0].ToString();
                dl.Add(BuildDomain(dt.Rows[idx]));
                
                while (dt.Rows[idx][0].ToString() == dt.Rows[idx + 1][0].ToString())
                {
                    dl.Add(BuildDomain(dt.Rows[idx + 1]));
                    idx++;
                }
                MongoHelper.MongoHelper2.InsertAll<Domain>(zone, dl);
                //MySQLHelper.ExecuteSql(string.Format("update dnsrecordstemp set isdelete={0} where zone='{1}'", 1, zone));
                idx++;

                if (c % 100 == 0 && c > 0)
                    Thread.Sleep(500);
                if (c % 1000 == 0 && c > 0)
                    Console.WriteLine("thread {0}, {1} collection create;use time {2}", ThreadIndex, c, watch.ElapsedMilliseconds);
                c++;
            }
            Console.WriteLine("thread {0}, {1} collection create;use time {2}", ThreadIndex, c, watch.ElapsedMilliseconds);

            watch.Stop();//停止计时
        }
        /// <summary>
        /// 
        /// </summary>
        public static void MongoInsertOne2()
        {
            string baseDic = AppDomain.CurrentDomain.BaseDirectory;
            if (Directory.Exists(baseDic + "jsondata"))
                Directory.Delete(baseDic + "jsondata", true);
            if (Directory.Exists(baseDic + "command"))
                Directory.Delete(baseDic + "command", true);

            long c = 0;
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时     
            DataSet ds = MySQLHelper.Query("select zone,host,type,data,ttl,mx_priority,view from dnsrecordstemp  ORDER BY Zone"); 
            long q = watch.ElapsedMilliseconds;
            DataTable dt = ds.Tables[0];
            long count = dt.Rows.Count;
            int idx = 0;
            Console.WriteLine("query use time：" + q.ToString() + ";" + count + " rows data");
            while (idx < count)
            {
                if (c % FileSize == 0) {
                    ShCommandCreate.WriteToShCommandFile(";",(c/FileSize).ToString());
                }

                string zone = dt.Rows[idx][0].ToString();
                //ShCommandCreate.WriteToJsonFile(zone,JsonHelper.SerializeObject(BuildDomain(dt.Rows[idx])));
                while (idx<(count-1)&&dt.Rows[idx][0].ToString() == dt.Rows[idx + 1][0].ToString())
                {
                    //ShCommandCreate.WriteToJsonFile(zone, JsonHelper.SerializeObject(BuildDomain(dt.Rows[idx + 1])));
                    idx++;
                }
                ShCommandCreate.WriteToShCommandFile("mongoimport -d BindDns -c "+zone+"  --file /root/jsondata/"+zone+".json;", (c / FileSize).ToString());
                //mongoimport --host mongodb1.example.net --port 37017 --username user --password pass --collection contacts --db marketing --file /opt/backups/mdb1-examplenet.json
                idx++;

                if (c % 10000 == 0 && c > 0)
                    Console.WriteLine("{0} collection create;use time {1}", c, watch.ElapsedMilliseconds);
                c++;
            }
            Console.WriteLine("{0} collection create;use time {1}", c, watch.ElapsedMilliseconds);

            watch.Stop();//停止计时
        }
        static Domain BuildDomain(DataRow dr)
        {
            Domain d = new Domain();
            d.Name = dr[1].ToString();
            d.Type = dr[2].ToString();
            if (d.Type == "MX")
                d.Rdata = dr[5].ToString() + " " + dr[3].ToString();
            else
                d.Rdata = dr[3].ToString();
            d.Ttl = int.Parse(dr[4].ToString());
            if (dr[5] != DBNull.Value) d.Mx_priority = int.Parse(dr[5].ToString());
            d.View = dr[6].ToString();
            return d;
        }
        /// <summary>
        /// functionMongoInsertFromDnsrecords
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        static Domain BuildDomain2(DataRow dr)
        {
            Domain d = new Domain();
            d.Zone = dr[0].ToString();
            d.Name = dr[1].ToString();
            d.Type = dr[2].ToString();
            if (d.Type == "MX")
                d.Rdata = dr[5].ToString() + " " + dr[3].ToString();
            else
                d.Rdata = dr[3].ToString();
            d.Ttl = int.Parse(dr[4].ToString());
            if (dr[5] != DBNull.Value) d.Mx_priority = int.Parse(dr[5].ToString());
            d.View = dr[6].ToString();
            return d;
        }
        static void MongoInsertFromAuthorities()
        {
            string baseDic = AppDomain.CurrentDomain.BaseDirectory;
            if (Directory.Exists(baseDic + "authdata"))
                Directory.Delete(baseDic + "authdata", true);
            if (Directory.Exists(baseDic + "command"))
                Directory.Delete(baseDic + "command", true);
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时     
            DataSet ds = MySQLHelper.Query("SELECT Zone,Host,Data,Type,TTL,Mbox,Serial,Refresh,Retry,Expire,Minimum FROM  authorities WHERE Zone IN (SELECT distinct Zone FROM zones WHERE NSState in (1,6,7)) ORDER BY Zone");
            long q = watch.ElapsedMilliseconds;
            DataTable dt = ds.Tables[0];
            int count = dt.Rows.Count;
            int idx = 0;
            int c = 0;
            Console.WriteLine("query use time：" + q.ToString() + ";" + count + " rows data");
            while (idx < count)
            {
                if (c % FileSize == 0)
                    ShCommandCreate.WriteToShCommandFile(";", "a" + (c / FileSize).ToString());

                string zone = dt.Rows[idx][0].ToString();
                if (idx == count - 1)
                    ShCommandCreate.WriteToJsonFile(zone, JsonHelper.SerializeObject(BuildDomainBase(dt.Rows[idx])), "authdata");
                else
                    ShCommandCreate.WriteToJsonFile(zone, JsonHelper.SerializeObject(BuildDomainBase(dt.Rows[idx], dt.Rows[idx + 1])), "authdata");

                while (idx < (count - 1) && dt.Rows[idx][0].ToString() == dt.Rows[idx + 1][0].ToString())
                {
                    if (idx == count - 2)
                    {
                        ShCommandCreate.WriteToJsonFile(zone, JsonHelper.SerializeObject(BuildDomainBase(dt.Rows[idx + 1])), "authdata");
                        break;
                    }
                    else if (idx == count - 1)
                    {
                        break;
                    }
                    else
                        ShCommandCreate.WriteToJsonFile(zone, JsonHelper.SerializeObject(BuildDomainBase(dt.Rows[idx + 1], dt.Rows[idx + 2])), "authdata");
                    idx++;
                }
                if(idx<count-1)
                    ShCommandCreate.WriteToShCommandFile("mongoimport -d BindDns -c " + zone + "  --file /root/jsondata/" + zone + ".json;", "a" + (c / FileSize).ToString());
                idx++;
                if (c % 10000 == 0 && c > 0)
                    Console.WriteLine("{0} collection create;use time {1}", c, watch.ElapsedMilliseconds);
                c++;
            }

            Console.WriteLine("{0} collection create;use time {1}", c-1, watch.ElapsedMilliseconds);
            watch.Stop();//停止计时//305049  913076
        }

        static DomainBase BuildDomainBase(DataRow dr, DataRow dr2= null)
        {
            DomainBase d = new DomainBase();
            if (dr2 != null)
            {
                d.Name = dr[1].ToString();
                d.Type = dr[2].ToString();
                if (d.Type == "SOA")
                    d.Rdata = dr2[2].ToString() + " " + dr2[5].ToString() + " " + dr2[6].ToString() + " " + dr2[7].ToString() + " " + dr2[8].ToString() + " " + dr2[9].ToString() + " " + dr[10].ToString();
                else
                    d.Rdata = dr[3].ToString();
                d.Ttl = int.Parse(dr[4].ToString());
            }
            else {
                d.Name = dr[1].ToString();
                d.Type = dr[2].ToString();
                d.Rdata = dr[3].ToString();
                d.Ttl = int.Parse(dr[4].ToString());
            }
            return d;
        }

        /// <summary>
        /// functionMongoInsertFromDnsrecords
        /// </summary>
        static void MongoInsertFromDnsrecords() {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时     
            DataSet ds = MySQLHelper.Query("select zone,host,type,data,ttl,mx_priority,view from dnsrecordsregular where zone in(select distinct zone from zones where nsstate=1) ORDER BY Zone");
            long q = watch.ElapsedMilliseconds;
            DataTable dt = ds.Tables[0];
            long count = dt.Rows.Count;
            int idx = 0;
            Console.WriteLine("query use time：" + q.ToString() + ";" + count + " rows data");

            using (Mongo mongo = new Mongo(Utility.MongoHelper.connectionString))
            {
                mongo.Connect();

                while (idx < count)
                {
                    List<Domain> dl = new List<Domain>();
                    while (idx < count&&dl.Count<1001)
                    {
                        dl.Add(BuildDomain2(dt.Rows[idx]));
                        idx++;
                        if (idx % 10000 == 0 && idx > 0)
                            Console.WriteLine("{0} collection create;use time {1}", idx, watch.ElapsedMilliseconds);
                    }
                    IMongoDatabase friends = mongo.GetDatabase(Utility.MongoHelper.database);
                    IMongoCollection<Domain> categories = friends.GetCollection<Domain>("dnsrecords2");
                    categories.Insert(dl, true);
                    dl.Clear();
                }
                Console.WriteLine("{0} collection create;use time {1}", idx, watch.ElapsedMilliseconds);

                watch.Stop();//停止计时

                mongo.Disconnect();

            }
        }

        static void WhoisInfoDeal() {
            string baseDic = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql"))
            {
                File.Delete(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql"));
          
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时
            DataSet ds = MySQLHelper.Query("select zone from zonestemp where nsstate=1 limit 0,100");

            DataTable dt = ds.Tables[0];
            long count = dt.Rows.Count;
            Console.WriteLine("Query Success "+count.ToString()+"   use time " + watch.ElapsedMilliseconds.ToString());
            int isright = 0;
            int iswrong = 0;
            string[] dnsserver = new string[] { "114.114.114.114", "223.5.5.5", "180.76.76.76", "119.29.29.29", "101.226.4.6", "123.125.81.6", "202.141.162.123","123.206.61.167", "101.6.6.6","8.8.8.8" };
            for(int idx=0;idx<count;idx++)
            {
                DataRow dr = dt.Rows[idx];
                string zone = dr[0].ToString();
                if (zone.EndsWith(".info")||zone.EndsWith(".cn")) {
                    string nsname= WhoisHelper.GetDnsServersStr(WhoisHelper.SearchAWhois(zone));

                    if (nsname.ToLower().Contains("xundns.com"))
                    {
                        //Write2File.WriteToFile("update zonestemp set isload=1 where zone='" + zone + "'");
                        MySQLHelper.ExecuteSql("update zonestemp set isload=1 where zone='" + zone + "'");
                        Console.WriteLine(zone + " is here use time" + watch.ElapsedMilliseconds);
                        isright++;
                    }
                    else { iswrong++; }
                }
                else
                {
                    try
                    {
                        int aidx = idx % 10;
                        DNS.Client.ClientResponse respon = new DNS.Client.DnsClient(dnsserver[aidx]).Resolve(zone, DNS.Protocol.RecordType.NS);
                        if (respon.AnswerRecords.Count > 0)
                        {
                            DNS.Protocol.ResourceRecords.NameServerResourceRecord r = (DNS.Protocol.ResourceRecords.NameServerResourceRecord)respon.AnswerRecords[0];

                            if (r.NSDomainName.ToString().ToLower().Contains("xundns.com"))
                            {
                                Write2File.WriteToFile("update zonestemp set isload=1 where zone='" + zone + "'");
                                Console.WriteLine(zone + " is here use time" + watch.ElapsedMilliseconds);
                                isright++;
                            }
                        }
                        else { iswrong++; }
                        Thread.Sleep(500);
                    }
                    catch (Exception ex) {
                        string re = ex.ToString();
                    }
                }
                if (iswrong % 10 == 0 && iswrong > 0) {
                    Console.WriteLine(iswrong.ToString() + " is not here use time"+watch.ElapsedMilliseconds);
                }
            }
            Console.WriteLine(isright.ToString() + " Rows Updated;   use time" + watch.ElapsedMilliseconds.ToString());
            watch.Stop();
        }

        static void QueryDNSAnalysis() {

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时
            DataSet ds = MySQLHelper.Query("select zone from zonestemp where nsstate not in(1,2,4) and isload=0 limit 0,100");

            DataTable dt = ds.Tables[0];
            long count = dt.Rows.Count;
            int isright = 0;
            string[] dnsserver = new string[] { "114.114.114.114", "117.50.11.11", "180.76.76.76", "210.2.4.8", "101.226.4.6", "123.125.81.6", "202.141.162.123", "123.206.61.167", "101.6.6.6", "123.206.61.167" };
            for (int idx = 0; idx < count; idx++)
            {
                string zone = dt.Rows[idx][0].ToString();
                int aidx = idx % 10;
                try
                {
                    DNS.Client.ClientResponse respon = new DNS.Client.DnsClient(dnsserver[aidx]).Resolve(zone, DNS.Protocol.RecordType.NS);
                    if (respon.AnswerRecords.Count > 0)
                    {
                        DNS.Protocol.ResourceRecords.NameServerResourceRecord r = (DNS.Protocol.ResourceRecords.NameServerResourceRecord)respon.AnswerRecords[0];
                        if (r.NSDomainName.ToString().ToLower().Contains("xundns.com"))
                        {
                            MySQLHelper.ExecuteSql("update zonestemp set isload=1 where zone='" + zone + "'");
                            isright++;
                        }
                        else
                            MySQLHelper.ExecuteSql("update zonestemp set isload=-1 where zone='" + zone + "'");
                    }
                    else
                        WhoisDealing(zone);
                    Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    Write2File.WriteLogToFile(zone + "   use " + dnsserver[aidx] + "   " + ex.ToString());
                    Console.WriteLine(zone + " use " + dnsserver[aidx]+" query faile");
                    WhoisDealing(zone);
                    Console.WriteLine("mission restart");
                    QueryDNSAnalysis();
                }
            }
            Console.WriteLine("mission over");
            if (Convert.ToInt32(MySQLHelper.Query("select count(1) from zonestemp where nsstate not in(1,2,4) and isload=0 limit 0,100").Tables[0].Rows[0][0]) > 0)
                QueryDNSAnalysis();
            watch.Stop();
        }
        static void WhoisDealing(string zone)
        {
            try
            {
                string whoisinfo = WhoisHelper.SearchAWhois(zone);
                string servername = WhoisHelper.GetDnsServersStr(whoisinfo);
                if (servername.ToLower().Contains("xundns.com"))
                {
                    MySQLHelper.ExecuteSql("update zonestemp set isload=1 where zone='" + zone + "'");
                }
                else
                {
                    MySQLHelper.ExecuteSql("update zonestemp set isload=-1 where zone='" + zone + "'");
                }
            }
            catch (Exception ex)
            {
                MySQLHelper.ExecuteSql("update zonestemp set isload=-2 where zone='" + zone + "'");
                Write2File.WriteLogToFile(zone + "   whoisException    " + ex.ToString());
            }
        }

        //public static void ClientQuery(string domain)
        //{
        //    DnsMessage dnsMessage = DnsClient.Default.Resolve(domain, RecordType.A);
        //    if ((dnsMessage == null) || ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain)))
        //    {
        //        Console.WriteLine("DNS request failed");
        //    }
        //    else
        //    {
        //        foreach (DnsRecordBase dnsRecord in dnsMessage.AnswerRecords)
        //        {
        //            ARecord aRecord = dnsRecord as ARecord;
        //            if (aRecord != null)
        //            {
        //                Console.WriteLine("DNS request successfully : {0}", aRecord.Address.ToString());
        //            }
        //        }
        //    }
        //}
    }
    internal class Domain : DomainBase
    {
        public int Mx_priority { get; set; }
        public string View { get; set; } = "";
    }
    internal class DomainBase
    {
        public string Zone { get; set; }
        public string Name { get; set; } = "nametest";
        public string Type { get; set; } = "A";
        public string Rdata { get; set; }
        public int Ttl { get; set; } = 60;

    }
}
