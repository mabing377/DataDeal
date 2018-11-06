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
            //Console.WriteLine("5-CheckMXData");
            //Console.WriteLine("6-RefreshNewColumn");
            //Console.WriteLine("7-RefreshOldData");
            //Console.WriteLine("8-DeleteSOANS");
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
                //case 53:
                //    CheckMXData();
                //    break;
                //case 54:
                //    RefreshNewColumn();
                //    break;
                //case 55:
                //    RefreshOldData();
                //    break;
                //case 56:
                //    DeleteSOANS();
                //    break;
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
}
