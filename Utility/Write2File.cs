using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public class Write2File
    {
        public static void WriteToFile(string text, bool append = true, bool iswriteline = true)
        {
            string baseDic = AppDomain.CurrentDomain.BaseDirectory;
            string path = baseDic + "SqlStr\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".sql";


            using (FileStream fs = new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    if (iswriteline)
                        sw.WriteLine(text);
                    else sw.Write(text);
                }
            }
        }
        public static void WriteLogToFile(string text, bool append = true)
        {
            string baseDic = AppDomain.CurrentDomain.BaseDirectory;
            string path = baseDic + "Log\\" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".log";


            using (FileStream fs = new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                     sw.WriteLine(System.DateTime.Now.ToString()+"     "+text);
                }
            }
        }
    }
}
