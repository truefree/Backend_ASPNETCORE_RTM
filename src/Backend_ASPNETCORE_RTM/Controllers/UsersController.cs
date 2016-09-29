using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Backend_ASPNETCORE_RTM.Models;
using System.Net.Http;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Backend_ASPNETCORE_RTM.Controllers
{
    [Route("api/[controller]")]
    public class UsersController : Controller
    {

        private readonly IUserRepository _users;
        private ITOTPHelper _totpHelper;

        // truefree 20160519
        // Controller 생성자는 DI pattern을 따름
        // 에 뭐 사실은 컨테이너가 모든 dependency를 찾아서 여기 Interface들에 new object를 넣어줌
        public UsersController(IUserRepository users, ITOTPHelper TOTPHelper)
        {
            _users = users;
            _totpHelper = TOTPHelper;
        }

        // 기본 접근하면 모든 profile을 반환...할 필요는 없는데?!
        // user perspective로 생각할 것...
        // 뭐 나와야 돼?????
        [HttpGet]
        public UserModel[] GetAll()
        {
            return _users.GetAllUser();
        }

        [HttpGet("{email}")]
        public IActionResult GetUserByEmail(string email)
        {
            UserModel u = _users.GetUserByLoginID(email);
            if (u == null)
            {
                return NotFound();
            }

            return new ObjectResult(u);
        }

        [HttpGet("{id}", Name = "GetUser")]
        public IActionResult GetUserByID(string id)
        {
            UserModel u = _users.GetUserByID(id);

            if(u == null)
            {
                return NotFound();
            }

            return new ObjectResult(u);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            UserModel user = _users.GetUserByID(id);

            if(user == null)
            {
                // user가 없으면 404
                return NotFound();
            }

            bool deleteResult = false;
            deleteResult = await _users.DeleteUserAsync(user);

            if(deleteResult)
            {
                return Ok();
            } else
            {
                // internal server error
                return StatusCode(500);
            }

        }

        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromBody] UserModel user)
        {
            // Request가 잘못 되었으면 400
            if(user == null || string.IsNullOrEmpty(user.loginID))
            {
                return BadRequest();
            }

#if DEBUG
#else
            // CmnMgt에 있는지 check...
            if(_users.CheckUserExistanceFromDB(user.loginID) == false)
            {
                // 없는 user면 404
                return NotFound();
            }
#endif


            // DB에 이미 있으면 Get으로 Redirect
            UserModel u = _users.GetUserByLoginID(user.loginID);

            if(u != null)
            {
                return RedirectToRoute("GetUser", new { controller = "Users", id = u.internalID });
            }

            user.internalID = Guid.NewGuid();   
            //string newUserID = user.internalID.ToString();
            //string secretKey = _totpHelper.GetSecretKey(newUserID);
            //if(string.IsNullOrEmpty(secretKey) == true)
            //{
            //    secretKey = _totpHelper.GenerateSecretKey();
            //    _totpHelper.SetSecretKey(newUserID, secretKey);
            //}

            //string intervalCode = _totpHelper.GetCode(secretKey);

            //// send SMS
            //try {
            //    //_totpHelper.RecordSendMessage_SMS("truefree@sk.com", user.loginID, DateTime.Now, "Enter digits below to your app :: " + intervalCode);
            //} catch(Exception e)
            //{
            //    return StatusCode(500);
            //}

            // 302 Redirect
            // client에서 SMS key와 함께 다시 POST하기를...
            //return RedirectToRoute("MFAChallenge", new { controller = "Users", id = user.internalID });
            return RedirectToRoute("MFARequest", new { controller = "Users", id = user.internalID.ToString(), user = user});
        }

        [HttpGet("{id}/mfa", Name ="MFARequest")]
        public async Task<IActionResult> MFARequest(string id, [FromQuery] string loginID)
        {
            string secretKey = _totpHelper.GetSecretKey(id);
            if(string.IsNullOrEmpty(secretKey) == true)
            {
                secretKey = _totpHelper.GenerateSecretKey();
                _totpHelper.SetSecretKey(id, secretKey);
            }

            string intervalCode = _totpHelper.GetCode(secretKey);

            // send SMS
            try
            {
#if DEBUG
                _totpHelper.RecordSendMessage_SMS("truefree@sk.com", loginID, DateTime.Now, "Enter digits below to your app :: " + intervalCode);
#else
                _totpHelper.RecordSendMessage_SMS("truefree@sk.com", loginID, DateTime.Now, "Enter digits below to your app :: " + intervalCode);
#endif
            }
            catch(Exception e)
            {
                return StatusCode(500);
            }

            return Ok();
        }

        [HttpPost("{id}/mfa", Name = "MFAChallenge")]
        //[ActionName("MFAChallenge")]
        public async Task<IActionResult> MFAChallange([FromBody] UserModel user, [FromQuery] string code, string id)
        {
            // Request가 잘못 되었으면 400
            Guid g = new Guid();
            if (string.IsNullOrEmpty(code) || string.IsNullOrWhiteSpace(code) || Guid.TryParse(id, out g) == false
                || user == null || string.IsNullOrEmpty(user.loginID))
            {
                return BadRequest();
            }

            // DB에 이미 있으면 Get으로 Redirect
            UserModel u = _users.GetUserByID(id);

            if(u != null)
            {
                return RedirectToRoute("GetUser", new { controller = "Users", id = u.internalID });
            }
            
            // check code
            if (_totpHelper.CheckCode(id, code) == false)
            {
                // code 안맞으면 401~~~
                return Unauthorized();
            }

            // MFA 통과 시 GUID 설정
            user.internalID = new Guid(id);
            user.OTPKey = _totpHelper.GetSecretKey(id.ToString());

            u = await _users.AddUserAsync(user);
            return CreatedAtRoute("GetUser", new { controller = "Users", id = u.internalID }, u);
        }

        [HttpGet("phrase")]
        public async Task<IActionResult> GetVerficationPhrase()
        {
            Models.SpeakerRecognition.Contract.Verification.VerificationPhrase[] texts;
            try
            {
                texts = await _users.GetVerificationPhaseAsync();
            }
            catch(Exception ex)
            {
                return StatusCode(500);
            }

            return new OkObjectResult(texts);
        }

        [HttpPost("{id}/enroll")]
        public async Task<IActionResult> Enroll(string id, HttpContent stream)
        {
            UserModel u = null;
            u = _users.GetUserByID(id);

            if(u == null)
            {
                return BadRequest();
            }


            u = await _users.EnrollProfileAsync(u, stream);

            if(u == null)
            {
                return BadRequest();
            }

            return new ObjectResult(u);
        }

#region Sample generated code
        //// GET: api/values
        //[HttpGet]
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        //// GET api/values/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/values
        //[HttpPost]
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT api/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/values/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
#endregion

    }
}
