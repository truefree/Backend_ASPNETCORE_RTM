
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Backend_ASPNETCORE_RTM.Models.SpeakerRecognition.Contract.Verification;


namespace Backend_ASPNETCORE_RTM.Models
{
    public interface IUserRepository
    {
        UserModel[] GetAllUser();
        UserModel GetUserByID(string internalID);
        UserModel GetUserByLoginID(string loginID);
        Task<UserModel> AddUserAsync(UserModel user); //should be async 
        Task<VerificationPhrase[]> GetVerificationPhaseAsync();
        Task<UserModel> EnrollProfileAsync(UserModel user, HttpContent content);
        Task<bool> DeleteUserAsync(UserModel user);

        bool CheckUserExistanceFromDB(string loginID);

    }
}
