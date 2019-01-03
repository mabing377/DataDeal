using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class AuthEntity
    {
        public long rid { get; set; }
        public string domain { get; set; }
        public string name { get; set; }
        public string type { get; set; }// enum('A','SOA','NS','MX','CNAME','PTR','TXT','SRV','AAAA') NOT NULL COMMENT '类型',
        public string rdata { get; set; }
        public int ttl { get; set; } = 600;
        public string view { get; set; } = "Def";
        public string is_stop { get; set; } = "N";
    }
}
