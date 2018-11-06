using BindDns.MongoDBEntity;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utility;

namespace ViewIP
{
    class Program
    {
        static int Main(string[] args)
        {
            string basepath = AppDomain.CurrentDomain.BaseDirectory;

            GetIP(basepath + "acl\\");
            Console.WriteLine("=================================================================1");
            GetIP(basepath + "province\\");
            Console.WriteLine("=================================================================2");
            Console.ReadKey();
            return 1;
        }
        static void DoAction(string path,int level) {
            try
            {
                string[] files = Directory.GetFiles(path);
                int count = 0;
                foreach (string file in files)
                {
                    string content;
                    string view = "";
                    StreamReader sr = new StreamReader(file, Encoding.Default);
                    List<ViewIP> dl = new List<ViewIP>();
                    var client = DriverConfiguration.Client;
                    var db = client.GetDatabase(DriverConfiguration.DatabaseNamespace.DatabaseName);
                    while ((content = sr.ReadLine()) != null)
                    {
                        if (content.Contains("{"))
                        {
                            view = content.Substring(4, content.Length - 6).Trim();
                        }
                        else if (content.Contains("}") || content.Contains("#"))
                        {
                        }
                        else
                        {
                            content = content.Remove(content.Length - 1);
                            string sip = getStartIp(content);
                            long minIp = IpToInt(sip);
                            string eip = getEndIp(content);
                            long maxIp = IpToInt(eip);
                            if (level == 1 && view == "TelDef")
                            {
                                var builder = Builders<ViewIP>.Filter;
                                long dcount = db.GetCollection<ViewIP>("levelIp").Find(builder.And(builder.Eq("start", minIp), builder.Eq("end", maxIp), builder.Eq("view", view))).ToList<ViewIP>().Count;// MongoHelper.GetCount<ViewIP>("zonesip", new Document("start", minIp).Add("end", maxIp).Add("view", view));
                                if (dcount == 0) {
                                    ViewIP v = new ViewIP();
                                    v.start = minIp;
                                    v.end = maxIp;
                                    v.view = view;
                                    v.level = 0;
                                    dl.Add(v);
                                }
                            }
                            else
                            {
                                ViewIP v = new ViewIP();
                                v.start = minIp;
                                v.end = maxIp;
                                v.view = view;
                                v.level = level;
                                dl.Add(v);
                            }
                        }
                    }
                    if (dl.Count > 0)
                    {
                        db.GetCollection<ViewIP>("levelIp").InsertMany(dl);
                        Console.WriteLine(view + " Handled Complete");
                        count++;
                    }
                }
                Console.WriteLine(count + " File Handled");
            }
            catch (Exception ex)
            {
                string msg = ex.ToString();
            }
        }
        static void GetIP(string path) {
            try
            {
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    string content;
                    string view = "";
                    StreamReader sr = new StreamReader(file, Encoding.Default);
                    while ((content = sr.ReadLine()) != null)
                    {
                        if (content.Contains("{"))
                        {
                            view = content.Substring(4, content.Length - 6).Trim();
                            Console.WriteLine(view);
                        }
                        else if (content.Contains("}") || content.Contains("#"))
                        {
                        }
                        else
                        {
                            content = content.Remove(content.Length - 1);
                            if (content.Contains("211.89.227"))
                                Console.WriteLine(view+" =======================");
                            if (content.Contains("211.89.229"))
                                Console.WriteLine(view);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = ex.ToString();
            }
        }
        public static long IpToInt(string ip)
        {
            char[] separator = new char[] { '.' };
            string[] items = ip.Split(separator);
            return long.Parse(items[0]) << 24
                    | long.Parse(items[1]) << 16
                    | long.Parse(items[2]) << 8
                    | long.Parse(items[3]);
        }

        public static string IntToIp(long ipInt)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append((ipInt >> 24) & 0xFF).Append(".");
            sb.Append((ipInt >> 16) & 0xFF).Append(".");
            sb.Append((ipInt >> 8) & 0xFF).Append(".");
            sb.Append(ipInt & 0xFF);
            return sb.ToString();
        }
        /** 
         * 根据掩码位数计算掩码 
         * @param maskIndex 掩码位 
         * @return 子网掩码 
         */
        public static string getNetMask(string maskIndex)
        {
            StringBuilder mask = new StringBuilder();
            int inetMask = 0;
            inetMask = int.Parse(maskIndex);
            if (inetMask > 32)
            {
                return null;
            }
            // 子网掩码为1占了几个字节  
            int num1 = inetMask / 8;
            // 子网掩码的补位位数  
            int num2 = inetMask % 8;
            int [] array = new int[4];
            for (int i = 0; i < num1; i++)
            {
                array[i] = 255;
            }
            for (int i = num1; i < 4; i++)
            {
                array[i] = 0;
            }
            for (int i = 0; i < num2; num2--)
            {
                array[num1] += 1 << 8 - num2;
            }
            for (int i = 0; i < 4; i++)
            {
                if (i == 3)
                {
                    mask.Append(array[i]);
                }
                else
                {
                    mask.Append(array[i] + ".");
                }
            }
            return mask.ToString();
        }
        /** 
        * 根据网段计算起始IP 网段格式:x.x.x.x/x 
        * 一个网段0一般为网络地址,255一般为广播地址. 
        * 起始IP计算:网段与掩码相与之后加一的IP地址 
        * @param segment  网段 
        * @return 起始IP 
        */
        public static string getStartIp(string segment)
        {
            
            StringBuilder startIp = new StringBuilder();
            if (segment == null)
            {
                return null;
            }
            string []arr  = segment.Split('/');
            string ip = arr[0];
            string maskIndex = arr[1];
            string mask = getNetMask(maskIndex);
            if (4 != ip.Split('.').Length || mask == null)
            {
                return null;
            }
            int [] ipArray = new int[4];
            int [] netMaskArray = new int[4];
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    ipArray[i] = int.Parse(ip.Split('.')[i]);
                    netMaskArray[i] = int.Parse(mask.Split('.')[i]);
                    if (ipArray[i] > 255 || ipArray[i] < 0 || netMaskArray[i] > 255 || netMaskArray[i] < 0)
                    {
                        return null;
                    }
                    ipArray[i] = ipArray[i] & netMaskArray[i];
                    if (i == 3)
                    {
                        startIp.Append(ipArray[i] + 1);
                    }
                    else
                    {
                        startIp.Append(ipArray[i] + ".");
                    }
                }
                catch (Exception e)
                {

                }
            }
            return startIp.ToString();
        }

        /** 
         * 根据网段计算结束IP 
         * @param segment 
         * @return 结束IP 
         */
        public static string getEndIp(string segment)
        {
            StringBuilder endIp = new StringBuilder();
            string startIp = getStartIp(segment);
            if (segment == null)
            {
                return null;
            }
            string [] arr = segment.Split('/');
            string maskIndex = arr[1];
            //实际需要的IP个数  
            int hostNumber = 0;
            int [] startIpArray = new int[4];
            try
            {
                hostNumber = 1 << 32 - (int.Parse(maskIndex));
                for (int i = 0; i < 4; i++)
                {
                    startIpArray[i] = int.Parse(startIp.Split('.')[i]);
                    if (i == 3)
                    {
                        startIpArray[i] = startIpArray[i] - 1;
                        break;
                    }
                }
                startIpArray[3] = startIpArray[3] + (hostNumber - 1);
            }
            catch (Exception e)
            {
            }

            if (startIpArray[3] > 255)
            {
                int k = startIpArray[3] / 256;
                startIpArray[3] = startIpArray[3] % 256;
                startIpArray[2] = startIpArray[2] + k;
            }
            if (startIpArray[2] > 255)
            {
                int j = startIpArray[2] / 256;
                startIpArray[2] = startIpArray[2] % 256;
                startIpArray[1] = startIpArray[1] + j;
                if (startIpArray[1] > 255)
                {
                    int k = startIpArray[1] / 256;
                    startIpArray[1] = startIpArray[1] % 256;
                    startIpArray[0] = startIpArray[0] + k;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (i == 3)
                {
                    startIpArray[i] = startIpArray[i] - 1;
                }
                if ("" == endIp.ToString() || endIp.Length == 0)
                {
                    endIp.Append(startIpArray[i]);
                }
                else
                {
                    endIp.Append("." + startIpArray[i]);
                }
            }
            return endIp.ToString();
        }

        public static string[] getView(string path) {
            string[] files = Directory.GetFiles(path);
            string[] views= new string[100]{ "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""};
            int count = 0;
            foreach (string file in files)
            {
                string content;
                string view = "";
                StreamReader sr = new StreamReader(file, Encoding.Default);
                while ((content = sr.ReadLine()) != null)
                {
                    if (content.Contains("{"))
                    {
                        view = content.Substring(4, content.Length - 6).Trim();
                        views[count] = view;
                        count++;
                    }
                }
            }
            return views;
        }
        
    }
    internal class ViewIP
    {
        public long start { get; set; }
        public long end { get; set; }
        public string view { get; set; }
        public int level { get; set; }
    }
}
