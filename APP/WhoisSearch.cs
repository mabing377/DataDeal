using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utility;

namespace APP
{
    public partial class WhoisSearch : Form
    {
        public WhoisSearch()
        {
            InitializeComponent();
        }
        public void StartThread()
        {
            //string baseDic = AppDomain.CurrentDomain.BaseDirectory;
            //if (File.Exists(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql"))
            //{
            //    File.Delete(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql");
            //}            
            //Directory.CreateDirectory(Path.GetDirectoryName(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql"));


            //MySQLHelper.ExecuteSql("truncate table zonestemp");
            //int r = MySQLHelper.ExecuteSql("INSERT INTO zonestemp(ID, Zone, GroupID, Active, UserID, DomainLevel, TempDomainLevel, StartDate, EndDate, Password, State, ValidateRank, CreateTime, SiteID, NSState, CheckTime, ActivityTime, FatherZoneID, RecordID, NSLastCheck, ContentLevel, UseCount, RZone, IsBindNS, PartnerAccount, LastName, KFTime, DisplayZone, TempLevelTerm, ForceStop, IsDelete, NoArrest, DNSPriority)SELECT ID, Zone, GroupID, Active, UserID, DomainLevel, TempDomainLevel, StartDate, EndDate, Password, State, ValidateRank, CreateTime, SiteID, NSState, CheckTime, ActivityTime, FatherZoneID, RecordID, NSLastCheck, ContentLevel, UseCount, RZone, IsBindNS, PartnerAccount, LastName, KFTime, DisplayZone, TempLevelTerm, ForceStop, IsDelete, NoArrest, DNSPriority FROM zones");
            //if (r > 0)
            //{
            DataSet ds = MySQLHelper.Query("select zone from zonestemp where nsstate=1 and isload=0 limit 0,100");
          
            DataTable dt = ds.Tables[0];
            long count = dt.Rows.Count;
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时
            int isright = 0,dealt=0;               
            foreach (DataRow dr in dt.Rows) {
                string zone = dr[0].ToString();
                string whoisinfo = WhoisHelper.SearchAWhois(zone);
                string servername = WhoisHelper.GetDnsServersStr(whoisinfo);
                if (servername.ToLower().Contains("xundns.com"))
                {
                    MySQLHelper.ExecuteSql("update zonestemp set isload=1 where zone='" + zone + "'");
                    isright++;
                }
                else
                {
                    MySQLHelper.ExecuteSql("update zonestemp set isload=-1 where zone='" + zone + "'");
                }
                dealt++;
                Thread.Sleep(100);
                //if (dealt % 10 == 0 && dealt > 0) {
                    this.textBox1.Text = this.textBox1.Text +dealt.ToString()+"Row Query;  "+ isright.ToString() + " Rows Updated;  Use Time " + watch.ElapsedMilliseconds.ToString()+Environment.NewLine;
                //}
            }
            this.textBox1.Text = this.textBox1.Text + dealt.ToString() + "Row Query;  " + isright.ToString() + " Rows Updated;  Use Time " + watch.ElapsedMilliseconds.ToString();
            watch.Stop();
            //}
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartThread();
        }
        private void TestDig()
        {
            string[] dnsserver = new string[] { "114.114.114.114", "117.50.11.11", "180.76.76.76", "210.2.4.8", "101.226.4.6", "123.125.81.6", "202.141.162.123", "123.206.61.167", "101.6.6.6", "123.206.61.167" };
            foreach (string d in dnsserver)
            {
                try
                {
                    DNS.Client.ClientResponse respon = new DNS.Client.DnsClient(d).Resolve("0-03.cn", DNS.Protocol.RecordType.NS);
                    if (respon.AnswerRecords.Count > 0)
                    {
                        DNS.Protocol.ResourceRecords.NameServerResourceRecord r = (DNS.Protocol.ResourceRecords.NameServerResourceRecord)respon.AnswerRecords[0];
                        this.textBox1.Text =this.textBox1.Text+ d + "   " + r.NSDomainName + Environment.NewLine;
                    }
                }
                catch (Exception ex)
                {
                    this.textBox1.Text = this.textBox1.Text + d + Environment.NewLine;
                }
                //textBox1.Text = JsonConvert.SerializeObject(respon.AnswerRecords);
            }

            //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            //watch.Start();//开始计时
            //DataSet ds = MySQLHelper.Query("select zone from zonestemp where nsstate=1 and isload=0 limit 0,100");

            //DataTable dt = ds.Tables[0];
            //long count = dt.Rows.Count;
            //int isright = 0;
            //int iswrong = 0;
            //for (int idx = 0; idx < count; idx++)
            //{
            //    DataRow dr = dt.Rows[idx];
            //    string zone = dr[0].ToString();
            //    int aidx = idx % 10;
            //    try
            //    {
            //        DNS.Client.ClientResponse respon = new DNS.Client.DnsClient(dnsserver[aidx]).Resolve(zone, DNS.Protocol.RecordType.NS);
            //        if (respon.AnswerRecords.Count > 0)
            //        {
            //            DNS.Protocol.ResourceRecords.NameServerResourceRecord r = (DNS.Protocol.ResourceRecords.NameServerResourceRecord)respon.AnswerRecords[0];
            //            if (r.NSDomainName.ToString().ToLower().Contains("xundns.com"))
            //            {
            //                MySQLHelper.ExecuteSql("update zonestemp set isload=1 where zone='" + zone + "'");
            //                isright++;
            //            }
            //            else
            //                MySQLHelper.ExecuteSql("update zonestemp set isload=-1 where zone='" + zone + "'");
            //        }
            //        else { iswrong++; }
            //    }
            //    catch (Exception ex)
            //    {
            //        Write2File.WriteLogToFile(zone+"   use "+ dnsserver[aidx] +"   "+ ex.ToString());
            //        WhoisDealing(zone);
            //        TestDig();
            //    }
            //}
            //this.textBox1.Text = Environment.NewLine + this.textBox1.Text + "mission over";
            //if (Convert.ToInt32(MySQLHelper.Query("select count(1) from zonestemp where nsstate=1 and isload=0 limit 0,100").Tables[0].Rows[0][0]) > 0)
            //    TestDig();
            //watch.Stop();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "Log\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            if (File.Exists(path))
                File.Delete(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            TestDig();
        }

        private void WhoisDealing(string zone) {
            try
            {
                string whoisinfo = WhoisHelper.SearchAWhois(zone);
                string servername = WhoisHelper.GetDnsServersStr(whoisinfo);
                if (servername.ToLower().Contains("xundns.com"))
                {
                    MySQLHelper.ExecuteSql("update zonestemp set isload=1 where zone='" + zone + "'");
                }
                else
                {
                    MySQLHelper.ExecuteSql("update zonestemp set isload=-1 where zone='" + zone + "'");
                }
            }
            catch(Exception ex)
            {
                MySQLHelper.ExecuteSql("update zonestemp set isload=-2 where zone='" + zone + "'");
                Write2File.WriteLogToFile(zone + "   whoisException    " + ex.ToString());
            }
        }
    }
}
//