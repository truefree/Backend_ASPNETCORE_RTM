using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Backend_ASPNETCORE_RTM.Models.SpeakerRecognition;
using Backend_ASPNETCORE_RTM.Models.SpeakerRecognition.Contract.Verification;
using System.Data.SqlClient;

namespace Backend_ASPNETCORE_RTM.Models
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDBContext _dbContext;

        private readonly ISpeakerVerificationServiceClient _client;

        public async Task<VerificationPhrase[]> GetVerificationPhaseAsync()
        {
            return await _client.GetPhrasesAsync(_client.Options.Locale);
        }

        public UserRepository(UserDBContext dbContext, ISpeakerVerificationServiceClient client)
        {
            _dbContext = dbContext;
            _client = client;
        }

        public UserModel[] GetAllUser()
        {
            var u = from m in _dbContext.Users
                    select m;

            return u.ToArray<UserModel>();
        }

        public UserModel GetUserByID(string internalID)
        {
            var user = from m in _dbContext.Users
                       where m.internalID.ToString().Equals(internalID, StringComparison.CurrentCultureIgnoreCase)
                       select m;

            if(user.Count() <= 0)
            {
                return null;
            }

            return user.First();
        }

        public UserModel GetUserByLoginID(string loginID)
        {
            var u = from m in _dbContext.Users
                    where m.loginID.Equals(loginID, StringComparison.CurrentCultureIgnoreCase)
                    select m;
            
            if(u.Count() <= 0)
            {
                return null;
            }

            return u.First();
        }

        public async Task<UserModel> AddUserAsync(UserModel user)
        {
            _dbContext.Add<UserModel>(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        //public async Task<UserModel> CreateProfileAsync(UserModel user)
        //{
        //    CreateProfileResponse res = await _client.CreateProfileAsync(_client.Options.Locale);
        //    user.profileID = res.ProfileId.ToString();
        //    _dbContext.Update<UserModel>(user);

        //    return user;
        //}

        private async Task<Models.SpeakerRecognition.Contract.EnrollmentStatus> GetUserEnrollStatusAsync(string profileID)
        {
            Profile userStatus = await _client.GetProfileAsync(new Guid(profileID));
            return userStatus.EnrollmentStatus;
        }

        public async Task<bool> DeleteUserAsync(UserModel user)
        {
            // validation은 controller 에서...
            // 이건 DAO! 있다고 가정하고 진행한다잉
            try
            {
                if(string.IsNullOrEmpty(user.profileID) == false)
                {
                    await _client.DeleteProfileAsync(new Guid(user.profileID));
                }

                _dbContext.Remove<UserModel>(user);
                await _dbContext.SaveChangesAsync();
            } catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public async Task<UserModel> EnrollProfileAsync(UserModel user, HttpContent content)
        {
            if(string.IsNullOrEmpty(user.profileID))
            {
                // create profile first
                CreateProfileResponse res = await _client.CreateProfileAsync(_client.Options.Locale);
                user.profileID = res.ProfileId.ToString();
                _dbContext.Update<UserModel>(user);
                await _dbContext.SaveChangesAsync();
            }

            Models.SpeakerRecognition.Contract.EnrollmentStatus status = await GetUserEnrollStatusAsync(user.profileID);

            if (status == SpeakerRecognition.Contract.EnrollmentStatus.Enrolling)
            {
                Enrollment enrollResult;
                Stream mc = await content.ReadAsStreamAsync();
                enrollResult = await _client.EnrollAsync(mc, new Guid(user.profileID));

                if(enrollResult.RemainingEnrollments <= 0)
                {
                    user.IsEnrolled = true;
                }
                else
                {
                    user.IsEnrolled = false;
                }
            } else if (status == SpeakerRecognition.Contract.EnrollmentStatus.Enrolled)
            {
                user.IsEnrollCompleted = true;
            } else
            {
                user.IsEnrollCompleted = false;
            }


            _dbContext.Update<UserModel>(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        public bool CheckUserExistanceFromDB(string loginID)
        {
            int result = 0;
            using(SqlConnection conn = new SqlConnection("data source = 210.211.71.144; Database = CmnMgt; User ID = cmnmgtdbuser; Password = cmnmgtdbuser12#$;"))
            {
                using(SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT COUNT(userid) FROM tb_User with(NOLOCK) where EmailAddress = @loginID";
                    cmd.Parameters.AddWithValue("@loginID", loginID);

                    conn.Open();
                    result = (int)cmd.ExecuteScalar();
                }
            }

            if(result > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}
