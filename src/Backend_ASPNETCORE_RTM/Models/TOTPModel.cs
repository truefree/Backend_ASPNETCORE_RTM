using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend_ASPNETCORE_RTM.Models
{
    public class SecretsModel
    {
        public int no { get; set; }
        public Guid internalID { get; set; }
        public string secret { get; set; }
    }

    public class UsedCodesModel
    {
        public int no { get; set; }
        public Guid internalID { get; set; }
        public long interval { get; set; }
    }
}
