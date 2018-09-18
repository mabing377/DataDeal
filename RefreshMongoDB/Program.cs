using BindDns.MongoDBEntity;
using Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using Utility;

namespace RefreshMongoDB
{
    class Program
    {
        static void Main(string[] args)
        {
                Console.WriteLine("程序功能：");
                Console.WriteLine("1-CheckZonesCount;");
                Console.WriteLine("2-CheckDataIntegrality");
                Console.WriteLine("3-ReMoMain");
            Console.WriteLine("4-UpdateZonesID");
                Console.WriteLine("5-TestZonesID");
            //Console.WriteLine("5-delete no SOA or NS");
            //Console.WriteLine("6-MongoDBTest");
            //Console.WriteLine("7-User Data Transfer");
            Console.Write("请输入对应的数字：");
                int input = Console.Read();
                string basepath = AppDomain.CurrentDomain.BaseDirectory;
                //Console.WriteLine("你输入的是：" + input.ToString());
                switchaction:
                switch (input)
                {
                    case 49:
                        CheckZonesCount();
                        break;
                    case 50:
                    CheckDataIntegrality();
                        break;
                    case 51:
                    ReMoMain();
                        break;
                    case 52:
                    UpdateZonesID();
                        break;
                case 53:                    
                    TestZonesID();
                    break;
                //case 54:
                //    mongotest();
                //    break;
                //case 55:
                //    DataTransfer();
                //    break;
                default:
                        break;
                }
                input = Console.Read();
                goto switchaction;
            Console.ReadKey();
        }
        static void TestZonesID() {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时  
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            IMongoCollection<ZonesSimple> categories = db.GetCollection<ZonesSimple>("zones");

            var count = categories.Count(Builders<ZonesSimple>.Filter.Empty);

            Console.WriteLine("Start");
            int idx = 0;
            int c = 0;
            do
            {
                var zlist = categories.Find(Builders<ZonesSimple>.Filter.Empty).Skip(idx * 50000).Limit(50000).ToList<ZonesSimple>();
                Console.WriteLine("Get     Data " + idx + "  use time" + watch.ElapsedMilliseconds);
                List<ZonesSimple> zslist = new List<ZonesSimple>();
                List<string> dArry = new List<string>();
                foreach (ZonesSimple z in zlist)
                {
                    string str = StringHelper.CalculateMD5Hash(z.domain).ToLower();
                    if (z.id.ToString() != str)
                    {
                        c++;
                    }
                }
                idx++;
                Console.WriteLine(c);
            }
            while (idx * 50000 < count);
            Console.WriteLine("End  use time" + watch.ElapsedMilliseconds);
        }
        static void UpdateZonesID()
        {

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时  
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            IMongoCollection<ZonesSimple> categories = db.GetCollection<ZonesSimple>("zones");

            var count = categories.Count(Builders<ZonesSimple>.Filter.Empty);
            
            Console.WriteLine("Start");
            int idx = 0;
            int wc = 0;
            do
            {
                var zlist = categories.Find(Builders<ZonesSimple>.Filter.Empty).Skip(idx*500).Limit(500).ToList<ZonesSimple>();
                Console.WriteLine("Get     Data " + idx + "  use time" + watch.ElapsedMilliseconds);
                foreach(ZonesSimple z in zlist)
                {
                    if (z.id.ToString() != StringHelper.CalculateMD5Hash(z.domain).ToLower())
                    {
                        z.id = new ObjectId(StringHelper.CalculateMD5Hash(z.domain).ToLower());
                        var builder = Builders<ZonesSimple>.Filter;
                        var filter = builder.And(builder.Eq("domain", z.domain));
                        try
                        {
                            categories.DeleteOne(filter);
                            categories.InsertOne(z);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("occur exception");
                        }
                    }
                    else {
                        wc++;
                        Console.WriteLine(z.domain +" without process");
                    }
                }
                Console.WriteLine("Process Data " + idx+ "  use time" + watch.ElapsedMilliseconds);
                idx++;
            }
            while (idx*500 < count);
            Console.WriteLine("wc = "+ wc);
            Console.WriteLine("End  use time" + watch.ElapsedMilliseconds);
        }
        static void CheckZonesCount() {
            DataTable dtid = MySQLHelper.Query("select min(id),max(id) from zones").Tables[0];
            long min = Convert.ToInt32(dtid.Rows[0][0]);
            long max = Convert.ToInt32(dtid.Rows[0][1]);

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时  
            long index = 3282504;
            do
            {
                DataTable dt = MySQLHelper.Query("SELECT z.id,z.zone,z.userid,0 as level,z.nsstate from zones as z where z.id between " + (index - 1000) + " and " + index + "").Tables[0];
                List<zones> zl = DtToList<zones>.ConvertToModel(dt);

                var client = DriverConfiguration.Client;
                var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);

                IMongoCollection<ZonesSimple> collection = db.GetCollection<ZonesSimple>("zones");
                foreach (zones z in zl)
                {
                    ZonesSimple zs = Row2ZoneSimple(z);
                    var builder = Builders<ZonesSimple>.Filter;
                    long c = collection.Find<ZonesSimple>(builder.And(builder.Eq("domain", zs.domain))).Count();
                    if (c > 1)
                        Console.WriteLine(zs.domain + "   count=" + c);
                }
                index = index - 1001;
                
                Console.WriteLine("min={0};max={1};index={2}; Use time={3};", min, max, index, watch.ElapsedMilliseconds);
                Console.WriteLine("==============================================");
            } while (index < max);
            Console.WriteLine("End min={0};max={1};index={2};use time {3}", min, max, index, watch.ElapsedMilliseconds);
            watch.Stop();//停止计时
        }
        static void CheckDataIntegrality() {
            List<TempZoneID> temp4 = DtToList<TempZoneID>.ConvertToModel(MySQLHelper.Query("select id as zoneid from Temp where type=4").Tables[0]);
            DataTable dtid = MySQLHelper.Query("select min(id),max(id) from zones").Tables[0];
            long min = Convert.ToInt32(dtid.Rows[0][0]);
            long max = Convert.ToInt32(dtid.Rows[0][1]);

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时  
            long index = max;
            do
            {
                DataTable dt = MySQLHelper.Query("SELECT z.id,z.zone,z.userid,0 as level,z.nsstate from zones as z where z.userid<>348672 and z.Active='Y' and z.ForceStop='N' and z.id between " + (index - 1000) + " and " + index + "").Tables[0];
                List<zones> zonesList = DtToList<zones>.ConvertToModel(dt);
                List<zones> zl = new List<zones>();
                foreach (zones z in zonesList)
                {
                    if (temp4.FindAll(tz => tz.zoneid == z.id).Count == 0)
                        zl.Add(z);
                }
                Console.WriteLine("Data Filter;Use time={0};", watch.ElapsedMilliseconds);

                List<ZonesSimple> zslist = new List<ZonesSimple>();
                List<AuthoritiesSimple>[] ala = new List<AuthoritiesSimple>[16] { new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>() };
                List<DnsRecordsSimple>[] dla = new List<DnsRecordsSimple>[16] { new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>() };

                foreach (zones z in zl)
                {
                    ZonesSimple zs = Row2ZoneSimple(z);
                    zslist.Add(zs);

                    string rrcol = StringHelper.CalculateMD5Hash(zs.domain).ToLower().Substring(0, 1);
                    //
                    DataSet ds = MySQLHelper.Query("select id,zone,host,data,type,ttl,mbox,serial,refresh,retry,expire,minimum ," + z.userid + " as userid from authorities where zoneid=" + z.id + " order by type;" +
                                                    "select id,zoneid,zone,host,type,data,ttl,view,mx_priority,userid from dnsrecords where active='Y'and type<>'PTR' and  zoneid= " + z.id + ";");

                    DataTable adt = ds.Tables[0];
                    DataTable rdt = ds.Tables[1];
                    //
                    List<authorities> alist = new List<authorities>();
                    List<AuthoritiesSimple> aslist = new List<AuthoritiesSimple>();
                    if (adt.Rows.Count > 0)
                        aslist = Row2Authorities(DtToList<authorities>.ConvertToModel(adt));
                    else
                        break;
                    //
                    List<dnsrecords> dlist = DtToList<dnsrecords>.ConvertToModel(rdt);
                    List<DnsRecordsSimple> dslist = new List<DnsRecordsSimple>();
                    List<dnsrecords> wrongList = new List<dnsrecords>();
                    foreach (dnsrecords d in dlist)
                    {
                        if (CheckRecordData(d.data, d.type, d.view, d.host) && CheckRecordHost(d.host, d.type))
                            dslist.Add(Row2DnsRecords(d));
                        else
                            //记录违法的records
                            wrongList.Add(d);
                    }

                }


                index = index - 1001;
                Console.WriteLine("Mongo Insert Success; Use time={0};", watch.ElapsedMilliseconds);
                DnsUpdateInsert(zl);

                Console.WriteLine("UpdateQueue Insert Success; Use time={0};", watch.ElapsedMilliseconds);

                Console.WriteLine("min={0};max={1};index={2};", min, max, index);
                Console.WriteLine("==============================================");
            } while (index < max);
            Console.WriteLine("End min={0};max={1};index={2};use time {3}", min, max, index, watch.ElapsedMilliseconds);
            watch.Stop();//停止计时
        }
        static void ReMoMain() {
            List<TempZoneID> temp4 = DtToList<TempZoneID>.ConvertToModel(MySQLHelper.Query("select id as zoneid from Temp where type=4").Tables[0]);
            DataTable dtid = MySQLHelper.Query("select min(id),max(id) from zones").Tables[0];
            long min = Convert.ToInt32(dtid.Rows[0][0]);
            long max = Convert.ToInt32(dtid.Rows[0][1]);

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时  
            long index = max;
            do
            {
                DataTable dt = MySQLHelper.Query("SELECT z.id,z.zone,z.userid,CASE when d.maxfensheng<1 then 0 else 1 end as level,z.nsstate from zones as z left join domainlevel as d on z.DomainLevel=d.levelvalue where z.userid<>348672 and z.Active='Y' and z.ForceStop='N' and z.id between " + (index - 1000) + " and " + index + "").Tables[0];
                List<zones> zonesList = DtToList<zones>.ConvertToModel(dt);
                List<zones> zl = new List<zones>();
                foreach (zones z in zonesList)
                {
                    if (temp4.FindAll(tz => tz.zoneid == z.id).Count == 0)
                        zl.Add(z);
                }
                Console.WriteLine("Data Filter;Use time={0};", watch.ElapsedMilliseconds);

                List<ZonesSimple> zslist = new List<ZonesSimple>();
                List<AuthoritiesSimple>[] ala = new List<AuthoritiesSimple>[16] { new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>() };
                List<DnsRecordsSimple>[] dla = new List<DnsRecordsSimple>[16] { new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>() };

                foreach (zones z in zl)
                {
                    ZonesSimple zs = Row2ZoneSimple(z);
                    zslist.Add(zs);

                    string rrcol = StringHelper.CalculateMD5Hash(zs.domain).ToLower().Substring(0, 1);
                    //
                    DataSet ds = MySQLHelper.Query("select id,zone,host,data,type,ttl,mbox,serial,refresh,retry,expire,minimum ," + z.userid + " as userid from authorities where zoneid=" + z.id + " order by type;" +
                                                    "select id,zoneid,zone,host,type,data,ttl,view,mx_priority,userid from dnsrecords where active='Y'and type<>'PTR' and  zoneid= " + z.id + ";");

                    DataTable adt = ds.Tables[0];
                    DataTable rdt = ds.Tables[1];
                    //
                    List<authorities> alist = new List<authorities>();
                    List<AuthoritiesSimple> aslist = new List<AuthoritiesSimple>();
                    if (adt.Rows.Count > 0)
                        aslist = Row2Authorities(DtToList<authorities>.ConvertToModel(adt));
                    else
                        break;
                    //
                    List<dnsrecords> dlist = DtToList<dnsrecords>.ConvertToModel(rdt);
                    List<DnsRecordsSimple> dslist = new List<DnsRecordsSimple>();
                    List<dnsrecords> wrongList = new List<dnsrecords>();
                    foreach (dnsrecords d in dlist)
                    {
                        if (CheckRecordData(d.data, d.type, d.view, d.host) && CheckRecordHost(d.host, d.type))
                            dslist.Add(Row2DnsRecords(d));
                        else
                            //记录违法的records
                            wrongList.Add(d);
                    }

                }


                index = index - 1001;
                Console.WriteLine("Mongo Insert Success; Use time={0};", watch.ElapsedMilliseconds);
                DnsUpdateInsert(zl);

                Console.WriteLine("UpdateQueue Insert Success; Use time={0};", watch.ElapsedMilliseconds);

                Console.WriteLine("min={0};max={1};index={2};", min, max, index);
                Console.WriteLine("==============================================");
            } while (index < max);
            Console.WriteLine("End min={0};max={1};index={2};use time {3}", min, max, index, watch.ElapsedMilliseconds);
            watch.Stop();//停止计时
        }
        static void MongoOperation(List<ZonesSimple> zslist, List<AuthoritiesSimple> aslist, List<DnsRecordsSimple> dslist, List<dnsrecords> wrongList)
        {
            List<string> domainlist = new List<string>();
            foreach (ZonesSimple zs in zslist) {
                domainlist.Add(zs.domain);
            }
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);

            IMongoCollection<ZonesSimple> categories = db.GetCollection<ZonesSimple>("zones");
            categories.DeleteMany(Builders<ZonesSimple>.Filter.In("domain", domainlist));
            //categories.DeleteOne(Builders<ZonesSimple>.Filter.Eq("domain", zs.domain));
            categories.InsertMany(zslist);

            //if (dslist.Count > 0)
            //{
            //    IMongoCollection<AuthoritiesSimple> collection1 = db.GetCollection<AuthoritiesSimple>(rrcol);
            //    collection1.DeleteMany(Builders<AuthoritiesSimple>.Filter.Eq("domain", zs.domain));
            //    collection1.InsertMany(aslist);
            //}
            //if (dslist.Count > 0)
            //{
            //    IMongoCollection<DnsRecordsSimple> collection2 = db.GetCollection<DnsRecordsSimple>(rrcol);
            //    collection2.InsertMany(dslist);
            //}
            //if (wrongList.Count > 0)
            //{
            //    IMongoCollection<dnsrecords> collection2 = db.GetCollection<dnsrecords>("illegaldnsrecords");
            //    collection2.InsertMany(wrongList);
            //}

        }
        static ZonesSimple Row2ZoneSimple(zones tz)
        {
            ZonesSimple z = new ZonesSimple();
            z.userid = Convert.ToInt32(tz.userid);
            z.domain = tz.zone.ToLower() + ".";
            z.rrcol = Utility.StringHelper.CalculateMD5Hash(z.domain).Substring(0, 1).ToLower();
            z.level = Convert.ToInt32(tz.level);
            z.nsstate = tz.nsstate;
            return z;
        }
        static List<AuthoritiesSimple> Row2Authorities(List<authorities> drl)
        {
            List<AuthoritiesSimple> dl = new List<AuthoritiesSimple>();
            for (int i = 0; i < drl.Count; i++)
            {
                AuthoritiesSimple d = new AuthoritiesSimple();
                d.rid = -long.Parse(drl[i].id.ToString());
                d.domain = drl[i].zone.ToLower() + ".";
                d.name = drl[i].host.ToLower();
                d.type = drl[i].type.ToString();
                if (d.type == "SOA")//Mbox,Serial,Refresh,Retry,Expire,Minimum
                    d.rdata = drl[i + 1].data.ToString() + " " + drl[i + 1].mbox.ToString() + " " + drl[i + 1].serial.ToString() + " " + drl[i + 1].refresh.ToString() + " " + drl[i + 1].retry.ToString() + " " + drl[i + 1].expire.ToString() + " " + drl[i + 1].minimum.ToString();
                else
                    d.rdata = drl[i].data.ToString();
                d.ttl = int.Parse(drl[i].ttl.ToString());
                d.userid = Convert.ToInt32(drl[i].userid);
                dl.Add(d);
            }
            return dl;
        }
        static DnsRecordsSimple Row2DnsRecords(dnsrecords dr)
        {
            DnsRecordsSimple d = new DnsRecordsSimple();
            d.rid = long.Parse(dr.id.ToString());
            d.domain = dr.zone.ToString().ToLower() + ".";
            d.name = dr.host.ToString().ToLower();
            d.type = dr.type.ToString();
            if (d.type == "MX")
                d.rdata = dr.mx_priority.ToString() + " " + dr.data.ToString().ToLower();
            else if (d.type == "TXT")
                d.rdata = dr.data.ToString().Replace("\"", string.Empty);
            else
                d.rdata = dr.data.ToString();
            d.ttl = Convert.ToInt32(dr.ttl);
            d.view = dr.view.ToString();
            d.userid = Convert.ToInt32(dr.userid);
            return d;
        }

        #region CheckData
        /// <summary>
        /// 验证是否子域名Host
        /// </summary>
        public const string CheckChildDomain = @"([\w-]+\.)*[\w-@]+";
        /// <summary>
        /// 验证主机记录
        /// </summary>
        public const string CheckHost = @"^([a-zA-Z0-9.*_@-]+|[a-zA-Z0-9.*_@-]*[\u2E80-\u9FFF]+|[\u2E80-\u9FFF]+[a-zA-Z0-9.*_@-]*)$";
        /// <summary>
        /// 验证Host首尾字符
        /// </summary>
        public const string CheckDnsName = @"^([^.-]+|[^.-]+.*[^.-]+|[^.-]+.*|.*[^.-]+)$";
        /// <summary>
        /// 验证子域名长度
        /// </summary>
        public const string CheckChildDomainLength = @"^(\*\.|(([^.]{1,63}\.)?){1,}[^.]{1,63})$";
        /// <summary>
        /// 验证IPV4
        /// </summary>
        public const string CheckIPV4 = @"^(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])$";
        /// <summary>
        /// 验证IPV6
        /// </summary>
        public const string CheckIPV6 = @"^\s*((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:)))(%.+)?\s*$";
        /// <summary>
        /// 验证是否合法Data
        /// </summary>
        public const string CheckData = @"^([_a-zA-Z0-9\u2E80-\u9FFF\uFF08\uFF09\u00b7]([-_a-zA-Z0-9\u2E80-\u9FFF\uFF08\uFF09\u00b7]{0,61}[a-zA-Z0-9\u2E80-\u9FFF\uFF08\uFF09\u00b7])?\.)+[a-zA-Z\u2E80-\u9FFF\uFF08\uFF09\u00b7]{2,18}?\.$";
        /// <summary>
        /// 验证IP形式的MX记录
        /// </summary>
        public const string CheckMX_IP = @"^(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.$";

        /// <summary>
        /// 验证SRV类型的记录值
        /// </summary>
        public const string CheckSRVData = @"^\d+\s+\d+\s+\d+\s+([a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,18}\.$";
        /// <summary>
        /// 验证TXT类型的记录值
        /// </summary>
        public const string CheckTXTData = @"^[-\\A-Za-z0-9*?_~=:;.@+^\/!""\s]+$";
        /// <summary>
        /// 判断记录的Host是否合法
        /// </summary>
        /// <param name="aRecord"></param>
        /// <param name="domainConfig"></param>
        private static bool CheckRecordHost(string Host, string Type)
        {
            if (Type != "PTR")
            {
                if (Host != "*")
                {
                    if (!Regex.IsMatch(Host, CheckChildDomain, RegexOptions.IgnoreCase) || Host.EndsWith("."))
                        return false;
                    if (Host.Contains("*") && (!Host.StartsWith("*") || Regex.Matches(Host, @"\*").Count > 1))
                        return false;
                }

                if (Host.Contains("@") && Host != "@")
                    return false;

                if (!Regex.IsMatch(Host, CheckHost, RegexOptions.IgnoreCase))
                    return false;
                if (!Regex.IsMatch(Host.Substring(0, 1), CheckDnsName) || !Regex.IsMatch(Host.Substring(Host.Length - 1, 1), CheckDnsName))
                    return false;
                if (!Regex.IsMatch(Host, CheckChildDomainLength))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 判断记录的值是否合法
        /// </summary>
        /// <param name="aRecord"></param>
        private static bool CheckRecordData(string Data, string Type, string View, string Host)
        {
            #region 判断数据的值是否合法

            if (Data.Length > 252)
                return false;

            if (Type == "A")
            {
                if (!Regex.IsMatch(Data, CheckIPV4, RegexOptions.IgnoreCase))
                    return false;
            }

            if (Type == "AAAA")
            {
                if (!Regex.IsMatch(Data, CheckIPV6))
                    return false;
            }

            if (Type == "CNAME" || Type == "NS" || Type == "PTR")
            {
                if (!Regex.IsMatch(Data, CheckData, RegexOptions.IgnoreCase))
                    return false;//记录的值不是一个合法的地址！
            }

            if (Type == "NS")
            {
                if (View != "Def") return false;//NS记录仅能添加通用线路类型！");
                if (Host == "" || Host == "@") return false;//NS记录不允许添加空主机头的记录！");
            }
            if (Type == "SRV")
            {
                if (!Regex.IsMatch(Data, CheckSRVData, RegexOptions.IgnoreCase)) return false;//记录的值不合法！", Type));
            }
            if (Type == "TXT")
            {
                if (!Regex.IsMatch(Data, CheckTXTData, RegexOptions.IgnoreCase)) return false;//记录的值不合法！", Type));
            }

            if (Type == "MX")
            {
                if (!Regex.IsMatch(Data, CheckData, RegexOptions.IgnoreCase) && !Regex.IsMatch(Data, CheckMX_IP, RegexOptions.IgnoreCase)) return false;//MX记录的值不是一个合法的地址！");
            }
            return true;
            #endregion
        }
        #endregion

        public static void DnsUpdateInsert(List<zones> zl) {

            String connsql = ConfigurationManager.AppSettings["SQLServerConnectionString"];// ""; // 数据库连接字符串,database设置为自己的数据库名，以Windows身份验证
            try
            {
                using (SqlConnection conn = new SqlConnection())
                {
                    conn.ConnectionString = connsql;
                    conn.Open(); // 打开数据库连接
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;
                    foreach (zones z in zl) {
                        String sql = string.Format("INSERT into DNSUpdateQueue(ZoneID,Domain,Status,ModifyState)VALUES({0},'{1}',0,0);",z.id,z.zone); // 查询语句
                        cmd.CommandText = sql;
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close(); // 关闭数据库连接
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
