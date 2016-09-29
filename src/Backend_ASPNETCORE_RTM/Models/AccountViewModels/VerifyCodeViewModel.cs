using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Backend_ASPNETCORE_RTM.Models.AccountViewModels
{
    public class VerifyCodeViewModel
    {
        [Required]
        public string Code { get; set; }

        public string Email { get; set; }

        public string ReturnUrl { get; set; }

        public bool LoggedIn { get; set; }
    }
}
