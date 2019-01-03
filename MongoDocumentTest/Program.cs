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

namespace MongoDocumentTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();//开始计时  
                DataTable dtz = MySQLHelper.Query("SELECT * from zones where id=3118661").Tables[0];
                DataTable dta = MySQLHelper.Query("SELECT * from authorities where zoneid=3118661").Tables[0];
                DataTable dtd = MySQLHelper.Query("SELECT * from dnsrecords where zoneid=3118661").Tables[0];

                List<Zones> zonesList = DtToList<Zones>.ConvertToModel(dtz);

                Zones zone = zonesList[0];
                ZonesEntity ze = new ZonesEntity();
                ze.id = Utility.StringHelper.CalculateMD5Hash(zone.Zone + ".").ToLower();
                ze.userid = Convert.ToInt32(zone.UserID);
                ze.domain = zone.Zone;
                ze.level = zone.DomainLevel > 0 ? 1 : 0;
                ze.nsstate = zone.NSState;
                ze.is_stop = zone.Active == "Y" ? "N" : "Y";
                ze.force_stop = zone.ForceStop;
                ze.rdomain = zone.RZone;
                List<AuthEntity> alist = new List<AuthEntity>();
                foreach (DataRow dr in dta.Rows) {
                    AuthEntity ae = new AuthEntity();
                    ae.rid = Convert.ToInt32(dr["id"]);
                    ae.domain = ze.domain;
                    ae.name = dr["host"].ToString();
                    ae.type = dr["type"].ToString();
                    if (ae.type == "SOA")
                        //Mbox,Serial,Refresh,Retry,Expire,Minimum
                        ae.rdata = dr["data"].ToString() + " " + dr["mbox"].ToString() + " " + dr["serial"].ToString() + " " + dr["refresh"].ToString() + " " + dr["retry"].ToString() + " " + dr["expire"].ToString() + " " + dr["minimum"].ToString();
                    else
                        ae.rdata = dr["data"].ToString();
                    ae.ttl = Convert.ToInt32(dr["ttl"]);
                    ae.view = "Def";
                    ae.view = "Y";
                    alist.Add(ae);
                }
                List<RecordEntity> rlist = new List<RecordEntity>();
                foreach (DataRow dr in dtd.Rows)
                {
                    RecordEntity re = new RecordEntity();
                    re.rid = Convert.ToInt32(dr["id"]);
                    re.domain = ze.domain;
                    re.name = dr["host"].ToString();
                    re.type = dr["type"].ToString();
                    if (re.type == "MX")
                        //Mbox,Serial,Refresh,Retry,Expire,Minimum
                        re.rdata = dr["mx_priority"].ToString() + " " + dr["data"].ToString();
                    else if (re.type == "TXT")
                        re.rdata = dr["data"].ToString().Replace("\"", string.Empty);
                    else
                        re.rdata = dr["data"].ToString();
                    re.ttl = Convert.ToInt32(dr["ttl"]);
                    re.view = dr["view"].ToString();
                    re.view = dr["active"].ToString() == "Y" ? "N" : "Y";
                    rlist.Add(re);
                }
                ze.authorities = alist;
                ze.records = rlist;
                var client = DriverConfiguration.Client;
                var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
                IMongoCollection<ZonesEntity> categories = db.GetCollection<ZonesEntity>("ZonesEntiy");


                categories.InsertOne(ze);
                Console.WriteLine("MongoDB Inserted;               Use time={0};", watch.ElapsedMilliseconds);

                watch.Stop();//停止计时
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
             
        }
    }
}
