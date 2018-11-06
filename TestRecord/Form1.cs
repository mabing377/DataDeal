using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestRecord
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CheckRecordHost(this.textBox1.Text.Trim(), this.textBox2.Text.Trim());
            CheckRecordData(this.textBox3.Text.Trim(), this.textBox2.Text.Trim(), this.textBox4.Text.Trim(), this.textBox1.Text.Trim());
        }






        #region CheckData
        /// <summary>
        /// 验证是否子域名Host
        /// </summary>
        public const string CheckChildDomain = @"([\w-]+\.)*[\w-@]+";
        /// <summary>
        /// 验证主机记录
        /// </summary>
        public const string CheckHost = @"^([a-zA-Z0-9.*_@-]+|[a-zA-Z0-9.*_@-]*[\u2E80-\u9FFF]+|[\u2E80-\u9FFF]+[a-zA-Z0-9.*_@-]*)$";
        /// <summary>
        /// 验证Host首尾字符
        /// </summary>
        public const string CheckDnsName = @"^([^.-]+|[^.-]+.*[^.-]+|[^.-]+.*|.*[^.-]+)$";
        /// <summary>
        /// 验证子域名长度
        /// </summary>
        public const string CheckChildDomainLength = @"^(\*\.|(([^.]{1,63}\.)?){1,}[^.]{1,63})$";
        /// <summary>
        /// 验证IPV4
        /// </summary>
        public const string CheckIPV4 = @"^(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1}|[1-9]\d{1}|1\d\d|2[0-4]\d|25[0-5])$";
        /// <summary>
        /// 验证IPV6
        /// </summary>
        public const string CheckIPV6 = @"^\s*((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:)))(%.+)?\s*$";
        /// <summary>
        /// 验证是否合法Data
        /// </summary>
        public const string CheckData = @"^([_a-zA-Z0-9\u2E80-\u9FFF\uFF08\uFF09\u00b7]([-_a-zA-Z0-9\u2E80-\u9FFF\uFF08\uFF09\u00b7]{0,61}[a-zA-Z0-9\u2E80-\u9FFF\uFF08\uFF09\u00b7])?\.)+[a-zA-Z\u2E80-\u9FFF\uFF08\uFF09\u00b7]{2,18}?\.$";
        /// <summary>
        /// 验证IP形式的MX记录
        /// </summary>
        public const string CheckMX_IP = @"^(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.$";

        /// <summary>
        /// 验证SRV类型的记录值
        /// </summary>
        public const string CheckSRVData = @"^\d+\s+\d+\s+\d+\s+([a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,18}\.$";
        /// <summary>
        /// 验证TXT类型的记录值
        /// </summary>
        public const string CheckTXTData = @"^[-\\A-Za-z0-9*?_~=:;.@+^\/!""\s]+$";
        /// <summary>
        /// 判断记录的Host是否合法
        /// </summary>
        /// <param name="aRecord"></param>
        /// <param name="domainConfig"></param>
        private static bool CheckRecordHost(string Host, string Type)
        {
            if (Type != "PTR")
            {
                if (Host != "*")
                {
                    if (!Regex.IsMatch(Host, CheckChildDomain, RegexOptions.IgnoreCase) || Host.EndsWith("."))
                        return false;
                    if (Host.Contains("*") && (!Host.StartsWith("*") || Regex.Matches(Host, @"\*").Count > 1))
                        return false;
                }

                if (Host.Contains("@") && Host != "@")
                    return false;

                if (!Regex.IsMatch(Host, CheckHost, RegexOptions.IgnoreCase))
                    return false;
                if (!Regex.IsMatch(Host.Substring(0, 1), CheckDnsName) || !Regex.IsMatch(Host.Substring(Host.Length - 1, 1), CheckDnsName))
                    return false;
                if (!Regex.IsMatch(Host, CheckChildDomainLength))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 判断记录的值是否合法
        /// </summary>
        /// <param name="aRecord"></param>
        private static bool CheckRecordData(string Data, string Type, string View, string Host)
        {
            #region 判断数据的值是否合法

            if (Data.Length > 252)
                return false;

            if (Type == "A")
            {
                if (!Regex.IsMatch(Data, CheckIPV4, RegexOptions.IgnoreCase))
                    return false;
            }

            if (Type == "AAAA")
            {
                if (!Regex.IsMatch(Data, CheckIPV6))
                    return false;
            }

            if (Type == "CNAME" || Type == "NS" || Type == "PTR")
            {
                if (!Regex.IsMatch(Data, CheckData, RegexOptions.IgnoreCase))
                    return false;//记录的值不是一个合法的地址！
            }

            if (Type == "NS")
            {
                if (View != "Def") return false;//NS记录仅能添加通用线路类型！");
                if (Host == "" || Host == "@") return false;//NS记录不允许添加空主机头的记录！");
            }
            if (Type == "SRV")
            {
                if (!Regex.IsMatch(Data, CheckSRVData, RegexOptions.IgnoreCase)) return false;//记录的值不合法！", Type));
            }
            if (Type == "TXT")
            {
                if (!Regex.IsMatch(Data, CheckTXTData, RegexOptions.IgnoreCase)) return false;//记录的值不合法！", Type));
            }

            if (Type == "MX")
            {
                if (!Regex.IsMatch(Data, CheckData, RegexOptions.IgnoreCase) && !Regex.IsMatch(Data, CheckMX_IP, RegexOptions.IgnoreCase)) return false;//MX记录的值不是一个合法的地址！");
            }
            return true;
            #endregion
        }
        #endregion
    }
}
