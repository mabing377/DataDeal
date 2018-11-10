using BindDns.MongoDBEntity;
using Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Utility;

namespace ProcessRecords
{
    class Program
    {
        private static string basepath = AppDomain.CurrentDomain.BaseDirectory;
        static void Main(string[] args)
        {
            //Logger.Init(basepath+ "config\\log4net.conf");
            Console.WriteLine("程序功能：");
            Console.WriteLine("1-peocess wrong data;");
            Console.WriteLine("2-RefreshSOANS");
            Console.WriteLine("3-RefreshRecord");
            Console.WriteLine("4-RefreshBindZones");
            Console.WriteLine("5-CheckMXData");
            Console.WriteLine("6-RefreshRDomain");
            Console.WriteLine("7-RefreshRecordsN");
            Console.WriteLine("8-RefreshRDomain2");
            Console.Write("请输入对应的数字：");
            int input = Console.Read();
            //Console.WriteLine("你输入的是：" + input.ToString());
            switchaction:
            switch (input)
            {
                case 49:
                    DoAction2(basepath + "file\\");//dnsla.txt;
                    break;
                case 50:
                    RefreshSOANS();
                    break;
                case 51:
                    RefreshRecord();
                    break;
                case 52:
                    RefreshBindZones();
                    break;
                case 53:
                    DeleteIgnoreHost();
                    break;
                case 54:
                    RefreshRDomain();
                    break;
                case 55:
                    RefreshRecordsN();
                    break;
                case 56:
                    RefreshRDomain2();
                    break;
                default:
                    break;
            }
            input = Console.Read();
            goto switchaction;
            
            Console.ReadKey();
        }
        static void DoAction(string path)
        {
            try
            {
                string[] files = Directory.GetFiles(path);
                int count = 0;
                foreach (string file in files)
                {
                    string content;
                    StreamReader sr = new StreamReader(file, Encoding.Default);
                    var client = DriverConfiguration.Client;
                    var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
                    string processZone = "";
                    while ((content = sr.ReadLine()) != null)
                    {
                        string zone = "";
                        if (content.Contains("[err]"))
                        {
                            if (content.Contains(". async without rr"))//没有解析记录
                                zone = content.Substring(content.LastIndexOf(':') + 2, content.IndexOf(". async without rr") - content.LastIndexOf(':')-1);
                            else if (content.Contains(". async without SOA"))//没有SOA
                                zone = content.Substring(content.LastIndexOf(':') + 2, content.IndexOf(". async without SOA") - content.LastIndexOf(':')-1);
                            else if (content.Contains(". async parse rdata error"))//解析记录格式不合法
                                zone = content.Substring(content.LastIndexOf(':') + 2, content.IndexOf(". async parse rdata error") - content.LastIndexOf(':')-1);                         
                        }
                        if (zone != "") {
                            if (zone.StartsWith("@")) zone = zone.Substring(2, zone.Length - 3);
                            else zone = zone.Substring(0, zone.Length - 1);

                            if (!processZone.Contains(zone))
                            {
                                string rrcol = Utility.StringHelper.CalculateMD5Hash(zone + ".").Substring(0, 1).ToLower();

                                if (content.Contains(". async without SOA"))
                                {
                                    IMongoCollection<ZonesSimple> categoriesZ = db.GetCollection<ZonesSimple>("zones");
                                    IMongoCollection<AuthoritiesSimple> categoriesA = db.GetCollection<AuthoritiesSimple>(rrcol);
                                    var filterA = Builders<AuthoritiesSimple>.Filter;
                                    var builderA = filterA.And(filterA.Eq("domain", zone + '.'), filterA.Lt("rid", 0));
                                    var aCount = categoriesA.Find(builderA).Count();

                                    IMongoCollection<DnsRecordsSimple> categoriesD = db.GetCollection<DnsRecordsSimple>(rrcol);
                                    var filterD = Builders<DnsRecordsSimple>.Filter;
                                    var builderD = filterD.And(filterD.Eq("domain", zone + '.'), filterD.Gt("rid", 0));
                                    var dCount = categoriesD.Find(builderD).Count();

                                    DataTable dt = MySQLHelper.Query("select a.id,a.zone,host,data,type,ttl,mbox,serial,refresh,retry,expire,minimum ,t.userid,a.zoneid from authorities as a left join zones as t on a.ZoneID=t.id where a.zone='" + zone + "' and t.userid<>348672 ").Tables[0];
                                    List<authorities> aListtemp = DtToList<authorities>.ConvertToModel(dt);
                                    var soa = aListtemp.FindAll(a => a.type == "SOA");
                                    if (aCount == 0)
                                    {
                                        if (aListtemp.Count == 0)
                                        {
                                            categoriesZ.DeleteMany(Builders<ZonesSimple>.Filter.Eq("domain", zone + "."));
                                            categoriesD.DeleteMany(Builders<DnsRecordsSimple>.Filter.Eq("domain", zone + "."));
                                        }
                                        if (soa.Count == 1)
                                        {
                                            categoriesA.InsertMany(Row2Authorities(aListtemp));
                                        }
                                        else
                                        {
                                            Console.WriteLine("monogdb acount={0},mysql acount={1},zone={2}", aCount, dt.Rows.Count, zone);
                                        }
                                    }
                                    else
                                    {
                                        DataTable dtZ = MySQLHelper.Query("select id,zone from zones where zone='" + zone + "' and userid<>348672 ").Tables[0];
                                        if (dtZ.Rows.Count == 1)
                                        {
                                            string zoneid = dtZ.Rows[0][0].ToString();
                                            List<authorities> aListInsert = DtToList<authorities>.ConvertToModel(MySQLHelper.Query("select a.id,a.zone,host,data,type,ttl,mbox,serial,refresh,retry,expire,minimum ,t.userid,a.zoneid from authorities as a left join zones as t on a.ZoneID=t.id where a.zoneid=" + zoneid).Tables[0]);
                                            if (aListInsert.Count > 0)
                                            {
                                                categoriesA.DeleteMany(builderA);
                                                categoriesA.InsertMany(Row2Authorities(aListInsert));
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("zone data count  {0}  zone: {1}", dtZ.Rows.Count, zone);
                                        }
                                    }
                                }                                
                                count++;
                            }

                        }
                        processZone = processZone + zone;
                    }
                    Console.WriteLine(count);
                }
            }
            catch (Exception ex)
            {
                string msg = ex.ToString();
            }
        }

        static void DoAction2(string path)
        {
            try
            {
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    string content;
                    StreamReader sr = new StreamReader(file, Encoding.Default);
                    var client = DriverConfiguration.Client;
                    var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
                    List<string> processZone = new List<string>() ;
                    string sql = @"SELECT z.id,z.zone,z.userid,CASE when d.maxfensheng<1 then 0 else 1 end as level,z.nsstate,z.active,z.forcestop from zones as z left join domainlevel as d on z.DomainLevel=d.levelvalue where z.zone='{0}';
                                   select a.id,a.zone,host,data,type,ttl,mbox,serial,refresh,retry,expire,minimum ,a.zoneid,z.userid from authorities as a left join zones as z on z.id=a.zoneid where a.zone='{0}';
                                   select d.id,d.zoneid,d.zone,d.host,d.type,d.data,d.ttl,d.view,d.mx_priority,d.userid,d.active from dnsrecords as d where d.zone='{0}'";
                    while ((content = sr.ReadLine()) != null)
                    {
                        string zone = "";
                        if (content.Contains("[err]"))
                        {
                            if (content.Contains(". async without rr"))//没有解析记录
                                zone = content.Substring(content.LastIndexOf(':') + 2, content.IndexOf(". async without rr") - content.LastIndexOf(':') - 1);
                            else if (content.Contains(". async without SOA"))//没有SOA
                                zone = content.Substring(content.LastIndexOf(':') + 2, content.IndexOf(". async without SOA") - content.LastIndexOf(':') - 1);
                            else if (content.Contains(". async parse rdata error"))//解析记录格式不合法
                                zone = content.Substring(content.LastIndexOf(':') + 2, content.IndexOf(". async parse rdata error") - content.LastIndexOf(':') - 1);


                            if (zone.StartsWith("@")) zone = zone.Substring(2, zone.Length - 3);
                            else zone = zone.Substring(0, zone.Length - 1);

                            if (!processZone.Contains(zone))
                            {
                                processZone.Add(zone);

                                string rrcol = Utility.StringHelper.CalculateMD5Hash(zone + ".").Substring(0, 1).ToLower();
                                DataSet ds = MySQLHelper.Query(string.Format(sql, zone));

                                IMongoCollection<ZonesSimple> categoriesZ = db.GetCollection<ZonesSimple>("zones");
                                var zCount = categoriesZ.Find(Builders<ZonesSimple>.Filter.Eq("domain", zone + ".")).Count();

                                IMongoCollection<AuthoritiesSimple> categoriesA = db.GetCollection<AuthoritiesSimple>(rrcol);
                                var filterA = Builders<AuthoritiesSimple>.Filter;
                                var builderA = filterA.And(filterA.Eq("domain", zone + '.'), filterA.Lt("rid", 0));
                                var aCount = categoriesA.Find(builderA).Count();

                                IMongoCollection<DnsRecordsSimple> categoriesD = db.GetCollection<DnsRecordsSimple>(rrcol);
                                var filterD = Builders<DnsRecordsSimple>.Filter;
                                var builderD = filterD.And(filterD.Eq("domain", zone + '.'), filterD.Gt("rid", 0));
                                var dCount = categoriesD.Find(builderD).Count();

                                DataTable dtz = ds.Tables[0];
                                DataTable dta = ds.Tables[1];
                                DataTable dtd = ds.Tables[2];

                                //Logger.Info(string.Format("\r\n" + zone + "\r\nmysql zcount={0}  acount={1}  dcount={2}\r\nMongo zcount={3}  acount={4}  dcount={5}\r\n===============================================\r\n", ds.Tables[0].Rows.Count, ds.Tables[1].Rows.Count, ds.Tables[2].Rows.Count, zCount, aCount, dCount));
                                LoggerAdvance.AddLog(string.Format("\r\n" + zone + "\r\nmysql zcount={0}  acount={1}  dcount={2}\r\nMongo zcount={3}  acount={4}  dcount={5}\r\n===============================================", ds.Tables[0].Rows.Count, ds.Tables[1].Rows.Count, ds.Tables[2].Rows.Count, zCount, aCount, dCount), "ProcessData", "");
                                Console.WriteLine(string.Format("\r\n" + zone + "\r\nmysql zcount={0}  acount={1}  dcount={2}\r\nMongo zcount={3}  acount={4}  dcount={5}\r\n===============================================", ds.Tables[0].Rows.Count, ds.Tables[1].Rows.Count, ds.Tables[2].Rows.Count, zCount, aCount, dCount));
                                if (zCount == dtz.Rows.Count && aCount == dta.Rows.Count && dCount == dtd.Rows.Count)
                                    continue;
                                else
                                {
                                }
                            }
                        }
                    }

                    Console.WriteLine("processed zone " + processZone.Count);
                }
            }
            catch (Exception ex)
            {
                string msg = ex.ToString();
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


        static void RefreshSOANS()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时 
            string[] collection = new string[] {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
            try
            {
                var client = DriverConfiguration.Client;
                var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
                IMongoCollection<ZonesSimple> categoriesZ = db.GetCollection<ZonesSimple>("zones");
                foreach (string c in collection){
                    IMongoCollection<AuthoritiesSimple> categoriesA = db.GetCollection<AuthoritiesSimple>(c);
                    List<ZonesSimple> zoneslist = categoriesZ.Find(Builders<ZonesSimple>.Filter.Eq("rrcol", c)).ToList<ZonesSimple>();
                    Console.WriteLine(c + " collection count= " + zoneslist.Count);
                    List<string> zoneList = new List<string>();
                    int needProcess = 0;
                    for (int i = 0; i < zoneslist.Count; i++) {
                        zoneList.Add(zoneslist[i].domain);
                        if ((i > 0 && i % 100 == 0)||i==zoneslist.Count-1)
                        {
                            var filterA = Builders<AuthoritiesSimple>.Filter;
                            var builderA = filterA.And(filterA.In("domain", zoneList), filterA.Lt("rid", 0));
                            List<AuthoritiesSimple> alist = categoriesA.Find(builderA).ToList<AuthoritiesSimple>();
                            foreach (string zone in zoneList) {
                                int count = alist.FindAll(a => a.domain == zone).Count();
                                if (count == 0 || count > 5) {
                                    if (count > 5)
                                        categoriesA.DeleteMany(Builders<AuthoritiesSimple>.Filter.And(Builders<AuthoritiesSimple>.Filter.Eq("domain", zone), Builders<AuthoritiesSimple>.Filter.Lt("rid", 0)));
                                    DataTable dt = MySQLHelper.Query("select a.id, a.zone, host, data, type, ttl, mbox, serial, refresh, retry, expire, minimum, z.userid, a.zoneid from zones as z LEFT JOIN authorities as a on z.id = a.zoneid where a.zone is not null and z.zone = '"+zone.Substring(0,zone.Length-1)+"'").Tables[0];
                                    if (dt.Rows.Count > 0)
                                    {
                                        List<AuthoritiesSimple> ASList = Row2Authorities(DtToList<authorities>.ConvertToModel(dt));
                                        categoriesA.InsertMany(ASList);
                                        LoggerAdvance.AddLog("collection = " + c + "  domain= " + zone + "  insert SOANS count=" + ASList.Count, "RefreshSOANS", "");
                                        Console.WriteLine("collection = " + c + "  domain= " + zone + "  insert SOANS count=" + ASList.Count);
                                    }
                                    else {
                                        LoggerAdvance.AddLog("collection = " + c + "  domain= " + zone + "  has no SOANS", "RefreshSOANS", "");
                                    }
                                    needProcess++;
                                }
                            }
                            zoneList.Clear();
                        }
                        if (i > 0 && i % 1000 == 0)
                        {
                            Console.WriteLine(c + " collection processing " + needProcess + " idx= " + i + " use time" + watch.ElapsedMilliseconds);
                        }
                    }
                    Console.WriteLine(c + " collection processed use time" + watch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                string msg = ex.ToString();
                Console.WriteLine(msg);
                LoggerAdvance.AddLog(msg, "RefreshSOANSException", "");
            }
        }

        static void RefreshRecord() {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时 
            string[] collection = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
            try
            {
                var client = DriverConfiguration.Client;
                var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
                IMongoCollection<ZonesSimple> categoriesZ = db.GetCollection<ZonesSimple>("zones");
                foreach (string c in collection)
                {
                    IMongoCollection<DnsRecordsSimple> categoriesD = db.GetCollection<DnsRecordsSimple>(c);
                    //从zones中按照rrcol去除全部数据。
                    List<ZonesSimple> zoneslist = categoriesZ.Find(Builders<ZonesSimple>.Filter.Eq("rrcol", c)).ToList<ZonesSimple>();
                    Console.WriteLine(c + " collection count= " + zoneslist.Count);

                    List<string> zoneList = new List<string>();
                    string zoneStr = "'";
                    for (int i = 0; i < zoneslist.Count; i++)
                    {
                        string domain = zoneslist[i].domain;
                        zoneList.Add(domain);
                        zoneStr = zoneStr + domain.Substring(0, domain.Length - 1)+"','";
                        if ((i > 0 && i % 100 == 0) || i == zoneslist.Count - 1)
                        {
                            var filterD = Builders<DnsRecordsSimple>.Filter;
                            var builderD = filterD.And(filterD.In("domain", zoneList), filterD.Gt("rid", 0));
                            
                            string sql1 = "select d.id,d.zoneid,d.zone,d.host,d.type,d.data,d.ttl,d.view,d.mx_priority,d.userid,d.active from dnsrecords as d  where type<>'PTR' and zone in("+ zoneStr.Substring(0, zoneStr.Length-2)+");";
                            DataTable dtd = MySQLHelper.Query(sql1).Tables[0];
                            List<dnsrecords> dlist= DtToList<dnsrecords>.ConvertToModel(dtd);

                            List<DnsRecordsSimple> dslist = new List<DnsRecordsSimple>();

                            foreach (dnsrecords dr in dlist) {
                                if (CheckRecordHost(dr.host, dr.type) && CheckRecordData(dr.data, dr.type, dr.view, dr.host))
                                {
                                    dslist.Add(Row2DnsRecord(dr));
                                }
                                else
                                {
                                    LoggerAdvance.AddLog("collection = " + c + "  domain= " + dr.zone + " illegal record " + dr.id+" type= "+dr.type, "RefreshRecord", "");
                                    Console.WriteLine("collection = " + c + "  domain= " + dr.zone + "  illegal record " + dr.id+" type= "+dr.type);
                                }
                            }
                            categoriesD.DeleteMany(builderD);
                            if(dslist.Count>0)
                                categoriesD.InsertMany(dslist);
                            zoneList.Clear();
                            zoneStr = "'";
                        }
                        if (i > 0 && i % 500 == 0)
                        {
                            Console.WriteLine(c + " collection processing idx= " + i + " use time" + watch.ElapsedMilliseconds);
                        }
                    }
                    Console.WriteLine(c + " collection processed use time" + watch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                string msg = ex.ToString();
                Console.WriteLine(msg);
                LoggerAdvance.AddLog(msg, "RefreshSOANSException", "");
            }
        }
        static void RefreshBindZones() {

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时
            string[] collection = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };

            //List<zoneswithbind>[] dlz = new List<zoneswithbind>[16] { new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>(), new List<zoneswithbind>() };
            //List<ZonesSimple>[] dlzs = new List<ZonesSimple>[16] { new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>() };
            List<DnsRecordsSimple>[] dld = new List<DnsRecordsSimple>[16] { new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>() };

            DataTable dt = MySQLHelper.Query("SELECT z.id,z.zone,z.userid,CASE when d.maxfensheng<1 then 0 else 1 end as level,z.nsstate,z.active,z.forcestop,z.rzone from zones as z left join domainlevel as d on z.DomainLevel=d.levelvalue where z.userid<>348672 and isbindns=1").Tables[0];
            Console.WriteLine("zone count +"+dt.Rows.Count );

            DataTable dtd = MySQLHelper.Query("select d.id,d.zoneid,d.zone,d.host,d.type,d.data,d.ttl,d.view,d.mx_priority,d.userid,d.active from dnsrecords as d where type<>'PTR' and zone in('sw021.com','xrwy.com','021-86.com','qhtff.com','yn997.com','qx3271.tk','dihuaad.com','ka84.com','1380318.com','10pad.com','jiajgw168.com','haohappy123.com','eshidai8.com','sh419.com','sz-wealth.com','520kkn.com','fyzfw.com','mfav.org','tthappy518.com','yngl.com','0769118.com','66ds.com.cn','55b55.com','55j55.com','55y55.com','baidu1xia.com','hg035.com','taiyangao.com','taiyangci.com','1chuo.com','1guai.com','355cn.com','676m.com','8kua.com','china199.com','china878.com','55879999.com','626655111.com','66772222.com','77775432.com','7betbo.com','7mae.com','7mbu.com','7mck.com','88885432.com','981110.com','9betbo.com','ab17888.com','aobo44.com','b90099.com','bet20022.com','bj6611.com','bok1888.com','cs089.com','fh99888.com','g32222.com','h2889.com','hg22118.com','hg30122.com','hg4222.com','hg66600.com','hg6867.com','hg88117.com','jg22555.com','jsa888.com','la01111.com','n68688.com','sts99999.com','xh888888.com','00005432.com','0betbo.com','22225432.com','33335432.com','55555432.com','5586111.com','5586333.com','5586777.com','5586999.com','55871111.com','55873333.com','55875555.com','dianji100.com','32499.com','qcyz755.com','huakesm.com','a163a.com','feifancx.com','yy2202.tk','qitianlegou1.com','98xj.net','tx0000.com','fongsunge.cn','5ilb.com','513pk.com','mir01.net','94ks.com','yqq177.com','sunyunfeng.jp','28cms.com','rddhw.com','dftb.com.cn','utcloud.com','sexj50h0a.com','122100.tk','green-ihome.com','lz848.com','se1se2.tk','dao029.com','ydy1698.com','fanxing.cc','7000tl.com','pk5201.com','295au.com','gyycxx.com','999meng.com','163zxd.com','xn--djr47xi85c.xn--fiqs8s','xn--siq29xrp0d.cn','bbre.tk','pertemao.com','xn--nftz2e.xn--fiqs8s','13487.com','88lian.com','shengwangpay.com','dwcq2.com','yzqn.tk','zjutv.tk','510.net.cn','xunshimin.com','bb102.com','xn--cpq941n.xn--fiqs8s','sinoboss.org','aibuy.tk','90hdh.com','gkhwz.com','kanhw.com','hbbht.com','tangkk.tk','8xxyy.com','feesou.com','dz515.com','wzf888.com','liming.cn','v3dj.com','517fh.com','172go.com','linxiqun.com','tianmaoe.com','sltianqi.net','325cpa.com','huigu.org','shop3695310.tk','9zpc.com','010qqq.com','baipengyi.cn','dfsr178.com','zkdh.net','xuanyifang88.com','baobei.com','yayasj.com','sheiganpk.com','811888.com','kakancom.com','ka55558.com','wang.cn','zgdyfz.com','fasifu.com','wanbo999.com','qiuvps.com','yy9876.com','gaosanbaban.com','lg6888.com','cenxitv.com','cqsanjian.cn','linesky.cn','72xc.com','xn--qpr88o.xn--fiqs8s','538972.com','cpyg999.com','ruixuelg.com','cyewz.com','csshangpin.com','wanxiw.com','91pc.com','dfdns.com','lsgw168.com','8vpay.net','0109000.net','yyymmm.com','haoxingguoji.com','chaogu558.com','yjm88.com','wego114.com','fs952.com','9988w.com','jn7788.com','123ib.com','qqxzs.cn','9173gg.net','wsx.la','yth.la','seolei.asia','001wed.com','airjordan11japan.com','22pk.us','eymyi.us','adsaj.asia','muckx.asia','oszone.tk','5188jm.com','pai777.com','payguanfang.com','hardyjeff.com','liwell.com.cn','jjp66.com','namedwater.com','kswoo.com','1007movie.net','ab195.com','pt30ds.com','beiyaxinghui189.net','99read.com','sngah.com','itcot.com.cn','uu125.com','xj920.com','qxn123.com','mybaobei.tk','xdjsc.com','tx2000.com','dt632.com','weiweisite.com','scpc.cc','hyg88.com','aotian.tk','aihuoxian.com','100cn.tk','yutao6.com','jiayu998.com','gufengyun.com','5sav.com','cao50.com','134p.com','chenijing.com','baobuji.ml','91ju1ju.com','hmcsf.com','shifei.cn','shuye.tk','ca28.com','200is.com','hangtiangd.com','yybs998.com','zzyahua.com','visonsci.com','zhiaibaobao.com','fugu2002.com','cdiorbag.com','hyyiou.com','bbb02.tk','xdqtlbb.tk','51popo.tk','yd0803.com','vcrtl.net','1ttxp.com','qtab.tk','wulegae.com','sky135.com','520kino.cn','xs925.com','2013zjwstv.com','ganwu.tk','12345good.com','huaxin9.com','seaskyccl.com','htysjd.com','jsmir2s.com','itaobaol.com','39mu.com','adu88.com','laigowu.com','advertisingmaps.com','yas8.us','pcaat.com','yx195.com','fnrgm.com','gpdjy.com','expensfy.com','2pc.net','xiaobao.com','ztgj12.com','pinfun.com','jievin.tk','tbao99.com','kehuzhan.com','law567.com','52awe.com','go98998.com','88hous.com','shengshiwangmeng7198.com','755bja.com','hrqkj.com','zejmw.com','czypb.com','bdptm.com','bdcpx.com','anyixuang.com','cctv518.cn','cslggj.com','ppbs.com','soso263.com','daidandan.com','jrfdj.com','65kf.com','bdcms.tk','mcyzw.com','zhaoquanhua.cn','cnnctvn.tk','exhyd.com','tgdgm.com','fdgyy.com','dsbcd.com','zjyxd.com','900133.com','90997.com','czslzc.com','anchorhighmarina.com','fj9.cn','jushangguoji66-34.com','rentiku.com','mooncity.tk','waptw.cc','cjw.com','lxjx.com','wanmm.com','f3322.org','vipxyw.tk','hydraulic.tk','class21.tk','qq249679311.tk','qqworld.org','swidc.info','rdpgc.com','nm858.com','sogo.ga','0561.la','gzqzc.com','maigejiu.com','dmnico.cn','cmcc8.tk','bnsf.cn','comgowo28.com','xkp.com','jsdpx.com','56se.com','js8877.com','zyhbd.com','0746cc.com','90fj.com','rickykwok.la','alarmspecs.com','sq517.com','sddkg.com','610124.cc','ynda.cc','2013.in','onefloor.cn','tboosu.cc','755.com','jk958.com','zpi4.com','energytoy.com','aihui.com','pay1.net','4u3.com','7gp.com','db519.com','oe52.net','sp512.com','tianyuelegou.com','qieryi.com','ysf818.com','1919996867.com','hexincnc.com','suyingqq.tk','365mu.com','0371818.com','comolinux.com','cqccedu.cn','luherx.com','1004tv.cn','guide2013.tk','learjawholesalefacoty.com','bdportal24.info','daimlerchryslercars.com','skodasales.com','intncc.com','wifimedia.mobi','guolianbao.com','7duzs.com','wenshangtong.com','33xie.com','hg8007.com','xiaokong.com','lele661.com','xiaoshang88.cn','optshr.com','yangwjin.com','pangdouya.com','av69.net','ybh.in','pk651.com','zhongya-yanshi.com','shangdai-yanshi.com','kongtie-yanshi.com','37500.tk','rtysba.com','colormall.cn','bananavison.tk','h-deathcraft.cc','ok896.com','ztaobao.tk','11xa.com','yanshizhang-xygj.com','kehuzhang-xygj.com','lbzb.com','qiqisea1.org','orbu.net','bselves.tk','129mail.com','efang.cn','toybooks.com.cn','baidu3.com','soxpx.com','fitku.com','zyhny.com','rentiyishuu.org','xingjiaotupian.org','ccc36666.org','51pdu.com','vvnas.com','colorad.cn','wbjvip.com','20fff.info','13588084590.com','123678.org','yzke.info','tk5.info','psp9.info','eczhmc.com','niha333o.com','288-yanshizhan.com','plaweb.com','hehe.com','zxgw920.com','queen.com','zyz1964.com','20seo.com','177pai.com','iher.tk','9ini.tk','bgh8.com','yugaoyang.com','8858198.com','sy-mv.com','jm123.org','bnsf.com.cn','cn058.cn','dao195.com','mc195.com','ejz8.com','0565zp.com','59wx.org','17yyba.com','060la.com','suyuepao.com','yafei521.com','zz195.com','huipingguoji.com','downme.org','ttsm886.com','niceweb.cc','luyitaobao.com','945z.com','ylwang.la','dajiawan.com.cn','liujun.tk','haitongsh.com','53sj.cn','12580xc.tk','gj510.com','ssdj.org','xmshaiya.tk','yuepinwang.com','googlepplus.com','avbd.com','24fanfan.com','ppcareer.com.cn','ziyuan.com','dh.vg','600mu.com','lx0830.com','ss99.org','817123.info','zuixindianyingg.org','ouzhou9.com','oc668.com','biswz.com','yn82.com','qfafa.com','tvmao.biz','mo.com','288.cn','tswpcb.com','kx621.com','sunantong.com','znhome.com','bj76.com','mzsmt.com','huihezi.com','qq1284781016.net','fxx1.com','longhucq.com','china.com','devialet.in','test02.com','quklive.com.cn','2126.biz','tm863.com','urtracker.org','weddongwire.com','szrht.com','25zd.tk','9588.us','shawacademy.co','spyker.co','700xj.com','gaogulou.com','theblacktux.ru','studiorex.org','yifi.com','xindutl.com','2125.biz','dongguan126.com','hao2011.cn','shenyaoled.cn','abevep.com','newaction.gq','jiuyuehua.com','americansdyg.com','naozaiguoguo.com','wzq566999.com','rueaec.com','qaicee.com','0743063.cn','mcdowei.com','hf5991.com','dnf-1200.cn','dadayouxi.com','yjgsc.net','av1080.com','yl.com','aokiweb.cc','qunshua.ml','mq5u.com','ha49.com','vu78.com','5xxz.com','zc1ds5ccv13vcf5fdv.cn','itunes-appid.com','icloud-appleid.com','icluod-itunse.com','x.xxx','icioud.cc','itunes-aopleie.com','icloud-aipple.com','icloud-imacid.com','shmilyelva.com','songsong168.com','20020127.com','kawines.com','laikanle.com','icloud-center.com','bnubp.com','xiaofanju.com','ktf925.com','icioud.com.co','odsky.ml','onlylovebaby.com','chinachn.ml','icloud-back.com','yzc666.com','hhsc666.com','162000.com','dandan520.com','winyn.com','9.com','6117.com','5937.com','telina.org','xinsushang.com','carsremote.com','haosifu.wang','xgp.net.cn','topcraft.top','lanwankj.com','muqingshangmao.com','crhsi.org','srmzw.pw','xn--114-069dv09d7ut.com','d9tu.com','runyang521.com','currencyfair.org','atd010.com','saiki.top','l26.cn','88liuyue.com','100k8.com','acm6.com','longhu88.com','25258.wang','paganimotors.com','xingxingsc8.com','mir3g2005.com','handchannel.com','299sf.com','xhgw888.com','gebangji.com','jszhabei.cn','umui.net','iying.me','ledian.co','laptopfix.org','0837.tk','ccftn.com.cn','bb6228.com','bzrate.com','5092524.com','xuelichaxun.com','af8.co','lexuan.com','gwskg.com','smart3d.com.cn','e-music.com.cn','leitou.net','weimi.com','bnbmhome.com','cihe.net','costco.me','hfngf.com','aaimports.com','tupperwareonline.com','fmccb.cn','fashion360.com.cn','ctjcf.com','woetou.com','guoyanzz.tk','gzgd.org','yenda.com.cn','guku.com','nander.cn','16970km.tk','ydw88.cn','huaweimatebook.net','mikifuns.pw','z1699.com','zangyao.cn','leyes.com.cn','mmmar.cn','bowg.net','bigbrother.cc','zysc0506.com','yiyasc8.com','raywu.cn','uc769.com','xx7123.com','ev-autos.com','haoav01.com','blz01.com','caihengtong.com','lxwl88.com','kdm86.com','xiaoyu.com','xy.com','akanmai.com','8ff1.com','dfmlktv.club','kingview.tk','sf5888.com','clantian.com','zxl110033.cn','577jm.com','duolaiduo8.com','ejisuml.cn','haoa30.com','wcoserver.com','wcourl.com','xdyi.com','306.in','rola.cc','ok10000.com','kuajingkanbing.com','ly9377.xyz','edingche.cn','haoav12.com','rmlv.com.cn','esumiao.com','metalstar.org','vuda.cn','dayisheng.com','xxylw.tk','ewnf.net','yunfangyu.net','yuheng.cn','taxonline.cn','grati.cn','fjdshjlksdfjshyy.cn','sweett.gq','blz22.com','cjrx.com.cn','ff8aaa.com','sun18001.com','yangshengxing.com','huangzao.com','22xs.net','caipark.cn','xiangweimou.com','hjd0.com','xiubiying.com','lelejinfu.com','shopcluis.com','zihas.com','ncrcf.com','fujinyouxi.com','cnxnzd.com','datavisualize.com','dilyfx.com','scdy.cn','hghg1234.com','helpme.so','965310967.com','solarclub.cn','cj12345.com','mczldj.com','leadertips.com','highrivertoyota.com','xyzz.lol','hello-japan.com.cn','cc005.com','95616.cc','13ar.cn','pk960.com','rg100.cn','fkccb.com','hj2002.com','mmxx55.com','66tyty.com','ff8xin.com','1s138.com','hgaidj8.com','s138aa1.com','zz85967.com','kj929.com','hhhgma10.com','11hgab.com','katon.cn','yygj.net','wraye.ml','shizairen.com','ff8a2.com','xn--vhq58fq6ajz7b.xn--fiqs8s','2017la.top','ybyb444.com','40002bet.com','kj939.com','xqle.com.cn','lxs520.com','harryz.vip','szsfwq.cn','12580xyz.xyz','gebijqr.com','skytone.co','chaozhipiao.com','hghgai9.com','faxu.com.cn','bybya11.com','bybya12.com','2021.co','hsht88.com','st162013.com','mangtou.net','ffsuncity.com','chenlong.tk','lwjy.net','ppp5.com','c66pp.com','66pp1.com','1ppp5.com','52geography.tk','533166.com','40002.com','12345cd.com','xee78.top','jsyl91.com','ss20.top','wangheschool.com','danchun.com','jsyl97.com','970043.com','6kua.com','ty8883.com','525402.top','yiphotostudio.com','yoosao.cn','982898.com','ylnm8.com','e63999.com','0358.la','qxa003.com','aks400.com','daj050.com','huzhou56.com','v0106.com','1v3v.com','yunyohui.com','222b12.com','sun0053.com','worldserver1.ml','meiguikm.top','yanz.com','fkw.cn','xingwei800.com','ahjd66.com','22ff8.com','shangli520zxm.com','x3bbb.com','66ka.cn','1yh345.com','08hcp.vip','bolezi.com','88hg100.com','88hg70.com','66pp21.com','1vn2.com','zhaohy.net','haoav23.com','0060.net','59s138.com','jingyuyy.com','nock.club','5igow.com','mhy.top','9977077.com','88hg056.com','8ff0.com','ty9830.com','8ff005.com','hjd19.com','lexkji.cn','ff8vip012.com','s138vip12.com','777bkk.com','yh345a.com','111aim.com','y011.com','facai6789.com','iphoneapp.vip','miaqq.com','ydgj9999.com','6068688.com','yh345k.com','8ff009.com','ynxuan.com','vjc144.com','v993.com','vvo1.com','anyuanba.net','128mi.cn','877zr.com','7682200.com','66pp300.com','mztyd.com.cn','nanamp1.com','xx88y.com','88y50.com','jj88y.com','lzfdsw.cn','vnsr2019a.com','04vnsr.com','baiziyang.com','v66pp.com','8v2v.com','v83231.com','16s138.com','vnsr2020a.com','2vn4.com','flycool.top','7k14.com','jiuyingiis.cn','88hg116.com','8ff101.com','64s138.com','60s138.com','425.cn','adminwujie.cn','444hjd.com','88hg220.com','js5566.live','5087.co','tw-zy.com','91vnsr.com','vnsrwangluo.com','v6n22.com','q88vnsr.com','a88vnsr.com','moym.top','7gets.com','fxn866.com','dfsf7.com','000hh5.com','222m.com','msu1800.com','12s138.com','vkh7788.com','my601.cn','0006602.com','aqdyal.com','88w31.com','xtfac345.com','v89n.com','v553.com','xj8vip.com','safeclub.net','567f6.com','gydg.com','xhgw.cn','tt2a.com','s88hg.com','ffdse11.com','36tyty.com','66pp81.com','88f31.com','12tyty.com','v001v.com','abba11.com','8ppp5.com','111f11.com','222f1.com','adeep1.com','xjc2019.com','88hgh.com','88hg3.com','88x10.com','88x100.com','6a88hg.com','zoho.party','1plv.com','qq88y.com','yy55y.com','789f1.com','ewqe1.com','b88hg.com')").Tables[0];
            Console.WriteLine("dnsrecord count +" + dtd.Rows.Count);

            List<zoneswithbind> zonesList = DtToList<zoneswithbind>.ConvertToModel(dt);
            List<dnsrecords> recordList = DtToList<dnsrecords>.ConvertToModel(dtd);

            Console.WriteLine("DataTable Convert to ModelList; Use time={0};", watch.ElapsedMilliseconds);

            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            IMongoCollection<ZonesSimple> categoriesZ = db.GetCollection<ZonesSimple>("zones");

            string domain = "";
            List<ZonesSimple> zslist = new List<ZonesSimple>();
            List<string> domainlist = new List<string>();
            for (int i = 0; i < zonesList.Count; i++) 
            {
                zoneswithbind z = zonesList[i];
                domain = z.zone + ".";
                domainlist.Add(domain);//用于删除MongoDB中zones
                zslist.Add(Row2ZoneSimple(z));//添加到简化列表

                string collectionname = StringHelper.CalculateMD5Hash(domain).ToLower().Substring(0, 1);
                int idx = Int32.Parse(collectionname, System.Globalization.NumberStyles.HexNumber);
                List<dnsrecords> dl = recordList.FindAll(d => d.zone == z.rzone);
                foreach (dnsrecords d in dl)
                {
                    d.zone = z.zone;
                    dld[idx].Add(Row2DnsRecord(d));
                }

                if ((i > 0 & i % 100 == 0) || i == zonesList.Count - 1)
                {

                    categoriesZ.DeleteMany(Builders<ZonesSimple>.Filter.In("domain", domainlist));
                    categoriesZ.InsertMany(zslist);
                    foreach (string c in collection)
                    {
                        IMongoCollection<DnsRecordsSimple> categoriesD = db.GetCollection<DnsRecordsSimple>(c);
                        categoriesD.DeleteMany(Builders<DnsRecordsSimple>.Filter.And(Builders<DnsRecordsSimple>.Filter.In("domain", domainlist), Builders<DnsRecordsSimple>.Filter.Gt("rid", 0)));

                        int idx2 = Int32.Parse(c, System.Globalization.NumberStyles.HexNumber);
                        if(dld[idx2].Count>0)
                            categoriesD.InsertMany(dld[idx2]);
                    }
                    Console.WriteLine("process count= " + i + "Use time={0};", watch.ElapsedMilliseconds);
                }
                
            }
            Console.WriteLine("end; " +  watch.ElapsedMilliseconds);




            //foreach (string c in collection)
            //{
            //    var idx = Int32.Parse(c, System.Globalization.NumberStyles.HexNumber);
            //    string zoneStr = "'";
            //    List<zoneswithbind> zwblist = dlz[idx];
            //    for (int i = 0; i < zwblist.Count; i++)
            //    {
            //        zoneStr = zoneStr + zwblist[i].zone + "','";
            //        if ((i > 0 && i % 100 == 0) || i == zwblist.Count - 1)
            //        {
            //            DataTable dta = MySQLHelper.Query("select a.id,a.zone,host,data,type,ttl,mbox,serial,refresh,retry,expire,minimum ,t.userid,a.zoneid from authorities as a left join zones as t on a.ZoneID=t.id where a.Zone in(" + zoneStr.Substring(0, zoneStr.Length - 2) + ") and t.userid<>348672 order by a.zone,a.type").Tables[0];
            //            List<authorities> aList = DtToList<authorities>.ConvertToModel(dta);
            //        }
            //    }
            //}



            //var client = DriverConfiguration.Client;
            //var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            //IMongoCollection<ZonesSimple> categories = db.GetCollection<ZonesSimple>("zones");
            //if (dl.Count > 0)
            //    try
            //    {
            //        categories.InsertMany(dl);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //dl.Clear();
            //Console.WriteLine("MongoDB Inserted;               Use time={0};", watch.ElapsedMilliseconds);
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
        static DnsRecordsSimple Row2DnsRecord(dnsrecords dr)
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
        static List<DnsRecordsSimple> Row2DnsRecords(List<dnsrecords> drlist)
        {
            List<DnsRecordsSimple> dslist = new List<DnsRecordsSimple>();
            foreach (dnsrecords dr in drlist)
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
                dslist.Add(d);
            }
            return dslist;
        }
        static void DeleteSOANS() {
            string sql1 = @"select z.id,z.zone,z.isbindns,a.id as aid,a.zoneid as azid,a.zone as azone,a.type as atype
                            from zones as z 
                            left join authorities as a on z.zone=a.Zone
                            where z.zone in('25ff8.com','27ff8.com','28ff8.com','2ff8.com','30ff8.com','789f.com','a111f.com','88f43.com','88f45.com','88f46.com','88f47.com','88f48.com','88f49.com','88f50.com','88f51.com','88f53.com','88f54.com','88f56.com','88f57.com','88f59.com','88f60.com','88f61.com','88f62.com','88f63.com','88f64.com','88f65.com','88f67.com','88f69.com','88f70.com','88f71.com','88f72.com','88f73.com','88f74.com','88f75.com','88fbet.com','88hjd.com','8ff003.com','8ff016.com','8ff019.com','8ff021.com','8ff024.com','8ff030.com','8ff107.com','8ff115.com','8ff119.com','8ff77.com','8ffbet.com','dongguanfanyi.com','ff8d.com','ff8g.com','ff8p.com','ff8q.com','ff8ttt.com','ff8x.com','ff8y.com','ff8z.com','gxnntly.com','sdhrjx.com','sun0012.com','sun0013.com','sun0015.com','sun0017.com','sun0021.com','sun0026.com','sun0027.com','sun0031.com','sun0032.com','sun0034.com','sun0035.com','sun0040.com','sun0057.com','sun0067.com','sun0073.com','sun0074.com','sun1801.com','sun1802.com','sun1803.com','sun1805.com','sun1806.com','sun1809.com','sun1810.com','sun1812.com','sun1815.com','sun1816.com','sun1820.com','sun1821.com','sun1823.com','ty9992.com','ty9998.com','tyty11.com','tyty195.com','v553.com','hjd95.com','xin5ff.com','xunjiedx.com','xxbb44.com','xxhh44.com','yangxuelei.com','zbysilver.com','zgle.tw','hh88hg.com','hjd001.com','hjd008.com','hnit110.net','hnxlzxxl.com','hnzxny.com','hebeiwanda.com','hztzjxss.com','i88hg.com','ii88hg.com','ingco-power.com','iowrt.cn','j88hg.com','honghoupifa.com','hspdzx.com','hveqqm.loan','jiahedi.com','jj88hg.com','dysdch.com','e88hg.com','ee88hg.com','ehomewell.com','fsllvz.cn','g88hg.com','ff801.com','ff807.com','ff88hg.com','ff8b.com','ff8j.com','ff8r.com','ff8v.com','ff8vip961.com','fjwl-sjz.cn','fkbke.cn','ggxx6.com','greengoldcn.com','30steel.com','30tyty.com','31tyty.com','32tyty.com','34tyty.com','35tyty.com','37tyty.com','38tyty.com','1tyc1.com','20ff8.com','20tyty.com','21ff8.com','21tyty.com','222a11.com','222a12.com','222b11.com','222d11.com','222e9.com','222f10.com','222f11.com','222f12.com','222f22.com','222f5.com','222f7.com','222f8.com','222f9.com','222m1.com','222m10.com','222m3.com','222m6.com','222m7.com','222m9.com','23ff8.com','23tyty.com','25tyty.com','26ff8.com','26tyty.com','27tyty.com','28tyty.com','29ff8.com','29tyty.com','000v4.com','000v8.com','01ff8.com','023zfzs.com','128030.org','128030.vip','13tyty.com','14tyty.com','15tyty.com','16tyty.com','17tyty.com','18ff8.com','18tyty.com','19tyty.com','40tyty.com','41tyty.com','46tyty.com','47tyty.com','48tyty.com','49tyty.com','50tyty.com','53tyty.com','542f.com','54tyty.com','56tyty.com','57tyty.com','59tyty.com','88f100.com','88f101.com','88f102.com','88f103.com','88f104.com','88f105.com','88f106.com','88f107.com','88f108.com','88f109.com','88f110.com','88f32.com','88f34.com','88f35.com','88f36.com','88f37.com','88f38.com','88f39.com','88f40.com','88f41.com','88f42.com','88f78.com','88f80.com','88f81.com','88f82.com','88f83.com','88f84.com','88f85.com','88f86.com','88f87.com','88f89.com','88f90.com','88f91.com','88f92.com','88f93.com','88f94.com','88f95.com','88f96.com','88f97.com','88f98.com','88hg11.com','88hg12.com','88hg13.com','88hg14.com','88hg15.com','88hg17.com','88hg18.com','88hg19.com','88hg20.com','88hg21.com','88hg22.com','88hg23.com','88hg25.com','88hg26.com','88hg27.com','88hg28.com','88hg29.com','88hg30.com','88hg31.com','88hg32.com','88hg34.com','88hg35.com','88hg36.com','88hg37.com','88hg38.com','88hg39.com','88hg40.com','88hg41.com','88hg43.com','88hg44.com','88hg45.com','88hg47.com','88hg48.com','88hg49.com','88hg50.com','88hg51.com','88hg52.com','88hg53.com','88hg54.com','88hg56.com','88hg57.com','88hg58.com','88hg59.com','88hg60.com','88hg61.com','88hg62.com','88hg63.com','88hg64.com','88hg65.com','88hg67.com','88hg68.com','88hg69.com','88hgh.com','88hgt.com','8ff017.com','8ff018.com','8ff022.com','8ff025.com','8ff102.com','8ff117.com','8ff120.com','8ff33.com','8ff44.com','72tyty.com','73tyty.com','74tyty.com','75tyty.com','76tyty.com','78tyty.com','79tyty.com','80tyty.com','81tyty.com','82tyty.com','60tyty.com','61tyty.com','62tyty.com','63tyty.com','64tyty.com','67tyty.com','68tyty.com','69tyty.com','70tyty.com','71tyty.com','acuc.tw','ahktzs.com','aijiat.com','99f2247.com','99f2248.com','99f2249.com','99f2250.com','99f2251.com','99f2252.com','99f2253.com','99f2254.com','99f2255.com','99f2256.com','99f2257.com','99f2258.com','99f2259.com','99f2260.com','99f2261.com','99f2262.com','99f2263.com','99f2264.com','99f2265.com','99f2266.com','aoli.tw','aqgnvq.cn','aqkxik.cn','arenor.cn','artnmu.cn','attydg.cn','bb88hg.com','bbzy168.com','bc5555.com','bcwzdd.com','v330.com','tllagyxx.com','vnsr032.com','vnsr1011.com','vnsr1012.com','vnsr1014.com','vnww.tw','wmye.tw','ty1868.com','tytya5.com','ufwh.tw','ugkiv.cn','woyuen.com','xapii.cn','xbfi.tw','ssc020.com','sun0024.com','sun0060.com','sun0064.com','sutongwlw.com','rurewv.loan','rydqjt.com','rdidk.cn','rflyz.cn','rfpu.tw','rr88hg.com','sh-bestshelf.com','shbssy.com','tianjinjinhe.com','bjkesy.com','bnpbjk.cn','bokangjd.com','bzdhsw.com','chinataxservices.com','chutong56.com','co-good.com','cvmf.tw','cc88hg.com','cecepgroup.com','cylichao.com','d88hg.com','kaitaiindustries.com','kdokld.loan','kk88hg.com','kmemek.cn','kmeo.tw','kmkszy.loan','kpopxj.cn','kqzie.cn','liangyu2008.com','lmdaiu.cn','loveg.net','lxryhy.cn','lygoldenladies.com','m-routing.com','maebdo.cn','l88hg.com','medo.tw','minglumedia.com','mingrongx.com','mo1p.com','mpkuq.cn','njrunyou.com','nsgnyf.loan','ntwil.cn','nuqoo.com','p88hg.com','pp88hg.com','pukunhee.com','o88hg.com','qq88hg.com','qrkpg.cn')";
            string sql2= @" SELECT DISTINCT zone,count(1) as c from zones 
                            where zone in( '25ff8.com','27ff8.com','28ff8.com','2ff8.com','30ff8.com','789f.com','a111f.com','88f43.com','88f45.com','88f46.com','88f47.com','88f48.com','88f49.com','88f50.com','88f51.com','88f53.com','88f54.com','88f56.com','88f57.com','88f59.com','88f60.com','88f61.com','88f62.com','88f63.com','88f64.com','88f65.com','88f67.com','88f69.com','88f70.com','88f71.com','88f72.com','88f73.com','88f74.com','88f75.com','88fbet.com','88hjd.com','8ff003.com','8ff016.com','8ff019.com','8ff021.com','8ff024.com','8ff030.com','8ff107.com','8ff115.com','8ff119.com','8ff77.com','8ffbet.com','dongguanfanyi.com','ff8d.com','ff8g.com','ff8p.com','ff8q.com','ff8ttt.com','ff8x.com','ff8y.com','ff8z.com','gxnntly.com','sdhrjx.com','sun0012.com','sun0013.com','sun0015.com','sun0017.com','sun0021.com','sun0026.com','sun0027.com','sun0031.com','sun0032.com','sun0034.com','sun0035.com','sun0040.com','sun0057.com','sun0067.com','sun0073.com','sun0074.com','sun1801.com','sun1802.com','sun1803.com','sun1805.com','sun1806.com','sun1809.com','sun1810.com','sun1812.com','sun1815.com','sun1816.com','sun1820.com','sun1821.com','sun1823.com','ty9992.com','ty9998.com','tyty11.com','tyty195.com','v553.com','hjd95.com','xin5ff.com','xunjiedx.com','xxbb44.com','xxhh44.com','yangxuelei.com','zbysilver.com','zgle.tw','hh88hg.com','hjd001.com','hjd008.com','hnit110.net','hnxlzxxl.com','hnzxny.com','hebeiwanda.com','hztzjxss.com','i88hg.com','ii88hg.com','ingco-power.com','iowrt.cn','j88hg.com','honghoupifa.com','hspdzx.com','hveqqm.loan','jiahedi.com','jj88hg.com','dysdch.com','e88hg.com','ee88hg.com','ehomewell.com','fsllvz.cn','g88hg.com','ff801.com','ff807.com','ff88hg.com','ff8b.com','ff8j.com','ff8r.com','ff8v.com','ff8vip961.com','fjwl-sjz.cn','fkbke.cn','ggxx6.com','greengoldcn.com','30steel.com','30tyty.com','31tyty.com','32tyty.com','34tyty.com','35tyty.com','37tyty.com','38tyty.com','1tyc1.com','20ff8.com','20tyty.com','21ff8.com','21tyty.com','222a11.com','222a12.com','222b11.com','222d11.com','222e9.com','222f10.com','222f11.com','222f12.com','222f22.com','222f5.com','222f7.com','222f8.com','222f9.com','222m1.com','222m10.com','222m3.com','222m6.com','222m7.com','222m9.com','23ff8.com','23tyty.com','25tyty.com','26ff8.com','26tyty.com','27tyty.com','28tyty.com','29ff8.com','29tyty.com','000v4.com','000v8.com','01ff8.com','023zfzs.com','128030.org','128030.vip','13tyty.com','14tyty.com','15tyty.com','16tyty.com','17tyty.com','18ff8.com','18tyty.com','19tyty.com','40tyty.com','41tyty.com','46tyty.com','47tyty.com','48tyty.com','49tyty.com','50tyty.com','53tyty.com','542f.com','54tyty.com','56tyty.com','57tyty.com','59tyty.com','88f100.com','88f101.com','88f102.com','88f103.com','88f104.com','88f105.com','88f106.com','88f107.com','88f108.com','88f109.com','88f110.com','88f32.com','88f34.com','88f35.com','88f36.com','88f37.com','88f38.com','88f39.com','88f40.com','88f41.com','88f42.com','88f78.com','88f80.com','88f81.com','88f82.com','88f83.com','88f84.com','88f85.com','88f86.com','88f87.com','88f89.com','88f90.com','88f91.com','88f92.com','88f93.com','88f94.com','88f95.com','88f96.com','88f97.com','88f98.com','88hg11.com','88hg12.com','88hg13.com','88hg14.com','88hg15.com','88hg17.com','88hg18.com','88hg19.com','88hg20.com','88hg21.com','88hg22.com','88hg23.com','88hg25.com','88hg26.com','88hg27.com','88hg28.com','88hg29.com','88hg30.com','88hg31.com','88hg32.com','88hg34.com','88hg35.com','88hg36.com','88hg37.com','88hg38.com','88hg39.com','88hg40.com','88hg41.com','88hg43.com','88hg44.com','88hg45.com','88hg47.com','88hg48.com','88hg49.com','88hg50.com','88hg51.com','88hg52.com','88hg53.com','88hg54.com','88hg56.com','88hg57.com','88hg58.com','88hg59.com','88hg60.com','88hg61.com','88hg62.com','88hg63.com','88hg64.com','88hg65.com','88hg67.com','88hg68.com','88hg69.com','88hgh.com','88hgt.com','8ff017.com','8ff018.com','8ff022.com','8ff025.com','8ff102.com','8ff117.com','8ff120.com','8ff33.com','8ff44.com','72tyty.com','73tyty.com','74tyty.com','75tyty.com','76tyty.com','78tyty.com','79tyty.com','80tyty.com','81tyty.com','82tyty.com','60tyty.com','61tyty.com','62tyty.com','63tyty.com','64tyty.com','67tyty.com','68tyty.com','69tyty.com','70tyty.com','71tyty.com','acuc.tw','ahktzs.com','aijiat.com','99f2247.com','99f2248.com','99f2249.com','99f2250.com','99f2251.com','99f2252.com','99f2253.com','99f2254.com','99f2255.com','99f2256.com','99f2257.com','99f2258.com','99f2259.com','99f2260.com','99f2261.com','99f2262.com','99f2263.com','99f2264.com','99f2265.com','99f2266.com','aoli.tw','aqgnvq.cn','aqkxik.cn','arenor.cn','artnmu.cn','attydg.cn','bb88hg.com','bbzy168.com','bc5555.com','bcwzdd.com','v330.com','tllagyxx.com','vnsr032.com','vnsr1011.com','vnsr1012.com','vnsr1014.com','vnww.tw','wmye.tw','ty1868.com','tytya5.com','ufwh.tw','ugkiv.cn','woyuen.com','xapii.cn','xbfi.tw','ssc020.com','sun0024.com','sun0060.com','sun0064.com','sutongwlw.com','rurewv.loan','rydqjt.com','rdidk.cn','rflyz.cn','rfpu.tw','rr88hg.com','sh-bestshelf.com','shbssy.com','tianjinjinhe.com','bjkesy.com','bnpbjk.cn','bokangjd.com','bzdhsw.com','chinataxservices.com','chutong56.com','co-good.com','cvmf.tw','cc88hg.com','cecepgroup.com','cylichao.com','d88hg.com','kaitaiindustries.com','kdokld.loan','kk88hg.com','kmemek.cn','kmeo.tw','kmkszy.loan','kpopxj.cn','kqzie.cn','liangyu2008.com','lmdaiu.cn','loveg.net','lxryhy.cn','lygoldenladies.com','m-routing.com','maebdo.cn','l88hg.com','medo.tw','minglumedia.com','mingrongx.com','mo1p.com','mpkuq.cn','njrunyou.com','nsgnyf.loan','ntwil.cn','nuqoo.com','p88hg.com','pp88hg.com','pukunhee.com','o88hg.com','qq88hg.com','qrkpg.cn')
                            group by zone order by c desc";
            DataTable dta = MySQLHelper.Query(sql1).Tables[0];
            DataTable dtz = MySQLHelper.Query(sql2).Tables[0];
            Console.WriteLine("count1= " + dta.Rows.Count + "   counte2=" + dtz.Rows.Count);

            List<zonecount> zclist= DtToList<zonecount>.ConvertToModel(dtz);

            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            foreach (DataRow dr in dta.Rows)
            {
                string zone = dr["zone"].ToString();
                var c = zclist.Find(zc => zc.zone == zone).c;
                if ( c== 1)
                {
                    if (dr["id"].ToString() != dr["azid"].ToString()) {
                        long aid = Convert.ToInt32(dr["aid"]);
                        string rrcol = Utility.StringHelper.CalculateMD5Hash(zone + ".").Substring(0, 1).ToLower();
                        IMongoCollection<AuthoritiesSimple> categoriesA = db.GetCollection<AuthoritiesSimple>(rrcol);
                        categoriesA.DeleteOne(Builders<AuthoritiesSimple>.Filter.Eq("rid",-aid));
                        MySQLHelper.ExecuteSql("delete from authorities where id="+aid);
                        LoggerAdvance.AddLog(zone + "   " + aid + "   delete","deleteSOANS","");
                    }
                }
                else if (c == 2)
                {
                   
                }
                else {
                    LoggerAdvance.AddLog(zone + "   exception", "deleteSOANS", "");
                }
            }
            LoggerAdvance.AddLog("count=1 processed","deleteSOANS","");

            foreach (zonecount zc in zclist) {
                if (zc.c == 2) {
                    string sql3 = @"SELECT z.id,z.zone,a.id as aid,a.type as atype,d.zone as dzone
                                from zones as z LEFT JOIN authorities as a on z.ID=a.ZoneID 
                                left join dnsrecords as d on d.zoneid=z.id
                                where z.zone='" + zc.zone + "'";
                    DataTable dt = MySQLHelper.Query(sql3).Tables[0];
                    List<zad> zadlist= DtToList<zad>.ConvertToModel(dt);
                    int dcount = zadlist.FindAll(zad => zad.dzone != "").Count();
                    if (dcount == 0) {

                    }
                }
            }            
        }


        static void DeleteIgnoreHost() {
            string sql1 = "select d.id,d.zoneid,d.zone,d.host,d.type,d.data,d.ttl,d.view,d.mx_priority,d.userid,d.active from dnsrecords as d  where (`Host` like '%@%' and `Host` <>'@') or (`Host`='@' and Type='NS')";
            DataTable dtd = MySQLHelper.Query(sql1).Tables[0];
            Console.WriteLine("getdata count= "+dtd.Rows.Count);
            List<dnsrecords> dlist = DtToList<dnsrecords>.ConvertToModel(dtd);
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            foreach (dnsrecords d in dlist) {
                string domain = d.zone + ".";
                string rrcol = Utility.StringHelper.CalculateMD5Hash(domain).Substring(0, 1).ToLower();
                IMongoCollection<DnsRecordsSimple> categoriesD = db.GetCollection<DnsRecordsSimple>(rrcol);
                var count = categoriesD.Find(Builders<DnsRecordsSimple>.Filter.Eq("rid", d.id)).ToList<DnsRecordsSimple>().Count;
                if (count > 0)
                {
                    Console.WriteLine("zone= " + d.zone + " id= " + d.id + " type= " + d.type);
                    categoriesD.DeleteMany(Builders<DnsRecordsSimple>.Filter.Eq("rid", d.id));
                }
            }

        }



        static void RefreshRDomain()
        {
            string sql = @"select zone,rzone from zones where zone<>rzone;
                           select distinct rzone as rzone from zones where zone<>rzone;";
            DataSet ds = MySQLHelper.Query(sql);
            Console.WriteLine("get data " + ds.Tables[0].Rows.Count + "  " + ds.Tables[1].Rows.Count);
            List<zoneAndrzone> zlist = DtToList<zoneAndrzone>.ConvertToModel(ds.Tables[0]);

            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            IMongoCollection<ZonesSimple> categoriesZ = db.GetCollection<ZonesSimple>("zones");
            int count = 0;
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时 
            foreach (DataRow dr in ds.Tables[1].Rows)
            {
                List<string> zonelist = (from z in zlist where z.rzone == dr["rzone"].ToString() select z.zone + ".").ToList<string>();
                var builder = Builders<ZonesSimple>.Filter;
                var filter = builder.In("domain", zonelist);
                string rrcol = StringHelper.CalculateMD5Hash(dr["rzone"].ToString() + ".").ToLower().Substring(0, 1);
                var update = Builders<ZonesSimple>.Update.Set("rdomain", dr["rzone"].ToString() + ".").Set("rrcol", rrcol);
                categoriesZ.UpdateMany(filter, update);
                count++;
                if (count > 0 && count % 50 == 0)
                {
                    Console.WriteLine("count= {0} time= {1}", count, watch.ElapsedMilliseconds);
                }
            }
            Console.WriteLine("count= {0} time= {1}", count, watch.ElapsedMilliseconds);
        }

        static void RefreshRDomain2()
        {
            string zoneStr = "anydzun.com,kaznzhuan.com,eiztzi.com,cayczui.com,gezqzhui.com,pizezhu.com,dizwzong.com,hezpzhuang.com,lazyzhua.com,fayfzhun.com,zgcjw114.com,yztzf.com,wwwnenas.com,udgqd.cn,tjzgg.com,smazs.com,scemcy.com,odpum.cn,mxuorb.cn,momoda1314.com,moertesi.com,mk67a.com,lt5p.cn,klokyw.cn,jxmcx.com,imvfyb.cn,hepidemic.com,heibaid.com,gyxtsys.com,ghdclt.com,ghdcdh.com,emg-sh.cn,dy8880.com,dljgnk.com,cxqyfhc.com,crestkaihuaixiao.com,bmhtmp.com,bjxyc6.cn,azhanhui.com,amdkdl.cn,alskdj.cn,695f.cn,51gfx.com,sisubbs.cn,zhe-wen.cn,yuqioes.cn,jdxzs.wang,cyllife.cn,51zizhi.cn,dggy888.cn,tvmaker.cn,sfzcake.cn,vstreet.cn,xdfyled.cn,hodojia.cn,hljbhcz.cn,hanyork.cn,xiao800.cn,store999.cn,shuiyige.cn,xinyuetu.cn,99huimin.cn,noichina.cn,fsyidali.cn,pcjidian.cn,whhongyu.cn,qjtravel.cn,appboxes.cn,feusgx.cn,zhongia.cn,cyhwang.cn,120wang.cn,ledoute.cn,sgkwdzp.cn,ihxjgi.cn,vjvcui.cn,foiaqt.cn,oviyjb.cn,svwksc.cn,rxqaba.cn,pgecbt.cn,ahdhjg.cn,rivocg.cn,tlkoew.cn,gygfrh.cn,ccjgag.cn,tliamd.cn,kqnugs.cn,amihkc.cn,kstcby.cn,tnpqpm.cn,vuxkpy.cn,zoegkx.cn,ljxzjq.cn,ftlpdt.cn,cjwoar.cn,ppcwke.cn,ipovjg.cn,fuaant.cn,kajywr.cn,gzqokt.cn,bkcpgv.cn,htwodr.cn,fjtymy.cn,jmvkrx.cn,cvaath.cn,ckwcqx.cn,hysdfs.cn,hebdll.cn,voolun.cn,hhdhsk.cn,sxsmsq.cn,ahhxjj.cn,ztggfw.cn,ennisi.cn,fswlkj.cn,mrhecn.cn,mznxbb.cn,extabr.cn,85594.com,ufsna.com,chinauo.com,cndetion.net,hwygww.cf,hwyguu.cf,hwygzz.cf,hwygxx.cf,hwygcc.cf,hwygss.cf,hwygkk.cf,hwygmm.cf,bohanmu.com,xszdk.com,95082.com,59713.cn,xyrks.tw,59531.cn,28618.cn,sbkdk.com,xnrng.tw,fqcgh.tw,fucmj.tw,koqcu.tw,khqxj.tw,ysbdn.tw,fycys.tw,fwcsr.tw,yfbti.tw,ftcre.tw,pnxfk.tw,kiqbu.tw,fwccg.tw,fqcsp.tw,khqvg.tw,xursb.tw,khqok.tw,xzril.tw,yxbdr.tw,kxqjq.tw,xprgo.tw,pqxbg.tw,pdxsm.tw,xjrfr.tw,kvqji.tw,yebyu.tw,xqrly.tw,yybob.tw,kiqsd.tw,xaruj.tw,ydbsv.tw,yybew.tw,fxcml.tw,kcqcb.tw,pgxnd.tw,ftcng.tw,yqbop.tw,fycws.tw,plxba.tw,pexzj.tw,kiqmg.tw,xzrlu.tw,yeblp.tw,pzxuh.tw,pbxyq.tw,kxqnk.tw,86263263.com,recordprograms.com,jlsouser.com,summainfo.com,fidfed.com,shhouniao.com,dinyingtiantang.com,bdscjy.com,online-girls-games.com,chnsantai.com,fenxiaomi.com,dnhjpjw.com,qdcosway.com,suntourchina.com,scdzfh.com,hsd-edu.com,shenzhen80.com,khqnbbs.com,fjdnmy.com,jiahepifu.com,nblrgs.com,yaoncg.com,jajru.com,xiangheyi.com,ccofee.com,fangshen888.com,70caijing.com,hzzhqx.com,sh1pin.com,wettrannypics.com,acu-cut.com,economic-forum.com,namefinderslists.com,pulmonarydata.com,saturnstpaul.com,matrix-consultants.com,amarillo-tx.com,bjtzdb.com,bjzhuoya.com,51wjlweixiu.com,czhxfj.com,wzdongou.com,yudingimd.com,lhszyy.com,lofier.com,xingyujiw.com,ahmiaolaozu.com,lunwen36.com,44gew.com,cameronjamesgroup.com,miugopay.com,i-hera.com,footfetishjoy.com,ddcctv.com,bagwovenmachine.com,zjgqyjx.com,yycjdq.com,ew998.com,t6533.com,pcpeijian.com,zhouxinym.com,szdreamy.com,babesgonude.com,luckyknit.com,0311999.com,chimevisual.com,bjkdxy.com,hnfenglu.com,c9rubber.com,ylyxj.com,hj911.com,thm88.com,bdqn5.com,wellpm.com,lvnenghome.com,zgshnk.com,gmtspindle.com,wzhheng.com,hanjinbao.com,xlgss.com,shandonglian.com,ws-fx.com,51mgt.com,dhesjx.com,sanxingad.com,ahxfyy.com,cscrfw.com,9weini.com,dianhuacn.com,qx-art.com,sz-tssg.com,cailiys.com,tj-unique.com,newnewfamily.com,hncj88.com,dbx88.com,gn772.com,kaiduyi.com,cganime.com,gaoyao0758.com,sczql88.com,wzndd.com,cnzsweb.com,95091.com,97caomei.com,yxh07.com,lulu8.net,lulu8.com,ikan199.com,ikan188.com,ikan18.com,yxh02.com,ccpp500e.com,ccpp500n.com,ccpp500m.com,ccpp500p.com,ccpp500k.com,ccpp500j.com,ccpp500d.com,ccpp500c.com,ccpp500b.com,www-ccpp500.com,ccpp500a.com,ccpp500f.com,ccpp500.info,www-cp500.com,ccpp500.net,ccpp500.org,ccpp500.com,hg101-js-gov.cn,hg100-js-gov.cn,hg99-js-gov.cn,xmgxzc.com,hg98-js-gov.cn,hg97-js-gov.cn,hg96-js-gov.cn,hg95-js-gov.cn,hg94-js-gov.cn,hg93-js-gov.cn,hg92-js-gov.cn,hg91-js-gov.cn,hg90-js-gov.cn,hg89-js-gov.cn,hg88-js-gov.cn,hg87-js-gov.cn,hg86-js-gov.cn,hg85-js-gov.cn,hg84-js-gov.cn,hg83-js-gov.cn,hg82-js-gov.cn,hg81-js-gov.cn,icp500.cc,hg80-js-gov.cn,hg79-js-gov.cn,hg78-js-gov.cn,hg77-js-gov.cn,hg76-js-gov.cn,hg75-js-gov.cn,hg74-js-gov.cn,hg73-js-gov.cn,hg72-js-gov.cn,hg71-js-gov.cn,hg70-js-gov.cn,hg69-js-gov.cn,hg68-js-gov.cn,hg67-js-gov.cn,hg66-js-gov.cn,hg65-js-gov.cn,hg63-js-gov.cn,hg62-js-gov.cn,hg61-js-gov.cn,hg60-js-gov.cn,hg59-js-gov.cn,hg58-js-gov.cn,huixiansudi.com,defineoyun.com,hg57-js-gov.cn,mosuten.com,cpastdonat.com,julietkakish.com,kolpakovtrio.com,pgbweb.com,karelfelipe.com,hg56-js-gov.cn,199jewelry.com,laftrading.com,blmplastik.com,cantaraville.com,eltonmarcia.com,flybyrc.com,hg55-js-gov.cn,aneldegrau.com,thecrabmovie.com,arsubtilior.com,myrale.com,devotionmed.com,hg54-js-gov.cn,azvdo.com,lefouzytout.com,xuntide.com,sirdholera.com,chinasjia.com,hg53-js-gov.cn,hjertesprak.com,blinkoz.com,wtcvib.com,laughallnite.com,coolsculptny.com,boracaysong.com,hg52-js-gov.cn,mtbserbia.com,peggydart.com,saudedaalma.com,mycarstairs.com,sarcoplex.com,hg51-js-gov.cn,cimlistak.com,delishstjo.com,howardlockie.com,1zop.com,ddsflss20.com,hg50-js-gov.cn,chkuaidian.com,bcartguild.com,altertechit.com,majorjumpers.com,amoshaley.com,dadged.com,hg49-js-gov.cn,mrnapoleon.com,earnpips.com,djyros.com,pjscoinserie.com,euserral.com,datesmapp.com,hg48-js-gov.cn,prtechem.com,nancyskowbo.com,insurancetj.com,trocalingua.com,gzyifeiart.com,hg47-js-gov.cn,nahidmannon.com,photozesar.com,juliadelarue.com,tfoxsearch.com,tradermm.com,hg46-js-gov.cn,yumlinux.com,burjalsafwa.com,assurconcept.com,itsheatpumps.com,tvclcps.com,eternisez.com,hg45-js-gov.cn,enteratejobs.com,hoangdonganh.com,nusby.com,subaole.com,josepalomero.com,daneandward.com,hg44-js-gov.cn,topcatshirts.com,uaacampus.com,stataguide.com,aviontest.com,studiodior.com,hg43-js-gov.cn,dia-art.com,karmadillocf.com,xksb168.com,lcxc88.com,dangywluo.com,hg42-js-gov.cn,hangxingbao.com,selktutoring.com,naborscanada.com,hg41-js-gov.cn,hg40-js-gov.cn,hg39-js-gov.cn,hg38-js-gov.cn,hg37-js-gov.cn,hg36-js-gov.cn,hg35-js-gov.cn,hg34-js-gov.cn,hg33-js-gov.cn,hg32-js-gov.cn,hg31-js-gov.cn,hg30-js-gov.cn,hg29-js-gov.cn,hg28-js-gov.cn,hg27-js-gov.cn,hg26-js-gov.cn,hg25-js-gov.cn,hg24-js-gov.cn,hg23-js-gov.cn,hg22-js-gov.cn,hg21-js-gov.cn,hg20-js-gov.cn,hg19-js-gov.cn,hg18-js-gov.cn,hg17-js-gov.cn,hg16-js-gov.cn,hg15-js-gov.cn,hg14-js-gov.cn,hg13-js-gov.cn,hg12-js-gov.cn,hg11-js-gov.cn,hg10-js-gov.cn,hg09-js-gov.cn,hg08-js-gov.cn,797970ee.com,hg07-js-gov.cn,hg06-js-gov.cn,hg05-js-gov.cn,hg04-js-gov.cn,hg03-js-gov.cn,hg02-js-gov.cn,hg01-js-gov.cn,flowportalcd.cn,hg203-js-gov.cn,hg202-js-gov.cn,hg201-js-gov.cn,hg200-js-gov.cn,hg199-js-gov.cn,hg198-js-gov.cn,hg197-js-gov.cn,hg196-js-gov.cn,hg195-js-gov.cn,hg194-js-gov.cn,hg193-js-gov.cn,hg192-js-gov.cn,hg191-js-gov.cn,hg190-js-gov.cn,hg189-js-gov.cn,hg188-js-gov.cn,hg187-js-gov.cn,hg186-js-gov.cn,hg185-js-gov.cn,hg184-js-gov.cn,hg183-js-gov.cn,hg182-js-gov.cn,hg181-js-gov.cn,hg180-js-gov.cn,hg179-js-gov.cn,hg178-js-gov.cn,hg177-js-gov.cn,hg176-js-gov.cn,hg175-js-gov.cn,hg174-js-gov.cn,hg173-js-gov.cn,hg172-js-gov.cn,hg171-js-gov.cn,hg170-js-gov.cn,hg169-js-gov.cn,hg168-js-gov.cn,hg167-js-gov.cn,hg166-js-gov.cn,hg165-js-gov.cn,hg163-js-gov.cn,flowportalxa.cn,hg162-js-gov.cn,hg161-js-gov.cn,hg160-js-gov.cn,hg159-js-gov.cn,hg157-js-gov.cn,hg156-js-gov.cn,hg155-js-gov.cn,hg154-js-gov.cn,hg153-js-gov.cn,hg152-js-gov.cn,hg151-js-gov.cn,hg150-js-gov.cn,hg149-js-gov.cn,hg148-js-gov.cn,hg147-js-gov.cn,hg146-js-gov.cn,hg145-js-gov.cn,hg144-js-gov.cn,hg143-js-gov.cn,hg142-js-gov.cn,hg141-js-gov.cn,hg140-js-gov.cn,hg139-js-gov.cn,hg138-js-gov.cn,hg137-js-gov.cn,hg136-js-gov.cn,hg135-js-gov.cn,hg134-js-gov.cn,hg133-js-gov.cn,hg132-js-gov.cn,hg131-js-gov.cn,hg130-js-gov.cn,hg129-js-gov.cn,hg128-js-gov.cn,hg127-js-gov.cn,hg126-js-gov.cn,hg125-js-gov.cn,hg124-js-gov.cn,hg123-js-gov.cn,jiangsusuanwujinghua.com,hg122-js-gov.cn,hg121-js-gov.cn,hg120-js-gov.cn,hg119-js-gov.cn,hg118-js-gov.cn,hg117-js-gov.cn,hg116-js-gov.cn,hg115-js-gov.cn,hg114-js-gov.cn,hg113-js-gov.cn,hg112-js-gov.cn,hg111-js-gov.cn,hg110-js-gov.cn,051987771888.com,hg109-js-gov.cn,hg108-js-gov.cn,hg107-js-gov.cn,hg106-js-gov.cn,hg105-js-gov.cn,hg104-js-gov.cn,hg103-js-gov.cn,hg102-js-gov.cn,hg7979-js-gov.cn,hg6969-js-gov.cn,hg5959-js-gov.cn,hg4949-js-gov.cn,hg3939-js-gov.cn,hg2929-js-gov.cn,hg299-js-gov.cn,hg298-js-gov.cn,hg297-js-gov.cn,hg296-js-gov.cn,hg295-js-gov.cn,hg294-js-gov.cn,hg293-js-gov.cn,hg292-js-gov.cn,hg291-js-gov.cn,hg290-js-gov.cn,hg289-js-gov.cn,hg288-js-gov.cn,hg287-js-gov.cn,hg286-js-gov.cn,hg285-js-gov.cn,hg284-js-gov.cn,hg283-js-gov.cn,hg282-js-gov.cn,hg281-js-gov.cn,hg280-js-gov.cn,hg279-js-gov.cn,hg278-js-gov.cn,hg277-js-gov.cn,234cai.top,hg276-js-gov.cn,hg275-js-gov.cn,hg274-js-gov.cn,hg273-js-gov.cn,hg272-js-gov.cn,hg271-js-gov.cn,hg270-js-gov.cn,hg269-js-gov.cn,hg268-js-gov.cn,hg267-js-gov.cn,hg266-js-gov.cn,hg265-js-gov.cn,hg263-js-gov.cn,hg262-js-gov.cn,hg261-js-gov.cn,hg259-js-gov.cn,hg258-js-gov.cn,hg257-js-gov.cn,hg256-js-gov.cn,hg255-js-gov.cn,hg254-js-gov.cn,hg253-js-gov.cn,hg252-js-gov.cn,hg251-js-gov.cn,hg250-js-gov.cn,hg249-js-gov.cn,hg248-js-gov.cn,hg247-js-gov.cn,hg246-js-gov.cn,hg245-js-gov.cn,hg244-js-gov.cn,hg243-js-gov.cn,hg242-js-gov.cn,hg241-js-gov.cn,hg240-js-gov.cn,hg239-js-gov.cn,hg238-js-gov.cn,hg237-js-gov.cn,hg236-js-gov.cn,hg235-js-gov.cn,hg234-js-gov.cn,hg233-js-gov.cn,hg232-js-gov.cn,hg231-js-gov.cn,hg230-js-gov.cn,hg229-js-gov.cn,hg228-js-gov.cn,hg227-js-gov.cn,hg226-js-gov.cn,hg225-js-gov.cn,hg224-js-gov.cn,hg223-js-gov.cn,hg222-js-gov.cn,hg221-js-gov.cn,hg220-js-gov.cn,hg219-js-gov.cn,hg218-js-gov.cn,hg217-js-gov.cn,hg216-js-gov.cn,hg215-js-gov.cn,hg214-js-gov.cn,hg213-js-gov.cn,hg212-js-gov.cn,hg211-js-gov.cn,hg210-js-gov.cn,hg209-js-gov.cn,hg208-js-gov.cn,hg207-js-gov.cn,hg206-js-gov.cn,hg205-js-gov.cn,hg204-js-gov.cn,xpj9269-hg-gov.cn,xpj9247-hg-gov.cn,ss926221.com,wuchengya.com,xpj9999-hg-gov.cn,xpj9996-hg-gov.cn,xpj9966-hg-gov.cn,xpj9866-hg-gov.cn,xpj9366-hg-gov.cn,xpj9365-hg-gov.cn,xpj9363-hg-gov.cn,xpj9362-hg-gov.cn,xpj9361-hg-gov.cn,xpj9360-hg-gov.cn,xpj9359-hg-gov.cn,xpj9358-hg-gov.cn,xpj9357-hg-gov.cn,xpj9356-hg-gov.cn,xpj9355-hg-gov.cn,xpj9354-hg-gov.cn,xpj9353-hg-gov.cn,xpj9352-hg-gov.cn,xpj9351-hg-gov.cn,xpj9350-hg-gov.cn,xpj9349-hg-gov.cn,xpj9348-hg-gov.cn,xpj9347-hg-gov.cn,xpj9346-hg-gov.cn,xpj9345-hg-gov.cn,xpj9344-hg-gov.cn,xpj9343-hg-gov.cn,xpj9342-hg-gov.cn,xpj9341-hg-gov.cn,xpj9340-hg-gov.cn,xpj9339-hg-gov.cn,xpj9338-hg-gov.cn,xpj9337-hg-gov.cn,xpj9336-hg-gov.cn,xpj9335-hg-gov.cn,xpj9334-hg-gov.cn,xpj9333-hg-gov.cn,xpj9332-hg-gov.cn,xpj9331-hg-gov.cn,xpj9330-hg-gov.cn,xpj9329-hg-gov.cn,xpj9328-hg-gov.cn,xpj9327-hg-gov.cn,xpj9326-hg-gov.cn,xpj9325-hg-gov.cn,xpj9324-hg-gov.cn,0513abaa.com,zymsxy.com,ahgyly.com,bjbulaze.com,calilord.com,xpj9323-hg-gov.cn,ccetsy.com,chinakri.com,cndnsj.com,csetsy.com,danbdang.com,xpj9322-hg-gov.cn,dletsy.com,doplive.com,ecfszx.com,enjoyjp.com,etgmarket.com,fszyt.com,xpj9321-hg-gov.cn,fudaishu.com,gcxhcs.com,gpuer.com,gzetsy.com,xpj9320-hg-gov.cn,xpj9319-hg-gov.cn,xpj9318-hg-gov.cn,xpj9317-hg-gov.cn,xpj9316-hg-gov.cn,xpj9315-hg-gov.cn,xpj9314-hg-gov.cn,xpj9313-hg-gov.cn,xpj9312-hg-gov.cn,xpj9311-hg-gov.cn,xpj9310-hg-gov.cn,xpj9309-hg-gov.cn,xpj9308-hg-gov.cn,xpj9307-hg-gov.cn,xpj9306-hg-gov.cn,xpj9305-hg-gov.cn,xpj9304-hg-gov.cn,xpj9303-hg-gov.cn,xpj9302-hg-gov.cn,xpj9301-hg-gov.cn,xpj9300-hg-gov.cn,451v.com,xpj9299-hg-gov.cn,xpj9298-hg-gov.cn,xpj9297-hg-gov.cn,xpj9296-hg-gov.cn,xpj9295-hg-gov.cn,xpj9294-hg-gov.cn,471e.com,xpj9293-hg-gov.cn,472e.com,xpj9292-hg-gov.cn,xpj9291-hg-gov.cn,62bu.com,xpj9290-hg-gov.cn,62ce.com,xpj9289-hg-gov.cn,xpj9288-hg-gov.cn,xpj9287-hg-gov.cn,k4128.com,xpj9286-hg-gov.cn,xpj9285-hg-gov.cn,xpj9284-hg-gov.cn,xpj9283-hg-gov.cn,xpj9282-hg-gov.cn,xpj9281-hg-gov.cn,xpj9280-hg-gov.cn,xpj9279-hg-gov.cn,xpj9278-hg-gov.cn,xpj9277-hg-gov.cn,xpj9276-hg-gov.cn,xpj9275-hg-gov.cn,xpj9274-hg-gov.cn,xpj9273-hg-gov.cn,xpj9272-hg-gov.cn,xpj9271-hg-gov.cn,xpj9270-hg-gov.cn,xpj9268-hg-gov.cn,xpj9267-hg-gov.cn,xpj9266-hg-gov.cn,xpj9265-hg-gov.cn,xpj9263-hg-gov.cn,xpj9262-hg-gov.cn,xpj9261-hg-gov.cn,xpj9260-hg-gov.cn,xpj9259-hg-gov.cn,xpj9258-hg-gov.cn,xpj9257-hg-gov.cn,xpj9256-hg-gov.cn,xpj9255-hg-gov.cn,xpj9254-hg-gov.cn,xpj9253-hg-gov.cn,cscs.com,xpj9252-hg-gov.cn,xpj9251-hg-gov.cn,xpj9250-hg-gov.cn,xpj9249-hg-gov.cn,xpj9248-hg-gov.cn,xpj9246-hg-gov.cn,xpj9245-hg-gov.cn,xpj9244-hg-gov.cn,xpj9243-hg-gov.cn,xpj9242-hg-gov.cn,xpj9241-hg-gov.cn,xpj9240-hg-gov.cn,xpj9239-hg-gov.cn,xpj9238-hg-gov.cn,xpj9237-hg-gov.cn,xpj9236-hg-gov.cn,xpj9235-hg-gov.cn,xpj9234-hg-gov.cn,xpj9233-hg-gov.cn,xpj9232-hg-gov.cn,xpj9231-hg-gov.cn,xpj9230-hg-gov.cn,xpj9229-hg-gov.cn,xpj9228-hg-gov.cn,xpj9227-hg-gov.cn,xpj9226-hg-gov.cn,xpj9225-hg-gov.cn,xpj9224-hg-gov.cn,xpj9223-hg-gov.cn,xpj9222-hg-gov.cn,xpj9221-hg-gov.cn,xpj9220-hg-gov.cn,xpj9219-hg-gov.cn,xpj9218-hg-gov.cn,xpj9217-hg-gov.cn,xpj9216-hg-gov.cn,xpj9215-hg-gov.cn,xpj9214-hg-gov.cn,xpj9213-hg-gov.cn,xpj9212-hg-gov.cn,xpj9211-hg-gov.cn,xpj9210-hg-gov.cn,xpj9209-hg-gov.cn,xpj9208-hg-gov.cn,xpj9207-hg-gov.cn,xpj9206-hg-gov.cn,xpj9205-hg-gov.cn,xpj9204-hg-gov.cn,xpj9203-hg-gov.cn,xpj9202-hg-gov.cn,xpj9201-hg-gov.cn,xpj9200-hg-gov.cn,xpj9199-hg-gov.cn,xpj9198-hg-gov.cn,xpj9197-hg-gov.cn,xpj9196-hg-gov.cn,xpj9195-hg-gov.cn,xpj9194-hg-gov.cn,xpj9193-hg-gov.cn,xpj9191-hg-gov.cn,xpj9190-hg-gov.cn,xpj9189-hg-gov.cn,xpj9188-hg-gov.cn,xpj9187-hg-gov.cn,xpj9186-hg-gov.cn,xpj9185-hg-gov.cn,xpj9184-hg-gov.cn,xpj9183-hg-gov.cn,xpj9182-hg-gov.cn,xpj9181-hg-gov.cn,xpj9180-hg-gov.cn,xpj9179-hg-gov.cn,xpj9178-hg-gov.cn,xpj9177-hg-gov.cn,xpj9176-hg-gov.cn,xpj9175-hg-gov.cn,xpj9174-hg-gov.cn,xpj9173-hg-gov.cn,xpj9172-hg-gov.cn,xpj9171-hg-gov.cn,xpj9170-hg-gov.cn,xpj9169-hg-gov.cn,xpj9168-hg-gov.cn,sswenhua.com,jiwenchina.com,jindutv.com,jbqxw.com,ipbzs.com,hzetsy.com,hzditu.com,huawennetwork.com,hljwlw.com,hdlfsh.com,hdfkw.com,hanyuansolar.com,hanruicn.com,0575dx.com,money329.com,shoufanli.com,xfbb88.com,vipmsh.com,b8ant.tk,b8ant.ml,b8ant.ga,b8ant.gq,b8ant.cf,sx-56.com,aliyao.wang,liucura.com,ixiabt.com,mh160.com,biljin.com,12366.ml,chinairc.net,hnygxp.com,sexshop888.com,llll66661.com,wwwxuynxqu.com,wwwrdhnntr.com,h9852.com,h9853.com,h9861.com,h9862.com,h9863.com,h9890.com,h9901.com,h9902.com,h9903.com,h9905.com,h9906.com,h9910.com,h9915.com,h9921.com,h9930.com,h9950.com,h9951.com,h9963.com,h9982.com,h9983.com,paipai87.com,10693382.com,21100266.com,36902567.com,smi33006.com,worryfreeproducts.com,estudiotorossian.com,djmikeygallagher.com,oralhygienecentre.com,growwealthonline.com,huawei-submarine.com.cn,submarinehuawei.com.cn,pattayatraveller.net,superwindamericas.com,vickymarfashions.com,albertahovercraft.com,buildingonadream.com,ocweddingceremony.com,streetsurfingblog.com,128yc.com,hothezi.top,web-142.com,niubox.top,dkkomo.com,tp718.com,liyesd.com,rzbyfc.com,juherh.com,php500.com,wo880.com,jshayy.com,sxyjly.com,ntsnzs.com,yi-8.com,general678.com,hbeptri.com,bihada-style.com,anhuicanada.com,lunwenchong.com,myfybjy.com,szhoau.com,sz5t.com,wdfwater.com,cocashi.com,cpicnb.com,heyuantour.com,jxrddb.com,rezhagangqiu.cn,wzwebdesign.com,chinasaltyh.com,tianshuaicn.com,guandaojiaodai.com,lushangonglue.com,gardeninghk.com,czjingujian.com,hebeichuangxin.cn,wxtguanwang.com,shicai-wang.com,yeyezhiboapp.com,12306dir.com,nyxiaochi.cn,friendzs.com,groupware-news.com,360yingshidaquan.cn,jc2009-v8.cn,2345xitong.cn,hxtoutiao.cn,bawaimai.cn,cokolo.cn,bjcsam.com,ahsxzw.com,hbalcl.com,hbhjmk.com,gxsnpj.com,hbcccl.com,yndjdh.com,fjyzyy.com,gdybjd.com,gxtmym.com,gxdzlc.com,twdgxf.com,qhwymk.com,gswtym.com,sxssdq.com,zjhspd.com,hnscdt.com,tjckmm.com,lnhhbz.com,jsyjmk.com,yndqbl.com,gzsbtx.com,gdtygk.com,shszgw.com,zjqfym.com,zqysny.com,scthzj.com,twslbl.com,fjwhzx.com,hbajcx.com,zqqhtx.com,gxqdmm.com,hnjpdq.com,zqtagw.com,qhhyfp.com,hnyddh.com,gxsxjc.com,bjcybz.com,tjdqjz.com,sxjqfp.com,hbjrfp.com,gzqmsj.com,scstdq.com,hnjxcy.com,gssmcp.com,sxhfrh.com,hnwkjc.com,ahssdt.com,gxqlqd.com,bjcgjq.com,fjjkjd.com,lnffcy.com,twhzgw.com,ahqhsp.com,hbtfmm.com,twydpx.com,hnykls.com,gztqtx.com,jljwxl.com,inesaim.com,tt11228.com,huaxingguanggao.com,tt1195.com,tt6824.com,huaxiangtugong.cn,4008601808.cn,holmesstone.cn,magicceramic.cn,ksdlphs.cn,zuoyouzichan.cn,huinx.cn,sydys.cn,ncyskj.cn,yxnu.net,tt6815.com,cs120.com.cn,yujinsuo.cc,afjdwx.cc,szsxwz.com,ncdszx.com,t-shirtsz.com,lcwxssj.com,rlglcz.com,101yiyuan.com,rwxjf.com,xiyuedoor.com,klaiwei.com,weizhenjiang.net,yue-eda.com,ontime000.com,bzok.com,fabumy.com,qugoujie.com,cy1978.com,hbxpdj.com,698960.com,by189.cn,gsxdsy.com,gdxlws.com,ciotiems.com,gpcsz.com,lpwhcb.com,hakaiyue.com,rmwgou.com,lgsc315.com,oursibe.com,lyzxdbd.com,lqwq889.com,minlizhu.com,hjylktv.com,2014lu.com,366ttt.com,eee627.com,eee516.com,eee817.com,eee426.com,j8sscb.com,jznrhs.com,51bjzjx.com,gzywits.com,gsjsjgw.com,cqsscqun.com,qdlongxi.com,hljswrhy.com,udp-up-dht.org,vzjfa.com,longhutc.com,020benefit.com,028xfyy.com,0451luntai.com,zh-yan.com,yueguiyinm.com,yimeng168.com,yaojishu.com,xiaoxiaolaoshu.com,wxjindianzi.com,zzzmmm1314.cn,vikramsir.com,tc7955.com,taodadasc.com,szsdaz.com,addv0gor.com,soushula.com,shanlongbsq.com,rzkuaiyi.com,qrrqrr.com,qafrjt.cn,panpucci.com,nanbodashu.com,mrdesign.com.cn,mikeshox.com,mbabz.com,lymlud.cn,jylg66.com,jnmwbc.cn,azazq.tk,hxinlv.com,huaquaneco.com,giuia.com,fprufd.cn,faoaa.com,ewckcl.cn,cryhtt.com,ccsgyzxzx.com,bjgxpfk.com,bdyin.com,aqlts.com,aokunwl.com,cnspeaker.net,loveyunduo.com,zhuangbagong.cn,messedesign-berlin.com,webdesigner-paris.com,washingtonpolity.com,michigancatauctions.com,prebugsolutions.com,backtohealthofoakdale.com,rbalocksmithandsecure.com,virginiaduckhunting.com,itstheruddyfuture.com,sedendenizcilik.com,sevenstarsdecor.com,treesoffortvancouver.com,newcitygentledental.com,albuquerquedriveshaft.com,advancedappraisalsrvcs.com,usadissertationwriters.com,dan4hire.com,bjhjpx.com,hjssc02.com,51ksgs.com,jqbbyy.com,jscouncil.com,jz-space.com,kaidayinwu.com,kmgtsw.com,ksn-lorton.com,laomaisai.com,lawtjx.com,lpxdermyy.com,lqsjzjxcyxgs.com,ltchzc.com,mangyoubuluo.com,mysw666.com,njyitai.com,nntieyi.com,bjzhsyzx.com,boreatwork.com,pwachive.com,pxsrxjy.com,brbaida.com,c2bdoor.com,qjtattoo.com,sdjlzl.com,ch1502.com,cnskparts.com,szhuiguotong.com,tjtdbzrq.com,toudidm.com,wak-ec.com,xigua19.com,wokpolis.com,wyethlive.com,xbmlighting.com,xffire.com,dggzs0519.com,yoyosuncar.com,yrsy6688.com,ykkkyy.com,yuw-steel.com,zgkjglzzs.com,zgylouzx.com,zhangjie8.com,zhimalu.com,dmlbmh.com,nbzywj.com,dzxuexiba.com,guofang11.com,fenwowang.com,ytlymjj.com,frfzly.com,fudi-taize.com,gasxgd.com,jzhmhp.com,0755xjx.com,3358068.com,atjiaoyu.com,baishiwh.com,boshilinkj.com,csscnh.com,dehengwf.com,fenbibi.com,hiyigo.com,hntycm.com,htzdhsb.com,huanyiwz.com,hyl-sz.com,lechenjiaoyu.com,lifa456.com,lkzhengyuan.com,mixingcanyin.com,moseasy.com,mudi2007.com,pnkart.com,qzylled.com,sxjyjhq.com,whychjc.com,xajdcoo.com,xuanyuanjijin.com,yjbsjw.com,yjdagl.com,010baopi.com,bjskcsp.com,ct-esc.com,hitachiktwx.com,lrshiye888.com,lyyj66.com,shalsdrsqwx.com,speedeforce88.com,smartcomlczh.com,xajjhtls.com,luyuansuye.com,yimowenhua1982.com,944zy.la,yht8888.com,asdfesfe001.com,332zb.com,ligcanhelp.com,tandaclear.com,seetoad.com,seetoads.com,seatoads.com,harvestpci.com,partymutts.com,mattstaker.com,covershotz.com,thepacecar.com,eyehubmail.com,reeceglass.com,shtywh.com,dirtyhorry.com,wiigrouped.com,heftydecks.com,qitaxiangguanzhe.com,guoyikoujia.com,longlongsuiyue.com,wulongxiaoyue.com,longzaitianlong.com,tianxiayinghao.com,hailindadi.com,daoliu365.com,daoliu365.co,wangzhezuiwudi.com,daoliu123.com,daoliu666.com,daoliu999.com,yiyuanduobo.com,luolanxifang.com,yantaikangle.com,dingdaocn.com,chuanpie.com,xieechun.com,wc698.com,zuoyusc.com,weilongguojiyule.com,yonglehuangguang.com,bailechengyule888.com,zhengxinyule8.com,dingboyule8.com,boyitangyule8.com,yueboguojiyule.com,bolinyule818.com,yamiyule818.com,qiantuyule888.com,xinwangyule818.com,wansenyule.com,xingyuyule888.com,bolaiyule818.com,jmsljcs.com,zhaoxiaowan.com,hdpcfix.com,hai01.com,leonres.com,banbbs.com,028club.com,567news.com,juyuh.com,wx-jl.com,cqcmjl.com,899wh.com,shenyangxinyi.com,jiazhihuifund.com,yhbzzh.com,badunhuwai.com,yangshuoinn.com,cainiaocoffee.com,zh3dmodel.com,youtingmodel.com,tjhanaeng.com,newyeargoods.com,tuandaren88.com,reshuiqichangjia.com,weitaoxing.com,zcys1592.com,zjhaitiangd.com,jzshengdehuaiyao.com,slmgame.cn,yesast.cn,nxs69.cn,snf5n.cn,shefind.cn,swyg7.cn,m9bsy.cn,lik188.cn,betslm.cn,ssllmm.cn,xpj9367-hg-gov.cn,bbinslm.cn,6617593.cn,xpj9167-hg-gov.cn,rzzqmy.cn,enpfy.cn,xpj9166-hg-gov.cn,mymun.cn,xpj9165-hg-gov.cn,kubet.cn,yhslm.cn,xpj9163-hg-gov.cn,stubborn.cn,sznzw.cn,xpj9162-hg-gov.cn,qc43.cn,xpj9161-hg-gov.cn,balloonia.cn,xpj9160-hg-gov.cn,hysntf.cn,xpj9159-hg-gov.cn,vzpldu.cn,lytyswkj.cn,xpj9158-hg-gov.cn,akpwhs.cn,xpj9157-hg-gov.cn,jlbok.cn,xpj9156-hg-gov.cn,mmszwd.cn,cn200.cn,xpj9155-hg-gov.cn,aubufr.cn,bainq.cn,xpj9154-hg-gov.cn,gjkuge.cn,xpj9153-hg-gov.cn,nunnx.cn,tzxdcg.cn,xpj9152-hg-gov.cn,baibf.cn,xpj9151-hg-gov.cn,affkdz.cn,jun80.cn,xpj9150-hg-gov.cn,wiujfh.cn,xpj9149-hg-gov.cn,cn-pos.cn,khwwum.cn,xpj9148-hg-gov.cn,lieko.cn,icshw.com,xpj9147-hg-gov.cn,endwwr.cn,wowyao.com,xu1688.cn,shouyesheji.com,xpj9146-hg-gov.cn,woqyug.cn,benbohouse.com,xpj9145-hg-gov.cn,shsqgjg.com,120keji.cn,xipinle.com,anuvtq.cn,xpj9144-hg-gov.cn,acheligue.com,dhdgyx.cn,xpj9143-hg-gov.cn,acftheseven.com,wpqasp.cn,acepaydayloans.net,xpj9142-hg-gov.cn,mnzgz.cn,accessfolder.com,fwqgei.cn,xpj9141-hg-gov.cn,accessbinghamton.com,5itw.cn,abusinessmanagement.com,xpj9140-hg-gov.cn,szykj.cn,abriefhiatus.com,myshao.cn,xpj9139-hg-gov.cn,abreueoliveira.com,hngsy.cn,abletechelectronics.com,xpj9138-hg-gov.cn,1yqn.cn,abjartourism.com,16db.cn,xpj9137-hg-gov.cn,aaajphandbags.com,hongyuan8.cn,acproduccionespe.com,xpj9136-hg-gov.cn,picaronapo.com,szjfw.cn,xpj9135-hg-gov.cn,phurahong.com,jskkk.cn,szjpw.cn,parksumin.com,xpj9134-hg-gov.cn,ownmall.cn,parkerwinder.com,xpj9133-hg-gov.cn,visel.cn,pampz.com,788331.cn,pahrbod.com,xpj9132-hg-gov.cn,5iwatch.cn,pagepresto.com,xpj9131-hg-gov.cn,mycashfirst.com,my50thyr.com,xpj9130-hg-gov.cn,muvudance.com,xpj9129-hg-gov.cn,multiagric.com,modernoluk.com,xpj9128-hg-gov.cn,laurenshiro.com,xpj9127-hg-gov.cn,lasgidiceo.com,lapeacecorps.com,xpj9126-hg-gov.cn,kpccenter.com,kontisasansor.com,xpj9125-hg-gov.cn,kone520.com,xpj9124-hg-gov.cn,khanwaqas.com,kazakhunion.com,xpj9123-hg-gov.cn,kauju.com,xpj9122-hg-gov.cn,evlerecicek.com,evilcp.com,xpj9121-hg-gov.cn,dvxvideo.com,xpj9120-hg-gov.cn,artmopro.com,smsabzar.com,xpj9119-hg-gov.cn,shandonglulu.com,xpj9118-hg-gov.cn,tjfuruige.com,artorob.com,xpj9117-hg-gov.cn,yhzaetsy.com,xpj9116-hg-gov.cn,gz-genss.com,zhongzilian369.com,xpj9115-hg-gov.cn,xpj9114-hg-gov.cn,xpj9113-hg-gov.cn,xpj9112-hg-gov.cn,xpj9111-hg-gov.cn,xpj9110-hg-gov.cn,xpj9109-hg-gov.cn,xpj9108-hg-gov.cn,xpj9107-hg-gov.cn,xpj9106-hg-gov.cn,xpj9105-hg-gov.cn,xpj9104-hg-gov.cn,xpj9103-hg-gov.cn,xpj9102-hg-gov.cn,xpj9101-hg-gov.cn,xpj9100-hg-gov.cn,xpj9099-hg-gov.cn,xpj9098-hg-gov.cn,xpj9097-hg-gov.cn,xpj9096-hg-gov.cn,xpj9095-hg-gov.cn,xpj9094-hg-gov.cn,xpj9093-hg-gov.cn,xpj9092-hg-gov.cn,xpj9091-hg-gov.cn,xpj9090-hg-gov.cn,xpj9089-hg-gov.cn,xpj9088-hg-gov.cn,xpj9087-hg-gov.cn,xpj9086-hg-gov.cn,xpj9085-hg-gov.cn,xpj9084-hg-gov.cn,xpj9083-hg-gov.cn,xpj9082-hg-gov.cn,xpj9081-hg-gov.cn,xpj9080-hg-gov.cn,xpj9079-hg-gov.cn,xpj9078-hg-gov.cn,xpj9077-hg-gov.cn,xpj9076-hg-gov.cn,xpj9075-hg-gov.cn,xpj9074-hg-gov.cn,xpj9073-hg-gov.cn,xpj9072-hg-gov.cn,xpj9071-hg-gov.cn,xpj9070-hg-gov.cn,xpj9069-hg-gov.cn,xpj9068-hg-gov.cn,tuz3.cn,8627727.com,opafs.com,dadamiao.com,aokesikongtiaowx.com,rdabrw.com,dzdxfhj.com,sdyameisi.com,iqqemail.com,phithetapsi.com,8642000.com,9817727.com,resoklahoma.com,beautysalongrace.net,greenarkproperty.com,anzhuo88.com,ssongmarket.com,dxthings.com,charaq.com,cmmseo.com,cambridgeux.com,bainayinshua.com,015419.com,caradansie.com,mousukoshi.com,kristenbush.com,ringsring.com,sunlvto.com,047767.com,charmodel.com,mersinbasket.com,rajnisonkar.com,camsexstrip.com,apiwww.com,battledrawn.com,eloyaltyclub.com,josephbooks.com,hotshiz.com,itelectual.com,mdschmuck.com,0736go.net,gourmetcrisp.com,shaylashope.com,redfactores.com,btd-led.com,hellbuster.com,itelectu.com,088166.com,smauflcable.com,kidocomo.com,zuobiaopai.net,itlectua.com,spamculprit.com,pegascinema.com,julyvintage.com,huijiad.com,grannyspets.com,bdcqmy.com,teambogo.com,mibellaluna.com,shiftupdate.com,shijiebest.com,pitchcake.com,srisaiganesh.com,zhongjiangyouzhiboshi.com,canhelpuget.com,monising.com,nadiashow.com,hazeyshark.com,hotelminos.com,guoshengjin.com,eberndorf.com,feishiwujin.com,muinesailing.com,davidseguin.com,homeudecor.com,funchews.com,keyfull.net,maxfiltra.com,fuxyz.com,babydollbar.com,aliensporn.com,buyplusbonus.com,ataomimi.com,amarellius.com,shuzishop2015.com,puertafacil.com,myriamboyer.com,hafc666.com,cvfm1.com,changtai-valve.com,michalmarcol.com,024gogo.com,mlhealthfit.com,fcisthekey.com,hnjly.net,cparkerphoto.com,yedszambia.com,juyuetz.com,sonsofboru.com,psiquisnet.com,cqzbrl.com,borsarmulu.com,chariotgauge.com,bonnidance.com,shbaole.com,cadeauenfant.com,694786.com,tnteaparty.com,3387727.com,standishwine.com,robostowing.com,imjingang.com,gongjugui0311.com,autoimportsa.com,3557727.com,lcbcyouth.com,lyt8.net,davincikeys.com,010machine.com,tejememucho.com,3997727.com,spartankenpo.com,chinanicestock.com,tutuimoveis.com,3357727.com,beppu-ofuku.com,3367727.com,tortugaart.com,merkelcita.com,jamesaknight.com,meihaosp.com,webcamoyunu.com,maxpower-tuning.net,agsarastirma.com,zuanshicaipiao.com,zamannegar.com,imangascans.com,3657727.com,timezonepain.com,ledsimports.com,zunhaigolf.com,guaragi.com,zengshicai.com,kambujainn.com,huashimusic.com,carolvandyck.com,3877727.com,tariflerim1.com,cuijingyi.com,nhactreno1.com,z9wan.com,akanefamily.com,corevaluespt.com,victoruchoa.com,yonpromosyon.com,designbymar.com,mgigraphics.com,enzymesbytai.com,thegrovefilm.com,meatsplosion.com,heljdajastuk.com,3727727.com,danmoivn.com,interlamina.com,nlsgwau.com,autokouluun.com,hanganjiaoyu.com,oasisverseau.com,znysg.com,drtomvolm.com,sytxys.com,dashofmagic.com,hbclweb.com,eastarmarine.com,lcdtvdealers.com,3217727.com,andrerecinto.com,7227727.com,csrgatineau.com,rfacrilicos.com,bzxlsl.com,karencegalis.com,zbxldjd.com,reminiscedvd.com,zhengtai68.com,charlesgaby.com,shichangtoutiao.com,fbcparainen.com,chaysean.com,doismiledoze.com,shuimitao1.com,chuangstudio.com,720jing.com,cosmicremix.com,rotbalansci.com,tzbj168.com,dakikhost.com,cityhavefun.com,devianatrt.com,zhidao168.net,completemac.com,zhidao1688.net,rosegroupusa.com,40qp.com,dailyyomiuri.com,tongwangwangxiao.com,cogemahague.com,lioydstsb.com,gokepsr.com,ekbkesf.com,magnatuneasy.com,zgimc.net,0371liaotu.com,jwfirearms.com,zhuliao.net,florcomflor.com,timearlsteam.com,dailybethea.com,calebmarker.com,brandonmagar.com,inoxlubewest.com,clzzyjy.com,curtshirley.com,zbkehui.com,skipraid.com,clscougars.com,zhuanqianbao.com,fmfabulosa.com,088824.com,dannyandjen.com,markpedrelli.com,cityblur.com,buyu8898.com,margieboyd.com,shujuma.com,aitiwenz.com,come-better.com,ishoptahoe.com,jxcme.com,vwiplaw.com,pigcoder.com,supercheaprv.com,albionfloral.com,taobaoyougou5.com,winmindztech.com,cubaforyumas.com,bitsofwonder.com,dripfic.com,altmethods.com,saranaauto.com,yealakeville.com,mkute.com,ngscript.com,dirtykeywest.com,advexweb.com,cobhatgroup.com,trydovaya.com,derdakarakis.com,karumetours.com,mpdhost.com,embersimmers.com,rapidfiremtb.com,collinkilty.com,calsistowx.com,wnbcthunder.com,3gweiyu.com,bbmill.com,lubolang.net,582app.com,yspj5.com,ccyzgg.com,wsera.com,avamztx.com,xpj9067-hg-gov.cn,xpj9066-hg-gov.cn,xpj9065-hg-gov.cn,xpj9063-hg-gov.cn,xpj9062-hg-gov.cn,xpj9061-hg-gov.cn,xpj9060-hg-gov.cn,xpj9059-hg-gov.cn,xpj9058-hg-gov.cn,xpj9057-hg-gov.cn,xpj9056-hg-gov.cn,xpj9055-hg-gov.cn,xpj9054-hg-gov.cn,xpj9053-hg-gov.cn,xpj9052-hg-gov.cn,xpj9051-hg-gov.cn,xpj9050-hg-gov.cn,xpj9049-hg-gov.cn,xpj9048-hg-gov.cn,xpj9047-hg-gov.cn,xpj9046-hg-gov.cn,xpj9045-hg-gov.cn,xpj9044-hg-gov.cn,xpj9043-hg-gov.cn,xpj9042-hg-gov.cn,xpj9041-hg-gov.cn,xpj9040-hg-gov.cn,xpj9039-hg-gov.cn,xpj9038-hg-gov.cn,xpj9037-hg-gov.cn,xpj9036-hg-gov.cn,xpj9035-hg-gov.cn,xpj9034-hg-gov.cn,xpj9033-hg-gov.cn,xpj9032-hg-gov.cn,xpj9031-hg-gov.cn,xpj9030-hg-gov.cn,xpj9029-hg-gov.cn,xpj9028-hg-gov.cn,xpj9027-hg-gov.cn,xpj9026-hg-gov.cn,xpj9025-hg-gov.cn,xpj9024-hg-gov.cn,xpj9023-hg-gov.cn,xpj9022-hg-gov.cn,xpj9021-hg-gov.cn,xpj9020-hg-gov.cn,xpj9019-hg-gov.cn,xpj9018-hg-gov.cn,xpj9017-hg-gov.cn,xpj9016-hg-gov.cn,xpj9015-hg-gov.cn,xpj9014-hg-gov.cn,xpj9013-hg-gov.cn,xpj9012-hg-gov.cn,xpj9011-hg-gov.cn,xpj9010-hg-gov.cn,xpj9009-hg-gov.cn,xpj9008-hg-gov.cn,xpj9007-hg-gov.cn,xpj9006-hg-gov.cn,2-com2.com,xpj9005-hg-gov.cn,xpj9004-hg-gov.cn,xpj9003-hg-gov.cn,xpj9002-hg-gov.cn,waimai001.com,xpj9001-hg-gov.cn,8187727.com,xpj9000-hg-gov.cn,lvjielaw.com,banglianzaixian.com,langangkeji.com,xpj8999-hg-gov.cn,gdxianghao.net,gz-fitting.com,133fc.com,xpj8998-hg-gov.cn,5777727.com,lexuejy.com,51ytr.com,111-elevator.com,ynjlkj.com,xpj8997-hg-gov.cn,laoboshijc.com,laosuannong.com,newlinan.net,xpj8996-hg-gov.cn,xpj8995-hg-gov.cn,xpj8994-hg-gov.cn,xpj8993-hg-gov.cn,xpj8992-hg-gov.cn,xpj8991-hg-gov.cn,a608608.me,xpj8990-hg-gov.cn,xpj8989-hg-gov.cn,xpj8988-hg-gov.cn,xpj8987-hg-gov.cn,xpj8986-hg-gov.cn,xpj8985-hg-gov.cn,xpj8984-hg-gov.cn,xpj8983-hg-gov.cn,xpj8982-hg-gov.cn,b608608.me,c608608.me,d608608.me,e608608.me,f608608.me,g608608.me,xpj8981-hg-gov.cn,h608608.me,i608608.me,j608608.me,k608608.me,l608608.me,m608608.me,xpj8980-hg-gov.cn,n608608.me,o608608.me,p608608.me,q608608.me,r608608.me,xpj8979-hg-gov.cn,s608608.me,t608608.me,u608608.me,v608608.me,w608608.me,xpj8978-hg-gov.cn,x608608.me,y608608.me,z608608.me,xpj8977-hg-gov.cn,xpj8976-hg-gov.cn,taomck.com,5227727.com,xpj8975-hg-gov.cn,hdx52ae.com,hnmyjia.com,xpj8974-hg-gov.cn,nnnk120.net,tc753.com,324500.com,xpj8973-hg-gov.cn,kejiansw.com,zoeethailand.com,xpj8972-hg-gov.cn,edu-edu.net,xpj8971-hg-gov.cn,xpj8970-hg-gov.cn,xpj8969-hg-gov.cn,5goumai.com,770app.com,86sinomedical.net,xpj8968-hg-gov.cn,code4android.com,china-disheng.com,xpj8967-hg-gov.cn,soaiyou.com,yiyuanyaoye.net,yanyb.net,youkat.net,goto-china.com,ydrfsm.com,ylxlhy.com,yufajq.com,ceoluntan.com,liangxiaojing.com,szdicheng.com,ipingpai.com,haosteels.com,0246802468.net,8127727.com,dianbowang.net,xpj8966-hg-gov.cn,jubusiness.com,jiajialegw.com,1359o.com,xpj8965-hg-gov.cn,tc752.com,tc759.com,6h008.com,tc763.com,tc782.com,xpj8963-hg-gov.cn,tc783.com,155wg.com,jiangnantaikang.com,192app.com,347767.com,xpj8962-hg-gov.cn,xiaoyistar.com,suzouseo.com,hbshengjing.com,xpj8961-hg-gov.cn,yh999z.com,kwikad.com,xpj8960-hg-gov.cn,taokshow.com,kuandian110.com,247500.com,xpj8959-hg-gov.cn,tjshuaxia.com,tcc188.com,hxcp1188.com,360dust.com,xpj8958-hg-gov.cn,8817727.com,xpj8957-hg-gov.cn,xpj8956-hg-gov.cn,xpj8955-hg-gov.cn,xpj8954-hg-gov.cn,xpj8953-hg-gov.cn,xpj8952-hg-gov.cn,xpj8951-hg-gov.cn,xpj8950-hg-gov.cn,xpj8949-hg-gov.cn,xpj8948-hg-gov.cn,xpj8947-hg-gov.cn,xpj8946-hg-gov.cn,xpj8945-hg-gov.cn,xpj8944-hg-gov.cn,xpj8943-hg-gov.cn,xpj8942-hg-gov.cn,xpj8941-hg-gov.cn,cqteyou.com,8d98.com,xpj8940-hg-gov.cn,bjhysqd.com,keltss.com,xpj8939-hg-gov.cn,xpj8938-hg-gov.cn,wininshipping.com,8837727.com,58jrc.com,xpj8937-hg-gov.cn,xintongjt.com,51kingtrip.com,52xunwei.com,913app.com,xpj8936-hg-gov.cn,chaogoule.com,123imall.com,xpj8935-hg-gov.cn,1vr1.com,c9n9.com,454bdf.com,jmzxjx.com,1988seo.com,xpj8934-hg-gov.cn,hypyqq.com,xinghuiauto.com,xpj8933-hg-gov.cn,suxun368.net,51kuaimi.com,xpj8932-hg-gov.cn,xpj8931-hg-gov.cn,xpj8930-hg-gov.cn,xpj8929-hg-gov.cn,xpj8928-hg-gov.cn,xpj8927-hg-gov.cn,xpj8926-hg-gov.cn,xpj8925-hg-gov.cn,xpj8924-hg-gov.cn,xpj8923-hg-gov.cn,xpj8922-hg-gov.cn,xpj8921-hg-gov.cn,xpj8920-hg-gov.cn,xpj8919-hg-gov.cn,xpj8918-hg-gov.cn,xpj8917-hg-gov.cn,xpj8916-hg-gov.cn,xpj8915-hg-gov.cn,xpj8914-hg-gov.cn,xpj8913-hg-gov.cn,xpj8912-hg-gov.cn,xpj8911-hg-gov.cn,xpj8910-hg-gov.cn,xpj8909-hg-gov.cn,xpj8908-hg-gov.cn,xpj8907-hg-gov.cn,xpj8906-hg-gov.cn,xpj8905-hg-gov.cn,xpj8904-hg-gov.cn,xpj8903-hg-gov.cn,xpj8902-hg-gov.cn,xpj8901-hg-gov.cn,xpj8900-hg-gov.cn,xpj8899-hg-gov.cn,xpj8898-hg-gov.cn,xpj8897-hg-gov.cn,xpj8896-hg-gov.cn,xpj8895-hg-gov.cn,xpj8894-hg-gov.cn,xpj8893-hg-gov.cn,xpj8892-hg-gov.cn,xpj8891-hg-gov.cn,xpj8890-hg-gov.cn,xpj8889-hg-gov.cn,xpj8888-hg-gov.cn,xpj8887-hg-gov.cn,xpj8886-hg-gov.cn,xpj8885-hg-gov.cn,xpj8884-hg-gov.cn,xpj8883-hg-gov.cn,xpj8882-hg-gov.cn,xpj8881-hg-gov.cn,xpj8880-hg-gov.cn,xpj8879-hg-gov.cn,xpj8878-hg-gov.cn,xpj8877-hg-gov.cn,xpj8876-hg-gov.cn,xpj8875-hg-gov.cn,xpj8874-hg-gov.cn,xpj8873-hg-gov.cn,xpj8872-hg-gov.cn,xpj8871-hg-gov.cn,xpj8870-hg-gov.cn,xpj8869-hg-gov.cn,xpj8868-hg-gov.cn,xpj8867-hg-gov.cn,xpj8866-hg-gov.cn,quanmin.me,bet-473-gov.cn,bet-472-gov.cn,bet-471-gov.cn,bet-470-gov.cn,bet-469-gov.cn,ag3141-js-gov.cn,ag3131-js-gov.cn,ag3121-js-gov.cn,ag3101-js-gov.cn,ag301-js-gov.cn,ag300-js-gov.cn,ag299-js-gov.cn,ag298-js-gov.cn,ag297-js-gov.cn,ag296-js-gov.cn,ag295-js-gov.cn,ag294-js-gov.cn,ag293-js-gov.cn,ag292-js-gov.cn,ag291-js-gov.cn,ag289-js-gov.cn,ag288-js-gov.cn,ag287-js-gov.cn,ag286-js-gov.cn,ag285-js-gov.cn,ag284-js-gov.cn,ag283-js-gov.cn,ag282-js-gov.cn,ag277-js-gov.cn,ag276-js-gov.cn,24555666.com,ag275-js-gov.cn,ag274-js-gov.cn,ag273-js-gov.cn,ag272-js-gov.cn,ag271-js-gov.cn,ag270-js-gov.cn,ag269-js-gov.cn,ag268-js-gov.cn,ag267-js-gov.cn,ag266-js-gov.cn,ag265-js-gov.cn,ag263-js-gov.cn,ag262-js-gov.cn,ag261-js-gov.cn,ag260-js-gov.cn,ag259-js-gov.cn,yy6789.com,ag258-js-gov.cn,ag257-js-gov.cn,ag256-js-gov.cn,ag255-js-gov.cn,ag254-js-gov.cn,ag253-js-gov.cn,ag252-js-gov.cn,ag251-js-gov.cn,ag250-js-gov.cn,ag249-js-gov.cn,ag248-js-gov.cn,ag247-js-gov.cn,ag246-js-gov.cn,kxm778.com,ag245-js-gov.cn,ag244-js-gov.cn,ag243-js-gov.cn,ag242-js-gov.cn,ag241-js-gov.cn,ag240-js-gov.cn,ag239-js-gov.cn,ag238-js-gov.cn,ag237-js-gov.cn,ag236-js-gov.cn,ag235-js-gov.cn,ag234-js-gov.cn,ag233-js-gov.cn,ag232-js-gov.cn,ag231-js-gov.cn,ag230-js-gov.cn,ag229-js-gov.cn,ag228-js-gov.cn,ag227-js-gov.cn,ag226-js-gov.cn,ag225-js-gov.cn,ag224-js-gov.cn,ag223-js-gov.cn,ag222-js-gov.cn,ag221-js-gov.cn,ag220-js-gov.cn,ag219-js-gov.cn,ag218-js-gov.cn,ag217-js-gov.cn,ag216-js-gov.cn,ag215-js-gov.cn,ag214-js-gov.cn,ag213-js-gov.cn,ag212-js-gov.cn,ag210-js-gov.cn,ag209-js-gov.cn,ag208-js-gov.cn,ag207-js-gov.cn,ag206-js-gov.cn,ag205-js-gov.cn,ag204-js-gov.cn,kucai168.com,ag203-js-gov.cn,ag202-js-gov.cn,ag201-js-gov.cn,ag200-js-gov.cn,ag199-js-gov.cn,ag198-js-gov.cn,ag197-js-gov.cn,ag196-js-gov.cn,ag195-js-gov.cn,ag194-js-gov.cn,ag193-js-gov.cn,ag192-js-gov.cn,ag191-js-gov.cn,ag190-js-gov.cn,ag188-js-gov.cn,ag187-js-gov.cn,ag186-js-gov.cn,ag185-js-gov.cn,ag184-js-gov.cn,ag183-js-gov.cn,inclub.org,wenhr.com,office-career.com,ag182-js-gov.cn,fuhetech.com,psmmedia.com,bigongsi.com,xdhire.com,jmcun.net,ag181-js-gov.cn,ag180-js-gov.cn,ag179-js-gov.cn,ag178-js-gov.cn,ag176-js-gov.cn,ag174-js-gov.cn,ag173-js-gov.cn,ag171-js-gov.cn,ag170-js-gov.cn,ag169-js-gov.cn,ag168-js-gov.cn,ag167-js-gov.cn,ag166-js-gov.cn,ag165-js-gov.cn,ag163-js-gov.cn,ag162-js-gov.cn,ag161-js-gov.cn,ag160-js-gov.cn,ag159-js-gov.cn,ag158-js-gov.cn,ag157-js-gov.cn,ag156-js-gov.cn,ag155-js-gov.cn,ag153-js-gov.cn,ag152-js-gov.cn,ag151-js-gov.cn,ag150-js-gov.cn,ag149-js-gov.cn,ag148-js-gov.cn,ag144-js-gov.cn,ag142-js-gov.cn,ag139-js-gov.cn,ag138-js-gov.cn,ag137-js-gov.cn,ag136-js-gov.cn,ag133-js-gov.cn,ag132-js-gov.cn,ag131-js-gov.cn,ag130-js-gov.cn,ag129-js-gov.cn,ag128-js-gov.cn,ag127-js-gov.cn,ag126-js-gov.cn,ag125-js-gov.cn,ag124-js-gov.cn,ag123-js-gov.cn,ag122-js-gov.cn,ag121-js-gov.cn,ag120-js-gov.cn,rmtklb.com,ag119-js-gov.cn,ag118-js-gov.cn,ag117-js-gov.cn,szctgd.com,ag116-js-gov.cn,ag115-js-gov.cn,stxtdjc.com,ag113-js-gov.cn,ag112-js-gov.cn,ag111-js-gov.cn,ag110-js-gov.cn,ag109-js-gov.cn,ag108-js-gov.cn,ag107-js-gov.cn,ag106-js-gov.cn,ag105-js-gov.cn,ag104-js-gov.cn,ag103-js-gov.cn,ag102-js-gov.cn,ag101-js-gov.cn,ag100-js-gov.cn,ag99-js-gov.cn,ag98-js-gov.cn,ag97-js-gov.cn,zhongjunkonggu.com,ag96-js-gov.cn,ag95-js-gov.cn,emailitlater.com,ag94-js-gov.cn,ag93-js-gov.cn,ag92-js-gov.cn,xdc1688.com,ag91-js-gov.cn,ag90-js-gov.cn,examjy.com,ag89-js-gov.cn,17jzba.com,jsatlpaint.com,bestmeizi.com,milfpussyhunter.com,ag88-js-gov.cn,ag87-js-gov.cn,zgsmhyw.com,ag86-js-gov.cn,ag85-js-gov.cn,52ahkm.com,ag84-js-gov.cn,ag83-js-gov.cn,dian1000.com,ag82-js-gov.cn,ag81-js-gov.cn,ag80-js-gov.cn,ag79-js-gov.cn,ag78-js-gov.cn,ag77-js-gov.cn,ag76-js-gov.cn,xnktyy.com,ag75-js-gov.cn,ag74-js-gov.cn,beegxvideos.com,syjrfbj.com,shglbf.com,flsdk.com,jbyyyc.com,intelligenthousedesign.com,mmaming.com,shaklee0553.com,lhwuye.com,ag73-js-gov.cn,ag72-js-gov.cn,ag71-js-gov.cn,ag70-js-gov.cn,ag69-js-gov.cn,ag68-js-gov.cn,ag67-js-gov.cn,ag66-js-gov.cn,ag65-js-gov.cn,ag63-js-gov.cn,ag62-js-gov.cn,ag61-js-gov.cn,ag60-js-gov.cn,ag59-js-gov.cn,ag58-js-gov.cn,ag57-js-gov.cn,ag56-js-gov.cn,ag55-js-gov.cn,ag54-js-gov.cn,ag53-js-gov.cn,ag52-js-gov.cn,ag51-js-gov.cn,ag50-js-gov.cn,ag49-js-gov.cn,ag48-js-gov.cn,ag47-js-gov.cn,ag46-js-gov.cn,ag45-js-gov.cn,ag44-js-gov.cn,ag43-js-gov.cn,ag42-js-gov.cn,ag41-js-gov.cn,ag40-js-gov.cn,ag39-js-gov.cn,qmenglish.cn,ag38-js-gov.cn,ag37-js-gov.cn,ag36-js-gov.cn,ag35-js-gov.cn,ag34-js-gov.cn,ag33-js-gov.cn,ag32-js-gov.cn,ag31-js-gov.cn,ag30-js-gov.cn,ag29-js-gov.cn,ag28-js-gov.cn,ag27-js-gov.cn,ag26-js-gov.cn,ag25-js-gov.cn,ag24-js-gov.cn,ag23-js-gov.cn,ag22-js-gov.cn,ag21-js-gov.cn,ag20-js-gov.cn,ag19-js-gov.cn,ag18-js-gov.cn,ag17-js-gov.cn,ag16-js-gov.cn,ag15-js-gov.cn,ag14-js-gov.cn,ag13-js-gov.cn,ag12-js-gov.cn,ag11-js-gov.cn,ag10-js-gov.cn,ag09-js-gov.cn,ag08-js-gov.cn,ag07-js-gov.cn,ag06-js-gov.cn,ag05-js-gov.cn,ag04-js-gov.cn,ag03-js-gov.cn,ag02-js-gov.cn,ag01-js-gov.cn,ag0024-js-gov.cn,ag0023-js-gov.cn,ag0022-js-gov.cn,ag0021-js-gov.cn,ag0020-js-gov.cn,ag0019-js-gov.cn,ag0018-js-gov.cn,ag0017-js-gov.cn,ag0016-js-gov.cn,ag0014-js-gov.cn,ag0013-js-gov.cn,ag0012-js-gov.cn,ag0011-js-gov.cn,968878.net,ag607-js-gov.cn,ag606-js-gov.cn,ag605-js-gov.cn,ag604-js-gov.cn,ag603-js-gov.cn,ag602-js-gov.cn,ag601-js-gov.cn,ag600-js-gov.cn,ag599-js-gov.cn,ag598-js-gov.cn,ag597-js-gov.cn,ag596-js-gov.cn,ag595-js-gov.cn,ag594-js-gov.cn,ag593-js-gov.cn,ag592-js-gov.cn,ag591-js-gov.cn,ag590-js-gov.cn,ag589-js-gov.cn,ag588-js-gov.cn,ag587-js-gov.cn,ag586-js-gov.cn,ag585-js-gov.cn,ag584-js-gov.cn,ag583-js-gov.cn,ag582-js-gov.cn,ag581-js-gov.cn,ag580-js-gov.cn,ag579-js-gov.cn,ag578-js-gov.cn,ag577-js-gov.cn,ag576-js-gov.cn,ag575-js-gov.cn,ag574-js-gov.cn,ag573-js-gov.cn,ag572-js-gov.cn,ag571-js-gov.cn,ag570-js-gov.cn,ag569-js-gov.cn,ag568-js-gov.cn,ag567-js-gov.cn,ag566-js-gov.cn,ag565-js-gov.cn,ag563-js-gov.cn,ag562-js-gov.cn,ag561-js-gov.cn,ag560-js-gov.cn,ag559-js-gov.cn,ag558-js-gov.cn,ag557-js-gov.cn,ag556-js-gov.cn,ag555-js-gov.cn,ag554-js-gov.cn,ag553-js-gov.cn,ag552-js-gov.cn,ag551-js-gov.cn,ag550-js-gov.cn,ag549-js-gov.cn,ag548-js-gov.cn,ag547-js-gov.cn,ag546-js-gov.cn,ag545-js-gov.cn,ag544-js-gov.cn,ag543-js-gov.cn,ag542-js-gov.cn,ag541-js-gov.cn,ag540-js-gov.cn,ag539-js-gov.cn,ag538-js-gov.cn,ag537-js-gov.cn,ag536-js-gov.cn,ag535-js-gov.cn,ag534-js-gov.cn,ag533-js-gov.cn,ag532-js-gov.cn,ag531-js-gov.cn,ag530-js-gov.cn,ag529-js-gov.cn,ag528-js-gov.cn,ag527-js-gov.cn,ag526-js-gov.cn,ag525-js-gov.cn,ag524-js-gov.cn,ag523-js-gov.cn,ag522-js-gov.cn,ag521-js-gov.cn,ag520-js-gov.cn,ag519-js-gov.cn,ag518-js-gov.cn,ag517-js-gov.cn,ag516-js-gov.cn,ag515-js-gov.cn,ag514-js-gov.cn,ag513-js-gov.cn,ag512-js-gov.cn,ag511-js-gov.cn,ag510-js-gov.cn,ag509-js-gov.cn,ag508-js-gov.cn,ag507-js-gov.cn,zyyschina.com,ag506-js-gov.cn,ag505-js-gov.cn,ag504-js-gov.cn,ag503-js-gov.cn,ag502-js-gov.cn,ag501-js-gov.cn,ag500-js-gov.cn,80uo.com,ag499-js-gov.cn,ag498-js-gov.cn,ag497-js-gov.cn,ag496-js-gov.cn,ag495-js-gov.cn,ag494-js-gov.cn,ag493-js-gov.cn,ag492-js-gov.cn,ag491-js-gov.cn,ag490-js-gov.cn,ag489-js-gov.cn,ag488-js-gov.cn,ag487-js-gov.cn,ag486-js-gov.cn,ag485-js-gov.cn,ag484-js-gov.cn,ag483-js-gov.cn,ag482-js-gov.cn,ag481-js-gov.cn,ag480-js-gov.cn,ag479-js-gov.cn,ag478-js-gov.cn,ag477-js-gov.cn,ag476-js-gov.cn,ag475-js-gov.cn,ag474-js-gov.cn,ag473-js-gov.cn,ag472-js-gov.cn,ag471-js-gov.cn,ag470-js-gov.cn,ag469-js-gov.cn,ag468-js-gov.cn,ag467-js-gov.cn,ag466-js-gov.cn,ag465-js-gov.cn,ag463-js-gov.cn,ag462-js-gov.cn,ag461-js-gov.cn,ag460-js-gov.cn,ag459-js-gov.cn,ag458-js-gov.cn,ag457-js-gov.cn,ag456-js-gov.cn,ag455-js-gov.cn,ag454-js-gov.cn,ag453-js-gov.cn,ag452-js-gov.cn,ag451-js-gov.cn,ag450-js-gov.cn,ag449-js-gov.cn,ag448-js-gov.cn,ag447-js-gov.cn,ag446-js-gov.cn,ag445-js-gov.cn,ag444-js-gov.cn,ag443-js-gov.cn,ag442-js-gov.cn,ag441-js-gov.cn,ag440-js-gov.cn,ag439-js-gov.cn,ag438-js-gov.cn,ag437-js-gov.cn,ag436-js-gov.cn,ag435-js-gov.cn,ag434-js-gov.cn,ag433-js-gov.cn,ag432-js-gov.cn,ag431-js-gov.cn,ag430-js-gov.cn,ag429-js-gov.cn,ag428-js-gov.cn,ag427-js-gov.cn,ag426-js-gov.cn,ag425-js-gov.cn,ag424-js-gov.cn,ag423-js-gov.cn,ag422-js-gov.cn,ag421-js-gov.cn,ag420-js-gov.cn,ag419-js-gov.cn,ag418-js-gov.cn,ag417-js-gov.cn,ag416-js-gov.cn,ag415-js-gov.cn,612109.com,ag414-js-gov.cn,ag413-js-gov.cn,ag412-js-gov.cn,ag411-js-gov.cn,ag410-js-gov.cn,ag409-js-gov.cn,ag408-js-gov.cn,ag407-js-gov.cn,ag406-js-gov.cn,welt8.com,zzccmy.top,58sisuiji.com,365a2.cc,365a6.cc,baiyexshkj.com,hbmxbz.com,jinnuo1.com,lizhuguanggao.com,lxwlxp.com,mlgjds.com,ok38888.com,qy0554.com,shquanyijj.com,szfeitiancheng.com,xun9u.com,tianzi10000.com,trd-alison.com,yida2006.com,dgyscz.com,1392022.com,olx123.com,henanmeicheng.com,wed0838.com,pjgeyin.com,hwhy123.com,lixiangcp.com,51kof.com,wwww168.com,99shuke.com,kbaih5.com,weikesz.com,osysm.com,lkshouji.com,dushijc.com,njjlshjl.com,arkumi.com,jinshangdp.com,xyrtbj.com,ykmengxi.com,chenyanzhi.com,ntcyy.com,rz148.com,ekoko-createur.com,tjzhengyangchun.com,0319jcw.com,bss2008.com,tengfeitech.com,cfzmt.com,520265.com,uonanz.com,91iworld.com,12pi.com,hdlwh.com,sugeluns.com,rainbow-coating.com,jtjd-tech.com,zy0518.com,youkai8.com,fhrtys.com,xingchihuo.com,glanzheng.com,gzlzzjy.com,gzsm8888.com,topspeed-logistics.com,al0n4k.com,guanghuajiazheng.com,lcz-ata.com,xianjita.com,bapeople.com,jieshiban.com,808tt.com,kuante168.com,51jyjy.com,zjknt.com,fyec-plastics.com,stealth-prison.com,gznh168.com,tjqhcssy.com,fantaoke.com,wowpresa.com,dgepu.com,xhrklt.com,manesong.com,znzxc8.com,banka8.com,tisevip.com,nasunl.cn,zcmfzb.com,hojavq.cn,goldenv.cn,qd108.com,sdzuifeng.com,dhkjgw.com,wsczyzs.com,xianjck.com,sdqxcy.com,yujiasc.com,sxderong.com,dcloudy.com,eqibuy.com,comkudi.com,sofan123.com,518wh.com,tzstzs.com,sclhwjy.com,ayhggs.com,hshgzy.com,huimu698.com,jsgbjg.com,xhh688.com,kdyes.com,xmhsdz.com,bjlycits.com,bzcxglc.com,whjdkc.com,ytkjdzsw.com,dlpylp.com,sxshyj.com,zcwljy.com,mslnyhy.com,tcytea.com,mxqhys.com,yaobianmei.com,doohalert.com,ssgebbs.com,wfqingxiji.com,gzhsjf.com,fycarpet.com,jndzrsz.com,sangedianzi.com,meiqishow.com,zibowuchen.com,jxoppo.com,ahlygy.com,jinghuagw.com,gotuangou.com,cjpqq.com,jinlai99.com,czspmx.com,tjsdxgt.com,u10hui.com,3g789.com,hyncjd.com,kdllight.com,114xfw.com,f4show.com,020fr.com,52meinvtu.com,518movie.com,fyzszx.com,zcszzb.com,dajinhu111.com,ffxiaoshuo.com,old114.com,wrwxpg.com,xiaoniux.com,yaopinpai.com,cztup.com,aichaosy.com,xiaoyaosf.com,xuan2011.com,gzdwn.com,szdzjx.com,dbzhaopin.com,xqwwl.com,uooer.com,csmxjj.com,madeinyc.com,cjanswer.com,sjtzgl.com,tangywh.com,stthbj.com,shthhb.com,kaluodiya.com,cdchikan.com,ydlmysgs.com,510050.cn,sqshmzx.net,fh2886.com,fh3884.com,fh39100.com,wxad012.cn,cqjipiao.com,xiaolo.cn,lhzgkj.cn,bjxdxfbj.com.cn,67555666.com,frero.top,qpj3.com,il80.com,il60.com,900888.com,mengdongol.com,xinccn.com,hk52b.com,909009000.com,y15ab.com,9qwa6.com,c55rr.com,08mvp.net,vvu85.com,xkyl999.com,df66f.com,cpp5858.com,dz888.vip,kabindasp.cn,hg58.pw,5197.com,9789.pw,8918.pw,8578.pw,hg5123.pw,66dz.vip,aa07.cc,aa27.cc,aa47.cc,aa57.cc,aa67.cc,aa87.cc,aa17.cc,aa2018.cc,aa288.cc,aa488.cc,aa088.cc,aa588.cc,aa188.cc,aa388.cc,aa688.cc,aa988.cc,cc288.cc,cc488.cc,aa2088.cc,aa2188.cc,aa2388.cc,aa37.cc,4717pay.com,haohao.ph,tuzi.ph,keai.ph,wnsr.ph,cp568.com,4717jqr.com,09ting.com,kundiu.com,nicai2.com,50026888.com,5908vip.com,k1150.com,1351444.com,7874222.com,7873444.com,8589222.com,9817111.com,9812111.com,9812000.com,9811444.com,1392333.com,1355111.com,1354000.com,1353444.com,00770a.com,funlotto.net,8005690.com,66889app.com,037017.com,037018.com,037019.com,397030.com,397031.com,397032.com,397035.com,397036.com,397037.com,301205.com,301217.com,301260.com,d866.vip,301262.com,301263.com,301267.com,301270.com,301271.com,301272.com,301273.com,301276.com,301280.com,301283.com,301285.com,301287.com,funcai.co,301290.com,funcai.net,301292.com,funcai.cc,301293.com,funcai5678.com,301295.com,funlottocdn.com,301297.com,fcai007.com,301312.com,5108vip.com,fcai588.com,301320.com,funlotto.co,301325.com,301326.com,301327.com,301357.com,301362.com,308327.com,00770b.com,00770c.com,00770d.com,00770e.com,00770f.com,00770g.com,00770h.com,00770i.com,00770j.com,00770k.com,00770l.com,308312.com,00770m.com,308275.com,00770n.com,308279.com,00770o.com,308291.com,00770p.com,308392.com,00770q.com,308396.com,00770r.com,308502.com,00770s.com,308507.com,00770t.com,308509.com,00770u.com,308512.com,88f76.com,00770v.com,302720.com,00770w.com,302617.com,00770x.com,302576.com,00770y.com,302590.com,00770z.com,302592.com,302593.com,302596.com,302537.com,302601.com,302603.com,302613.com,302615.com,501043.com,501048.com,501248.com,501334.com,501379.com,501408.com,501428.com,501438.com,501440.com,501441.com,501442.com,501445.com,i38922.com,501446.com,501447.com,501451.com,0cud.cn,501554.com,501624.com,501720.com,501721.com,501723.com,501730.com,501732.com,501736.com,501751.com,501752.com,501763.com,woaimage.com,501931.com,99fvip1.com,99fvip2.com,88fvip1.com,88fvip2.com,xiaoyunym.cn,gouwu88.info,aaa88f.com,bbb88f.com,eee88f.com,bjpsb.com,hncstv.com,2hua.com,qzjkw.net,hhh88f.com,kanguv.com,wanwoool.com,kkk88f.com,qqq88f.com,whh0000.com,whh0022.com,whh0033.com,whh0055.com,whh0066.com,whh0088.com,sss88f.com,whh0099.com,whh1111.com,whh1122.com,whh1133.com,whh1155.com,whh1166.com,whh1177.com,ppp88f.com,whh1188.com,whh1199.com,whh2017.com,whh2211.com,whh2222.com,whh2233.com,rrr88f.com,whh2255.com,whh2277.com,whh2288.com,whh2299.com,whh3311.com,whh3322.com,whh3355.com,xxx88f.com,whh3366.com,whh3377.com,whh3388.com,whh3399.com,whh4411.com,whh4422.com,whh4433.com,yyy88f.com,whh4466.com,whh4488.com,whh4499.com,whh5188.com,whh5500.com,whh5511.com,ggg88f.com,whh5522.com,whh5533.com,whh5555.com,whh5566.com,whh5599.com,whh6600.com,whh6611.com,ooo88f.com,whh6622.com,whh6655.com,whh6666.com,whh6677.com,whh6688.com,whh6699.com,whh7700.com,whh7711.com,whh7733.com,whh7755.com,whh7766.com,nnn88f.com,whh7777.com,whh7788.com,whh8811.com,whh8822.com,whh8855.com,whh8866.com,whh8899.com,ttt88f.com,whh9900.com,whh9911.com,whh9955.com,whh9966.com,whh9977.com,whh9988.com,mmm88f.com,jjj88f.com,iii88f.com,60123x.com,fff88f.com,ccc88f.com,ddd88f.com,552286.cc,lll88f.com,boobei.com.cn,ssbqgs.cn,479509.com,yhshenbo.com,09079pay.com,gcp68.com,ssss918.com,ylz1688.com,71700.com,suydfgw7ge.com,694991.com,ttt560.com,lixingshop.com,5105668.com,jp061.com,0732a.com,jp062.com,jp063.com,jp056.com,jp057.com,jp059.com,002008cs.com,00066zfb.com,58830.com,00066yh.com,xxl666888.com,99f31.com,cai66999.com,cai55666.com,cai55999.com,cai88666.com,cai22333.com,99f32.com,cai66777.com,cai55777.com,cai88555.com,fh111999.com,fh555999.com,99f33.com,99f35.com,99f37.com,99f42.com,99f41.com,99f48.com,99f43.com,7uoo.cn,807ck.com,99f38.com,43tyty.com,1808018.com,42tyty.com,99f49.com,99f45.com,99f44.com,99f40.com,gmdun.cn,99f34.com,99f39.com,3808038.com,jucangtianxia.com,c6786666.com,c6785555.com,c6783333.com,c6782222.com,99f36.com,c6781111.com,c6780000.com,c6780088.com,c6780099.com,c6780077.com,c6780022.com,c6780011.com,c678777.com,c678555.com,c678444.com,c678333.com,c678222.com,c678111.com,4819811.com,c678009.com,c678008.com,881wns.com,c678007.com,i5i3.com,50026q.com,50026s.com,c999123.com,50026r.com,50026x.com,50026w.com,1992661.com,50026v.com,50026u.com,50026t.com,122163.com,999123cc.com,caipiao66.com,00066333.com,80861.net,00066777.com,00066999.com,fa99ty.com,fa99f.com,tv99f.com,bc99f.com,2xtyc.com,1169a.com,630.cc,512oo.com,ab99f.com,5xtyc.com,wlale.com,1xtyc.com,4xtyc.com,3xtyc.com,996410.com,996042.com,hj144.top,89060.com,418674.com,pk0108.com,fzl3g.com,78785v.com,78785j.com,78785p.com,h78785.com,9988yhny.com,z78785.com,s78785.com,u78785.com,x78785.com,p78785.com,78785q.com,t78785.com,78785t.com,78785z.com,78785n.com,kuaihugao.cn,lcw1818.com,418673.com,418675.com,418672.com,b78785.com,d78785.com,n78785.com,q78785.com,418671.com,57177a.com,57177b.com,57177c.com,57177d.com,57177e.com,57177f.com,57177g.com,57177h.com,57177i.com,57177j.com,57177k.com,57177l.com,57177m.com,57177n.com,57177o.com,57177p.com,57177q.com,57177r.com,57177s.com,57177t.com,57177u.com,57177v.com,57177w.com,57177x.com,57177y.com,57177z.com,ltw5858.com,piyt8.com,hyl11.cc,rew43.com,mlk62.com,up4gv.com,bvc43.com,kaihugao.cn,mp3r4.com,37877.com,37877a.com,37877b.com,37877c.com,37877d.com,37877e.com,pp8ue.com,37877f.com,37877g.com,37877h.com,37877i.com,37877j.com,37877k.com,37877l.com,bnm98.com,37877m.com,37877n.com,37877o.com,37877p.com,37877q.com,37877r.com,37877s.com,37877t.com,37877u.com,37877v.com,37877w.com,37877x.com,37877y.com,b3zxc.com,37877z.com,8033cp.com,hyl22.net,566a.cc,566b.cc,566d.cc,566f.cc,566g.cc,566h.cc,566j.cc,566k.cc,566l.cc,566m.cc,566n.cc,566o.cc,566p.cc,566q.cc,566r.cc,566s.cc,566t.cc,566u.cc,566v.cc,566w.cc,566x.cc,566y.cc,566z.cc,677b.cc,677d.cc,677e.cc,677f.cc,677h.cc,677i.cc,677j.cc,677k.cc,677l.cc,677m.cc,677n.cc,677o.cc,677p.cc,677q.cc,677r.cc,677s.cc,677u.cc,677v.cc,677w.cc,677x.cc,677y.cc,677z.cc,xkyl99.com,hg58806.com,hr946.com,017980.com,013517.com,019715.com,019716.com,015097.com,hg588804.com,huashengdaili.com,t86988.com,tiao869.com,tiao869.co,163691.com,173135.com,851519.com,851516.com,553783.com,715163.com,376585.com,966273.com,966312.com,966713.com,966723.com,966729.com,966837.com,77722666.com,5511225.com,77711666.com,777111666.com,jin102.com,jin103.com,jin104.com,jin108.com,jin107.com,jin106.com,jin105.com,js102.cc,js103.cc,js106.cc,001313.cc,5511277.com,5511288.com,5511233.com,fc9002.com,eb159.com,001616.cc,001919.cc,001515.cc,js104.cc,js105.cc,js107.cc,js00039.com,js00051.com,js00056.com,js00057.com,js00059.com,js00067.com,js00069.com,js00071.com,07277a.com,07277b.com,07277c.com,996014.com,07277d.com,07277e.com,996024.com,996034.com,07277f.com,996041.com,07277g.com,07277h.com,996043.com,07277i.com,996045.com,07277j.com,996047.com,07277k.com,996054.com,996084.com,07277l.com,996094.com,07277m.com,996401.com,07277n.com,996402.com,07277o.com,996403.com,07277p.com,996404.com,07277q.com,996405.com,07277r.com,996407.com,07277s.com,07277t.com,07277u.com,07277v.com,07277w.com,07277x.com,07277y.com,07277z.com,6658809.com,996420.com,996430.com,996460.com,996470.com,996480.com,072081.com,072091.com,036041.com,036051.com,036061.com,036071.com,036081.com,033090.com,033091.com,033092.com,033094.com,033095.com,033096.com,033097.com,050021.com,050031.com,050041.com,050061.com,050071.com,050081.com,050091.com,065061.com,065071.com,065081.com,065091.com,018091.com,018092.com,018093.com,018094.com,018095.com,974211.com,974311.com,974511.com,974611.com,974711.com,974811.com,974911.com,768409.com,230019.com,230021.com,230035.com,230040.com,230041.com,230042.com,230043.com,230046.com,230047.com,230049.com,230014.com,230034.com,230054.com,bodhiinstitute.org,230061.com,230062.com,230063.com,js00072.com,230064.com,js00079.com,230065.com,js00090.com,230074.com,js00097.com,230084.com,js00102.com,230094.com,js00105.com,760012.com,js00106.com,760013.com,js00107.com,760014.com,js00108.com,760015.com,js00109.com,760016.com,js00112.com,760019.com,js00113.com,760059.com,js00120.com,760070.com,js00121.com,760090.com,js00122.com,js00901.com,js00902.com,js00903.com,js00905.com,js00906.com,js00907.com,js00908.com,js00909.com,v0219.com,1064.com,000567.vip,000678.vip,bytomwallet.com,000789.vip,firetoo.com,hg91003.com,hg91004.com,hg91005.com,hg91006.com,hg91007.com,hg91008.com,hg91009.com,hg38380.com,hg30009.com,hg60009.com,hg80009.com,hg35350.com,hg84840.com,hg89890.com,hg94940.com,hg91001.com,v0218.com,hg91002.com,v0217.com,v0215.com,v0214.com,v0213.com,v0212.com,v0019.com,v0019.cc,87690j.com,87690i.com,v0018.com,v0018.cc,alpine.com.cn,6658808.com,jx0212.com,768400.com,768401.com,768402.com,768403.com,768404.com,app1064.com,768405.com,vip1064.com,768406.com,6658807.com,768407.com,768408.com,js33660.com,js33880.com,js89890.com,js99110.com,6658806.com,6658803.com,v0018.net,6658802.com,js00330.com,js00550.com,js00660.com,js33110.com,js33220.com,js33550.com,js00440.com,6658801.com,hg57570.com,hg67670.com,hg81810.com,hg21210.com,hg31310.com,hg61610.com,hg71710.com,hg91910.com,pay7k.com,327k.com,2567k.com,1567k.com,4467k.com,3367k.com,1167k.com,7kcai.com,cpc8.cc,cpc4.net,1064e.com,1064g.com,sj1064.com,a1064.com,097k.cc,0009969.com,017551.com,hg37370.com,hg95950.com,hg58801.vip,v8101.com,v8103.com,v8104.com,v8105.com,v8106.com,v8107.com,v8109.com,v8102.com,hzjfx.com,2347k.com,257k.com,2667k.com,3457k.com,357k.com,50026m.com,3667k.com,50026i.com,3767k.com,50026e.com,37877.cc,5767k.com,50026c.com,607k.com,637k.com,6787k.com,67k.vip,707k.com,727k.com,7kcai.cc,7kcai.net,7kcai.vip,9967k.com,cai7k.cc,cai7k.com,cp7k.cc,cp7k.com,zf41866.com,148du.com,248du.com,8ducp.com,4466b.cc,4466c.cc,4466d.cc,4466e.cc,4466f.cc,4466g.cc,4466h.cc,4466i.cc,4466j.cc,dh1064.com,4466l.cc,4466n.cc,4466o.cc,4466p.cc,4466q.cc,4466r.cc,4466s.cc,4466t.cc,4466u.cc,4466v.cc,4466w.cc,hg28280.com,4466x.cc,4466y.cc,4466z.cc,66784a.com,66784b.com,66784c.com,66784d.com,66784e.com,66784f.com,66784g.com,66784h.com,66784i.com,1064a.com,66784j.com,1064d.com,66784k.com,1064f.com,66784l.com,1064b.com,66784m.com,66784n.com,66784o.com,66784p.com,66784q.com,66784r.com,66784s.com,66784t.com,66784u.com,0011099.com,66784v.com,66784w.com,66784x.com,66784y.com,66784z.com,8dcp.cc,60317a.com,60317b.com,60317c.com,60317d.com,xuantianys.com,60317e.com,60317f.com,60317g.com,60317h.com,60317i.com,60317j.com,60317k.com,60317l.com,60317m.com,60317n.com,60317o.com,60317p.com,60317q.com,60317r.com,sha11.com,60317s.com,60317t.com,60317u.com,60317v.com,60317w.com,60317x.com,60317y.com,60317z.com,55616a.com,55616b.com,55616c.com,sha22.com,55616d.com,sha44.com,55616e.com,sha222.com,55616f.com,55616g.com,55616h.com,55616i.com,55616j.com,55616k.com,55616l.com,55616m.com,55616n.com,55616o.com,55616p.com,55616q.com,55616r.com,55616s.com,55616t.com,55616u.com,55616v.com,copyleftmarket.com,55616w.com,55616x.com,55616y.com,55616z.com,188djc.com,0011033.com,0011044.com,jiulu.xyz,54686a.com,54686c.com,54686g.com,54686h.com,54686m.com,54686k.com,54686u.com,00066a.com,54686w.com,54686x.com,54686y.com,h54686.com,k54686.com,iitmg.cn,56pay.com,383809.com,figoc.ml,hhcp02.com,00066b.com,00066d.com,00066f.com,00066g.com,00066h.com,00066i.com,00066j.com,00066k.com,00066l.com,00066m.com,00066n.com,00066o.com,00066p.com,00066q.com,00066r.com,00066s.com,v0019.net,00066t.com,v0019.email,00066u.com,v0019.com.cn,00066v.com,v0019.co,00066w.com,v0019.cn,00066x.com,00066y.com,v0018.email,00066z.com,v0018.com.cn,v0018.co,v0018.cn,2088258.com,557666a.com,557666c.com,557666m.com,557666u.com,557666w.com,557666s.com,557666y.com,557666n.com,8888996.com,7585.cc,syg18.com,668.ag,yyg18.com,88563333.com,lesga.cn,03909.com,jsp21.com,494903.com,7665.cc,6786.cc,ttll.tv,3666o.com,3666h.com,3666bb.com,84423.com,bingdou.net,939369.com,33479.com,47878j.com,47878n.com,47878i.com,47878m.com,47878q.com,47878y.com,47878s.com,47878x.com,47878o.com,47878k.com,47878d.com,47878f.com,47878p.com,47878b.com,47878e.com,47878u.com,47878g.com,47878w.com,47878l.com,47878h.com,47878z.com,47878t.com,47878v.com,47878r.com,47878c.com,47878ee.com,47878dd.com,47878jj.com,47878tt.com,47878ll.com,47878qq.com,47878ii.com,47878vv.com,47878mm.com,47878ss.com,47878oo.com,47878rr.com,47878nn.com,47878pp.com,47878cc.com,47878zz.com,47878kk.com,47878ww.com,47878ff.com,47878xx.com,47878yy.com,47878uu.com,47878gg.com,47878hh.com,47878bb.com,00066.com,yungouzf.com,88ty1.com,88ty2.com,88ty4.com,1aty.com,88ty5.com,aa8ty.com,qq8ty.com,t789y.com,bcvip3.com,aaty3.com,t345y.com,aaty1.com,88ty3.com,bcvip2.com,aaty2.com,bcvip1.com,oo8ty.com,pp8ty.com,t123y.com,9050.cc,878466.com,boyoucai.cc,boyoucai1.com,boyoucai2.com,boyoucai3.com,996745.com,776860.com,boyoucai3.cc,996732.com,662182.com,371.cc,371a.cc,371b.cc,371c.cc,371d.cc,371e.cc,371x.cc,866269.com,628987.com,195322.com,371g.cc,923633.com,921677.com,921977.com,979133.com,979233.com,646452.com,193629.com,983112.com,877802.com,321537.com,321693.com,876297.com,876295.com,155782.com,155792.com,855608.com,byc6888.com,808399.com,957806.com,580667.com,boyoucai4.com,boyoucai6.com,boyoucai7.com,962095.com,boyoucai5.cc,boyoucai4.cc,359932.com,832795.com,371f.cc,371i.cc,371j.cc,371k.cc,371y.cc,192711.com,192722.com,219553.com,371s.cc,917233.com,927233.com,166125.com,820737.com,388453.com,707602.com,786682.com,961205.com,960207.com,boyoucai1.cc,321953.com,960206.com,217556.com,286967.com,451956.com,511174.com,371h.cc,599309.com,308908.com,987581.com,boyoucai.vip,boyoucai4.net,792587.com,776089.com,boyoucai8.cc,boyoucai9.cc,776511.com,732779.com,371r.cc,371u.cc,371w.cc,193722.com,923733.com,boyoucai5.com,371v.cc,195622.com,698771.com,927633.com,916733.com,783386.com,707250.com,616632.com,381772.com,960923.com,960307.com,boyoucai2.cc,876312.com,960121.com,960913.com,755216.com,758022.com,786251.com,371t.cc,boyoucai8.com,boyoucai.net,boyoucaiagent.com,boyoucaiadmin.com,412688.com,866248.com,boyoucai9.com,996546.com,boyoucai6.cc,boyoucai7.cc,258768.com,832793.com,371l.cc,371m.cc,371n.cc,371o.cc,371p.cc,371q.cc,371z.cc,193522.com,866546.com,929533.com,927133.com,166127.com,693119.com,433112.com,288472.com,769639.com,875957.com,937652.com,boyoucail2.cc,629293.com,326265.com,960106.com,533786.com,575872.com,737249.com,755213.com,zzztools.com,gmkhk.com,jw.tc,pc480.com,guigushengtai.com,mycp002.com,my18009.com,my18008.com,my18007.com,my18006.com,my18005.com,my18004.com,my18003.com,my18002.com,my18168.com,mycp888.net,mycp003.com,mycp886.com,mycp885.com,80essex.com,numgrad.com,duftair.com,7cainiu.com,jxwuyun.com,dtdisk.com,ahskcc.com,hjcakes.com,x18jeed.com,6218896.com,akmesh.com,ajhydg.com,peilv9.com,jzschax.com,3011j.com,3011i.com,3011h.com,3011g.com,3011f.com,celestv.com,doxaban.com,bpsi-us.com,reo3pl.com,thaisis.com,jetsbbq.com,hopsew.com,cullout.com,yoceleb.com,dogwad.com,soluneg.com,hemcins.com,ahodge.com,debttld.com,songaga.com,stormrx.com,mkingec.com,ddbaits.com,8smbgm.com,2raindr.com,4raindr.com,2nsarv.com,t2foods.com,palrato.com,sw385.com,sw244.com,sw592.com,brkoto.com,htapnc.com,escbmrj.com,massace.com,calemw.com,kemfun.com,krdaxue.com,zzzeng.com,wtjdcjc.com,mjzulin.com,91ysipo.com,nodeuml.com,maimel.com,leekins.com,bbsame.com,fblojas.com,8257378.com,ahfnhb.com,diliptc.com,mia-q.com,zarpad.com,acneej.com,caulla.com,dayemuk.com,nldisk.com,nldisc.com,iobxp.com,gzlqfs.com,ykchat.com,gxhwhd.com,sunites.com,mektan.com,13hoog.com,exxtime.com,oiliban.com,qvomeis.com,gojings.com,3umaker.com,ytjdjy.com,cqdsjm.com,lygcsgg.com,daolfnb.com,hpzclub.com,omases.com,ac1997.com,172cc.com,focoda.com,liyusex.com,ap10086.com,gsxxxx.com,me1.cc,ag405-js-gov.cn,ag404-js-gov.cn,ag403-js-gov.cn,ag402-js-gov.cn,ag401-js-gov.cn,ag400-js-gov.cn,ag399-js-gov.cn,ag398-js-gov.cn,ag397-js-gov.cn,ag396-js-gov.cn,ag395-js-gov.cn,ag394-js-gov.cn,ag393-js-gov.cn,ag392-js-gov.cn,ag391-js-gov.cn,ag390-js-gov.cn,ag389-js-gov.cn,ag388-js-gov.cn,ag387-js-gov.cn,ag386-js-gov.cn,ag385-js-gov.cn,ag384-js-gov.cn,ag383-js-gov.cn,ag382-js-gov.cn,ag381-js-gov.cn,ag380-js-gov.cn,ag379-js-gov.cn,ag378-js-gov.cn,ag377-js-gov.cn,ag376-js-gov.cn,ag375-js-gov.cn,ag374-js-gov.cn,ag373-js-gov.cn,ag372-js-gov.cn,ag371-js-gov.cn,ag370-js-gov.cn,ag369-js-gov.cn,ag368-js-gov.cn,ag367-js-gov.cn,ag366-js-gov.cn,ag365-js-gov.cn,ag363-js-gov.cn,ag362-js-gov.cn,ag361-js-gov.cn,ag360-js-gov.cn,ag359-js-gov.cn,ag358-js-gov.cn,ag357-js-gov.cn,ag356-js-gov.cn,ag355-js-gov.cn,ag354-js-gov.cn,ag353-js-gov.cn,ag352-js-gov.cn,ag351-js-gov.cn,ag350-js-gov.cn,ag349-js-gov.cn,ag348-js-gov.cn,ag347-js-gov.cn,ag346-js-gov.cn,ag345-js-gov.cn,ag344-js-gov.cn,ag343-js-gov.cn,ag342-js-gov.cn,ag341-js-gov.cn,ag340-js-gov.cn,ag339-js-gov.cn,ag338-js-gov.cn,ag337-js-gov.cn,ag336-js-gov.cn,ag335-js-gov.cn,ag333-js-gov.cn,ag332-js-gov.cn,ag331-js-gov.cn,ag330-js-gov.cn,ag329-js-gov.cn,ag328-js-gov.cn,ag327-js-gov.cn,ag326-js-gov.cn,ag325-js-gov.cn,ag324-js-gov.cn,ag323-js-gov.cn,ag322-js-gov.cn,ag321-js-gov.cn,ag319-js-gov.cn,ag318-js-gov.cn,ag317-js-gov.cn,ag315-js-gov.cn,ag314-js-gov.cn,ag313-js-gov.cn,ag312-js-gov.cn,ag309-js-gov.cn,ag308-js-gov.cn,ag307-js-gov.cn,ag306-js-gov.cn,ag305-js-gov.cn,ag304-js-gov.cn,ag281-js-gov.cn,ag280-js-gov.cn,ag279-js-gov.cn,ag278-js-gov.cn,00pf.com,0401.org,66ig.com,07329999.com,fcht003.com,bet-6677-gov.cn,bet-6698-gov.cn,bet-6668-gov.cn,bet-6678-gov.cn,bet-6658-gov.cn,bet-6898-gov.cn,bet-6288-gov.cn,bet-6688-gov.cn,bet-566-gov.cn,bet-466-gov.cn,bet-465-gov.cn,bet-463-gov.cn,bet-462-gov.cn,bet-461-gov.cn,bet-460-gov.cn,bet-459-gov.cn,bet-458-gov.cn,bet-457-gov.cn,bet-456-gov.cn,bet-455-gov.cn,bet-454-gov.cn,bet-453-gov.cn,bet-452-gov.cn,bet-451-gov.cn,bet-450-gov.cn,bet-449-gov.cn,bet-448-gov.cn,bet-447-gov.cn,bet-446-gov.cn,bet-445-gov.cn,bet-444-gov.cn,bet-443-gov.cn,bet-442-gov.cn,bet-441-gov.cn,bet-440-gov.cn,bet-439-gov.cn,bet-438-gov.cn,bet-437-gov.cn,bet-436-gov.cn,bet-435-gov.cn,bet-434-gov.cn,bet-433-gov.cn,bet-432-gov.cn,bet-431-gov.cn,bet-430-gov.cn,bet-429-gov.cn,bet-428-gov.cn,bet-427-gov.cn,bet-426-gov.cn,bet-425-gov.cn,bet-423-gov.cn,bet-422-gov.cn,bet-421-gov.cn,bet-420-gov.cn,bet-419-gov.cn,bet-418-gov.cn,bet-417-gov.cn,bet-416-gov.cn,bet-415-gov.cn,bet-414-gov.cn,bet-412-gov.cn,bet-411-gov.cn,bet-409-gov.cn,bet-404-gov.cn,bet-402-gov.cn,bet-400-gov.cn,bet-399-gov.cn,bet-398-gov.cn,bet-397-gov.cn,bet-396-gov.cn,bet-395-gov.cn,bet-394-gov.cn,bet-393-gov.cn,bet-392-gov.cn,bet-391-gov.cn,bet-390-gov.cn,bet-389-gov.cn,bet-388-gov.cn,bet-387-gov.cn,bet-386-gov.cn,bet-385-gov.cn,bet-384-gov.cn,bet-383-gov.cn,bet-382-gov.cn,bet-381-gov.cn,bet-380-gov.cn,bet-379-gov.cn,bet-378-gov.cn,bet-376-gov.cn,bet-375-gov.cn,bet-374-gov.cn,bet-373-gov.cn,bet-372-gov.cn,bet-371-gov.cn,bet-370-gov.cn,bet-369-gov.cn,bet-368-gov.cn,bet-367-gov.cn,bet-366-gov.cn,bet-365-gov.cn,hersheycn.com,jshytc.cn,tdqcfw.cn,taishantuan.com,tkzcktv.com,gongyilin.cn,shuzidianshi.cn,flowportal.net,gddelong.com.cn,izxjr.com.cn,sungift-home.cn,czcjdj.cn,236zz.tv,suanminghunyin.com,hbucm.cn,fhg7788.com,11133aa.com,11133b.com,11133bb.com,11133cc.com,11133d.com,11133dd.com,11133e.com,11133ee.com,11133ff.com,11133g.com,11133gg.com,11133h.com,11133hh.com,11133i.com,11133ii.com,11133j.com,11133jj.com,11133k.com,11133kk.com,11133l.com,11133ll.com,11133m.com,11133mm.com,11133n.com,11133nn.com,11133o.com,11133oo.com,11133p.com,11133pp.com,11133q.com,11133qq.com,11133r.com,11133rr.com,11133s.com,11133ss.com,11133t.com,11133tt.com,11133u.com,11133v.com,11133vv.com,11133w.com,11133ww.com,11133x.com,11133xx.com,11133y.com,11133yy.com,11133z.com,11133zz.com,50584n.com,50584c.com,50584u.com,50584e.com,50584j.com,50584b.com,50584p.com,50584t.com,50584h.com,50584y.com,50584z.com,50584g.com,50584w.com,50584s.com,50584a.com,50584k.com,50584o.com,50584f.com,50584r.com,50584i.com,50584x.com,50584m.com,50584d.com,50584q.com,50584v.com,50584l.com,www-567ioyt-8-fdjhfhjgri5i78.com,a557666.com,557666h.com,557666b.com,557666d.com,jl533.com,567k3.cc,xmkungang.com.cn,sxhuachenmm.com,lngqpxk.com,whlish.com,xmmykj.com,gscmsz.com,facaicoin.com,yayingcaifu.com,gxbaoli.com,ljhlm.com,heruizb.com,hxmeili.com,dajinhu888.com,taonvwo.com,hetaozj.com,ytdulougs.com,www-78785.com,sdlwds.com,kaijuzckj.com,bairbaby.com,quweig.com,lhwdyj.com,hzqh520.com,1hucha.com,bknccq.com,btbubbs.com,pcqm9car.com,donaya.com,tubexing.com,tjsm886.com,tffcar.com,taoxjsc.com,slfxxw.com,sjztzsm.com,sxxinde.com,cdaga.com,772222.mobi,198gou.com,dlsunroom.com,miooim.com,gouugou.com,772222.io,51jishiyu.com,hnxigua.com,wydele.com,772222.tv,xingruei.com,donge365.com,hengtpm.com,bjgaf.com,tlw520.com,huihuiys.com,lbjdfpcb.com,joywinpay.com,yggcbl.com,yzdygj.com,hkglpj.com,hbjymy.com,hexdzme.com,gmffzz.com,wlwlseo.com,yimeisxq.com,abyb0m.com,gnguolu.com,fjzcgjs.com,hnsmdf.com,redianmen.com,jucailm.com,tiaozhongzi.com,chorokstars.com,jrsj9.com,leyoulehuo.com,wfjjwcdd.com,78785.com,557666.com,557666x.com,557666z.com,ikik365.com,iikk365.com,lklk365.com,llkk365.com,b999123.co,b999123.org,b999123.biz,b999123.net,b999123.io,b999123.com,999123.me,zz999123.com,dgongji.com,lasat.cn,guiweb.cn,bjzbg.cn,gegebang.cn,yingyongsx.cn,xiaoxiongmm.cn,lqlty.cn,tjpbs.cn,zstel.cn,metcom.cn,shldvip.cn,yifoo.cn,eaning.cn,karola.cn,cartune.cn,ecodrive.cn,dianyingji.cn,anway.cn,shfanqie.cn,jshngk.cn,askers.cn,uuhks.cn,hanguogongzhu.cn,fireservices.cn,ychbjx.cn,lcbtb.cn,bjdmk.cn,xwwaigua.cn,jldhto.cn,chexp.cn,ilyfe.cn,carac.cn,hazjw.cn,ganlaosu.cn,aclove.cn,ydnms.cn,gfbgov.cn,ltdjx.cn,redroseshop.cn,bszsu.cn,reating.cn,kqnet.cn,gdlook.cn,pengyoujiagong.cn,haitaiwuye.cn,toykohot.cn,saibos.cn,xyiyi.cn,yiyiguan.cn,tianlongren.cn,sosobao.cn,ptxbtx.cn,gobuya.cn,songcanwang.cn,wangxiaohu.cn,eetime.cn,cycard.cn,jicom.cn,cdtel.cn,882498.com,283736.com,655176.com,hiwhy.cn,772796.com,773185.com,ganbingyao.cn,832985.com,832697.com,665751.com,sxsldd.cn,587582.com,484245.com,797813.com,kaqise.cn,976286.com,976136.com,967385.com,blogcom.cn,972685.com,976739.com,296263.com,hoolee.cn,196265.com,263612.com,887945.com,ysctg.cn,282322.com,233693.com,286819.com,971322.com,hxfur.cn,975922.com,975122.com,973522.com,975722.com,965169.com,962602.com,962605.com,673518.com,686114.com,833721.com,284939.com,765497.com,255712.com,122431.com,967202.com,134268.com,255735.com,976951.com,711219.com,922691.com,822179.com,552183.com,526787.com,522962.com,971722.com,971522.com,971622.com,971922.com,867622.com,869322.com,272296.com,817156.com,912135.com,552692.com,776197.com,779526.com,656623.com,667283.com,692916.com,798227.com,211104.com,433360.com,361767.com,791831.com,772795.com,552763.com,776571.com,776215.com,676512.com,966895.com,961165.com,961156.com,961151.com,377yx.com,nhu-wd.com,cunnl.com,lp-yun.com,qqlilon.com,pai-liu.com,shou-rt.com,zhen-top.com,kejiyo.com,zcyouqin.com,xiazop.com,gongxp.com,chulawd.com,guanbk.com,mingtj.com,lieok.com,liaotf.com,maihnn.com,juntbb.com,pigynh.com,yuankol.com,50026.vip,5009822.com,5009811.com,5009733.com,5009722.com,5009711.com,5009655.com,5009633.com,5009622.com,5009611.com,5009511.com,5009211.com,haolemen.me,haolemen.net,8898779.com,haolemen.vip,haolemen.org,8896779.com,8895998.com,8896998.com,8895779.com,8838998.com,8978998.com,8897779.com,8968998.com,haolemen.cc,7799877.com,8958998.com,8918998.com,8897998.com,dabing1069.com,9977989.com,kmfuk.cn,cnciasi.com,tianfeinidi.com,pohennanfei.com,duojia360.com,voguethinker.com,xceni.com,ss6k.com,haicheng2005.com,iwohs.com,pengji123.com,witgarden.com,fempovcams.com,chechengshi.com,8887658.com,8882338.com,cerdai.com,fjtinron.com,lztrwh.com,shshuangtu.com,664f.com,sytaili.com,xakyjg.com,528ds.com,ctrpqrqd.com,ncsaqbrm.com,yuweitongtai.com,rjmudctq.com,dr-ghomeshi.com,flyingoveroz.com,sevens-home.com,bboxchina.com,gbfjnp.com,crumpster.com,hoorayharoo.com,nikelebron55.com,lumberstar.com,travelgoeson.com,youyongfq.com,lcaifu.com,yijiubi.com,52ltb.com,78w88.com,gdvti.com,5gcp.com,amaanakaah.com,gzmingpian.com,clonecircle.com,sanjiav2016.com,bravepublic.com,0557mpsoft.com,chushupai.com,vjjldln.com,amylylw.com,jsp02.com,jsp03.com,51qingxijix.com,jsp04.com,jsp05.com,jsp06.com,spacejam2016.com,jsp10.com,jsp12.com,jsp16.com,yiqiu788.com,jsp17.com,jsp19.com,nannan2018.com,jsp20.com,jsp22.com,jsp25.com,12go2florida.com,jsp27.com,jsp28.com,jsp29.com,daikuanqi.com,jsp30.com,jsp31.com,jsp32.com,hsxfyd.com,jsp33.com,jsp34.com,jsp35.com,bicwyx.com,jsp37.com,jsp38.com,jsp39.com,1866kitchens.com,jsp40.com,jsp42.com,jsp43.com,866travel.com,jsp44.com,jsp46.com,jsp48.com,jmjiegu.com,jsp49.com,jsp50.com,jsp52.com,ofssauna.com,jsp53.com,jsp54.com,jsp56.com,sanchengzs.com,jsp57.com,jsp58.com,jsp59.com,a-amateur.com,jsp60.com,jsp61.com,jsp62.com,sbr-store.com,jsp63.com,jsp64.com,jsp67.com,vaecaac.com,jsp70.com,jsp71.com,jsp72.com,czgar.com,jsp73.com,jsp74.com,jsp75.com,eialj.com,jsp76.com,jsp77.com,jsp78.com,sslsns.com,jsp79.com,70879.com,jsp80.com,jsp81.com,wohuitb.com,jsp83.com,jsp84.com,jsp87.com,2706232.com,jsp89.com,jsp90.com,jsp91.com,wangzuanke.tk,jsp92.com,4612914.com,jsp93.com,jsp94.com,jsp96.com,jsp97.com,5880275.com,wchfdjz.com,smxshi.com,9142699.com,wmgjjt.com,zqzlk.com,update-shop.com,csamen.com,redubi.com,guapt.com,kensedou.com,xin2ty.com,gzwjcars.com,prwhbesm.com,abrilfashion.com,iteqtgp.com,mpzwww.com,lidadongman.com,albirdy.com,cgweaver.com,xushanyin.com,sleekcall.com,treehippo.com,yangjianhua8.com,armoredrhino.com,zhiwenlive.com,oelrikon.com,mtcq33.com,baidukjw.com,tyfuyao.com,232999.com,232999y.com,232999t.com,232999r.com,beckelland.com,xtremeiam.com,flexyfashion.com,232999o.com,obifs.com,abcmeetings.com,manwithkilts.com,tamarahayle.com,mylivechurch.com,imeguatemala.com,theoparis.com,sirvonduke.com,amootabligh.com,belmontuae.com,xuanshiting.com,doktorkulisi.com,squashcube.com,ercichuangye.com,izumihousing.com,janefirbank.com,232999h.com,gusmckechnie.com,jordankreun.com,produconstru.com,hourbargain.com,epicbiscuit.com,fatcatville.com,chumpkingdom.com,theredcradle.com,nadocreative.com,wingustudios.com,pixipanama.com,techyzmundo.com,woopstore.com,galpinparts.com,damainname.com,chucktbeats.com,tanisyawney.com,bethesilence.com,turbobikeco.com,ejuiceindia.com,dvigraphics.com,chicagoalg.com,newyorkdon.com,tomskygroup.com,jbuffington.com,voiceofwine.com,studioamalia.com,ritmicanusan.com,ahpdomains.com,morpetgarden.com,ptcfastener.com,eishinsya.com,baharsstudio.com,biggersbluff.com,lawncaretlc.com,lolitatrends.com,airportsperu.com,ngpzdnipro.com,ozturktorna.com,matadornyc.com,kwamesworld.com,laosusedcars.com,zipinstore.com,clairelawson.com,xlovewebcams.com,youngebonies.com,cekcetercume.com,dmitriklimov.com,eminozkan.com,vmautogroup.com,justallmedia.com,heathrowmga.com,acacornhole.com,donastorey.com,nyitonline.com,bouncefind.com,budkracher.com,ingenionics.com,risergoodwyn.com,vbpdinput.com,vietrangdong.com,pipscotland.com,creatodrome.com,damfineart.com,miltownkings.com,topteamindia.com,afrfunding.com,kateyburns.com,twigchair.com,whizcarwash.com,munozcamero.com,topgunkennel.com,xtpaike.cn,xdtdc.cn,boyaart.com,warelive.com,shuatoptao.cn,011app.com,ccc87.cn,yy171.com,hongyeglassware.com,235347.com,yesechengren.com,akxu.com,mian-ji.com,bzjjd.cn,app667.com,app414.com,benzuowen.com,rx521.com,lrdncv.com,58wordpress.com,m76.com,246676.com,hacker-dalong.com,912003.com,tmonline.com.cn,525110.com,681486.com,margan.cn,642386.com,bitchin.cn,tellyou-mc.com,jcjszs.com,tldyhs.com,yydb8.com,chengduxiangshang.com,plwater.cn,jinyuanniushao.com,whdgthjx.com,0571kd.cn,yzjytwj.com,rdaavnne.cn,nbtons.com.cn,myxhcom.com,188qjcp.com,yijiaheshop.com,genehealth-jz.com,2017qjcp.com,xmtnykj.com,2604685.com,bjyesheng.com,dtfxvsd.com,xmndc.cn,6111117.com,muzhizhifu.com,7111113.com,7333331.com,renshanguijiaomuju.com,98kadi.com,7999991.com,scalershop.cn,cqzhuangdaren.net,zeppe-ah.com,cyanet.cn,tyfjp4.cn,tfyqj4.cn,chachinantao.cn,cn1405.cn,aqwbf7.cn,chunyujiaoyu.cn,bruncy.cn,bebet.cn,longyishop.com,sqnjngxk.cn,azaogvfo.cn,aisnj9.cn,83090.com,sz-shuanglong.com,ykk666.com,anxiw.top,xuerenshe.com,zwduobao.com,sundonggen.com,ityihong.com,xhc2045.com,kwnfurniture.com,iammanyu.com,418694.com,604686.com,50010001.com,luanshist.com,79zfw.cn,djklk.com,235601.com,yuanxinvr.com,zzshien.com,cq15589293080.cn,cshtzz.com,junbuwang.cn,erzaiwang.cn,diantuwang.cn,zuojiwang.cn,czhsi.com.cn,xiangnawang.cn,gffgzh.cn,quanjiawang.cn,shoujiaowang.cn,hk-fangchan.com,zpdwmihvqf.com,valpqzuxbm.com,lpsfmayawh.com,xiedewang.cn,bingshenwang.cn,lunfengwang.cn,kongouwang.cn,xwwwhobqhp.com,hongcheng888.cn,mianguiwang.cn,grobryoccy.com,binggenwang.cn,xinbanwang.cn,mjhchbyvds.com,i8tk.com,eaghmzneun.com,kmyzceylxq.com,radvnhmsnv.com,guangqishi.com,beishangwang.cn,pzndt.com,shangzongwang.cn,icwuwop.cn,kbemfzfkbu.com,ugrnswmmxi.com,gezhewang.cn,nrelqby.cn,bvdrrukbbm.com,lunouwang.cn,xznqzewvgc.com,depwwojepn.com,liuwen1688.com,dianguanwang.cn,gwdkrntxjr.com,fifsupn.cn,uscrsljiid.com,gqmcdvxrcl.com,cstrzwo.cn,uznavsf.cn,loisandbetty.com,xianouwang.cn,niguwang.cn,muzhongwang.cn,malswtb.cn,lpnvfxjr.cn,qhaiobj.cn,huiguanwang.cn,luonn.com,icavsxvcfe.com,tjxdtqn.cn,lwtkbqrm.cn,itour-cn.com,687486.com,22848.com,lxboyrhx.cn,asseknthmu.com,fhoorldtxn.com,xhgmzgceeo.com,cermjjuvii.com,uhgbxztbza.com,vmmrwuuwbu.com,xpgjqeaxep.com,fothoqersy.com,smtsmnpasf.com,anjbwnwiee.com,uahqjseyql.com,fokkmpswji.com,qzmhkpynxh.com,mlrstwc.cn,ikcvpypsuj.com,paiwubengwh.net,vjlhxbnijq.com,fgqbokcusb.com,rctnzjzqau.com,lixinshengxue.com,iovtrlt.cn,zcgs88888.com,oyunkuyusu.com,vtymoprnxn.com,uztydzdrlk.com,rkwtkbryqg.com,fhygmfvxfn.com,pbftjvnv.cn,pvobjsrzhf.com,ezivrztfel.com,tkiogoycvq.com,rmbpwnspag.com,qslgdqhyzx.com,olybmuomff.com,kplajyvfnq.com,qushiguanli.cn,zhihuanonline.com,agaret.cn,omdsgsgqge.com,poavcmiala.com,gkmfdhbnkj.com,blntknhunj.com,wsdsndkslt.com,lgdurqrwmh.com,fstfyofvgs.com,yvohilzdla.com,clrzlepmpl.com,izdphvvncz.com,idrqymndhw.com,qcvtbqbybe.com,szyishengda.com,556px.com,tezgvtchto.com,snidxxtbkl.com,wezzftyfak.com,hqzmlkcoqk.com,byrjfldkth.com,zlwtzlthdo.com,qsxyjy.com,lmxysc.com,suchiwl.com,vuwquulsql.com,hanyunhealth.com,yqimall.com,beibeitop.com,qylbmp.com,tanmul.com,zy-bx.com,xbikblu.cn,ncmyzy.com,liangzi86.com,ehg168.com,cyhciro.cn,xsynokt.cn,910yx.com,showedm.com,husotuh.cn,zvdtdby.cn,hnzhtrade.com,huaqi-xmall.com,klzw888.com,gcfuwu.com,dashparr.com,nobtgdd.cn,qj3000.com,htjf178.com,wlvijsi.cn,kmdlmc.com,huilongjy.com,zxhmddv.cn,92cloth.com,aenhnmb.cn,qjw188.com,xgerjhj.cn,renpeiwang.cn,cwghd.cn,xnb0971.com,noocqdr.cn,rzoivofu.cn,ghtdnix.cn,shangfeiguoji.com,sthao.net,shhk120.com";

            Console.WriteLine(zoneStr.Split(',').Length);
            string sql = @"select zone,rzone from zones where zone='{0}'";
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            IMongoCollection<ZonesSimple> categoriesZ = db.GetCollection<ZonesSimple>("zones");
            int count = 0;
            int unSame = 0;
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时 
            foreach (string zone in zoneStr.Split(','))
            {
                DataTable dt = MySQLHelper.Query(string.Format(sql,zone)).Tables[0];
                if (dt.Rows.Count > 0)
                {
                    string rzone = dt.Rows[0]["rzone"].ToString();
                    if (zone != rzone)
                    {
                        var builder = Builders<ZonesSimple>.Filter;
                        var filter = builder.Eq("domain", zone + ".");
                        string rrcol = StringHelper.CalculateMD5Hash(rzone + ".").ToLower().Substring(0, 1);
                        var update = Builders<ZonesSimple>.Update.Set("rdomain", rzone + ".").Set("rrcol", rrcol);
                        categoriesZ.UpdateMany(filter, update);
                        unSame++;
                    }
                }
                else
                {
                    Console.WriteLine("zone=  " + zone + " has delete");
                }

                count++;
                if (count > 0 && count % 50 == 0)
                {
                    Console.WriteLine("count= {0} time= {1}", count, watch.ElapsedMilliseconds);
                }
            }

            Console.WriteLine(zoneStr.Split(',').Length);
            Console.WriteLine("count= {0} unSame={2}  time= {1}", count, watch.ElapsedMilliseconds, unSame);
        }
        static void RefreshRecordsN()
        {
            string[] collection = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
            string sql = @"select d.id,d.zoneid,d.zone,d.host,d.type,d.data,d.ttl,d.view,d.mx_priority,d.userid,d.active  from dnsrecords as d where Active='N';";
            DataSet ds = MySQLHelper.Query(sql);
            Console.WriteLine("get data " + ds.Tables[0].Rows.Count);
            List<dnsrecords> dlist = DtToList<dnsrecords>.ConvertToModel(ds.Tables[0]);

            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            List<DnsRecordsSimple>[] dla = new List<DnsRecordsSimple>[16] { new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>(), new List<DnsRecordsSimple>() };
            
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时 
            foreach (dnsrecords d in dlist)
            {
                if (CheckRecordHost(d.host, d.type) && CheckRecordData(d.data, d.type, d.view, d.host))
                {
                    string rrcol = StringHelper.CalculateMD5Hash(d.zone + ".").ToLower().Substring(0, 1);
                    int idx = Int32.Parse(rrcol, System.Globalization.NumberStyles.HexNumber);
                    dla[idx].Add(Row2DnsRecord(d));
                }
            }
            foreach (string c in collection)
            {
                int idx = Int32.Parse(c, System.Globalization.NumberStyles.HexNumber);
                List<long> ridlist = new List<long>();
                foreach (DnsRecordsSimple d in dla[idx]) {
                    ridlist.Add(d.rid);
                }
                IMongoCollection<DnsRecordsSimple> categoriesZ = db.GetCollection<DnsRecordsSimple>(c);
                var builder = Builders<DnsRecordsSimple>.Filter;
                var filter = builder.In("rid", ridlist);
                var update = Builders<DnsRecordsSimple>.Update.Set("is_stop", "Y");
                categoriesZ.UpdateMany(filter, update);
                Console.WriteLine(c+ " time= {0}",  watch.ElapsedMilliseconds);
            }
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

    }
    internal class zad {
        public long id { get; set; }
        public string zone { get; set; }
        public long aid { get; set; }
        public string atype { get; set; }
        public string dzone { get; set; }

    }


    internal class zonecount {
        public string zone { get; set; }
        public int c { get; set; }
    }
    internal class zoneAndrzone {
        public string zone { get; set; }
        public string rzone { get; set; }
    }
}
