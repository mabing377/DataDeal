using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections;

namespace Utility
{
    /// <summary>
    /// 文本日志类
    /// </summary>
    internal class TxtLog
    {
        private Queue<string> buffer;
        string filename2;
        string logName;
        int fileGroupIdx = 1;
        string fileExpendName = ".txt";
        public TxtLog()
        {
            buffer = new Queue<string>();
            logName = string.Format("LOG{0}", DateTime.Now.ToString("yyyyMMddHHmmss"));

            Thread myThread = new Thread(new ThreadStart(dowrite));
            myThread.IsBackground = true;
            myThread.Start();
        }

        /// <summary>
        /// 分目录，分类型的日志类
        /// </summary>
        /// <param name="typeName"></param>
        public TxtLog(string typeName, string logDirectory)
        {

            string path = logDirectory;
            if (string.IsNullOrEmpty(logDirectory))
            {
                path = AppDomain.CurrentDomain.BaseDirectory + @"\Log";
            }
            path += @"\" + typeName;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            buffer = new Queue<string>();
            logName = string.Format(@"{1}\LOG{0}", DateTime.Now.ToString("yyyyMMddHHmmss"), path);

            Thread myThread = new Thread(new ThreadStart(dowrite));
            myThread.IsBackground = true;
            myThread.Start();
        }

        /// <summary>
        /// 添加日志
        /// </summary>
        /// <param name="logtext">日志内容</param>
        public void AddLog(string logtext,bool isdate)
        {
            string logtxt = isdate==true? string.Format("[{0}] {1}", DateTime.Now.ToString(), logtext):logtext;
            buffer.Enqueue(logtxt);
        }

        private void dowrite()
        {
            while (true)
            {
                if (buffer.Count > 0)
                {
                    try
                    {
                        filename2 = string.Format("{0}{1}{2}", logName, fileGroupIdx < 2 ? "" : "_" + fileGroupIdx.ToString("000"), fileExpendName);
                        FileInfo logInfo = new FileInfo(filename2);
                        if (logInfo.Exists && logInfo.Length > 1048576)
                        {
                            fileGroupIdx++;
                            continue;
                        }
                        using (var filewriter = File.AppendText(filename2))
                        {
                            while (buffer.Count > 0)
                            {
                                string str = buffer.Peek();
                                filewriter.WriteLine(str);
                                buffer.Dequeue();
                            }
                        }
                    }
                    catch
                    {
                    }

                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }

    public class TxtLogger
    {
        private static TxtLog myLog = new TxtLog();

        public static void AddLog(string log,bool isdate)
        {
            lock (myLog)
            {
                if (myLog == null)
                {
                    myLog = new TxtLog();
                    myLog.AddLog("日志开启", isdate);
                }

                myLog.AddLog(log, isdate);
            }
        }

    }

    /// <summary>
    /// 高级操作的日志类
    /// </summary>
    public class LoggerAdvance
    {
        private static Hashtable myAdvanceLogList = new Hashtable();
        public static void AddLog(string log, string typeName, string logDirectory,bool isdate=false)
        {
            lock (myAdvanceLogList)
            {
                TxtLog myLog = null;
                if (myAdvanceLogList.ContainsKey(typeName))
                {
                    myLog = (TxtLog)myAdvanceLogList[typeName];
                }
                else
                {
                    myLog = new TxtLog(typeName, logDirectory);
                    myAdvanceLogList.Add(typeName, myLog);
                    myLog.AddLog("创建日志文件！", isdate);
                }
                myLog.AddLog(log, isdate);
            }
        }
    }
}
