using BindDns.MongoDBEntity;
using Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
            Console.WriteLine("5-CheckMXData");
            //Console.WriteLine("5-delete no SOA or NS");
            Console.WriteLine("6-RefreshNewColumn");
            Console.WriteLine("7-RefreshOldData");
            Console.WriteLine("8-DeleteSOANS");
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
                case 53:
                    CheckMXData();
                    break;
                case 54:
                    RefreshNewColumn();
                    break;
                case 55:
                    RefreshOldData();
                    break;
                case 56:
                    DeleteSOANS();
                    break;
                case 57:
                    DeleteSOANS2();
                    break;
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
                    DataTable dt = MySQLHelper.Query("SELECT z.id,z.zone,z.userid,CASE when d.maxfensheng<1 then 0 else 1 end as level,z.nsstate,z.active,z.forcestop from zones as z left join domainlevel as d on z.DomainLevel=d.levelvalue where z.userid<>348672 and z.id between " + index + " and " + (index + 20000) + " and z.Active='Y' and z.ForceStop='N'").Tables[0];
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
                        try
                        {
                            categories.InsertMany(dl);
                        }
                        catch (Exception ex) {
                            Console.WriteLine(ex.Message);
                        }
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
            string md5 = Utility.StringHelper.CalculateMD5Hash(z.domain).ToLower();
            z.id = md5;
            z.userid = Convert.ToInt32(tz.userid);
            z.rrcol = md5.Substring(0, 1).ToLower();
            z.level = Convert.ToInt32(tz.level);
            z.nsstate = tz.nsstate;
            z.is_stop = tz.active == "Y" ? "N" : "Y";
            z.force_stop = tz.forcestop;
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
                d.is_stop = "N";
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
                    DataTable dt = MySQLHelper.Query("select d.id,d.zoneid,d.zone,d.host,d.type,d.data,d.ttl,d.view,d.mx_priority,d.userid,d.active from dnsrecords as d left join zones as t on d.ZoneID=t.id where d.Active='Y' and d.type<>'PTR' and d.ZoneID BETWEEN " + index + " and " + (index + 20000) + " and t.userid<>348672 and t.id is not NULL and t.Active='Y' and t.ForceStop='N' order by d.zone").Tables[0];
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
            d.is_stop = dr.active;
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

        #region 处理系统更新时，同步队列中的数据
        static void ProcessUpdateQueueData(){
            DataTable dt = SqlHelper.ExcuteTable("select DISTINCT ZoneID from DNSUpdateQueue where CreateTime>CONVERT(datetime,'2018-09-21 10:00:00',101) and CreateTime<CONVERT(datetime,'2018-09-21 11:00:00',101)");
            foreach (DataRow dr in dt.Rows) {
                long zoneid = Convert.ToInt32(dr[0]);
            }
        }
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

        #region 校验MXdata格式
        static void CheckMXData()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时  
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);

            for (int i = 0; i < 16; i++)
            {
                IMongoCollection<DnsRecordsSimple> collection = db.GetCollection<DnsRecordsSimple>(i.ToString("x"));
                var builder = Builders<DnsRecordsSimple>.Filter;
                var drslist = collection.Find(builder.And(builder.Eq("type", "MX"))).ToList<DnsRecordsSimple>();
                string ids = "";
                for (int j = 0; j < drslist.Count; j++) {
                    if (j > 0 && j % 10 == 9)
                    {
                        ids = ids + drslist[j].rid.ToString();
                        DataTable dt = MySQLHelper.Query("select d.id,d.zoneid,d.zone,d.host,d.type,d.data,d.ttl,d.view,d.mx_priority,d.userid from dnsrecords as d  where d.id in(" + ids + ")").Tables[0];
                        List<dnsrecords> rListtemp = DtToList<dnsrecords>.ConvertToModel(dt);
                        if (dt.Rows.Count > 0)
                        {
                            for (int k = j; k > j - 10; k--) {
                                var tmepRecord= rListtemp.Find(s => s.id.Equals(drslist[k].rid));
                                
                                if (tmepRecord != null && drslist[k].rdata != tmepRecord.mx_priority.ToString() + " " + tmepRecord.data.ToString().ToLower())
                                {
                                    Console.WriteLine(drslist[k].domain + "    " + drslist[k].rdata);
                                    Console.WriteLine(tmepRecord.mx_priority.ToString() + " " + tmepRecord.data.ToString().ToLower());
                                    Console.WriteLine("========================================");
                                }
                            }
                        }
                        ids = "";
                    }
                    else {
                        ids = ids + drslist[j].rid.ToString() + ",";
                    }
                }
                Console.WriteLine(i.ToString("x") + " Collection Processed; Use Time="+watch.ElapsedMilliseconds);
            }
        }
        #endregion


        #region 更新新添列
        static void RefreshNewColumn() {

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时 

            List<ZonesSimple> dl = new List<ZonesSimple>();
            List<AuthoritiesSimple>[] ala = new List<AuthoritiesSimple>[16] { new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>(), new List<AuthoritiesSimple>() };
            List<DnsRecordsSimple>[] dla = new List<DnsRecordsSimple>[16] { new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>() };

            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            IMongoCollection<ZonesSimple> cz = db.GetCollection<ZonesSimple>("zones");

            DataTable dt = MySQLHelper.Query("SELECT z.id,z.zone,z.userid,CASE when d.maxfensheng<1 then 0 else 1 end as level,z.nsstate,z.active,z.forcestop from zones as z left join domainlevel as d on z.DomainLevel=d.levelvalue where z.userid<>348672 and (z.Active='N' or z.ForceStop='Y' ) ").Tables[0];

            DataTable dta = MySQLHelper.Query("select a.id,a.zone,host,data,type,ttl,mbox,serial,refresh,retry,expire,minimum ,t.userid,a.zoneid from authorities as a left join zones as t on a.ZoneID=t.id where t.userid<>348672 and (t.Active='N' or t.ForceStop='Y')").Tables[0];

            DataTable dtd = MySQLHelper.Query("select d.id,d.zoneid,d.zone,d.host,d.type,d.data,d.ttl,d.view,d.mx_priority,d.userid,d.active from dnsrecords as d left join zones as t on d.ZoneID=t.id where t.userid<>348672 and (t.Active='N' or t.ForceStop='Y')").Tables[0];

            Console.WriteLine("zones total count  " + dt.Rows.Count);
            Console.WriteLine("authorities total count  " + dta.Rows.Count);
            Console.WriteLine("dnsrecords total count  " + dtd.Rows.Count);

            List< zones> zonesList= DtToList<zones>.ConvertToModel(dt);
            int deleteCount = 0;
            IList<string> zoneArry = new List<string>();
            foreach (zones z in zonesList)            {
                zoneArry.Add(z.zone + ".");

                ZonesSimple zs = Row2ZoneSimple(z);
                dl.Add(zs);
                if ((deleteCount > 0 && deleteCount % 200 == 0) || deleteCount==zonesList.Count)
                {
                    cz.DeleteMany(Builders<ZonesSimple>.Filter.In("domain", zoneArry));
                    string rrcol = StringHelper.CalculateMD5Hash(z.zone + ".").Substring(0, 1);
                    IMongoCollection<DnsRecordsSimple> cd = db.GetCollection<DnsRecordsSimple>(rrcol);
                    cd.DeleteMany(Builders<DnsRecordsSimple>.Filter.In("domain", zoneArry));

                    Console.WriteLine("delete count  " + deleteCount + " use time " + watch.ElapsedMilliseconds);

                    if (dl.Count > 0)
                    {
                        cz.InsertMany(dl);
                        Console.WriteLine("insert zones use time  " + watch.ElapsedMilliseconds);
                    }
                    zoneArry.Clear();
                    dl.Clear();
                }

                deleteCount++;
            }
            Console.WriteLine("delete complete use time "+watch.ElapsedMilliseconds);


            List<authorities> aList = DtToList<authorities>.ConvertToModel(dta);
            List<authorities> drl = new List<authorities>();
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
                
                drl.Clear();
                domain = "";
                idx++;
            }
            List<dnsrecords> rList = DtToList<dnsrecords>.ConvertToModel(dtd);
            List<dnsrecords> unList = new List<dnsrecords>();
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

            Console.WriteLine("start insert use time " + watch.ElapsedMilliseconds);


            for (int i = 0; i < 16; i++)
            {
                IMongoCollection<DnsRecordsSimple> collection = db.GetCollection<DnsRecordsSimple>(i.ToString("x"));
                if (ala[i].Count > 0)
                    collection.InsertMany(ala[i]);
                if (dla[i].Count > 0)
                    collection.InsertMany(dla[i]);
            }
            if (unList.Count > 0)
                Console.WriteLine("uninsert dnsrecords "+unList.Count);
            Console.WriteLine("mission complete   use time " + watch.ElapsedMilliseconds);
        }
        static void RefreshOldData() {
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
                    DataTable dt = MySQLHelper.Query("SELECT z.id,z.zone,z.userid,CASE when d.maxfensheng<1 then 0 else 1 end as level,z.nsstate,z.active,z.forcestop from zones as z left join domainlevel as d on z.DomainLevel=d.levelvalue where z.userid<>348672 and z.id between " + index + " and " + (index + 10000) + " and z.Active='Y' and z.ForceStop='N'").Tables[0];
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

                    int processCount = 0;
                    IList<string> zoneArry = new List<string>();
                    List<ZonesSimple> tempzl = new List<ZonesSimple>();
                    foreach (ZonesSimple zs in dl) {
                        zoneArry.Add(zs.domain);
                        tempzl.Add(zs);
                        if ((processCount > 0 && processCount % 200 == 0) || processCount == dl.Count - 1) {
                            List<ZonesSimple> mzl = categories.Find(Builders<ZonesSimple>.Filter.In("domain", zoneArry)).ToList<ZonesSimple>();
                            foreach (ZonesSimple z in tempzl) {
                                var tz = mzl.SingleOrDefault(s => s.domain == z.domain);
                                if (tz != null)
                                {
                                    if (tz.is_stop != z.is_stop || tz.force_stop != z.force_stop)
                                    {                                        
                                        var update = Builders<ZonesSimple>.Update.Set("is_stop", z.is_stop).Set("force_stop",z.force_stop);
                                        categories.UpdateOne(Builders<ZonesSimple>.Filter.Eq("domain", z.domain), update);
                                        Console.WriteLine("Update Domain " + z.domain);
                                    }
                                }
                                else {
                                    categories.InsertOne(z);
                                    Console.WriteLine("Insert Domain "+z.domain);
                                }
                            }
                            zoneArry.Clear();
                            tempzl.Clear();
                            Console.WriteLine("Process Count {0} Use Time {1}",processCount, watch.ElapsedMilliseconds);
                        }
                        processCount++;
                    }

                    dl.Clear();

                    index = index + 10001;
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
        #endregion


        #region 删除无域名的SOA/NS
        /// <summary>
        /// 删除无域名的SOA
        /// </summary>
        static void DeleteSOANS() {
            try
            {

                var client = DriverConfiguration.Client;
                var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();//开始计时 

                DataTable dt = MySQLHelper.Query("SELECT a.id,a.zone from authorities as a left join zones as z on a.ZoneID=z.ID WHERE z.id is NULL").Tables[0];
                Console.WriteLine("getdatatabel from mysql  rows count= {1}        Use Time={0};", watch.ElapsedMilliseconds, dt.Rows.Count);

                int processCount = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    string rrcol = Utility.StringHelper.CalculateMD5Hash(dr["zone"].ToString() + ".").Substring(0, 1).ToLower();
                    IMongoCollection<AuthoritiesSimple> categories = db.GetCollection<AuthoritiesSimple>(rrcol);
                    categories.DeleteMany(Builders<AuthoritiesSimple>.Filter.Eq("rid", -Convert.ToInt32(dr["id"])));
                    //MySQLHelper.ExecuteSql("delete from authorities where id="+ Convert.ToInt32(dr["id"]));
                    processCount++;
                    if (processCount > 0 && processCount % 100 == 0)
                    {
                        Console.WriteLine("delete count={1};               Use time={0};", watch.ElapsedMilliseconds, processCount);
                    }
                }
                Console.WriteLine("end delete count={1};               Use time={0};", watch.ElapsedMilliseconds, processCount);



                DataTable dt2 = MySQLHelper.Query("SELECT d.id,d.zone,d.ZoneID,d.type,z.ID from dnsrecords as d left join zones as z on d.ZoneID=z.ID where z.id is null").Tables[0];
                Console.WriteLine("getdatatabel from mysql  rows count= {1}        Use Time={0};", watch.ElapsedMilliseconds, dt2.Rows.Count);
                    
                processCount = 0;
                foreach (DataRow dr in dt2.Rows)
                {
                    string rrcol = Utility.StringHelper.CalculateMD5Hash(dr["zone"].ToString() + ".").Substring(0, 1).ToLower();
                    IMongoCollection<DnsRecordsSimple> categories = db.GetCollection<DnsRecordsSimple>(rrcol);
                    categories.DeleteMany(Builders<DnsRecordsSimple>.Filter.Eq("rid", Convert.ToInt32(dr["id"])));
                    //MySQLHelper.ExecuteSql("delete from dnsrecords where id=" + Convert.ToInt32(dr["id"]));
                    processCount++;
                 
                    if (processCount > 0 && processCount % 100 == 0)
                    {
                        Console.WriteLine("delete count={1};               Use time={0};", watch.ElapsedMilliseconds, processCount);
                    }
                }
                Console.WriteLine("end delete count={1};               Use time={0};", watch.ElapsedMilliseconds, processCount);
                watch.Stop();//停止计时
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// 删除有域名的SOA
        /// </summary>
        static void DeleteSOANS2()
        {
            try
            {

                var client = DriverConfiguration.Client;
                var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();//开始计时
                int pcount = 0; 
                //string sql = @"SELECT t.id,t.zone,a.ID as aid,a.Zone as azone,a.Type,d.ID as did,d.Zone as dzone from Temp as t  LEFT JOIN authorities as a on t.ID = a.ZoneID LEFT JOIN dnsrecords as d on t.ID = d.ZoneID where t.Type = 1;
                //            SELECT max(id) as ID from Temp where Type=2 group by zone;";
                //DataSet ds = MySQLHelper.Query(sql);
                //Console.WriteLine("getdata use time " + watch.ElapsedMilliseconds);
                //DataTable dt1 = ds.Tables[0];
                //DataTable dt2 = ds.Tables[1];
                //foreach (DataRow dr in dt1.Rows) {
                //    if (dr["dzone"].ToString() == "") {                        
                //        string rrcol = Utility.StringHelper.CalculateMD5Hash(dr["zone"].ToString() + ".").Substring(0, 1).ToLower();
                //        IMongoCollection<AuthoritiesSimple> categories = db.GetCollection<AuthoritiesSimple>(rrcol);
                //        categories.DeleteMany(Builders<AuthoritiesSimple>.Filter.Eq("rid", -Convert.ToInt32(dr["id"])));
                //        MySQLHelper.ExecuteSql("delete from authorities where id=" + Convert.ToInt32(dr["aid"]));
                //        MySQLHelper.ExecuteSql("delete from zones where id=" + Convert.ToInt32(dr["id"]));
                //        MySQLHelper.ExecuteSql("delete from Temp where type=1 and id=" + Convert.ToInt32(dr["id"]));
                //        pcount++;
                //    }
                //}
                //Console.WriteLine("delete no dnsrecords  count ="+pcount);
                //pcount = 0;
                //DataTable dtTemp = new DataTable();
                //foreach (DataRow dr in dt2.Rows) {
                //    long zoneid = Convert.ToInt32(dr["id"]);
                //    dtTemp = MySQLHelper.Query("SELECT id,zone from authorities where ZoneID =" + zoneid).Tables[0];
                //    foreach (DataRow dra in dtTemp.Rows)
                //    {
                //        string rrcol = Utility.StringHelper.CalculateMD5Hash(dra["zone"].ToString() + ".").Substring(0, 1).ToLower();
                //        IMongoCollection<AuthoritiesSimple> categories = db.GetCollection<AuthoritiesSimple>(rrcol);
                //        categories.DeleteMany(Builders<AuthoritiesSimple>.Filter.Eq("rid", -Convert.ToInt32(dra["id"])));
                //        pcount++;
                //    }
                //    MySQLHelper.ExecuteSql("delete from authorities where zoneid=" + zoneid);
                //    MySQLHelper.ExecuteSql("delete from zones where id=" + zoneid);
                //    MySQLHelper.ExecuteSql("delete from Temp where type=2 and id=" + zoneid);
                //}

                DataTable dt3 = MySQLHelper.Query("SELECT id,zone from authorities where ZoneID in(SELECT max(id) as ID from Temp where Type=1 group by zone)").Tables[0];
                Console.WriteLine("getdatatabel from mysql  rows count= {1}        Use Time={0};", watch.ElapsedMilliseconds, dt3.Rows.Count);

                foreach (DataRow dr in dt3.Rows)
                {
                    string rrcol = Utility.StringHelper.CalculateMD5Hash(dr["zone"].ToString() + ".").Substring(0, 1).ToLower();
                    IMongoCollection<AuthoritiesSimple> categories = db.GetCollection<AuthoritiesSimple>(rrcol);
                    categories.DeleteMany(Builders<AuthoritiesSimple>.Filter.Eq("rid", -Convert.ToInt32(dr["id"])));
                    MySQLHelper.ExecuteSql("delete from authorities where id=" + Convert.ToInt32(dr["id"]));
                    pcount++;
                }
                Console.WriteLine("end delete  Use time={0};", watch.ElapsedMilliseconds);
                watch.Stop();//停止计时
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion
    }
    public sealed class Auth : AuthoritiesSimple
    {
        public ObjectId _id { get; set; }
    }

    internal class Temp {

    }
}
