using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Configuration;
using System.IO;
using MongoDB;
using Utility;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace MySQL2MongoDB
{
    class Program
    {
        public static double tasksize = 0;
        public static int FileSize = Convert.ToInt32(ConfigurationManager.AppSettings["FileSize"]);
        public static int ThreadCount = Convert.ToInt32(ConfigurationManager.AppSettings["ThreadCount"]);
        static void Main(string[] args)
        {

            Console.WriteLine("程序功能：");
            Console.WriteLine("1-test ipv4;");
            Console.WriteLine("2-域名注册信息和解析状态查询并更新状态");
            Console.WriteLine("3-域名解析状态查询并更新状态");
            Console.Write("请输入对应的数字：");
            switchaction:
            CheckIPV4();
            goto switchaction;
            int input = Console.Read();
            string basepath = AppDomain.CurrentDomain.BaseDirectory;
            switch (input)
            {
                case 49:
                    CheckIPV4();
                    break;
                case 50:
                    break;
                case 51:
                    string path = basepath + "Log\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                    if (File.Exists(path))
                        File.Delete(path);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    QueryDNSAnalysis();
                    break;
                case 52:
                    break;
                default:
                    break;
            }
            Console.ReadKey();
        }
        static void CheckIPV4() {
            string CheckIPV4 = @"^(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])$";
            string input = Console.ReadLine();
            Console.WriteLine(Regex.IsMatch(input, CheckIPV4));
        }


    static void WhoisInfoDeal() {
            string baseDic = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql"))
            {
                File.Delete(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql"));
          
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时
            DataSet ds = MySQLHelper.Query("select zone from zonestemp where nsstate=1 limit 0,100");

            DataTable dt = ds.Tables[0];
            long count = dt.Rows.Count;
            Console.WriteLine("Query Success "+count.ToString()+"   use time " + watch.ElapsedMilliseconds.ToString());
            int isright = 0;
            int iswrong = 0;
            string[] dnsserver = new string[] { "114.114.114.114", "223.5.5.5", "180.76.76.76", "119.29.29.29", "101.226.4.6", "123.125.81.6", "202.141.162.123","123.206.61.167", "101.6.6.6","8.8.8.8" };
            for(int idx=0;idx<count;idx++)
            {
                DataRow dr = dt.Rows[idx];
                string zone = dr[0].ToString();
                if (zone.EndsWith(".info")||zone.EndsWith(".cn")) {
                    string nsname= WhoisHelper.GetDnsServersStr(WhoisHelper.SearchAWhois(zone));

                    if (nsname.ToLower().Contains("xundns.com"))
                    {
                        //Write2File.WriteToFile("update zonestemp set isload=1 where zone='" + zone + "'");
                        MySQLHelper.ExecuteSql("update zonestemp set isload=1 where zone='" + zone + "'");
                        Console.WriteLine(zone + " is here use time" + watch.ElapsedMilliseconds);
                        isright++;
                    }
                    else { iswrong++; }
                }
                else
                {
                    try
                    {
                        int aidx = idx % 10;
                        DNS.Client.ClientResponse respon = new DNS.Client.DnsClient(dnsserver[aidx]).Resolve(zone, DNS.Protocol.RecordType.NS);
                        if (respon.AnswerRecords.Count > 0)
                        {
                            DNS.Protocol.ResourceRecords.NameServerResourceRecord r = (DNS.Protocol.ResourceRecords.NameServerResourceRecord)respon.AnswerRecords[0];

                            if (r.NSDomainName.ToString().ToLower().Contains("xundns.com"))
                            {
                                Write2File.WriteToFile("update zonestemp set isload=1 where zone='" + zone + "'");
                                Console.WriteLine(zone + " is here use time" + watch.ElapsedMilliseconds);
                                isright++;
                            }
                        }
                        else { iswrong++; }
                        Thread.Sleep(500);
                    }
                    catch (Exception ex) {
                        string re = ex.ToString();
                    }
                }
                if (iswrong % 10 == 0 && iswrong > 0) {
                    Console.WriteLine(iswrong.ToString() + " is not here use time"+watch.ElapsedMilliseconds);
                }
            }
            Console.WriteLine(isright.ToString() + " Rows Updated;   use time" + watch.ElapsedMilliseconds.ToString());
            watch.Stop();
        }

        static void QueryDNSAnalysis() {

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();//开始计时
            DataSet ds = MySQLHelper.Query("select zone from zonestemp where nsstate not in(1,2,4) and isload=0 limit 0,100");

            DataTable dt = ds.Tables[0];
            long count = dt.Rows.Count;
            int isright = 0;
            string[] dnsserver = new string[] { "114.114.114.114", "117.50.11.11", "180.76.76.76", "210.2.4.8", "101.226.4.6", "123.125.81.6", "202.141.162.123", "123.206.61.167", "101.6.6.6", "123.206.61.167" };
            for (int idx = 0; idx < count; idx++)
            {
                string zone = dt.Rows[idx][0].ToString();
                int aidx = idx % 10;
                try
                {
                    DNS.Client.ClientResponse respon = new DNS.Client.DnsClient(dnsserver[aidx]).Resolve(zone, DNS.Protocol.RecordType.NS);
                    if (respon.AnswerRecords.Count > 0)
                    {
                        DNS.Protocol.ResourceRecords.NameServerResourceRecord r = (DNS.Protocol.ResourceRecords.NameServerResourceRecord)respon.AnswerRecords[0];
                        if (r.NSDomainName.ToString().ToLower().Contains("xundns.com"))
                        {
                            MySQLHelper.ExecuteSql("update zonestemp set isload=1 where zone='" + zone + "'");
                            isright++;
                        }
                        else
                            MySQLHelper.ExecuteSql("update zonestemp set isload=-1 where zone='" + zone + "'");
                    }
                    else
                        WhoisDealing(zone);
                    Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    Write2File.WriteLogToFile(zone + "   use " + dnsserver[aidx] + "   " + ex.ToString());
                    Console.WriteLine(zone + " use " + dnsserver[aidx]+" query faile");
                    WhoisDealing(zone);
                    Console.WriteLine("mission restart");
                    QueryDNSAnalysis();
                }
            }
            Console.WriteLine("mission over");
            if (Convert.ToInt32(MySQLHelper.Query("select count(1) from zonestemp where nsstate not in(1,2,4) and isload=0 limit 0,100").Tables[0].Rows[0][0]) > 0)
                QueryDNSAnalysis();
            watch.Stop();
        }
        static void WhoisDealing(string zone)
        {
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
            catch (Exception ex)
            {
                MySQLHelper.ExecuteSql("update zonestemp set isload=-2 where zone='" + zone + "'");
                Write2File.WriteLogToFile(zone + "   whoisException    " + ex.ToString());
            }
        }
    }
    internal class Domain : DomainBase
    {
        public int Mx_priority { get; set; }
        public string View { get; set; } = "";
    }
    internal class DomainBase
    {
        public string Zone { get; set; }
        public string Name { get; set; } = "nametest";
        public string Type { get; set; } = "A";
        public string Rdata { get; set; }
        public int Ttl { get; set; } = 60;

    }
}
