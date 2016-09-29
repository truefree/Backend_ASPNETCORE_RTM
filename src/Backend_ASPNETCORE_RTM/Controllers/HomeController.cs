using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;


namespace Backend_ASPNETCORE_RTM.Controllers
{
    public class HomeController : Controller
    {
        [Route("/", Name = "FrontDoor")]
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(AccountController.Login), "Account", new { ReturnUrl = "/" });
        }

        [Route("/Home", Name = "Home")]
        [HttpGet]
        public IActionResult Index(bool loggedIn = false)
        {

            if(loggedIn == false)
            {
                return RedirectToAction(nameof(AccountController.Login), "Account");
            }
            else
            {
                return View();
            }
        }
    }
}
