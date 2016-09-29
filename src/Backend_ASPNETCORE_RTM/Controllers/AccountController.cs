using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Backend_ASPNETCORE_RTM.Models.AccountViewModels;


// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Backend_ASPNETCORE_RTM.Controllers
{
    
    [Route("/Account")]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<Models.UserModel> _signInManager;
        private Backend_ASPNETCORE_RTM.Models.ITOTPHelper _totpHelper;
        private Backend_ASPNETCORE_RTM.Models.IUserRepository _user;
        public AccountController(Backend_ASPNETCORE_RTM.Models.ITOTPHelper ITOTPHelper, Backend_ASPNETCORE_RTM.Models.IUserRepository user)
        {
            this._totpHelper = ITOTPHelper;
            this._user = user;
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [Route("/Account/Login")]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [Route("/Account/Login", Name = "Login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if(ModelState.IsValid)
            {
                    return RedirectToAction(nameof(VerifyCode), new { Email = model.Email, ReturnUrl = returnUrl});
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        
        //
        // GET: /Account/VerifyCode
        
        [HttpGet]
        [Route("/Account/VerifyCode", Name = "VerifyCode")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string email, string returnUrl = null)
        {
            return View(new VerifyCodeViewModel { Email = email, ReturnUrl = returnUrl });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [Route("/Account/VerifyCode")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return View(model);
            }

            Backend_ASPNETCORE_RTM.Models.UserModel c = _user.GetUserByLoginID(model.Email);
            string internalID = c.internalID.ToString();

            if (_totpHelper.CheckCode(internalID, model.Code))
            {
                model.LoggedIn = true;

                return RedirectToAction(nameof(HomeController.Index), "Home", new { LoggedIn = true});
            } else
            {
                ModelState.AddModelError(string.Empty, "Invalid code.");
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.

            // Code Validation

            // return to info page

            //if(result.Succeeded)
            //{
            //    return RedirectToLocal(model.ReturnUrl);
            //}
            //if(result.IsLockedOut)
            //{
            //    return View("Lockout");
            //}
            //else
        }

    }

}
