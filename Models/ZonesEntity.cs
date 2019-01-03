using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class ZonesEntity
    {
        public string id { get; set; }
        public int userid { get; set; }
        public string domain { get; set; }
        public int level { get; set; }
        public int nsstate { get; set; }
        public string is_stop { get; set; } = "N";
        public string force_stop { get; set; } = "N";
        public string rdomain { get; set; }
        public List<AuthEntity> authorities { get; set; }
        public List<RecordEntity> records { get; set; }
    }
}
