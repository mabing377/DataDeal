using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online
{
    public class TempZoneID
    {
        public long zoneid { get; set; }
    }
    public class zones {
        public long id { get; set; }
        public long userid { get; set; }
        public string zone { get; set; }
        public long level { get; set; }
        public int nsstate { get; set; }
        public string active { get; set; } = "N";
        public string forcestop { get; set; } = "N";
    }
    public partial class Zones
    {
        public string _id { get; set; }
        public long ID
        {
            get; set;
        }

        public string Zone
        {
            get; set;
        }

        public int GroupID
        {
            get; set;
        }

        public string Active
        {
            get; set;
        }

        public long UserID
        {
            get; set;
        }

        public int DomainLevel
        {
            get; set;
        }

        public int TempDomainLevel
        {
            get; set;
        }

        public global::System.DateTime StartDate
        {
            get; set;
        }

        public global::System.DateTime EndDate
        {
            get; set;
        }

        public string Password
        {
            get; set;
        }

        public sbyte State
        {
            get; set;
        }

        public int ValidateRank
        {
            get; set;
        }

        public global::System.DateTime CreateTime
        {
            get; set;
        }

        public string SiteID
        {
            get; set;
        }

        public int NSState
        {
            get; set;
        }

        public global::System.DateTime CheckTime
        {
            get; set;
        }

        public global::System.DateTime ActivityTime
        {
            get; set;
        }

        public long FatherZoneID
        {
            get; set;
        }

        public long RecordID
        {
            get; set;
        }

        public global::System.DateTime NSLastCheck
        {
            get; set;
        }

        public int ContentLevel
        {
            get; set;
        }

        public int UseCount
        {
            get; set;
        }

        public string RZone
        {
            get; set;
        }

        public bool IsBindNS
        {
            get; set;
        }

        public string PartnerAccount
        {
            get; set;
        }

        public string LastName
        {
            get; set;
        }

        public global::System.DateTime KFTime
        {
            get; set;
        }

        public string DisplayZone
        {
            get; set;
        }

        public global::System.DateTime TempLevelTerm
        {
            get; set;
        }

        public string ForceStop
        {
            get; set;
        }

        public UInt64 IsDelete
        {
            get; set;
        }

        public string NoArrest
        {
            get; set;
        }

        public int DNSPriority
        {
            get; set;
        }


    }
}
