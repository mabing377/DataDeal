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


namespace Mongo2Redis
{
    class Program
    {
        static void Main(string[] args)
        {
            Redis.Init(ConfigurationManager.ConnectionStrings["Redis"].ToString());  //初始化Redis数据库
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时 
            string[] collection = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
            var client = DriverConfiguration.Client;
            var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
            IMongoCollection<ZonesSimple> categoriesZ = db.GetCollection<ZonesSimple>("zones");


            foreach (string c in collection)
            {
                if (c == "0")
                {
                    List<ZonesSimple> zlist = categoriesZ.Find(Builders<ZonesSimple>.Filter.Eq("rrcol", c)).ToList<ZonesSimple>();
                    Console.WriteLine("z"+zlist.Count +" Time="+ watch.ElapsedMilliseconds);
                    IMongoCollection<AuthoritiesSimple> categoriesA = db.GetCollection<AuthoritiesSimple>(c);
                    int zcount = 0;
                    List<string> domainList = new List<string>();
                    foreach (ZonesSimple z in zlist)
                    {
                        domainList.Add(z.domain);
                        zcount++;
                        if (zcount % 200 == 0)
                        {
                            List<AuthoritiesSimple> alist = categoriesA.Find(Builders<AuthoritiesSimple>.Filter.In("domain",domainList)).ToList<AuthoritiesSimple>();
                            foreach(string domain in domainList)
                            {
                                var talist = alist.FindAll(a => a.domain == domain).ToList<AuthoritiesSimple>();
                                RedisValue[] rArry = new RedisValue[talist.Count];
                                for (int i = 0; i < talist.Count; i++)
                                {
                                    rArry[i] = JsonConvert.SerializeObject(talist[i]);
                                }
                                Redis.DB(3).ListRightPush(domain, rArry);
                            }
                            Console.WriteLine(zcount + " Time=" + watch.ElapsedMilliseconds);
                        }
                    }
                }
            }
            Console.ReadKey();
        }
    }
}
