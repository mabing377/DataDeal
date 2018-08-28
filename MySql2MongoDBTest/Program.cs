using Models;
using MongoDB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Utility;

namespace MySql2MongoDBTest
{
    class Program
    {
        public static List<ViewEnum> vel = new List<ViewEnum>();
        public static long PageSize = 100000;
        static void Main(string[] args)
        {
            Console.WriteLine("程序功能：");
            Console.WriteLine("1-zones;");
            Console.WriteLine("2-dnsrecords");
            Console.WriteLine("3-authorities");
            Console.WriteLine("4-deletePTR");
            Console.WriteLine("5-delete no SOA or NS");
            Console.WriteLine("6-MongoDBTest");
            Console.WriteLine("7-User Data Transfer");
            Console.Write("请输入对应的数字：");
            int input = Console.Read();
            string basepath = AppDomain.CurrentDomain.BaseDirectory;
            //Console.WriteLine("你输入的是：" + input.ToString());
            switchaction:
            switch (input)
            {
                case 49:
                    MongoInsertFromZones();
                    break;
                case 50:
                    MongoInsertFromDnsrecords();
                    break;
                case 51:
                    MongoInsertFromAuthorities();
                    break;
                case 52:
                    DeletePRT();
                    break;
                case 53:
                    DeleteNoSOA();
                    break;
                case 54:
                    mongotest();
                    break;
                case 55:
                    DataTransfer();
                    break;
                default:
                    break;
            }
            input = Console.Read();
            goto switchaction;
            Console.ReadKey();
        }
        static void MongoInsertFromZones()
        {
            try
            {
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();//开始计时  

                DataSet ds = MySQLHelper.Query("SELECT z.id,z.zone,z.userid,CASE when d.maxfensheng<1 then 0 else 1 end as `level`,z.nsstate from zonestemp as z left join domainlevel as d on z.DomainLevel=d.levelvalue where z.id<860948 and z.NSState=1 and z.State=0");

                long q = watch.ElapsedMilliseconds;
                DataTable dt = ds.Tables[0];
                long count = dt.Rows.Count;
                int idx = 0;
                Console.WriteLine("query use time：" + q.ToString() + ";" + count + " rows data");

                using (Mongo mongo = new Mongo(MongoHelper.connectionString))
                {
                    mongo.Connect();
                    IMongoDatabase mongodatabase = mongo.GetDatabase(MongoHelper.database);
                    while (idx < count)
                    {
                        List<ZonesSimple> dl = new List<ZonesSimple>();
                        while (idx < count && dl.Count < 1001)
                        {
                            dl.Add(Row2ZoneSimple(dt.Rows[idx]));
                            idx++;
                            if (idx % 10000 == 0 && idx > 0)
                                Console.WriteLine("{0} row insert;use time {1}", idx, watch.ElapsedMilliseconds);
                        }
                        IMongoCollection<ZonesSimple> categories = mongodatabase.GetCollection<ZonesSimple>("zones");
                        if (dl.Count > 0)
                            categories.Insert(dl, true);
                        dl.Clear();
                    }
                    Console.WriteLine("{0} row insert;use time {1}", idx, watch.ElapsedMilliseconds);
                    mongo.Disconnect();
                    watch.Stop();//停止计时
                }
            }
            catch (Exception ex)
            {
                string re = ex.ToString();
            }
        }
        static void MongoInsertFromDnsrecords()
        {
            vel = MongoHelper.GetAll<ViewEnum>("zonesip_view");
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时     

            long totalCount = long.Parse(MySQLHelper.Query("SELECT count(1) from zonestemp  where id<860948 and NSState=1 and State=0").Tables[0].Rows[0][0].ToString());
            Console.WriteLine("{0} rows data", totalCount);
            long zoneidx = 0;
            do
            {
                DataSet ds = MySQLHelper.Query("select a.zone,a.host,a.type,a.data,a.ttl,a.view,a.mx_priority,a.userid,a.id from (SELECT DISTINCT zone from zonestemp where id<860948 and NSState=1 and State=0 limit " + zoneidx.ToString() + ","+ PageSize.ToString() + ")as t inner join dnsrecordstemp as a on t.zone=a.Zone");
                long q = watch.ElapsedMilliseconds;
                DataTable dt = ds.Tables[0];
                long count = dt.Rows.Count;
                Console.WriteLine("query use time：" + q.ToString() + ";" + count + " rows data");

                List<DnsRescordsSimple>[] dla = new List<DnsRescordsSimple>[16] { new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>(), new List<DnsRescordsSimple>() };

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    DnsRescordsSimple d = Row2DnsRecords(dr);
                    string collectionname = StringHelper.CalculateMD5Hash(d.domain).ToLower().Substring(0, 1);
                    int idx = Int32.Parse(collectionname, System.Globalization.NumberStyles.HexNumber);
                    dla[idx].Add(d);
                }
                Console.WriteLine("List count" + dla.Length);
                try
                {
                    using (Mongo mongo = new Mongo(MongoHelper.connectionString))
                    {
                        mongo.Connect();
                        IMongoDatabase mongodatabase = mongo.GetDatabase(MongoHelper.database);
                        for (int i = 0; i < 16; i++)
                        {
                            IMongoCollection<DnsRescordsSimple> collection = mongodatabase.GetCollection<DnsRescordsSimple>(i.ToString("x"));
                            if (dla[i].Count > 0)
                                collection.Insert(dla[i], true);
                            Console.WriteLine("List " + i.ToString("x") + " obj count " + dla[i].Count);
                        }
                        mongo.Disconnect();
                        Console.WriteLine("zoneidx {0};use time {1}", zoneidx / PageSize, watch.ElapsedMilliseconds);
                        zoneidx = zoneidx + PageSize;
                    }
                }
                catch (Exception ex)
                {
                    string re = ex.ToString();
                }
            } while (zoneidx < totalCount);
            watch.Stop();//停止计时
        }
        static void MongoInsertFromAuthorities()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时     
            DataTable idTab = MySQLHelper.Query("SELECT min(id),max(id) from zonestemp where id<860948 and NSState=1 and State=0").Tables[0];
            long minID = long.Parse(idTab.Rows[0][0].ToString());
            long maxID = long.Parse(idTab.Rows[0][1].ToString());
            long tempID = minID-1;
            do
            {
                DataSet ds = MySQLHelper.Query("SELECT a.Zone,Host,Data,Type,TTL,Mbox,Serial,Refresh,Retry,Expire,Minimum,t.userid,a.id FROM (SELECT DISTINCT zone,userid from (select * from  zonestemp where id<860948 and NSState=1 and State=0) as tab where tab.id>" + tempID.ToString()+ " and tab.id<=" + (tempID+PageSize).ToString() + ")as t inner join  authorities as a on t.zone=a.Zone");
                long q = watch.ElapsedMilliseconds;
                DataTable dt = ds.Tables[0];
                int count = dt.Rows.Count;
                int idx = 0;
                List<AuthoritiesSimple>[] ala = new List<AuthoritiesSimple>[16] { new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>() };

                List<AuthoritiesSimple> dl = new List<AuthoritiesSimple>();
                List<DataRow> drl = new List<DataRow>();
                string domain = "";
                while (idx < count)
                {
                    domain = dt.Rows[idx][0].ToString().ToLower() + ".";
                    string collectionname = StringHelper.CalculateMD5Hash(domain).ToLower().Substring(0, 1);
                    drl.Add(dt.Rows[idx]);
                    while (idx < (count - 1) && dt.Rows[idx][0].ToString() == dt.Rows[idx + 1][0].ToString())
                    {
                        drl.Add(dt.Rows[idx + 1]);
                        idx++;
                    }
                    if (drl.Count > 1)
                    {
                        dl = Row2Authorities(drl);
                    }
                    foreach (AuthoritiesSimple a in dl)
                    {
                        ala[Int32.Parse(collectionname, System.Globalization.NumberStyles.HexNumber)].Add(a);
                    }
                    idx++;

                    dl.Clear();
                    drl.Clear();
                    domain = "";

                }

                using (Mongo mongo = new Mongo(MongoHelper.connectionString))
                {
                    mongo.Connect();
                    IMongoDatabase mongodatabase = mongo.GetDatabase(MongoHelper.database);
                    for (int i = 0; i < 16; i++)
                    {
                        IMongoCollection<AuthoritiesSimple> collection = mongodatabase.GetCollection<AuthoritiesSimple>(i.ToString("x"));
                        if (ala[i].Count > 0)
                            collection.Insert(ala[i], true);
                        Console.WriteLine("List " + i.ToString("x") + " obj count " + ala[i].Count);
                    }
                    mongo.Disconnect();
                }
                Console.WriteLine("zoneid {0};use time {1}", tempID, watch.ElapsedMilliseconds);
                tempID = tempID + PageSize;
            } while (tempID < maxID+1);
                //Console.WriteLine("{0} row inserted;use time {1}", idx, watch.ElapsedMilliseconds);
                watch.Stop();//停止计时//305049  913076
        }
        static ZonesSimple Row2ZoneSimple(DataRow dr)
        {
            ZonesSimple z = new ZonesSimple();
            z.userid = Convert.ToInt32(dr[2]);
            z.domain = dr[1].ToString().ToLower() + ".";
            z.rrcol = Utility.StringHelper.CalculateMD5Hash(z.domain).Substring(0, 1).ToLower();
            z.level = Convert.ToInt32(dr[3]);
            z.nsstate= Convert.ToInt32(dr[4]);
            return z;
        }

        static DnsRescordsSimple Row2DnsRecords(DataRow dr)
        {
            DnsRescordsSimple d = new DnsRescordsSimple();
            d.rid= long.Parse(dr[8].ToString());
            d.domain = dr[0].ToString().ToLower() + ".";
            d.name = dr[1].ToString().ToLower();
            d.type = dr[2].ToString();
            if (d.type == "MX")
                d.rdata = dr[6].ToString() + " " + dr[3].ToString().ToLower();
            else if (d.type == "TXT")
                d.rdata = dr[3].ToString().Replace("\"",string.Empty);
            else
                d.rdata = dr[3].ToString();
            d.ttl = Convert.ToInt32(dr[4]);
            d.view = dr[5].ToString();
            d.userid = Convert.ToInt32(dr[7]);
            return d;
        }
        static List<AuthoritiesSimple> Row2Authorities(List<DataRow> drl)
        {
            List<AuthoritiesSimple> dl = new List<AuthoritiesSimple>();
            for (int i = 0; i < drl.Count; i++)
            {
                AuthoritiesSimple d = new AuthoritiesSimple();
                d.rid =-long.Parse(drl[i][12].ToString());
                d.domain = drl[i][0].ToString().ToLower() + ".";
                d.name = drl[i][1].ToString().ToLower();
                d.type = drl[i][3].ToString();
                if (d.type == "SOA")
                    d.rdata = drl[i + 1][2].ToString() + " " + drl[i + 1][5].ToString() + " " + drl[i + 1][6].ToString() + " " + drl[i + 1][7].ToString() + " " + drl[i + 1][8].ToString() + " " + drl[i + 1][9].ToString() + " " + drl[i + 1][10].ToString();
                else
                    d.rdata = drl[i][2].ToString();
                d.ttl = int.Parse(drl[i][4].ToString());
                d.userid = Convert.ToInt32(drl[i][11]);
                dl.Add(d);
            }
            return dl;
        }

        static void DeletePRT()
        {
            string[] collection = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
            var doc = new Document("type", "PTR");
            foreach (string c in collection)
            {
                List<DnsRescordsSimple> dl = MongoHelper.GetList<DnsRescordsSimple>(c, doc);
                if (dl.Count > 0)
                {
                    foreach (DnsRescordsSimple d in dl)
                    {
                        var docc = new Document("domain", d.domain).Add("rrcol", c);
                        MongoHelper.Delete<ZonesSimple>("zones", docc);
                        MongoHelper.Delete<DnsRescordsSimple>(c, doc);
                    }
                    Console.WriteLine("collection " + c + " dealt");
                }
                else Console.WriteLine("collection " + c + " dont need deal");
            }
            Console.WriteLine("Mission Over");
        }
        static void DeleteNoSOA()
        {
            string[] collection = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
            try
            {
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();//开始计时   
                foreach (string c in collection)
                {
                    var count = MongoHelper.GetCount<ZonesSimple>("zones", new Document("rrcol", c));
                    Console.WriteLine(c + " collection has " + count + " documents");

                    List<ZonesSimple> zl = MongoHelper.GetList<ZonesSimple>("zones", new Document("rrcol", c));
                    int zcount = 0;
                    int dealcount = 0;
                    foreach (ZonesSimple z in zl)
                    {
                        try
                        {
                            List<DnsRescordsSimple> dl = MongoHelper.GetList<DnsRescordsSimple>(z.rrcol, new Document("domain", z.domain));
                            if (dl.Count == 0)
                            {
                                MongoHelper.Delete<ZonesSimple>("zones", new Document("domain", z.domain).Add("rrcol", z.rrcol));
                                zcount++;
                            }
                            else
                            {
                                var soalist = dl.FindAll(d => d.type == "SOA");
                                var nslist = dl.FindAll(d => d.type == "NS");
                                if (soalist.Count == 0 || nslist.Count == 0)
                                {
                                    MongoHelper.Delete<ZonesSimple>("zones", new Document("domain", z.domain).Add("rrcol", z.rrcol));
                                    zcount++;
                                    MongoHelper.Delete<DnsRescordsSimple>(z.rrcol, new Document("domain", z.domain));
                                    if (soalist.Count == 0)
                                        Console.WriteLine(z.domain + " no dnsrecords SOA data");
                                    if (nslist.Count == 0)
                                        Console.WriteLine(z.domain + " no dnsrecords NS data");
                                }
                            }
                            dl.Clear();
                            dealcount++;
                            if (dealcount > 0 && dealcount % 100 == 0)
                                Console.WriteLine(dealcount + " documents deal use time " + watch.ElapsedMilliseconds);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(z.domain + "Exception");
                        }
                    }
                    Console.WriteLine(c + " collection deal");
                }
                watch.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception2 ");
            }
            Console.WriteLine("Mission Over");
        }
        public static void mongotest()
        {
            List<DnsRescordsSimple> zl = MongoHelper.GetList<DnsRescordsSimple>("0", new Document("domain", "xiangyapromos.com."));
            Console.WriteLine(zl.Count);
        }
        /// <summary>
        /// C#反射遍历对象属性
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="model">对象</param>
        public static Document Model2Document<T>(T model)
        {
            Type t = model.GetType();
            PropertyInfo[] PropertyList = t.GetProperties();
            Document doc = new Document();
            foreach (PropertyInfo item in PropertyList)
            {
                doc.Add(item.Name, item.GetValue(model, null));
            }
            return doc;
        }
        public static Document DataRow2Document(DataRow dr, DataColumnCollection cs)
        {
            Document doc = new Document();
            foreach (DataColumn dc in cs)
            {
                doc.Add(dc.ColumnName, dr[dc.ColumnName]);
            }
            return doc;
        }


        public static void DataTransfer()
        {
            string userid = "426446";
            DataSet zds = MySQLHelper.Query("SELECT * from zones where userid=" + userid);
            DataTable zdt = zds.Tables[0];
            IList<zones> zl = ConvertTo<zones>(zdt);
            if (zl.Count > 0)
                MongoHelper.InsertAll<zones>("zones", zl);
            Console.WriteLine("zones inserted");
            DataSet ads = MySQLHelper.Query("SELECT * from authorities where ZoneID in (select id from zones where UserID=" + userid + ");");
            DataTable adt = ads.Tables[0];
            IList<authorities> al = ConvertTo<authorities>(adt);
            if (al.Count > 0)
                MongoHelper.InsertAll<authorities>("authorities", al);
            Console.WriteLine("authorities inserted");
            DataSet dds = MySQLHelper.Query("SELECT * from dnsrecords where UserID=" + userid + ";");
            DataTable ddt = dds.Tables[0];
            IList<dnsrecords> dl = ConvertTo<dnsrecords>(ddt);
            if (dl.Count > 0)
                MongoHelper.InsertAll<dnsrecords>("dnsrecords", dl);
            Console.WriteLine("dnsrescords inserted");
        }

        public static IList<T> ConvertTo<T>(DataTable table)
        {
            if (table == null)
            {
                return null;
            }

            List<DataRow> rows = new List<DataRow>();

            foreach (DataRow row in table.Rows)
            {
                rows.Add(row);
            }

            return ConvertTo<T>(rows);
        }

        public static IList<T> ConvertTo<T>(IList<DataRow> rows)
        {
            IList<T> list = null;

            if (rows != null)
            {
                list = new List<T>();

                foreach (DataRow row in rows)
                {
                    T item = CreateItem<T>(row);
                    list.Add(item);
                }
            }

            return list;
        }

        public static T CreateItem<T>(DataRow row)
        {
            T obj = default(T);
            if (row != null)
            {
                obj = Activator.CreateInstance<T>();

                foreach (DataColumn column in row.Table.Columns)
                {
                    PropertyInfo prop = obj.GetType().GetProperty(column.ColumnName.ToLower());
                    try
                    {
                        object value = row[column.ColumnName];
                        prop.SetValue(obj, value, null);
                    }
                    catch
                    {  //You can log something here     
                       //throw;    
                    }
                }
            }

            return obj;
        }
        public static int GetView(string name)
        {
            ViewEnum ve = vel.Where(v => v.name.ToLower() == name.ToLower()).SingleOrDefault();
            return ve.view;
        }
    }
}
