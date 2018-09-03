using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class DnsRescords: DnsRescordsSimple
    {
        public int id{get;set;}
        public int zoneid{get;set;}
        public string zone{get;set;}
        public int mx_priority{get;set;}
        public string active{get;set;} //enum('Y','N') NOT NULL DEFAULT 'Y',
        public int domainlevel{get;set;} //int (11) NOT NULL DEFAULT '1',
        public string standby{get;set;}
        public int checkhostid { get; set; } = 0;// int(20) NOT NULL DEFAULT '0',
        public int isfensheng { get; set; } = 0;// int(20) NOT NULL DEFAULT '0',
        public int urlid { get; set; } = 0;// int(20) NOT NULL DEFAULT '0',
        public string str16{get;set;} 
    }
    public class DnsRescordsSimple
    {
        public long rid { get; set; }
        public int userid { get; set; }
        public string domain { get; set; }
        public string name { get; set; }
        public string type { get; set; }// enum('A','SOA','NS','MX','CNAME','PTR','TXT','SRV','AAAA') NOT NULL COMMENT '类型',
        public string rdata { get; set; }
        public int ttl { get; set; } = 600;
        public string view { get; set; } = "Def";
    }

    public class dnsrecords
    {
        public long id { get; set; }
        public long userid { get; set; }
        public long zoneid { get; set; }
        public string zone { get; set; }
        public string host { get; set; }
        public string type { get; set; }
        public int? mx_priority { get; set; }
        public string data { get; set; }
        public int ttl { get; set; }
        public string view { get; set; }
    }
}
