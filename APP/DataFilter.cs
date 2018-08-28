using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Utility;

namespace APP
{
    public class DataFilter
    {
        public static double tasksize = 0;
        public static int ThreadCount = Convert.ToInt32(ConfigurationManager.AppSettings["ThreadCount"]);

        static string zoneregstr = @"^(?=^.{3,255}$)[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+$";
        static string ipv4regstr = @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$";
        static string ipv6regstr = @"^([\da-fA-F]{1,4}:){6}((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$|^::([\da-fA-F]{1,4}:){0,4}((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$|^([\da-fA-F]{1,4}:):([\da-fA-F]{1,4}:){0,3}((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$|^([\da-fA-F]{1,4}:){2}:([\da-fA-F]{1,4}:){0,2}((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$|^([\da-fA-F]{1,4}:){3}:([\da-fA-F]{1,4}:){0,1}((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$|^([\da-fA-F]{1,4}:){4}:((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$|^([\da-fA-F]{1,4}:){7}[\da-fA-F]{1,4}$|^:((:[\da-fA-F]{1,4}){1,6}|:)$|^[\da-fA-F]{1,4}:((:[\da-fA-F]{1,4}){1,5}|:)$|^([\da-fA-F]{1,4}:){2}((:[\da-fA-F]{1,4}){1,4}|:)$|^([\da-fA-F]{1,4}:){3}((:[\da-fA-F]{1,4}){1,3}|:)$|^([\da-fA-F]{1,4}:){4}((:[\da-fA-F]{1,4}){1,2}|:)$|^([\da-fA-F]{1,4}:){5}:([\da-fA-F]{1,4})?$|^([\da-fA-F]{1,4}:){6}:$";
        static string domainregstr = @"^(?=^.{4,255}$)[a-zA-Z0-9]([-_a-zA-Z0-9]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([-a-zA-Z0-9]{0,61}[a-zA-Z0-9])?)*(\.[a-zA-Z0-9]+)\.$";
        static string hostregstr = @"^(?=^.{1,64}$)([a-zA-Z0-9_@-]+|\*)(\.([a-zA-Z0-9_@-]*)[a-zA-Z0-9_@])*$";
        static Regex zonereg = new Regex(zoneregstr);
        static Regex ipv4reg = new Regex(ipv4regstr);
        static Regex ipv6reg = new Regex(ipv6regstr);
        static Regex domainreg = new Regex(domainregstr);
        static Regex hostreg = new Regex(hostregstr);

        public void StartOperation() {
            #region 初始化日志配置
            string baseDic = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(baseDic + "App_Log\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".log"))
                File.Delete(baseDic + "App_Log\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".log");
            Logger.Init(baseDic + "Config\\log4net.conf");//初始化 log4net 日志
            #endregion

            //if (File.Exists(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql"))
            //{
            //    File.Delete(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql");
            //    Directory.CreateDirectory(Path.GetDirectoryName(baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql"));
            //}

            MySQLHelper.ExecuteSql("truncate table dnsrecordstemp");
            Console.WriteLine("Truncate Success");
            int r = MySQLHelper.ExecuteSql("INSERT INTO dnsrecordstemp(ID, UserID, ZoneID, Zone, Host, Type, Mx_priority, Data, TTL, View, Active, DomainLevel, Standby, CheckHostID, IsFensheng, URLID, Str16) SELECT ID, UserID, ZoneID, Zone, Host, Type, Mx_priority, Data, TTL, View, Active, DomainLevel, Standby, CheckHostID, IsFensheng, URLID, Str16 FROM dnsrecords");
            if (r > 0)
            {
                Console.WriteLine("Insert Success");
                DataSet ds = MySQLHelper.Query("SELECT count(1) FROM dnsrecordstemp");
                double totalcount = Convert.ToInt32(ds.Tables[0].Rows[0][0]);
                tasksize = Math.Ceiling(totalcount / ThreadCount);

                Console.WriteLine(string.Format("{0} rows data", totalcount));

                Thread[] tlist = new Thread[ThreadCount];
                for (int i = 0; i < ThreadCount; i++)
                {
                    tlist[i] = new Thread(new ParameterizedThreadStart(Dnsrecords2DataFilter));
                    tlist[i].Start(i);
                }
            }
            else
            {
                //Console.WriteLine("Inserted Fail");
            }
        }
        public static void Dnsrecords2DataFilter(object ThreadIndex)
        {
            try
            {
                int startindex = Convert.ToInt32(ThreadIndex) * Convert.ToInt32(tasksize);
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();//开始计时
                DataSet ds = MySQLHelper.Query("SELECT zone,host,data,type,mx_priority,ttl,view,id FROM dnsrecordstemp limit " + startindex.ToString() + "," + Convert.ToInt32(tasksize).ToString());

                long count = 0, updatecount = 0;
                Console.WriteLine(string.Format("Thread {0} Query Success,Use Time {1} Start Foreach.", ThreadIndex, watch.ElapsedMilliseconds));
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    string zone = dr[0].ToString();
                    string host = dr[1].ToString().Trim();
                    string data = dr[2].ToString().Trim();
                    string mx_priority = dr[4].ToString();
                    string type = dr[3].ToString();
                    string ttl = dr[5].ToString();
                    string view = dr[6].ToString();
                    ///Host和Data去除空格
                    if (host != dr[1].ToString() || data != dr[2].ToString())
                    {
                        MySQLHelper.ExecuteSql(string.Format("update dnsrecordstemp set Host='{0}',Data='{1}' where id={2}", host, data, dr[7]));
                        updatecount++;
                    }
                    //data去除http://和目录参数
                    if (zonereg.IsMatch(zone) && CheckHost(host) && data.StartsWith("http://"))
                    {
                        data = data.Remove(0, 7);
                        if (data.IndexOf('/') > 0)
                            data = data.Remove(data.IndexOf('/')) + ".";
                        MySQLHelper.ExecuteSql(string.Format("update dnsrecordstemp set Data='{0}' where id={1}", data, dr[7]));
                    }
                    //正则规整
                    if (type == "A")
                    {
                        if (!(zonereg.IsMatch(zone) && CheckHost(host) && ipv4reg.IsMatch(data)))
                            InsertTemp(zone, host, data, type, mx_priority, ttl, view, dr[7].ToString());
                    }
                    else if (type == "CNAME" || type == "NS" || type == "MX")
                    {
                        if (!(zonereg.IsMatch(zone) && CheckHost(host) && domainreg.IsMatch(data)))
                            InsertTemp(zone, host, data, type, mx_priority, ttl, view, dr[7].ToString());
                    }
                    else if (type == "AAAA")
                    {
                        if (!(zonereg.IsMatch(zone) && CheckHost(host) && ipv6reg.IsMatch(data)))
                            InsertTemp(zone, host, data, type, mx_priority, ttl, view, dr[7].ToString());
                    }
                    else if (type == "TXT")
                    {
                        if (zonereg.IsMatch(zone) && CheckHost(host) && data != "")
                        {

                        }
                        else InsertTemp(zone, host, data, type, mx_priority, ttl, view, dr[7].ToString());
                    }
                    else if (type == "SRV")
                    {
                        if (zonereg.IsMatch(zone) && CheckHost(host) && data.Split(' ').Length == 4)
                        {

                        }
                        else InsertTemp(zone, host, data, type, mx_priority, ttl, view, dr[7].ToString());
                    }
                    else
                    {
                    }
                    count++;
                    if (count % 1000 == 0 && count > 0)
                    {
                        long t = watch.ElapsedMilliseconds;
                        Console.WriteLine("Thread {0} ,{1} rows data;{2} rows updated;use time {3}", ThreadIndex, count.ToString(), updatecount, t);
                    }
                }
                Console.WriteLine("Thread {0} ,{1} rows data;{2} rows updated;use time {3}", ThreadIndex, count.ToString(), updatecount, watch.ElapsedMilliseconds);

            }
            catch (Exception ex)
            {
                Logger.Error("Exception2", ex);
            }
        }
     
        static void InsertTemp(string zone, string host, string data, string type, string mx, string ttl, string view, string id)
        {
            if (type == "MX")
                MySQLHelper.ExecuteSql(string.Format("INSERT INTO dnsrecords_aaaa(ID,Zone,Host,Type,Mx_priority,Data,TTL,View)values({0},'{1}','{2}','{3}',{4},'{5}',{6},'{7}')", id, zone, host, type, mx, data, ttl, view));
            else
                MySQLHelper.ExecuteSql(string.Format("INSERT INTO dnsrecords_aaaa(ID,Zone,Host,Type,Data,TTL,View)values({0},'{1}','{2}','{3}','{4}',{5},'{6}')", id, zone, host, type, data, ttl, view));
            MySQLHelper.ExecuteSql(string.Format("delete from dnsrecordstemp where id={0}", id));
        }
        static bool CheckHost(string host)
        {
            if (host.StartsWith("-") || host.EndsWith("-"))
                return false;
            else if (host == "@." || host == "@-")
                return false;
            else if (host == "_" || (host.StartsWith("_") && host.EndsWith("_")))
                return false;
            else if (!hostreg.IsMatch(host))
            {
                return false;
            }
            else return true;
        }
    }
}
