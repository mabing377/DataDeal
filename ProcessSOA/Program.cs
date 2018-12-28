using BindDns.MongoDBEntity;
using Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace ProcessSOA
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("程序功能：");
            Console.WriteLine("1-DeleteWithoutZone;");
            Console.WriteLine("2-DeleteBind");
            Console.WriteLine("3-CheckSOA");
            //Console.WriteLine("4-UpdateZonesID");
            //Console.WriteLine("5-TestZonesID");
            //Console.WriteLine("6-ExceptionLog");
            //Console.WriteLine("7-User Data Transfer");
            Console.Write("请输入对应的数字：");
            int input = Console.Read();
            string basepath = AppDomain.CurrentDomain.BaseDirectory;
            //Console.WriteLine("你输入的是：" + input.ToString());
            switchaction:
            switch (input)
            {
                case 49:
                    DeleteWithoutZone();
                    break;
                case 50:
                    DeleteBind();
                    break;
                case 51:
                    CheckSOA();
                    break;
                //case 52:
                //    UpdateZonesID();
                //    break;
                //case 53:
                //    TestZonesID();
                //    break;
                //case 54:
                //    ExceptionLog();
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
        static void DeleteWithoutZone() {

        }
        static void DeleteBind()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时  
            DataTable dt = MySQLHelper.Query("SELECT z.id,z.zone,z.userid,CASE when d.maxfensheng<1 then 0 else 1 end as level,z.nsstate,z.active,z.forcestop,z.rzone from zones as z left join domainlevel as d on z.DomainLevel=d.levelvalue where z.zone<>z.rzone").Tables[0];
            List<zones> zonesList = DtToList<zones>.ConvertToModel(dt);
            Console.WriteLine("GetDataTabel from mysql count={1} Use Time={0};", watch.ElapsedMilliseconds, dt.Rows.Count);
            List<ZonesSimple>[] ala = new List<ZonesSimple>[16] { new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>(), new List<ZonesSimple>() };
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
      
            foreach (zones z in zonesList)
            {
               ZonesSimple zs= Zones2ZonesSimple(z);
               ala[Int32.Parse(zs.rrcol, System.Globalization.NumberStyles.HexNumber)].Add(zs);                
            }
            string del = "";
            try
            {
                for (int i = 0; i < 16; i++)
                {
                    string rrcol = i.ToString("x");
                    IMongoCollection<AuthoritiesSimple> collection = db.GetCollection<AuthoritiesSimple>(rrcol);
                    Console.WriteLine(rrcol + "  " + ala[i].Count);
                    List<string> domainList = new List<string>();
                    int count = 0;
                    foreach (ZonesSimple zs in ala[i])
                    {
                        domainList.Add(zs.domain);
                        count++;
                        if (domainList.Count == 100|| count == ala[i].Count)
                        {
                            DeleteResult result= collection.DeleteMany(Builders<AuthoritiesSimple>.Filter.In("domain", domainList));
                            domainList.Clear();
                            Console.WriteLine(count);
                        }
                    }
                    Console.WriteLine("deal " + rrcol);
                }
            }
            catch (Exception ex) {
                string messget = ex.Message;
            }            
            Console.WriteLine("end deal ");
        }
        static void CheckSOA()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时  
          
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            
            for (int i = 0; i < 16; i++)
            {
                string rrcol = i.ToString("x");
                IMongoCollection<AuthoritiesSimple> collection = db.GetCollection<AuthoritiesSimple>(rrcol);
                List<AuthoritiesSimple> asList = collection.Find(Builders<AuthoritiesSimple>.Filter.Eq("type", "SOA")).ToList();
                Console.WriteLine(rrcol + " collection " + asList.Count);
                int count = 0;
                foreach (AuthoritiesSimple auth in asList) {
                    var list = asList.FindAll(a => a.domain == auth.domain).ToList();
                    if (list.Count > 1) {
                        Console.WriteLine(auth.domain+ "Error");
                    }
                    count++;
                    if (count % 1000 == 0) {
                        Console.WriteLine(count);
                    }
                }
                Console.WriteLine(rrcol +" deal");
            }

            Console.WriteLine("end deal ");
        }
        public static ZonesSimple Zones2ZonesSimple(zones theZone)
        {
            ZonesSimple zs = new ZonesSimple();
            
            string md5 = StringHelper.CalculateMD5Hash(theZone.zone + ".").ToLower();
            zs.id = md5;
            if (theZone.zone != theZone.rzone)
                md5 = StringHelper.CalculateMD5Hash(theZone.rzone + ".").ToLower();
            zs.userid = Convert.ToInt32(theZone.userid);
            zs.domain = theZone.zone + ".";
            zs.rrcol = md5.Substring(0, 1);
            zs.level = Convert.ToInt32(theZone.level);
            zs.nsstate = theZone.nsstate;
            zs.is_stop = theZone.active == "Y" ? "N" : "Y";
            zs.force_stop = theZone.forcestop;
            zs.rdomain = theZone.rzone + ".";
            zs.loadonstart = 0;
            return zs;
        }
    }
}
