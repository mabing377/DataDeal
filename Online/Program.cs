using BindDns.MongoDBEntity;
using Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Utility;


namespace Online
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("程序功能：");
            Console.WriteLine("1-zones;");
            Console.WriteLine("2-authorities");
            Console.WriteLine("3-dnsrecords");
            Console.WriteLine("4-checkSOA");
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
                    MongoInsertFromZones();
                    break;
                case 50:
                    MongoInsertFromAuthorities();
                    break;
                case 51:
                    MongoInsertFromDnsrecords();
                    break;
                case 52:
                    CheckSOACount();
                    break;
                //case 53:
                //    DeleteNoSOA();
                //    break;
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
        #region zones
        static void MongoInsertFromZones()
        {
            try
            {
                List<TempZoneID> temp1 = DtToList<TempZoneID>.ConvertToModel(MySQLHelper.Query("select id as zoneid from Temp where type in (1,4)").Tables[0]);
                DataTable dtid = MySQLHelper.Query("select min(id),max(id) from zones").Tables[0];
                long min = Convert.ToInt32(dtid.Rows[0][0]);
                long max = Convert.ToInt32(dtid.Rows[0][1]);

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();//开始计时  
                long index = min;
                do
                {
                    DataTable dt = MySQLHelper.Query("SELECT z.id,z.zone,z.userid,CASE when d.maxfensheng<1 then 0 else 1 end as level,z.nsstate from zones as z left join domainlevel as d on z.DomainLevel=d.levelvalue where z.userid<>348672 and z.id between " + index + " and " + (index + 20000) + " and z.Active='Y' and z.ForceStop='N'").Tables[0];
                    Console.WriteLine("GetDataTabel from mysql         Use Time={0};", watch.ElapsedMilliseconds);

                    List<zones> zonesList = DtToList<zones>.ConvertToModel(dt);
                    Console.WriteLine("DataTable Convert to ModelList; Use time={0};", watch.ElapsedMilliseconds);

                    List<ZonesSimple> dl = new List<ZonesSimple>();
                    foreach (zones z in zonesList)
                    {
                        if (temp1.FindAll(tz => tz.zoneid == z.id).Count == 0)
                            dl.Add(Row2ZoneSimple(z));
                    }
                    Console.WriteLine("Data Filter;                    Use time={0};", watch.ElapsedMilliseconds);

                    var client = DriverConfiguration.Client;
                    var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
                    IMongoCollection<ZonesSimple> categories = db.GetCollection<ZonesSimple>("zones");
                    if (dl.Count > 0)
                        categories.InsertMany(dl);
                    dl.Clear();
                    Console.WriteLine("MongoDB Inserted;               Use time={0};", watch.ElapsedMilliseconds);

                    index = index + 20001;
                    Console.WriteLine("min={0};max={1};index={2};use time={3}", min, max, index, watch.ElapsedMilliseconds);
                    Console.WriteLine("==============================================");
                } while (index < max);
                Console.WriteLine("End min={0};max={1};index={2};use time {3}", min, max, index, watch.ElapsedMilliseconds);
                watch.Stop();//停止计时
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        static ZonesSimple Row2ZoneSimple(zones tz)
        {
            ZonesSimple z = new ZonesSimple();
            z.domain = tz.zone.ToLower() + ".";
            string md5 = Utility.StringHelper.CalculateMD5Hash(z.domain);
            z.id = new ObjectId(md5);
            z.userid = Convert.ToInt32(tz.userid);
            z.rrcol = md5.Substring(0, 1).ToLower();
            z.level = Convert.ToInt32(tz.level);
            z.nsstate = tz.nsstate;
            return z;
        }
        #endregion

        #region authorities

        static void MongoInsertFromAuthorities()
        {
            try
            {
                List<TempZoneID> temp = DtToList<TempZoneID>.ConvertToModel(MySQLHelper.Query("select id as zoneid from Temp").Tables[0]);
                DataTable dtid = MySQLHelper.Query("select min(id),max(id) from zones").Tables[0];
                long min = Convert.ToInt32(dtid.Rows[0][0]);
                long max = Convert.ToInt32(dtid.Rows[0][1]);

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();//开始计时 
                long index = min;
                do
                {
                    DataTable dt = MySQLHelper.Query("select a.id,a.zone,host,data,type,ttl,mbox,serial,refresh,retry,expire,minimum ,t.userid,a.zoneid from authorities as a left join zones as t on a.ZoneID=t.id where a.ZoneID BETWEEN " + index + " and " + (index + 20000) + " and t.userid<>348672  and t.id is not NULL and t.Active='Y' and t.ForceStop='N'  order by a.zone,a.type").Tables[0];
                    Console.WriteLine("getdatatabel from mysql             use time={0};", watch.ElapsedMilliseconds);
                    List<authorities> aListtemp = DtToList<authorities>.ConvertToModel(dt);
                    Console.WriteLine("datatable convert to modellist;     use time={0};", watch.ElapsedMilliseconds);
                    List<authorities> aList = new List<authorities>();
                    foreach (authorities a in aListtemp)
                    {
                        if (temp.FindAll(tz => tz.zoneid == a.zoneid).Count == 0)
                            aList.Add(a);
                    }
                    List<AuthoritiesSimple>[] ala = new List<AuthoritiesSimple>[16] { new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>() };
                    List<authorities> drl = new List<authorities>();
                    List<AuthoritiesSimple> dl = new List<AuthoritiesSimple>();
                    string domain = "";
                    for (int idx = 0; idx < aList.Count;)
                    {
                        domain = aList[idx].zone.ToString().ToLower() + ".";
                        string collectionname = StringHelper.CalculateMD5Hash(domain).ToLower().Substring(0, 1);
                        drl.Add(aList[idx]);
                        while (idx < (aList.Count - 1) && aList[idx].zone == aList[idx + 1].zone)
                        {
                            drl.Add(aList[idx + 1]);
                            idx++;
                        }
                        if (drl.Count > 1)
                            foreach (AuthoritiesSimple a in Row2Authorities(drl))
                                ala[Int32.Parse(collectionname, System.Globalization.NumberStyles.HexNumber)].Add(a);

                        dl.Clear();
                        drl.Clear();
                        domain = "";
                        idx++;
                    }
                    Console.WriteLine("modellist convert;index={1};use time={0};", watch.ElapsedMilliseconds, index);
                    try
                    {
                        var client = DriverConfiguration.Client;
                        var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
                        for (int i = 0; i < 16; i++)
                        {
                            IMongoCollection<AuthoritiesSimple> collection = db.GetCollection<AuthoritiesSimple>(i.ToString("x"));
                            if (ala[i].Count > 0)
                                collection.InsertMany(ala[i]);
                        }
                        Console.WriteLine("mongodb inserted;index={1};use time={0};", watch.ElapsedMilliseconds, index);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Insert Data"+ex.Message);
                    }

                    index = index + 20001;
                    Console.WriteLine("min={0};max={1};index={2};use time={3}", min, max, index, watch.ElapsedMilliseconds);
                    Console.WriteLine("==============================================");
                } while (index < max);
                Console.WriteLine("end min={0};max={1};index={2};use time {3}", min, max, index, watch.ElapsedMilliseconds);
                watch.Stop();//停止计时
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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
        #endregion
        #region dnsrecords


        static void MongoInsertFromDnsrecords() {
            try
            {
                List<TempZoneID> temp = DtToList<TempZoneID>.ConvertToModel(MySQLHelper.Query("select id as zoneid from Temp").Tables[0]);
                DataTable dtid = MySQLHelper.Query("select min(id),max(id) from zones").Tables[0];
                long min = Convert.ToInt32(dtid.Rows[0][0]);
                long max = Convert.ToInt32(dtid.Rows[0][1]);

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();//开始计时  
                long index = min;
                do
                {
                    DataTable dt = MySQLHelper.Query("select d.id,d.zoneid,d.zone,d.host,d.type,d.data,d.ttl,d.view,d.mx_priority,d.userid from dnsrecords as d left join zones as t on d.ZoneID=t.id where d.Active='Y' and d.type<>'PTR' and d.ZoneID BETWEEN " + index + " and " + (index + 20000) + " and t.userid<>348672 and t.id is not NULL and t.Active='Y' and t.ForceStop='N' order by d.zone").Tables[0];
                    Console.WriteLine("GetDataTabel from mysql         Use Time={0};", watch.ElapsedMilliseconds);
                    List<dnsrecords> rListtemp = DtToList<dnsrecords>.ConvertToModel(dt);
                    Console.WriteLine("DataTable Convert to ModelList; Use time={0};", watch.ElapsedMilliseconds);
                    List<dnsrecords> rList = new List<dnsrecords>();
                    foreach (dnsrecords d in rListtemp)
                    {
                        if (temp.FindAll(tz => tz.zoneid == d.zoneid).Count == 0)
                            rList.Add(d);
                    }

                    List<dnsrecords> unList = new List<dnsrecords>();
                    List<DnsRecordsSimple>[] dla = new List<DnsRecordsSimple>[16] { new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>() };

                    foreach (dnsrecords dr in rList)
                    {
                        if (CheckRecordHost(dr.host, dr.type) && CheckRecordData(dr.data, dr.type, dr.view, dr.host))
                        {
                            DnsRecordsSimple d = Row2DnsRecords(dr);
                            string collectionname = StringHelper.CalculateMD5Hash(d.domain).ToLower().Substring(0, 1);
                            int idx = Int32.Parse(collectionname, System.Globalization.NumberStyles.HexNumber);
                            dla[idx].Add(d);
                        }
                        else
                        {
                            unList.Add(dr);
                        }
                    }
                    Console.WriteLine("ModelList Convert and Filter;   Use time={0};", watch.ElapsedMilliseconds);

                    var client = DriverConfiguration.Client;
                    var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);

                    for (int i = 0; i < 16; i++)
                    {
                        IMongoCollection<DnsRecordsSimple> collection = db.GetCollection<DnsRecordsSimple>(i.ToString("x"));
                        if (dla[i].Count > 0)
                            collection.InsertMany(dla[i]);
                    }
                    Console.WriteLine("MongoDB Inserted;               Use time={0};", watch.ElapsedMilliseconds);
                    if (unList.Count > 0)
                    {
                        IMongoCollection<dnsrecords> collection2 = db.GetCollection<dnsrecords>("dnsrecordswrong");
                        collection2.InsertMany(unList);
                    }
                    Console.WriteLine("MongoDB Inserted Wrong Data;    Use time={0};", watch.ElapsedMilliseconds);

                    index = index + 20001;
                    Console.WriteLine("min={0};max={1};index={2};use time {3}", min, max, index, watch.ElapsedMilliseconds);
                    Console.WriteLine("==============================================");
                } while (index < max);
                Console.WriteLine("End min={0};max={1};index={2};use time {3}", min, max, index, watch.ElapsedMilliseconds);
                watch.Stop();//停止计时
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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
        #endregion


        #region CheckAuthoritiesSOA
        static void CheckSOACount() {

            string[] collectionNames = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
            try
            {
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();//开始计时  
                List<Auth> al = new List<Auth>();
                foreach (string c in collectionNames)
                {

                    var client = DriverConfiguration.Client;
                    var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
                    
                    IMongoCollection<Auth> collection = db.GetCollection<Auth>(c);
                    var builder = Builders<Auth>.Filter;
                    al = collection.Find(builder.And(builder.Eq("type", "SOA"))).ToList<Auth>();

                    var groupList = al.GroupBy(x => new { x.domain })
                    .Select(group => new
                    {
                        Keys = group.Key,
                        Count=group.Count()
                    }).ToList();
                    var r = from g in groupList where g.Count>1 select g;
                    Console.WriteLine(c +" Collection "+r.Count());
                    if(r.Count()>0)
                        foreach (var rc in r)
                        {
                            Console.WriteLine(rc.Keys.domain+" This Domain Wrong");
                        }
                }
                watch.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("Mission Over");
        }
        #endregion
    }
    public sealed class Auth : AuthoritiesSimple
    {
        public ObjectId _id { get; set; }
    }
}
