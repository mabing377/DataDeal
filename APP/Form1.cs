using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB;
using Utility;
using System.Windows.Forms;
using System.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reflection;

namespace APP
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //DataSet ds = MySQLHelper.Query("SELECT zone,UserID from zonestemp where nsstate=1 limit 0,10000");
            DataSet ds = MySQLHelper.Query("SELECT count(1) from zonestemp where nsstate=1");
            MessageBox.Show(ds.Tables[0].Rows[0][0].ToString());
            BindUser();
        }
        /// <summary>
        /// BLDomain.cs 1400
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="groupID"></param>
        /// <param name="dkey"></param>
        /// <param name="isBMD"></param>
        /// <param name="isAndHMD"></param>
        /// <param name="dmState"></param>
        /// <param name="domainLevel"></param>
        /// <param name="contentLevel"></param>
        /// <param name="isOverVIP"></param>
        /// <param name="willOverVipDay"></param>
        /// <param name="includeBindDomain"></param>
        /// <param name="onlyBindDomain"></param>
        /// <param name="onlyNsRight"></param>
        /// <param name="lastKefuDay"></param>
        /// <param name="partnerAccount"></param>
        /// <param name="skipNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="px"></param>
        /// <param name="otherWhere"></param>
        /// <returns></returns>
        public static ListPageData<Zones> SelectDomainList(long userID, int groupID, string dkey, bool isBMD, bool isAndHMD, int dmState, int domainLevel, short contentLevel, bool isOverVIP, int willOverVipDay, bool includeBindDomain, bool onlyBindDomain, bool onlyNsRight, int lastKefuDay, string partnerAccount, int skipNumber, int pageSize, int px, Document otherWhere)
        {
            ListPageData<Zones> selResult = new ListPageData<Zones>();

            var doc = new Document();
             //string tj3= "";
            if (userID > 0)
            {
                doc.Add("UserID",userID);
                // tj3= tj3 + " and UserID='" + userID + "'";
            }
            if (groupID > 0)
            {
                doc.Add("GroupID", groupID);
                // tj3= tj3 + " and GroupID='" + groupID + "'";
            }
            if (!string.IsNullOrEmpty(dkey))
            {
                doc.Add("Zone", dkey);
                // tj3= tj3 + " and Zone ='" + dkey + "'";
            }

            if (isBMD)
            {
                doc.Add("ValidateRank", 20);
                // tj3= tj3 + " and ValidateRank=20";
            }

            if (!isAndHMD)
            {
                doc.Add("state", new Document("$ne",2));
                // tj3= tj3 + " and state<>2";
            }

            if (dmState >= 0)
            {
                doc.Add("state", dmState);
                // tj3= tj3 + " and state=" + dmState;
            }

            if (!includeBindDomain)
            {
                doc.Add("IsBindNS", 0);
                //不包含绑定的域名
                // tj3= tj3 + " and IsBindNS=0";
            }

            if (onlyBindDomain)
            {
                doc.Add("IsBindNS", 1);
                //只获取绑定的域名
                // tj3= tj3 + " and IsBindNS=1";
            }

            if (onlyNsRight)
            {
                doc.Add("$or", new Document("NSState", 1).Add("NSState", 6));
                //只获取NS正确的域名
                // tj3= tj3 + " and (NSState=1 or NSState=6)";
            }

            if (lastKefuDay > 0)
            {
                DateTime breakKefuDay = DateTime.Now.Date.AddDays(-lastKefuDay);
                doc.Add("KFTime", new Document("$lt", breakKefuDay));
                // tj3= tj3 + " and KFTime<'" + breakKefuDay + "'";
            }
            else if (lastKefuDay < 0)
            {
                DateTime breakKefuDay = DateTime.Now.Date.AddDays(lastKefuDay);
                doc.Add("KFTime", new Document("$gt", breakKefuDay));
                // tj3= tj3 + " and KFTime>'" + breakKefuDay + "'";
            }

            if (!string.IsNullOrEmpty(partnerAccount))
            {
                doc.Add("PartnerAccount", partnerAccount);
                //只获取某一个合作伙伴名下的域名
                // tj3= tj3 + " and PartnerAccount='" + partnerAccount + "'";
            }

            if (domainLevel > -1 && domainLevel < 1000)
            {
                doc.Add("DomainLevel", domainLevel);
                // tj3= tj3 + " and DomainLevel=" + domainLevel;
            }

            if (domainLevel == 10000)
            {
                doc.Add("DomainLevel", new Document("$gt",0));
                // tj3= tj3 + " and DomainLevel>0";
            }

            if (contentLevel > -1)
            {
                doc.Add("ContentLevel", contentLevel);
                // tj3= tj3 + " and ContentLevel=" + contentLevel;
            }

            if (otherWhere !=null)
            {
                otherWhere.CopyTo(doc);
                // tj3= tj3 + "and " + otherWhere;
            }

            if (isOverVIP)
            {
                if (willOverVipDay < 0)
                    willOverVipDay = 5;
                doc.Add("DomainLevel", new Document("$gt", 0)).Add("EndDate", new Document("$lt", DateTime.Now.AddDays(willOverVipDay).Date)).Add("state", new Document("$ne",6));
                // tj3= tj3 + " and DomainLevel>0 and EndDate<'" + DateTime.Now.AddDays(willOverVipDay).Date + "' and state<>6";
                px = 2;
            }

            //string tj5 = "FatherZoneID=0";
            doc.Add("FatherZoneID", 0);

            var sort = new Document("id", -1);
            string pxStr = "id desc";

            switch (px)
            {
                case 2:
                    pxStr = "EndDate";
                    sort = new Document("EndDate", 1);
                    break;
                case 3:
                    pxStr = "UseCount desc";
                    sort = new Document("UseCount", -1);
                    break;
            }
            
            int countDM = Convert.ToInt32(MongoHelper.GetCount<Zones>("zones", doc));
            if (pageSize > 0)
                selResult.DataArray = MongoHelper.GetList<Zones>("zones", doc, sort, skipNumber, pageSize).ToArray<Zones>();
            selResult.PageSize = pageSize;
            selResult.DataCount = countDM;
            return selResult;
        }


        private void button3_Click(object sender, EventArgs e)
        {

            ListPageData<DnsRescords> drl = SelectDomainListTest(2, 5);
            this.dataGridView1.DataSource = drl.DataList;
        }
        public static ListPageData<DnsRescords> SelectDomainListTest(int skipNumber, int pageSize)
        {
            ListPageData<DnsRescords> selResult = new ListPageData<DnsRescords>();

            var doc = new Document();
            doc.Add("zone", "cs004.com");

            var sort = new Document("host", -1);
            var selectdoc = new Document("host", 1).Add("zone", 1).Add("data", 1);
            int countDM = Convert.ToInt32(MongoHelper.GetCount<DnsRescords>("dnsrecords", doc));
            //selResult.DataList = MongoHelper.GetList<DnsRescords>("dnsrecords", doc, sort, skipNumber, pageSize);
            selResult.DataList = MongoHelper.GetList<DnsRescords>("dnsrecords",  doc,selectdoc, sort, skipNumber, pageSize);
            selResult.PageSize = pageSize;
            selResult.DataCount = countDM;
            return selResult;
        }



        /// <summary>
        /// 直接更新一个域名的状态信息  BLDomain.cs 1624
        /// </summary>
        /// <param name="zoneid">域名ID</param>
        /// <param name="newState">新状态（-1表示不修改状态）</param>
        /// <param name="active">是否解析（""表示不修改解析状态）</param>
        /// <param name="remark">备注</param>
        public static void AdminADomain(int  id, int newState, int newCKRank, string active)
        {
            string setData = "";
            var setdoc = new Document();
            if (newState > -1)
            {
                setdoc.Add("state", newState);
            }

            if (newCKRank > -1)
            {
                setdoc.Add("validaterand", newCKRank);
            }

            if (active != "" && (active == "Y" || active == "N"))
            {
                setdoc.Add("active", active);            
            }


            var wheredoc = new Document("id", id);
            if (setData != null)
            {
                MongoHelper.Update<Zones>("zones", setdoc, wheredoc);
                MessageBox.Show("end");
            }
        }
        public void InsertData() {
            Document d = new Document();
            for (int i = 0; i < 10; i++) {
                d = new Document {
                    { "ID",i.ToString()+"0000" },
                    { "Name","zhangsan"+i.ToString() },
                    { "Sex",i%2 },
                    { "Age",(i+20) }
                };
                MongoHelper.InsertOne<Document>("User",d);
                d.Clear();
            }
            MessageBox.Show("end");
        }
        public void UpdateData()
        {
            var wheredoc = new Document("ID", "30000");
            var setdoc = new Document("$set",new Document("Name", "sdafdgf"));
            
            MongoHelper.Update<User>("User", setdoc, wheredoc);
            //MongoHelper.Save<User>("User", u);
            MessageBox.Show("Complete");

            //var filter = Builders<BsonDocument>.Filter.Eq("counter", 1);
            //var updated = Builders<BsonDocument>.Update.Set("counter", 110);
            //MongoCollection collection = new MongoCollection();
            //var result = collection.UpdateOneAsync(filter, updated).Result;

            BindUser();
        }
        public void BindUser() {
            var ul = MongoHelper.GetAllByField<User>("User",new Document("Sex",1));
            this.dataGridView1.DataSource = ul;
           
        }
        #region 测试数据生成


        public void CreateZonesTestData() {
            DataSet ds = MySQLHelper.Query("SELECT ID, Zone, GroupID, Active, UserID, DomainLevel, TempDomainLevel, StartDate, EndDate, Password, State, ValidateRank, CreateTime, SiteID, NSState, CheckTime, ActivityTime, FatherZoneID, RecordID, NSLastCheck, ContentLevel, UseCount, RZone, IsBindNS, PartnerAccount, LastName, KFTime, DisplayZone, TempLevelTerm, ForceStop, IsDelete, NoArrest, DNSPriority FROM zones where nsstate=1");
            DataTable dt = ds.Tables[0];
            long count = dt.Rows.Count;
            int idx = 0;
            MessageBox.Show(count + " rows data");

            using (Mongo mongo = new Mongo(Utility.MongoHelper.connectionString))
            {
                mongo.Connect();

                while (idx < count)
                {
                    List<Zones> dl = new List<Zones>();
                    while (idx < count && dl.Count < 1001)
                    {
                        dl.Add(Row2Zone(dt.Rows[idx]));
                        idx++;
                    }
                    MongoDB.IMongoDatabase friends = mongo.GetDatabase(Utility.MongoHelper.database);
                    MongoDB.IMongoCollection<Zones> categories = friends.GetCollection<Zones>("zones");
                    categories.Insert(dl, true);
                    dl.Clear();
                }
                MessageBox.Show(idx + " mission over");

                mongo.Disconnect();

            }
        }
        static Zones Row2Zone(DataRow dr)
        {
            Zones z = new Zones();
            z.id = Convert.ToInt32(dr[0].ToString());
            z.zone = dr[1].ToString();
            z.groupid = Convert.ToInt32(dr[2].ToString());
            z.active = dr[3].ToString();
            z.userid = Convert.ToInt32(dr[4].ToString());
            z.domainlevel = Convert.ToInt32(dr[5].ToString());
            z.tempdomainlevel = Convert.ToInt32(dr[6].ToString());
            z.starttime = Convert.ToDateTime(dr[7].ToString());
            z.endtime = Convert.ToDateTime(dr[8].ToString());
            z.password = dr[9].ToString();
            z.state = Convert.ToInt32(dr[10].ToString());
            z.validaterand = Convert.ToInt32(dr[11].ToString());
            z.createtime = Convert.ToDateTime(dr[12].ToString());
            z.siteid = dr[13].ToString();
            z.nsstate = Convert.ToInt32(dr[14].ToString());
            z.checktime = Convert.ToDateTime(dr[15].ToString());
            z.activitytime = Convert.ToDateTime(dr[16].ToString());
            z.fatherzoneid = Convert.ToInt32(dr[17].ToString());
            z.recordid = Convert.ToInt32(dr[18].ToString());
            z.nslastcheck = Convert.ToDateTime(dr[19].ToString());
            z.contentlevel = Convert.ToInt32(dr[20].ToString());
            z.usecount = Convert.ToInt32(dr[21].ToString());
            z.rzone = dr[22].ToString();
            z.isbindns = Convert.ToBoolean(dr[23]) == false ? 0 : 1;
            z.partneraccount = dr[24].ToString();
            z.lastname = dr[25].ToString();
            z.kftime = Convert.ToDateTime(dr[26].ToString());
            z.displayzone = dr[27].ToString();
            z.templevelterm = Convert.ToDateTime(dr[28].ToString());
            z.forcestop = dr[29].ToString();
            z.isdelete = Convert.ToBoolean(dr[30]);
            z.noarrest = dr[31].ToString();
            z.dnspriority = Convert.ToInt32(dr[32].ToString());
            return z;
        }

        public void CreateDnsRecordsTestData()
        {
            DataSet ds = MySQLHelper.Query("SELECT t1.ID, t2.UserID, t2.ZoneID, t1.Zone, t1.Host, t1.Type, t1.Mx_priority, t1.Data, t1.TTL, t1.View, t2.Active, t2.DomainLevel, t2.Standby, t2.CheckHostID, t2.IsFensheng, t2.URLID, t2.Str16 FROM dnsrecordsregular as t1 LEFT join dnsrecords as t2 on t1.ID=t2.ID");
            DataTable dt = ds.Tables[0];
            long count = dt.Rows.Count;
            int idx = 0;
            MessageBox.Show(count + " rows data");

            using (Mongo mongo = new Mongo(Utility.MongoHelper.connectionString))
            {
                mongo.Connect();

                while (idx < count)
                {
                    List<DnsRescords> dl = new List<DnsRescords>();
                    while (idx < count && dl.Count < 1001)
                    {
                        dl.Add(Row2DnsRecords(dt.Rows[idx]));
                        idx++;
                    }
                    MongoDB.IMongoDatabase friends = mongo.GetDatabase(Utility.MongoHelper.database);
                    MongoDB.IMongoCollection<DnsRescords> categories = friends.GetCollection<DnsRescords>("dnsrescords");
                    categories.Insert(dl, true);
                    dl.Clear();
                }
                MessageBox.Show(idx + " mission over");

                mongo.Disconnect();

            }
        }
        static DnsRescords Row2DnsRecords(DataRow dr)
        {

            DnsRescords d = new DnsRescords();

            d.id = Convert.ToInt32(dr[0].ToString());
            d.userid = Convert.ToInt32(dr[1].ToString());
            d.zoneid= Convert.ToInt32(dr[2].ToString());
            d.zone=dr[3].ToString();
            d.host=dr[4].ToString();
            d.type=dr[5].ToString();
            if(dr[6]!=DBNull.Value)
                d.mx_priority= Convert.ToInt32(dr[6].ToString());
            d.data=dr[7].ToString();
            if (dr[8] != DBNull.Value)
                d.ttl= Convert.ToInt32(dr[8].ToString());
            d.view=dr[9].ToString();
            d.active=dr[10].ToString();
            d.domainlevel= Convert.ToInt32(dr[11].ToString());
            d.standby=dr[12].ToString();
            d.checkhostid= Convert.ToInt32(dr[13].ToString());
            d.isfensheng= Convert.ToInt32(dr[14].ToString());
            d.urlid= Convert.ToInt32(dr[15].ToString());
            d.str16 = dr[16].ToString();
            return d;
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            CreateZonesTestData();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CreateDnsRecordsTestData();
        }
        

        /// <summary>
        /// mongodb - demo
        /// </summary>
        public void Poll()
        {
            int PageSize = 2000;
            int CurrentPage = 1;
            //范围
            int Range = 2000;//单位米
            List<Domain> result = new List<Domain>();
            var doc = new Document
            {
                { "TestTime", new Document("$gt", DateTime.Now) },
                { "IsDelete", false }
            };
            //var doc = new Document();
            //doc.Add("TestTime", new Document("$gt", DateTime.Now));
            //doc.Add("IsDelete", false);
            var sort = new Document();
            sort.Add("CreateTime", 1);
            bool IsBreak = false;
            while (!IsBreak)
            {
                List<Domain> DataList = MongoHelper.GetList<Domain>("dnsrecords", doc, sort, CurrentPage, PageSize);
                foreach (var data in DataList)
                {
                    //防止匹配以后再次匹配
                    if (result.Select(x => x.ID).Contains(data.ID))
                        continue;

                    var OriginPointDC = new Document();
                    //起点匹配
                    OriginPointDC.Add("TestTime", new Document("$gt", data.TestTime.AddMinutes(-30)).Add("$lt", data.TestTime.AddMinutes(30)));
                    OriginPointDC.Add("IsDelete", false);
                    OriginPointDC.Add("ID", new Document("$ne", data.ID));
                    OriginPointDC.Add("OriginPoint", new Document("$near", new Document("$geometry", new Document("type", "Point").Add("Ttl", data.Ttl)).Add("$maxDistance", Range)));

                    List<Domain> ReturnList = MongoHelper.GetList<Domain>("dnsrecords", null, OriginPointDC, 1, 100000000);


                    var TerminalPointDC = new Document();
                    //终点匹配（因为mongodb不能一次匹配两个地理位置，只能一个个匹配，匹配终点，起点，再进一步进行筛选）
                    TerminalPointDC.Add("TestTime", new Document("$gt", data.TestTime.AddMinutes(-30)).Add("$lt", data.TestTime.AddMinutes(30)));
                    TerminalPointDC.Add("IsDelete", false);
                    TerminalPointDC.Add("ID", new Document("$ne", data.ID));
                    TerminalPointDC.Add("TerminalPoint", new Document("$near", new Document("$geometry", new Document("type", "Point").Add("Ttl", data.Ttl)).Add("$maxDistance", Range)));

                    List<Domain> ReturnList2 = MongoHelper.GetList<Domain>("dnsrecords", null, OriginPointDC, 1, 100000000);

                    var isT = false;
                    foreach (var r in ReturnList)
                    {
                        foreach (var r2 in ReturnList2)
                        {
                            //必须判断result是否已经存在该数据
                            if (r.ID == r2.ID && !result.Select(x => x.ID).Contains(r.ID))
                            {
                                //data.BatchID = Guid.NewGuid();
                                //r.BatchID = data.BatchID;
                                //result.Add(data);
                                //result.Add(r);
                                //isT = true; break;
                            }
                        }
                        if (isT) break;
                    }
                }
                CurrentPage++;
                if (DataList.Count() < PageSize)
                {
                    IsBreak = true;
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            InsertData();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            UpdateData();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            UpdateMany();
        }
        public void UpdateMany()
        {

            var wheredoc = new Document("Sex", 0);
            var setdoc = new Document("$set",new Document("Age",20));
            MongoHelper.UpdateWithColumn<User>("User", setdoc, wheredoc);
            MessageBox.Show("Complete");

            //var filter = Builders<BsonDocument>.Filter.Eq("counter", 1);
            //var updated = Builders<BsonDocument>.Update.Set("counter", 110);
            //MongoCollection collection = new MongoCollection();
            //var result = collection.UpdateOneAsync(filter, updated).Result;

            BindUser();
        }
    }
    public class DnsRescords 
    {
        public int id { get; set; }
        public int userid { get; set; }
        public int zoneid { get; set; }
        public string zone { get; set; }
        public int mx_priority { get; set; } = 0;
        public string active { get; set; } //enum('Y','N') NOT NULL DEFAULT 'Y',
        public int domainlevel { get; set; } //int (11) NOT NULL DEFAULT '1',
        public string standby { get; set; }
        public int checkhostid { get; set; } = 0;// bigint(20) NOT NULL DEFAULT '0',
        public int isfensheng { get; set; } = 0;// bigint(20) NOT NULL DEFAULT '0',
        public int urlid { get; set; } = 0;// bigint(20) NOT NULL DEFAULT '0',
        public string str16 { get; set; }
        public string host { get; set; }
        public string type { get; set; }// enum('A','SOA','NS','MX','CNAME','PTR','TXT','SRV','AAAA') NOT NULL COMMENT '类型',
        public string data { get; set; }
        public int ttl { get; set; } = 600;
        public string view { get; set; } = "def";
    }
    public class Zones
    {
        public string zone { get; set; }
        public int groupid { get; set; }
        public string active { get; set; }
        public int userid { get; set; }
        public int domainlevel { get; set; }
        public int tempdomainlevel { get; set; }
        public DateTime starttime { get; set; }
        public DateTime endtime { get; set; }
        public string password { get; set; }
        public int state { get; set; }
        public int validaterand { get; set; }
        public DateTime createtime { get; set; }
        public string siteid { get; set; }
        public int nsstate { get; set; }
        public DateTime checktime { get; set; }
        public DateTime activitytime { get; set; }
        public int fatherzoneid { get; set; }
        public int recordid { get; set; }
        public DateTime nslastcheck { get; set; }
        public int contentlevel { get; set; }
        public int usecount { get; set; }
        public string rzone { get; set; }
        public int isbindns { get; set; }
        public string partneraccount { get; set; }
        public string lastname { get; set; }
        public DateTime kftime { get; set; }
        public string displayzone { get; set; }
        public DateTime templevelterm { get; set; }
        public string forcestop { get; set; }
        public bool isdelete { get; set; } = false;
        public string noarrest { get; set; } = "N";
        public int dnspriority { get; set; }
        public int isload { get; set; } = 1;
        public int id { get; set; }
    }
    public class User
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int Sex { get; set; }
        public int Age { get; set; }
    }

}


//Query.All("name", "a", "b");//通过多个元素来匹配数组  
//Query.And(Query.EQ("name", "a"), Query.EQ("title", "t"));//同时满足多个条件  
//Query.EQ("name", "a");//等于  
//Query.Exists("type", true);//判断键值是否存在  
//Query.GT("value", 2);//大于>  
//Query.GTE("value", 3);//大于等于>=  
//Query.In("name", "a", "b");//包括指定的所有值,可以指定不同类型的条件和值  
//Query.LT("value", 9);//小于<  
//Query.LTE("value", 8);//小于等于<=  
//Query.Mod("value", 3, 1);//将查询值除以第一个给定值,若余数等于第二个给定值则返回该结果  
//Query.NE("name", "c");//不等于  
//Query.Nor(Array);//不包括数组中的值  
//Query.Not("name");//元素条件语句  
//Query.NotIn("name", "a", 2);//返回与数组中所有条件都不匹配的文档  
//Query.Or(Query.EQ("name", "a"), Query.EQ("title", "t"));//满足其中一个条件  
//Query.Size("name", 2);//给定键的长度  
//Query.Type("_id", BsonType.ObjectId );//给定键的类型  
//Query.Where(BsonJavaScript);//执行JavaScript  
//Query.Matches("Title",str);//模糊查询 相当于sql中like  -- str可包含正则表达式 