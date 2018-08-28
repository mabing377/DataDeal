using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Utility
{
    public class WhoisHelper
    {
        /// <summary>
        /// 查询Whois 信息
        /// </summary>
        /// <param name="domain">域名</param>
        /// <returns></returns>
        public static string SearchAWhois(string domain)
        {
            #region 查询Whois信息
            string pageHtml = "";
            string infoReg = "";
            string getUrl = "";
            
            infoReg = @"<div class=""main"">(?<whoinfo>(.|\n)+?)<div class=""footer"">";
            getUrl = string.Format("http://whoissoft.com/{0}", domain);

            try
            {
                WebRequest request = WebRequest.Create(getUrl);
                HttpWebRequest hRequest = (HttpWebRequest)request;
                hRequest.AllowAutoRedirect = true;
                hRequest.KeepAlive = true;
                //hRequest.Headers.Add("Host:whois.chinaz.com");
                //hRequest.Headers.Add("Content-Type:text/html;charset=utf-8");
                hRequest.ContentType = "text/html;charset=utf-8";
                hRequest.Headers.Add("Cache-Control: private");
                hRequest.UserAgent = "Laopei";
                hRequest.Headers.Add("Server: Microsoft-IIS/6.0");
                hRequest.Headers.Add("X-Powered-By: ASP.NET");
                hRequest.Headers.Add("X-AspNet-Version: 2.0.50727");

                HttpWebResponse response;
                try
                {
                    response = (HttpWebResponse)hRequest.GetResponse();
                }
                catch (WebException ex)
                {
                    response = (HttpWebResponse)ex.Response;
                }

                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream, Encoding.GetEncoding("utf-8"));
                pageHtml = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
                
                //如果是从WhoisSoft查询，则清除内容中的链接
                pageHtml = TextTool.StripHtmlClearTagA(pageHtml);

                var regResult = Regex.Match(pageHtml, infoReg, RegexOptions.IgnoreCase);
                string whois1 = "";

                if (regResult.Success)
                {
                    whois1 = regResult.Groups["whoinfo"].Value;
                }

                return whois1;
            }
            catch (Exception ex)
            {
                var x = ex.Message;
            }

            return "查询失败！";

            #endregion
        }

        /// <summary>
        /// 获取Whois信息中的Email
        /// </summary>
        /// <param name="whoisInfo"></param>
        /// <returns></returns>
        public static string GetWhoisEmail(string whoisInfo)
        {
            return GetWhoisEmail(whoisInfo, @"(?<emailadd>\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*)");
        }

        /// <summary>
        /// 获取Whois信息中的Email信息
        /// </summary>
        /// <param name="whoisInfo"></param>
        /// <returns></returns>
        public static string GetWhoisEmail(string whoisInfo, string emailaddPer)
        {
            var emailResult = Regex.Match(whoisInfo, emailaddPer, RegexOptions.IgnoreCase);

            if (emailResult.Success)
            {
                string emailAdd = emailResult.Groups["emailadd"].Value;
                return emailAdd;
            }

            return "";
        }

        /// <summary>
        /// 获取DNS服务器
        /// </summary>
        /// <param name="whoisInfo"></param>
        /// <returns></returns>
        public static string[] GetDnsServers(string whoisInfo)
        {
            var serverResult = Regex.Matches(whoisInfo, @"((DNS服务器：)|(Name Server:)).{0,3}?(?<whoinfo>([\w-]+\.)+[a-zA-Z]+)", RegexOptions.IgnoreCase);

            List<string> dnsServers = new List<string>();
            for (int i = 0; i < serverResult.Count; i++)
            {
                var aReg = serverResult[i];
                dnsServers.Add(aReg.Groups["whoinfo"].Value);
            }

            return dnsServers.ToArray();
        }
        /// <summary>
        /// 获取DNS服务器
        /// </summary>
        /// <param name="whoisInfo"></param>
        /// <returns></returns>
        public static string GetDnsServersStr(string whoisInfo)
        {
            var serverResult = Regex.Matches(whoisInfo, @"((DNS服务器：)|(Name Server:)).{0,3}?(?<whoinfo>([\w-]+\.)+[a-zA-Z]+)", RegexOptions.IgnoreCase);

            string dnsServers = "";
            for (int i = 0; i < serverResult.Count; i++)
            {
                var aReg = serverResult[i];
                dnsServers=dnsServers+aReg.Groups["whoinfo"].Value+",";
            }

            return dnsServers;
        }
    }
}
