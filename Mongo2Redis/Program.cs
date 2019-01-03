using BindDns.MongoDBEntity;
using Models;
using MongoDB.Driver;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Newtonsoft.Json;
using Utility;
using System.Dynamic;

namespace Mongo2Redis
{
    public class ZonesHash {
        public string Key { get; set; }
        public HashEntry[] EntryArry {get;set;}
    }
    public class ZonesSet {
        public string Key { get; set; }
        public RedisValue RedisValue { get; set; }
    }
    public class ViewSet {
        public string Key { get; set; }
        public RedisValue RedisValue { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Redis.Init(ConfigurationManager.ConnectionStrings["Redis"].ToString());  //初始化Redis数据库
            Console.WriteLine("程序功能：");
            Console.WriteLine("1-Transfer Zones;0-1000000");
            Console.WriteLine("2-Transfer Zones;1000000+");
            Console.WriteLine("3-Transfer Authorities&Records0-3");
            Console.WriteLine("4-Transfer Authorities&Records4-7");
            Console.WriteLine("5-Transfer Authorities&Records8-b");
            Console.WriteLine("6-Transfer Authorities&Recordsc-f");
            Console.WriteLine("7-Transfer levelip");

            Console.Write("请输入对应的数字：");
            int input = Console.Read();
            string basepath = AppDomain.CurrentDomain.BaseDirectory;
            //Console.WriteLine("你输入的是：" + input.ToString());
            switchaction:
            switch (input)
            {
                case 49:
                    DealZonesSimple1();
                    break;
                case 50:
                    DealZonesSimple2();
                    break;
                case 51:
                    DealRecord1();
                    break;
                case 52:
                    DealRecord2();
                    break;
                case 53:
                    DealRecord3();
                    break;
                case 54:
                    DealRecord4();
                    break;
                case 55:
                    DealView();
                    break;
                default:
                    break;
            }
            input = Console.Read();
            goto switchaction;
        }
        static void DealRecord()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            string[] collection = new string[] { "0", "1", "2", "3",  "4", "5", "6", "7",  "8", "9", "a", "b",  "c", "d", "e", "f" };
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            foreach (string c in collection)
            {
                IMongoCollection<DnsRecordsSimple> categoriesD = db.GetCollection<DnsRecordsSimple>(c);
                List<DnsRecordsSimple> dlist = categoriesD.Find(Builders<DnsRecordsSimple>.Filter.Empty).ToList<DnsRecordsSimple>();
                int count = 0;
                watch.Start();
                List<ZonesSet> zhlist = new List<ZonesSet>();
                foreach (DnsRecordsSimple d in dlist)
                {
                    ZonesSet zh = new ZonesSet();
                    zh.Key = d.domain;
                    zh.RedisValue = JsonConvert.SerializeObject(d);
                    zhlist.Add(zh);
                    count++;
                    if (zhlist.Count == 50000 || count == dlist.Count)
                    {
                        var batch = Redis.DB(2).CreateBatch();
                        foreach (var item in zhlist)
                        {
                            batch.SetAddAsync(item.Key, item.RedisValue);
                        }
                        batch.Execute();
                        zhlist.Clear();
                        Console.WriteLine(c + "   collection deal " + count + "  time" + watch.ElapsedMilliseconds);
                    }
                }
                Console.WriteLine(c + "   collection  " + watch.ElapsedMilliseconds);
                watch.Stop();
            }
        }
        static void DealRecord1()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            string[] collection = new string[] { "0", "1", "2", "3" };
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            foreach (string c in collection)
            {
                IMongoCollection<DnsRecordsSimple> categoriesD = db.GetCollection<DnsRecordsSimple>(c);
                List<DnsRecordsSimple> dlist = categoriesD.Find(Builders<DnsRecordsSimple>.Filter.Empty).ToList<DnsRecordsSimple>();
                int count = 0;
                watch.Start();
                List<ZonesSet> zhlist = new List<ZonesSet>();
                foreach (DnsRecordsSimple d in dlist)
                {
                    ZonesSet zh = new ZonesSet();
                    zh.Key = d.domain;
                    zh.RedisValue = JsonConvert.SerializeObject(d);
                    zhlist.Add(zh);
                    count++;
                    if (zhlist.Count == 50000 || count == dlist.Count)
                    {
                        var batch = Redis.DB(2).CreateBatch();
                        foreach (var item in zhlist)
                        {
                            batch.SetAddAsync(item.Key, item.RedisValue);
                        }
                        batch.Execute();
                        zhlist.Clear();
                        Console.WriteLine(c + "   collection deal " + count + "  time" + watch.ElapsedMilliseconds);
                    }
                }
                Console.WriteLine(c + "   collection  " + watch.ElapsedMilliseconds);
                watch.Stop();
            }
        }
        static void DealRecord2()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            string[] collection = new string[] { "4", "5", "6", "7" };
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            foreach (string c in collection)
            {
                IMongoCollection<DnsRecordsSimple> categoriesD = db.GetCollection<DnsRecordsSimple>(c);
                List<DnsRecordsSimple> dlist = categoriesD.Find(Builders<DnsRecordsSimple>.Filter.Empty).ToList<DnsRecordsSimple>();
                int count = 0;
                watch.Start();
                List<ZonesSet> zhlist = new List<ZonesSet>();
                foreach (DnsRecordsSimple d in dlist)
                {
                    ZonesSet zh = new ZonesSet();
                    zh.Key = d.domain;
                    zh.RedisValue = JsonConvert.SerializeObject(d);
                    zhlist.Add(zh);
                    count++;
                    if (zhlist.Count == 50000 || count == dlist.Count)
                    {
                        var batch = Redis.DB(2).CreateBatch();
                        foreach (var item in zhlist)
                        {
                            batch.SetAddAsync(item.Key, item.RedisValue);
                        }
                        batch.Execute();
                        zhlist.Clear();
                        Console.WriteLine(c + "   collection deal " + count + "  time" + watch.ElapsedMilliseconds);
                    }
                }
                Console.WriteLine(c + "   collection  " + watch.ElapsedMilliseconds);
                watch.Stop();
            }
        }
        static void DealRecord3()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            string[] collection = new string[] {  "8", "9", "a", "b" };
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            foreach (string c in collection)
            {
                IMongoCollection<DnsRecordsSimple> categoriesD = db.GetCollection<DnsRecordsSimple>(c);
                List<DnsRecordsSimple> dlist = categoriesD.Find(Builders<DnsRecordsSimple>.Filter.Empty).ToList<DnsRecordsSimple>();
                int count = 0;
                watch.Start();
                List<ZonesSet> zhlist = new List<ZonesSet>();
                foreach (DnsRecordsSimple d in dlist)
                {
                    ZonesSet zh = new ZonesSet();
                    zh.Key = d.domain;
                    zh.RedisValue = JsonConvert.SerializeObject(d);
                    zhlist.Add(zh);
                    count++;
                    if (zhlist.Count == 50000 || count == dlist.Count)
                    {
                        var batch = Redis.DB(2).CreateBatch();
                        foreach (var item in zhlist)
                        {
                            batch.SetAddAsync(item.Key, item.RedisValue);
                        }
                        batch.Execute();
                        zhlist.Clear();
                        Console.WriteLine(c + "   collection deal " + count + "  time" + watch.ElapsedMilliseconds);
                    }
                }
                Console.WriteLine(c + "   collection  " + watch.ElapsedMilliseconds);
                watch.Stop();
            }
        }
        static void DealRecord4()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            string[] collection = new string[] { "c", "d", "e", "f" };
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            foreach (string c in collection)
            {
                IMongoCollection<DnsRecordsSimple> categoriesD = db.GetCollection<DnsRecordsSimple>(c);
                List<DnsRecordsSimple> dlist = categoriesD.Find(Builders<DnsRecordsSimple>.Filter.Empty).ToList<DnsRecordsSimple>();
                int count = 0;
                watch.Start();
                List<ZonesSet> zhlist = new List<ZonesSet>();
                foreach (DnsRecordsSimple d in dlist)
                {
                    ZonesSet zh = new ZonesSet();
                    zh.Key = d.domain;
                    zh.RedisValue = JsonConvert.SerializeObject(d);
                    zhlist.Add(zh);
                    count++;
                    if (zhlist.Count == 50000 || count == dlist.Count)
                    {
                        var batch = Redis.DB(2).CreateBatch();
                        foreach (var item in zhlist)
                        {
                            batch.SetAddAsync(item.Key, item.RedisValue);
                        }
                        batch.Execute();
                        zhlist.Clear();
                        Console.WriteLine(c + "   collection deal " + count + "  time" + watch.ElapsedMilliseconds);
                    }
                }
                Console.WriteLine(c + "   collection  " + watch.ElapsedMilliseconds);
                watch.Stop();
            }
        }
        static void DealView()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
           
            IMongoCollection<ViewIP> categories = db.GetCollection<ViewIP>("levelIp");
            List<ViewIP> list = categories.Find(Builders<ViewIP>.Filter.Empty).SortBy(v=>v.start).ToList<ViewIP>();
            int count = 0;
            watch.Start();
            List<ViewSet> vslist = new List<ViewSet>();
            foreach (ViewIP v in list)
            {
                //Redis.DB(0).SetAdd(v.level.ToString(), JsonConvert.SerializeObject(v));
                ViewSet vs = new ViewSet();
                vs.Key = v.level.ToString();
                vs.RedisValue = JsonConvert.SerializeObject(v);
                vslist.Add(vs);
                count++;
                if (vslist.Count == 10000 || count == list.Count)
                {
                    var batch = Redis.DB(0).CreateBatch();
                    foreach (var item in vslist)
                    {
                        //batch.SortedSetAddAsync(item.Key, item.RedisValue,JsonConvert.DeserializeObject<ViewIP>(item.RedisValue).start);
                        batch.ListRightPushAsync(item.Key, item.RedisValue);
                    }
                    batch.Execute();
                    vslist.Clear();
                    Console.WriteLine("deal " + count + "  time" + watch.ElapsedMilliseconds);
                }
            }
            watch.Stop();
        }
        static void DealZonesSimple1()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            IMongoCollection<ZonesSimple> categoriesZ = db.GetCollection<ZonesSimple>("zones");

            List<ZonesSimple> zlist = categoriesZ.Find(Builders<ZonesSimple>.Filter.Empty).Skip(0).Limit(1000000).ToList<ZonesSimple>();
            Console.WriteLine("getdate");
            watch.Start();//开始计时 
            int count = 0;
            List<ZonesHash> zhlist = new List<ZonesHash>();
            foreach (ZonesSimple d in zlist)
            {
                ZonesHash zh = new ZonesHash();
                zh.Key = d.domain;
                zh.EntryArry = Model.GetEntrys<ZonesSimple>(d);
                zhlist.Add(zh);
                count++;
                if (zhlist.Count == 100000 || count == zlist.Count)
                {
                    var batch = Redis.DB(1).CreateBatch();
                    foreach (var item in zhlist)
                    {
                        batch.HashSetAsync(item.Key, item.EntryArry);
                    }
                    batch.Execute();
                    zhlist.Clear();
                    Console.WriteLine(count + "  time" + watch.ElapsedMilliseconds);
                }
            }
        }
        static void DealZonesSimple2()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            IMongoCollection<ZonesSimple> categoriesZ = db.GetCollection<ZonesSimple>("zones");

            List<ZonesSimple> zlist = categoriesZ.Find(Builders<ZonesSimple>.Filter.Empty).Skip(1000000).ToList<ZonesSimple>();
            Console.WriteLine("getdate");
            watch.Start();//开始计时 
            int count = 0;
            List<ZonesHash> zhlist = new List<ZonesHash>();
            foreach (ZonesSimple d in zlist)
            {
                ZonesHash zh = new ZonesHash();
                zh.Key = d.domain;
                zh.EntryArry = Model.GetEntrys<ZonesSimple>(d);
                zhlist.Add(zh);
                count++;
                if (zhlist.Count == 100000 || count == zlist.Count)
                {
                    var batch = Redis.DB(1).CreateBatch();
                    foreach (var item in zhlist)
                    {
                        batch.HashSetAsync(item.Key, item.EntryArry);
                    }
                    batch.Execute();
                    zhlist.Clear();
                    Console.WriteLine(count + "  time" + watch.ElapsedMilliseconds);
                }
            }
        }
    }
}