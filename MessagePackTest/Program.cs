using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePackTest
{
    [MessagePackObject]
    public class ZonesSimple
    {
        [Key(0)]
        public string id { get; set; }
        [Key(1)]
        public int userid { get; set; }
        [Key(2)]
        public string domain { get; set; }
        [Key(3)]
        public string rrcol { get; set; }
        [Key(4)]
        public int level { get; set; }
        [Key(5)]
        public int nsstate { get; set; }
        [Key(6)]
        public int loadonstart { get; set; }
        [Key(7)]
        public string is_stop { get; set; } = "N";
        [Key(8)]
        public string force_stop { get; set; } = "N";
        [Key(9)]
        public string rdomain { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            ZonesSimple zs = new ZonesSimple();
            zs.id = "77b93d79c72a431cf580a1b602020acd";
            zs.userid = 23423542;
            zs.domain = "wwww.sesf.com";
            zs.rrcol = "a";
            zs.level = 0;
            zs.nsstate = 1;
            zs.loadonstart = 1;
            zs.is_stop = "N";
            zs.force_stop = "N";
            zs.rdomain = "sdfe.com";
            // Creates serializer.

            var targetObject =zs;
            var stream = new MemoryStream();

            // 1. Create serializer instance.
            // 序列化
            var bytes = MessagePackSerializer.Serialize(targetObject);
            //反序列化
            var mc2 = MessagePackSerializer.Deserialize<ZonesSimple>(bytes);
            
            var json = MessagePackSerializer.ToJson(bytes);
            Console.WriteLine(json);
            Console.ReadKey();
        }
    }

   
}
