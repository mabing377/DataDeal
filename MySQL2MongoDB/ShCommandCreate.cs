using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQL2MongoDB
{
    public class ShCommandCreate
    {
        public static void WriteToJsonFile(string filename,string text,string pathtype= "jsondata", bool iswriteline = true, bool append = true)
        {
            string baseDic = AppDomain.CurrentDomain.BaseDirectory;
            string path = baseDic + pathtype+"\\" + filename + ".json";
            if (!File.Exists(path))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

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
        public static void WriteToShCommandFile(string text,string FileIndex) {

            string baseDic = AppDomain.CurrentDomain.BaseDirectory;
            string path = baseDic + "command\\" + System.DateTime.Now.ToString("yyyyMMdd") + FileIndex+".sh";
            if(!File.Exists(path))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.Write(text);
                }
            }
        }
    }
}
