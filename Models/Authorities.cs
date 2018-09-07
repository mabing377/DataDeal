using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class authorities
    {
        public long id { get; set; }
        public string zone { get; set; }
        public string host { get; set; }
        public string data { get; set; }
        public string type { get; set; }
        public int ttl { get; set; }
        public string mbox { get; set; }
        public int serial { get; set; }
        public int refresh { get; set; }
        public int retry { get; set; }
        public int expire { get; set; }
        public int minimum { get; set; }
        public long userid { get; set; }
    }
    public class AuthoritiesSimple:DnsRecordsSimple
    {
        //public long rid { get; set; }
        //public int userid { get; set; }
        //public string domain { get; set; }
        //public string name { get; set; }
        //public string type { get; set; } = "A";
        //public string rdata { get; set; }
        //public int ttl { get; set; } = 60;
        //public string view { get; set; } = "Def";
    }

}
