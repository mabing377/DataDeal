using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
namespace Utility
{
    public partial class TextTool
    {
        /// <summary>
        /// 软件版本号比较器
        /// </summary>
        /// <param name="v1">版本1（必须是以.为分界符的4组数字）</param>
        /// <param name="v2">版本2（必须是以.为分界符的4组数字）</param>
        /// <returns>1:v1大于v2|0:v1=v2|-1:v1小于v2</returns>
        public static int VersionCompare(string v1, string v2)
        {
            string[] c1 = v1.Split(new char[] { '.' });
            string[] c2 = v2.Split(new char[] { '.' });
            int result = 0;
            for (int i = 0; i < 4; i++)
            {
                int x1 = int.Parse(c1[i]);
                int x2 = int.Parse(c2[i]);
                if (x1 == x2)
                {
                    continue;
                }
                else if (x1 > x2)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }

            return result;
        }

        /// <summary>
        /// 检查Mysql的注入漏洞
        /// </summary>
        /// <param name="inputSqlStr"></param>
        /// <returns></returns>
        public static bool CheckMySqlSecurity(string inputSqlStr)
        {
            return true;
        }

        /// <summary>
        /// 获取一个颜色的16进制值
        /// </summary>
        /// <param name="colorID">颜色内部编码ID</param>
        /// <returns></returns>
        public static string GetAColorValue(int colorID)
        {
            int r = colorID * 9;
            if (r > 99)
                r = 99;
            int g = (colorID - 6) * 3;
            if (g < 0)
                g = -g;
            if (g > 99)
                g = 99;
            int b = (10 - colorID) * 6;
            if (b < 0)
                b = -b;
            if (b > 99)
                b = 99;
            string sr = r.ToString("D2");
            string sg = g.ToString("D2");
            string sb = b.ToString("D2");
            switch (colorID % 5)
            {
                case 0:
                    return sr + sg + sb;
                case 1:
                    return sg + sb + sr;
                case 2:
                    return sb + sg + sr;
                case 3:
                    return sr + sb + sg;
                case 4:
                    return sg + sr + sb;
                default:
                    return sb + sr + sg;
            }

        }

        /// <summary>
        /// 日期格式化为字符串
        /// </summary>
        /// <param name="date"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string DateToString(DateTime date, string format)
        {
            return date.ToString(format);
        }

        /// <summary>
        /// 验证一个URL是否为某些域名中某一个域下的连接
        /// </summary>
        /// <param name="url">被验证的网址</param>
        /// <param name="domainList">域名，可为多个</param>
        /// <param name="validatePrivatePath">是否验证内部连接（相对连接）true则当为内部连接时通过验证，false将不考虑是否内部连接</param>
        /// <returns></returns>
        public static bool CheckURLIsDomain(string url, string[] domainList, bool validatePrivatePath)
        {
            url = url.ToLower(); //转为小写形式
            if (validatePrivatePath)
            {
                if (!url.StartsWith("http://"))
                    return true;
            }

            foreach (string adomain in domainList)
            {
                string zyadomain = adomain.Replace(".", @"\.");
                bool oneresult = Regex.IsMatch(url, @"http://(\w{1,}\.{1})*" + zyadomain + "/.+", RegexOptions.Singleline);
                if (oneresult)
                    return true;
            }

            return false;

        }

        /// <summary>
        /// 获取一个随机的颜色（16进制的值）
        /// </summary>
        /// <returns></returns>
        public static string GetARadomColorValue()
        {
            string guid = Guid.NewGuid().ToString();
            return guid.Substring(0, 6);
        }

        /// <summary>
        /// 过滤所有的标志
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FilterAllSymbols(string str)
        {
            Regex myRegex = new Regex(@"[^\u4e00-\u9fa5^\d^\w]");
            return myRegex.Replace(str, " ");
        }

        /// <summary>
        /// 清除文本中的HTML格式，但保留BR换行
        /// </summary>
        /// <param name="strHtml"></param>
        /// <returns></returns>
        public static string StripHTMLAndKeepBR(string strHtml)
        {
            //替换<br/>为[br/]
            System.Text.RegularExpressions.Regex regexBR = new System.Text.RegularExpressions.Regex(@"<br[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            strHtml = regexBR.Replace(strHtml, "{br}");
            strHtml = StripHTML(strHtml);
            strHtml = strHtml.Replace("{br}", "<br />");
            return strHtml;
        }

        /// <summary>
        /// 清除文本中的HTML，包含转义（如*nbsp；等）
        /// </summary>
        /// <param name="strHtml"></param>
        /// <returns></returns>
        public static string StripHTMLCompriseTransferred(string strHtml)
        {
            var strTemp = StripHTML(strHtml);
            strTemp = Regex.Replace(strTemp, @"\&\w+\;", "");
            return strTemp;
        }


        /// <summary>
        ///过滤除特定标签和标签中的style
        /// 去除非DIV BR P A Table 等之外的标签，并自动填补这些标签的配对
        /// 2009-5 By 宋光明 
        /// </summary>
        /// <param name="strHtml"></param>
        /// <returns></returns>
        public static string StripHtmlExceptTags(string strHtml)
        {
            string[] defKeep = new string[] {
            "p","table","tr","td","a","div"
            };
            return StripHtmlExceptTags(strHtml, defKeep);
        }

        /// <summary>
        /// 过滤除特定标签和标签中的style
        /// 去除非DIV BR P A Table 等之外的标签，并自动填补这些标签的配对
        /// 2009-5 By 宋光明
        /// </summary>
        /// <param name="strHtml"></param>
        /// <returns></returns>
        public static string StripHtmlExceptTags(string strHtml, string[] keepTags)
        {
            #region Delete by 2009 11 17
            //string exceptHtmlTags
            //if (string.IsNullOrEmpty(strHtml)) return strHtml;
            //string exceptTag = @"p|\/p|table|\/table|tr|\/tr|td|\/td|a|\/a|br";
            //string exceptTags = @"p|table|tr|td|a|br";
            //if (!string.IsNullOrEmpty(exceptHtmlTags))
            //{
            //    string[] tags = exceptTags.Split('|');
            //    exceptTag = "";
            //    foreach(string etag in tags)
            //    {
            //        exceptTags += etag;
            //        exceptTags += @"|\/";
            //        exceptTags += etag;
            //        exceptTags += "|";
            //    }

            //    exceptTag = exceptTags.Substring(0, exceptTags.Length - 1);
            //    exceptTags = exceptHtmlTags;
            //}
            #endregion

            string expString = "";
            string backString = "";
            foreach (var aTag in keepTags)
            {
                var theTag = aTag.ToLower();
                if (theTag != "br")
                {
                    if (backString != "")
                        backString += "|";
                    expString += theTag + @"|\/" + theTag + "|";
                    backString += theTag;
                }
            }

            //过滤脚本及其内容
            System.Text.RegularExpressions.Regex regexScript = new System.Text.RegularExpressions.Regex(@"<[\s]*?script[^>]*?>[\s\S]*?<[\s]*?\/[\s]*?script[\s]*?>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //过滤CSS样式及其内容
            System.Text.RegularExpressions.Regex regexStyle = new System.Text.RegularExpressions.Regex(@"<[\s]*?style[^>]*?>[\s\S]*?<[\s]*?\/[\s]*?style[\s]*?>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //过滤除指定标签外的<>内的内容
            //System.Text.RegularExpressions.Regex regexExceptTag = new System.Text.RegularExpressions.Regex(@"<(?!" + exceptTag + @")[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // back System.Text.RegularExpressions.Regex regexExceptTag = new System.Text.RegularExpressions.Regex(@"<(?!p|\/p|table|\/table|tr|\/tr|td|\/td|a|\/a|br|div|\/div)[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regexExceptTag = new System.Text.RegularExpressions.Regex(@"<(?!" + expString + "br)[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //查找现有的标签
            System.Text.RegularExpressions.Regex regexTag = new System.Text.RegularExpressions.Regex(@"<[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //查找Style样式
            System.Text.RegularExpressions.Regex regexTagStyle = new System.Text.RegularExpressions.Regex(@"style=['|""][^>]*?['|""]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            strHtml = regexScript.Replace(strHtml, "");
            strHtml = regexStyle.Replace(strHtml, "");
            strHtml = regexExceptTag.Replace(strHtml, "");

            MatchCollection matchs = regexTag.Matches(strHtml);

            StringBuilder retstr = new StringBuilder();
            System.Collections.Stack myStack = new System.Collections.Stack();
            int begindex = 0;

            //System.Text.RegularExpressions.Regex regexETagBegin = new System.Text.RegularExpressions.Regex(@"<("+exceptTags+")", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //@"<(p|table|tr|td|a|br|div)"
            System.Text.RegularExpressions.Regex regexETagBegin = new System.Text.RegularExpressions.Regex(@"<(" + backString + (string.IsNullOrEmpty(backString) ? "" : "|") + "br)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //System.Text.RegularExpressions.Regex regexETagEnd = new System.Text.RegularExpressions.Regex(@"</(" + exceptTags + ")", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regexETagEnd = new System.Text.RegularExpressions.Regex(@"</(" + backString + ")", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (Match match in matchs)
            {
                bool writetag = true;
                string tmptag = regexTagStyle.Replace(match.Value, "");
                if (regexETagBegin.Match(tmptag).Success)
                {
                    string tagname = regexETagBegin.Match(tmptag).Groups[1].Value.ToLower();

                    if (tagname.Length != 0 && tmptag.Substring(tmptag.Length - 2, 1) != "/" && tagname != "br")
                        myStack.Push(tagname);
                }
                else
                {
                    string tagname = regexETagEnd.Match(tmptag).Groups[1].Value.ToLower();

                    if (myStack.Count > 0)
                    {
                        while (myStack.Peek().ToString() != tagname)
                        {
                            string popstack = myStack.Pop().ToString();
                            retstr.Append("</" + popstack + " >");
                            if (myStack.Count == 0) break;
                        }

                        if (myStack.Count > 0)
                        {
                            myStack.Pop();
                        }
                        else
                        {
                            writetag = false;
                        }
                    }
                    else
                    {
                        writetag = false;
                    }
                }

                int matchbegin = match.Index;
                if (matchbegin > begindex)
                {
                    retstr.Append(strHtml.Substring(begindex, matchbegin - begindex));
                }
                begindex = matchbegin + match.Length;
                if (writetag) retstr.Append(regexTagStyle.Replace(match.Value, ""));
            }

            if (begindex < strHtml.Length)
                retstr.Append(strHtml.Substring(begindex));

            while (myStack.Count > 0)
            {
                string tagname = myStack.Pop().ToString();
                retstr.Append("</" + tagname + " >");
            }

            return retstr.ToString();
        }

        /// <summary>
        /// 用传入的地址替换文本中href不是以 http:// 开头的a标签的href
        /// </summary>
        /// <param name="strHtml"></param>
        /// <param name="strHref"></param>
        /// <returns></returns>
        public static string ReplaceHtmlTagAHref(string strHtml, string strHref)
        {
            System.Text.RegularExpressions.Regex regexScript = new System.Text.RegularExpressions.Regex(@"<[\s]*?a[^>]*href=(?![""|']?http://)[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            strHtml = regexScript.Replace(strHtml, "<a href=\"" + strHref + "\" target=\"_blank\">");
            return strHtml;
        }

        /// <summary>
        /// 清除字符串中所有的链接
        /// </summary>
        /// <param name="strHtml"></param>
        /// <returns></returns>
        public static string StripHtmlClearTagA(string strHtml)
        {
            System.Text.RegularExpressions.Regex regexScript = new System.Text.RegularExpressions.Regex(@"<[\s]*?a[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regexScript2 = new System.Text.RegularExpressions.Regex(@"<[\s]*?\/a[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            strHtml = regexScript.Replace(strHtml, "");
            strHtml = regexScript2.Replace(strHtml, "");

            return strHtml;
        }

        /// <summary>
        /// 清除文本中的HTML格式
        /// </summary>
        /// <param name="strHtml"></param>
        /// <returns></returns>
        public static string StripHTML(string strHtml)
        {
            if (string.IsNullOrEmpty(strHtml)) return "";

            //过滤脚本及其内容
            System.Text.RegularExpressions.Regex regexScript = new System.Text.RegularExpressions.Regex(@"<[\s]*?script[^>]*?>[\s\S]*?<[\s]*?\/[\s]*?script[\s]*?>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //过滤CSS样式及其内容
            System.Text.RegularExpressions.Regex regexStyle = new System.Text.RegularExpressions.Regex(@"<[\s]*?style[^>]*?>[\s\S]*?<[\s]*?\/[\s]*?style[\s]*?>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //过滤所有<>内的内容
            System.Text.RegularExpressions.Regex regexTag = new System.Text.RegularExpressions.Regex(@"<[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            strHtml = regexScript.Replace(strHtml, "");
            strHtml = regexStyle.Replace(strHtml, "");
            strHtml = regexTag.Replace(strHtml, "");
            strHtml = strHtml.Replace("<", "&lt;");
            strHtml = strHtml.Replace(">", "&gt;");

            return strHtml;
        }

        /// <summary>
        /// 自动首行缩进
        /// </summary>
        /// <param name="formatText">文本</param>
        /// <returns></returns>
        public static string AutoFirstLineFormat(string formatText)
        {
            formatText = formatText.Replace("<BR>", "<br>");
            formatText = formatText.Replace("<Br>", "<br>");
            formatText = formatText.Replace("<bR>", "<br>");
            formatText = formatText.Replace("<P>", "<p>");
            formatText = formatText.Replace("<p>　　", "<p>");
            formatText = formatText.Replace("<br>　　", "<br>");
            formatText = formatText.Replace("<br>　", "<br>");
            formatText = formatText.Replace("<br>", "<br>　　");
            formatText = formatText.Replace("<p>", "　　");

            return "　　" + formatText;
        }

        /// <summary>
        /// 从左边截取文本的一部分
        /// </summary>
        /// <param name="inputText">文本内容</param>
        /// <param name="length">截取长度</param>
        /// <param name="moreString">当文本超过截取长度时，使用的省略符号</param>
        /// <returns></returns>
        public static string LeftString(string inputText, int length, string moreString)
        {
            if (inputText.Length > length)
            {
                inputText = inputText.Substring(0, length) + moreString;
            }

            return inputText;
        }

        /// <summary>
        /// 检验输入文本是否电子邮件
        /// </summary>
        /// <param name="inputText">被检验文本</param>
        /// <returns></returns>
        public static bool CheckIsEmail(string inputText)
        {
            return Regex.IsMatch(inputText, @"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");
        }
        /// <summary>
        /// 检测符合密码的格式
        /// </summary>
        /// <param name="inputText"></param>
        /// <returns></returns>
        public static bool CheckIsPwd(string inputText)
        {
            return Regex.IsMatch(inputText, @"[0-9a-zA-Z\.@#%]{6,20}");
        }
        /// <summary>
        /// 检测是否是手机
        /// </summary>
        /// <param name="inputText"></param>
        /// <returns></returns>
        public static bool CheckIsMobile(string inputText)
        {
            return Regex.IsMatch(inputText, @"^0{0,1}(1[0-9]{2})[0-9]{8}$");
        }
        /// <summary>
        /// 检测是否是身份证
        /// </summary>
        /// <param name="inputText"></param>
        /// <returns></returns>
        public static bool CheckIsIdCode(string inputText)
        {
            return Regex.IsMatch(inputText, @"/^(\d{15}$|^\d{18}$|^\d{17}(\d|X|x))$/");
        }
        /// <summary>
        /// 检测邮编格式是否正确
        /// </summary>
        /// <param name="inputText"></param>
        /// <returns></returns>
        public static bool CheckIsPostCode(string inputText)
        {
            return Regex.IsMatch(inputText, @"^[1-9]\d{5}$");
        }

        /// <summary>
        /// 检查一段文本中包含某字符串的个数
        /// </summary>
        /// <param name="inputText">文本</param>
        /// <param name="checkStringReg">字符串</param>
        /// <returns></returns>
        public static int GetAStringContainsNumber(string inputText, string checkStringReg)
        {
            var checkR = Regex.Matches(inputText, checkStringReg);
            return checkR.Count;
        }

        /// <summary>
        /// 验证输入的内容是否域名
        /// </summary>
        /// <param name="inputText"></param>
        /// <returns></returns>
        public static bool CheckIsDomain(string inputText)
        {
            return Regex.IsMatch(inputText, @"^([a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+\.?)$");
        }

        /// <summary>
        /// 从输入的文本中提取Email地址
        /// </summary>
        /// <param name="inputText">输入文本</param>
        /// <returns></returns>
        public static string GetEmail(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
                return "";
            if (CheckIsEmail(inputText))
            {
                return inputText;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// 检查密码是否合法
        /// </summary>
        /// <param name="inputText">密码文本</param>
        /// <returns></returns>
        public static bool CheckPasswordFormat(string inputText)
        {
            return Regex.IsMatch(inputText, @"[\w!@.#%()~]{6,18}");
        }

        /// <summary>
        /// 提取邮政地址（某某市某某街道某某楼某房间）
        /// </summary>
        /// <param name="inputText">输入文本</param>
        /// <returns></returns>
        public static string GetPostAddress(string inputText)
        {
            string taddress = TextTool.StripHTML(inputText);
            Match myMatch = Regex.Match(taddress, @"[\u4e00-\u9fa5\d-()（）\w]{0,200}");
            if (myMatch.Success)
                taddress = myMatch.Value;
            else
                taddress = "";
            return taddress;
        }

        /// <summary>
        /// 提取邮政编码
        /// </summary>
        /// <param name="inputText"></param>
        /// <returns></returns>
        public static string GetPostCode(string inputText)
        {
            //string yb = inputText.Trim();
            Match myMatch = Regex.Match(inputText, "[0123456789０１２３４５６７８９]{6}");
            if (myMatch.Success)
            {
                return myMatch.Value;
            }
            return "";
        }

        /// <summary>
        /// 从字符串中提取电话号码
        /// </summary>
        /// <param name="inputText"></param>
        /// <returns></returns>
        public static string GetTelnumber(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
                return "";
            //inputText = inputText.Replace(" ", "");
            //inputText = inputText.Replace("　", "");
            //inputText = StripHTML(inputText);
            Match myMatch = Regex.Match(inputText, @"[\d０１２３４５６７８９\(\)\s-—转（）]{6,50}");
            if (myMatch.Success)
            {
                return myMatch.Value;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// 获取联系人信息
        /// </summary>
        /// <param name="inputText">输入文本</param>
        /// <returns></returns>
        public static string GetLinkMan(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
                return "";
            inputText = inputText.Trim();
            inputText = StripHTML(inputText);
            if (inputText.Length > 30)
                return "";
            else
                return inputText;
        }

        /// <summary>
        /// 尝试转换为日期，转换失败则返回默认日期
        /// </summary>
        /// <param name="inputText">输入文本</param>
        /// <param name="defaultDate">默认日期</param>
        /// <returns></returns>
        public static DateTime TryConvertToDate(string inputText, DateTime defaultDate)
        {
            inputText = inputText.Trim();
            DateTime.TryParse(inputText, out defaultDate);
            return defaultDate;
        }

        /// <summary>
        /// 用来拆分列表的分隔符，不要使用-拆分，-用来连接职位类型Key中的文本
        /// </summary>
        private static readonly char[] splitchars = new char[] { ' ', ',', ';', '、', '+' };
        /// <summary>
        /// 将一个字符串拆分为列表（适用于连在一起的多个地名拆分为单个地名的列表）
        /// 如（河南、广州、湖北）
        /// </summary>
        /// <param name="inputText"></param>
        /// <returns></returns>
        public static string[] SplitToList(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
                return new string[0];
            string[] list = inputText.Split(splitchars, StringSplitOptions.RemoveEmptyEntries);
            return list;
        }

        /// <summary>
        /// 将一个字符串拆分为列表并返回第一个值或者Null
        /// </summary>
        /// <param name="inputText">输入内容</param>
        /// <returns></returns>
        public static string SplitAndGetFirstOrDefault(string inputText)
        {
            if (inputText == null)
                return "";
            string[] xList = SplitToList(inputText);
            return xList.FirstOrDefault();
        }


        /// <summary>
        /// 将一个字符串拆分为列表并返回最后一个值或者Null Empty
        /// </summary>
        /// <param name="inputText">输入内容</param>
        /// <returns></returns>
        public static string SplitAndGetLastOrDefault(string inputText)
        {
            if (inputText == null)
                return "";
            string[] xList = SplitToList(inputText);
            return xList.LastOrDefault();
        }

        /// <summary>
        /// 将一个String列表通过标识符组合成一个字符串
        /// </summary>
        /// <param name="stringArray">字符串数组</param>
        /// <param name="spSign">标识符，如;</param>
        /// <returns></returns>
        public static string ArrayToString(string[] stringArray, char spSign)
        {
            StringBuilder strB = new StringBuilder();
            foreach (var astring in stringArray)
            {
                strB.Append(astring);
                strB.Append(spSign);
            }

            if (strB.Length > 0)
                strB = strB.Remove(strB.Length - 1, 1);

            return strB.ToString();
        }

        /// <summary>
        /// 将数字转换为大些
        /// </summary>
        /// <param name="numberValue">数字值</param>
        /// <returns></returns>
        public static string NumberToCapital(int numberValue)
        {
            string result = "";
            if (numberValue < 0)
            {
                result += "负";
                numberValue = numberValue * -1;
            }

            if (numberValue < 10)
            {
                result = GetOneNumberString(numberValue);
            }
            else if (numberValue < 100)
            {
                string v = numberValue.ToString();
                string v1 = v.Substring(0, 1);
                string v2 = v.Substring(1, 1);
                int vv1 = int.Parse(v1);
                int vv2 = int.Parse(v2);
                if (vv1 > 1)
                    result += GetOneNumberString(vv1);
                result += "十";
                if (vv2 > 0)
                    result += GetOneNumberString(vv2);
            }
            else
            {
                return numberValue.ToString();
            }

            return result;
        }

        /// <summary>
        /// 从一个网址中提取域名信息
        /// </summary>
        /// <param name="url">网址</param>
        /// <returns></returns>
        public static string GetDomainFromURL(string url)
        {
            Regex myRegex = new Regex(@".+?\.(?<words>[^\\|/]+)");
            MatchCollection mcs = myRegex.Matches(url);
            if (mcs.Count > 0)
            {
                return mcs[0].Groups["words"].Value;
            }

            return "";
        }

        /// <summary>
        /// 从一段完整的URL中，缩减成段的URL显示名称
        /// 如：http://www.a.com/ss...99.html
        /// </summary>
        /// <param name="fullurl"></param>
        /// <param name="maxUrlLength">必须大于10</param>
        /// <returns></returns>
        public static string GetShortUrlName(string fullurl, int maxUrlLength)
        {
            if (string.IsNullOrEmpty(fullurl))
                return "";
            if (maxUrlLength < 15)
                return "http://";
            else
            {
                int fullLength = fullurl.Length;
                if (fullLength > maxUrlLength)
                {
                    int cutLength = fullLength - maxUrlLength + 3;
                    string resultUrl = fullurl.Substring(0, fullLength - 8 - cutLength) + "..." + fullurl.Substring(fullLength - 8);
                    return resultUrl;
                }
                else
                {
                    return fullurl;
                }
            }
        }

        /// <summary>
        /// 隐藏邮箱或姓名中间部分
        /// </summary>
        /// <param name="emailAddress">邮箱地址（或姓名）</param>
        /// <param name="showTrueStar">是否显示真实数量的*，如果选择false 则最多显示3个星</param>
        /// <returns></returns>
        public static string HideEmailCenter(string emailAddress, bool showTrueStar)
        {
            var partStrs = emailAddress.Split(new char[] { '@' });
            string str1 = emailAddress;
            string str2 = "";
            if (partStrs.Length > 0)
            {
                str1 = partStrs[0];
                if (partStrs.Length > 1)
                    str2 = "@" + partStrs[1];

                int getLength = str1.Length / 2;
                int starNumber = str1.Length - getLength;
                if (showTrueStar == false)
                {
                    if (starNumber > 3)
                        starNumber = 3;
                }
                string stars = "";
                for (int i = 0; i < starNumber; i++)
                {
                    stars += "*";
                }
                str1 = str1.Substring(0, getLength) + stars;
            }

            return str1 + str2;
        }


        /// <summary>
        /// 获取一个数字的大小写
        /// </summary>
        /// <param name="n">数字</param>
        /// <returns></returns>
        private static string GetOneNumberString(int n)
        {
            switch (n)
            {
                case 0:
                    return "零";
                case 1:
                    return "一";
                case 2:
                    return "二";
                case 3:
                    return "三";
                case 4:
                    return "四";
                case 5:
                    return "五";
                case 6:
                    return "六";
                case 7:
                    return "七";
                case 8:
                    return "八";
                case 9:
                    return "九";
            }
            return "";
        }

        /// <summary>
        /// 把大写的中文数字翻译成通用数字，最多支持2位，否则返回-1
        /// </summary>
        /// <param name="cnNumber">汉字数字，最多2位</param>
        /// <returns></returns>
        public static int GetNumberByChinese(string cnNumber)
        {
            if (string.IsNullOrEmpty(cnNumber) || cnNumber.Length > 2)
                return -1;
            string xw = cnNumber.Substring(0, 1);
            int v = WordToNumber(xw, cnNumber.Length == 1);
            int b = cnNumber.Length == 1 ? 1 : 10;
            return v * b;
        }


        /// <summary>
        /// 生成一个交易号码
        /// </summary>
        /// <returns></returns>
        public static string CreateABusinessCode()
        {
            string bCode = DateTime.Now.ToString("yyyyMMddhhmmss");
            Random aR = new Random();
            int sj = aR.Next(100, 999);
            return bCode + sj;
        }

        /// <summary>
        /// 把中文的汉字转换为整数
        /// </summary>
        /// <param name="cnWord">单个汉字</param>
        /// <param name="isSingle">该字是否为一串数字的一部分，是false 否true</param>
        /// <returns></returns>
        private static int WordToNumber(string cnWord, bool isSingle)
        {
            switch (cnWord)
            {
                case "零":
                    return 0;
                case "一":
                    return 1;
                case "二":
                    return 2;
                case "三":
                    return 3;
                case "四":
                    return 4;
                case "五":
                    return 5;
                case "六":
                    return 6;
                case "七":
                    return 7;
                case "八":
                    return 8;
                case "九":
                    return 9;
                case "十":
                    if (isSingle)
                        return 10;
                    else
                        return 1;
                case "两":
                    return 2;
                default:
                    return 0;
            }
        }
    }
}
