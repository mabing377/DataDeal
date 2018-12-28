using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class ViewIP
    {
        [Newtonsoft.Json.JsonIgnore()]
        public ObjectId _id { get; set; }
        public long start { get; set; }
        public long end { get; set; }
        public string view { get; set; }
        public int level { get; set; }
    }
}
