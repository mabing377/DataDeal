using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP
{
    public class Domain : DomainBase
    {
        public int Mx_priority { get; set; }
        public string View { get; set; } = "";
        public DateTime TestTime { get; set; }
    }
    public class DomainBase
    {
        public string ID { get; set; }
        public string Zone { get; set; }
        public string Name { get; set; } = "nametest";
        public string Type { get; set; } = "A";
        public string Rdata { get; set; }
        public int Ttl { get; set; } = 60;

    }
}
