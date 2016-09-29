using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Backend_ASPNETCORE_RTM.Models
{
    public class UserModel
    {
        public Guid internalID { get; set; }

        /// <summary>
        /// Email!
        /// </summary>
        public string loginID { get; set; }
        public string profileID { get; set; }
        public bool IsEnrolled { get; set; }
        public bool IsEnrollCompleted { get; set; }
        public string OTPKey { get; set; }
    }
}
