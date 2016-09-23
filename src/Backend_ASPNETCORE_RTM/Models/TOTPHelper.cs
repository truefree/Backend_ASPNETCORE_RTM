using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Backend_ASPNETCORE_RTM.Models
{
    public class TOTPHelper : ITOTPHelper
    {
        private readonly UserDBContext _dbContext;

        private const string allowedCharacters = "abcdefghijklmnopqrstuv0123456789"; // Due to Base32 encoding; https://code.google.com/p/vellum/wiki/GoogleAuthenticator
        private static int validityPeriodSeconds = 60; // RFC6238 4.1; X represents the time step in seconds (default value X = 30 seconds) and is a system parameter.
        private static int futureIntervals = 1; // How much time in the future can the client be; in validityPeriodSeconds intervals.
        private static int pastIntervals = 1; // How much time in the past can the client be; in validityPeriodSeconds intervals.
        private static int secretKeyLength = 16; // Must be a multiple of 8, iPhones accept up to 16 characters (apparently; didn't test it; don't own an iPhone)
        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Beginning of time, according to Unix

        public TOTPHelper(UserDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string GenerateSecretKey()
        {
            Random random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            return new string((new char[secretKeyLength]).Select(c => c = allowedCharacters[random.Next(0, allowedCharacters.Length)]).ToArray());
        }
        private long GetInterval(DateTime dateTime)
        {
            TimeSpan elapsedTime = dateTime.ToUniversalTime() - unixEpoch;
            return (long)elapsedTime.TotalSeconds / validityPeriodSeconds;
        }
        public string GetCode(string secretKey)
        {
            return GetCode(secretKey, DateTime.Now);
        }
        public string GetCode(string secretKey, DateTime when)
        {
            return GetCode(secretKey, GetInterval(when));
        }
        private string GetCode(string secretKey, long timeIndex)
        {
            var secretKeyBytes = Base32Encode(secretKey);
            //for (int i = secretKey.Length; i < secretKeyBytes.Length; i++) {secretKeyBytes[i] = 0;}
            HMACSHA1 hmac = new HMACSHA1(secretKeyBytes);
            byte[] challenge = BitConverter.GetBytes(timeIndex);
            if(BitConverter.IsLittleEndian) Array.Reverse(challenge);
            byte[] hash = hmac.ComputeHash(challenge);
            int offset = hash[19] & 0xf;
            int truncatedHash = hash[offset] & 0x7f;
            for(int i = 1; i < 4; i++)
            {
                truncatedHash <<= 8;
                truncatedHash |= hash[offset + i] & 0xff;
            }
            truncatedHash %= 1000000;
            return truncatedHash.ToString("D6");
        }
        public bool CheckCodeByKey(string secretKey, string code)
        {
            return CheckCode(secretKey, code, DateTime.Now);
        }
        private bool CheckCode(string secretKey, string code, DateTime when)
        {
            long currentInterval = GetInterval(when);
            bool success = false;
            for(long timeIndex = currentInterval - pastIntervals; timeIndex <= currentInterval + futureIntervals; timeIndex++)
            {
                string intervalCode = GetCode(secretKey, timeIndex);
                bool intervalCodeHasBeenUsed = false;// CodeIsUsed(upn, timeIndex);
                if(ConstantTimeEquals(intervalCode, code) && !intervalCodeHasBeenUsed)
                {
                    success = true;
                    // todo: add code here that adds the code for the user to codes used during an interval.
                    break;
                }
            }
            return success;
        }
        public bool CheckCode(string internalID, string code)
        {
            string secretKey = GetSecretKey(internalID);
            return CheckCode(secretKey, code, internalID, DateTime.Now);
        }
        private bool CheckCode(string secretKey, string code, string internalID, DateTime when)
        {
            long currentInterval = GetInterval(when);
            bool success = false;
            for(long timeIndex = currentInterval - pastIntervals; timeIndex <= currentInterval + futureIntervals; timeIndex++)
            {
                string intervalCode = GetCode(secretKey, timeIndex);
                bool intervalCodeHasBeenUsed = CodeIsUsed(internalID, timeIndex);
                if(!intervalCodeHasBeenUsed && ConstantTimeEquals(intervalCode, code))
                {
                    success = true;
                    SetUsedCode(internalID, timeIndex);
                    break;
                }
            }
            return success;
        }
        private byte[] Base32Encode(string source)
        {
            var bits = source.ToUpper().ToCharArray().Select(c => Convert.ToString(allowedCharacters.IndexOf(c), 2).PadLeft(5, '0')).Aggregate((a, b) => a + b);
            return Enumerable.Range(0, bits.Length / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, 8), 2)).ToArray();
        }
        protected bool ConstantTimeEquals(string a, string b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;

            for(int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)a[i] ^ (uint)b[i];
            }

            return diff == 0;
        }

        public string GetSecretKey(string internalID)
        {
            string result = null;
            var sk = from s in _dbContext.Secrets
                     where s.internalID.Equals(Guid.Parse(internalID))
                     select s;

            if (sk.Count() > 0)
            {
                result = sk.First().secret;
            }
            
            //using(SqlConnection sqlConnection = new SqlConnection(sqlConnectString))
            //{
            //    string sqlCommandString = "SELECT secret FROM Secrets WHERE upn = @upn";
            //    SqlCommand sqlCommand = new SqlCommand(sqlCommandString, sqlConnection);
            //    sqlConnection.Open();
            //    sqlCommand.Parameters.AddWithValue("@upn", upn);
            //    object oResult = sqlCommand.ExecuteScalar();
            //    result = (string)oResult;
            //}
            return result;
        }

        public void SetSecretKey(string internalID, string secret)
        {
            SecretsModel s = new SecretsModel();
            s.internalID = Guid.Parse(internalID);
            s.secret = secret;
            _dbContext.Add<SecretsModel>(s);
            _dbContext.SaveChangesAsync();
            //using(SqlConnection sqlConnection = new SqlConnection(sqlConnectString))
            //{
            //    string sqlCommandString = "INSERT INTO Secrets (upn, secret) VALUES (@upn, @secret)";
            //    SqlCommand sqlCommand = new SqlCommand(sqlCommandString, sqlConnection);
            //    sqlConnection.Open();
            //    sqlCommand.Parameters.AddWithValue("@upn", upn);
            //    sqlCommand.Parameters.AddWithValue("@secret", secret);
            //    sqlCommand.ExecuteNonQuery();
            //}
        }

        private bool CodeIsUsed(string internalID, long interval)
        {
            bool result = false;

            var code = from c in _dbContext.UsedCodes
                       where c.internalID.Equals(Guid.Parse(internalID)) && c.interval.Equals(interval)
                       select c;

            if (code.Count() > 0)
            {
                result = true;
            }

            // House Keeping

            var oldCodes = from c in _dbContext.UsedCodes
                   where c.internalID.Equals(Guid.Parse(internalID)) && (c.interval < (interval))
                   select c;

            foreach (var c in oldCodes)
            { 
                _dbContext.UsedCodes.Remove(c);
            }
            _dbContext.SaveChangesAsync();

            //using(SqlConnection sqlConnection = new SqlConnection(sqlConnectString))
            //{
            //    string sqlCommandString = "SELECT COUNT(*) FROM UsedCodes WHERE upn = @upn AND interval = @interval";
            //    SqlCommand sqlCommand = new SqlCommand(sqlCommandString, sqlConnection);
            //    sqlConnection.Open();
            //    sqlCommand.Parameters.AddWithValue("@upn", upn);
            //    sqlCommand.Parameters.AddWithValue("@interval", interval);
            //    int count = (int)sqlCommand.ExecuteScalar();
            //    result = (count > 0);

            //    // Housekeeping
            //    sqlCommandString = "DELETE FROM UsedCodes WHERE upn = @upn AND interval < @interval";
            //    sqlCommand = new SqlCommand(sqlCommandString, sqlConnection);
            //    sqlCommand.Parameters.AddWithValue("@upn", upn);
            //    sqlCommand.Parameters.AddWithValue("@interval", interval - (pastIntervals * 2));
            //    sqlCommand.ExecuteNonQuery();
            //}
            return result;
        }

        private void SetUsedCode(string internalID, long interval)
        {
            UsedCodesModel c = new UsedCodesModel();
            c.internalID = Guid.Parse(internalID);
            c.interval = interval;
            _dbContext.Add<UsedCodesModel>(c);
            _dbContext.SaveChangesAsync();
            //using(SqlConnection sqlConnection = new SqlConnection(sqlConnectString))
            //{
            //    string sqlCommandString = "INSERT INTO UsedCodes (upn, interval) VALUES (@upn, @interval)";
            //    SqlCommand sqlCommand = new SqlCommand(sqlCommandString, sqlConnection);
            //    sqlConnection.Open();
            //    sqlCommand.Parameters.AddWithValue("@upn", upn);
            //    sqlCommand.Parameters.AddWithValue("@interval", interval);
            //    sqlCommand.ExecuteNonQuery();
            //}
        }

        public void RecordSendMessage_SMS(String SenderEmailAddress, String ReceivereEmailAddress, DateTime SendTime, String Message)
        {
            using(SqlConnection conn = new SqlConnection("data source = 210.211.71.144; Database = CmnMgt; User ID = CmnMgtDBUser; Password = cmnmgtdbuser12#$;"))
            {
                using(SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Connection = conn;
                    cmd.CommandText = "SMS_Insert_SendMessage";

                    cmd.Parameters.AddWithValue("@SenderEmailAddress", SenderEmailAddress);
                    cmd.Parameters.AddWithValue("@ReceiveEmailAddress", ReceivereEmailAddress);
                    cmd.Parameters.AddWithValue("@SendDate", SendTime);
                    cmd.Parameters.AddWithValue("@Message", Message);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }            
        }

    }
}
