using System;
using System.Threading;

namespace SN__
{
    class Program
    {
        static void Main(string[] args)
        {
            int times = 0;
            while (true)
            {
                SNAdd();
                Console.WriteLine("{0} Times;", times+1);
                Thread.Sleep(3600*1000);
            }
            
        }
        public static void SNAdd() {
            //List<zoneslevel> zll= MongoHelper.GetAll<zoneslevel>("zoneslevel");
            //foreach (zoneslevel zl in zll) {
            //    MongoHelper.UpdateWithColumn<zoneslevel>("zoneslevel", new Document("$set", new Document("sn", zl.sn + 1)), new Document("level", zl.level));
            //}
            //MongoHelper.UpdateAll<zoneslevel>("zoneslevel",new Document("$inc",new Document("sn",1)));
        }
    }
    internal class zoneslevel {
        public int level { get; set; }
        public int total { get; set; }
        public Int64 sn { get; set; }
    }
}
