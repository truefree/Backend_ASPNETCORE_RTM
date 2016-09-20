﻿using System;

namespace Backend_ASPNETCORE_RTM.Models
{
    public interface ITOTPHelper
    {
        bool CheckCode(string upn, string code);
        bool CheckCodeByKey(string secretKey, string code);
        string GenerateSecretKey();
        string GetCode(string secretKey);
        string GetCode(string secretKey, DateTime when);
        string GetSecretKey(string upn);
        void RecordSendMessage_SMS(string SenderEmailAddress, string ReceivereEmailAddress, DateTime SendTime, string Message);
        void SetSecretKey(string upn, string secret);
    }
}