using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Zones: ZonesSimple
    {
        public string zone { get; set; }
        public int groupid { get; set; }
        public string active { get; set; }
        public int domainlevel { get; set; }
        public int tempdomainlevel { get; set; }
        public DateTime starttime { get; set; }
        public DateTime endtime { get; set; }
        public string password { get; set; }
        public int state { get; set; }
        public int validaterank { get; set; }
        public DateTime createtime { get; set; }
        public string siteid { get; set; }
        public int nsstate { get; set; }
        public DateTime checktime { get; set; }
        public DateTime activitytime { get; set; }
        public int fatherzoneid { get; set; }
        public int recordid { get; set; }
        public DateTime nslastcheck { get; set; }             
        public int contentlevel { get; set; }
        public int usecount { get; set; }
        public string rzone { get; set; }
        public int isbindns { get; set; }
        public string partneraccount { get; set; }
        public string lastname { get; set; }
        public DateTime kftime { get; set; }
        public string displayzone { get; set; }
        public DateTime templevelterm { get; set; }
        public string forcestop { get; set; }
        public bool isdelete { get; set; } = false;
        public string noarrest { get; set; } = "N";
        public int dnspriority { get; set; }
        public int isload { get; set; } = 1;
    }
    public class ZonesSimple
    {
        public int userid { get; set; }
        public string domain { get; set; }
        public string rrcol { get; set; }
        public int level { get; set; }
        public int nsstate { get; set; }
    }
    public class zones
    {

        public long id { get; set; }
        public string zone { get; set; }
        public int groupid { get; set; }
        public string active { get; set; }
        public long userid { get; set; }
        public int domainlevel { get; set; }
        public int tempdomainlevel { get; set; }
        public DateTime startdate { get; set; }
        public DateTime enddate { get; set; }
        public string password { get; set; }
        public int state { get; set; }
        public int validaterank { get; set; }
        public DateTime createtime { get; set; }
        public string siteid { get; set; }
        public int nsstate { get; set; }
        public DateTime checktime { get; set; }
        public DateTime activitytime { get; set; }
        public long fatherzoneid { get; set; }
        public long recordid { get; set; }
        public DateTime nslastcheck { get; set; }
        public int contentlevel { get; set; }
        public int usecount { get; set; }
        public string rzone { get; set; }
        public bool isbindns { get; set; }
        public string partneraccount { get; set; }
        public string lastname { get; set; }
        public DateTime kftime { get; set; }
        public string displayzone { get; set; }
        public DateTime templevelterm { get; set; }
        public string forcestop { get; set; }
        public bool isdelete { get; set; } = false;
        public string noarrest { get; set; } = "N";
        public int dnspriority { get; set; }
    }
}
