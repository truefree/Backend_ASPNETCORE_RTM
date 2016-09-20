﻿// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services (formerly Project Oxford): https://www.microsoft.com/cognitive-services
// 
// Microsoft Cognitive Services (formerly Project Oxford) GitHub:
// https://github.com/Microsoft/ProjectOxford-ClientSDK
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Newtonsoft.Json;
using System;

namespace Backend_ASPNETCORE_RTM.Models.SpeakerRecognition.Contract.Identification
{

    /// <summary>
    /// A class encpaulating the user profile for an identification tasks
    /// </summary>
    public class Profile : ProfileBase
    {
        /// <summary>
        /// Speaker profile ID
        /// </summary>
        [JsonProperty("identificationProfileId")]
        public Guid ProfileId { get; set; }

        /// <summary>
        /// The total length of audio - in seconds of speech - submitted for enrollment
        /// </summary>
        [JsonProperty("enrollmentSpeechTime")]
        public double EnrollmentSpeechSeconds { get; set; }

        /// <summary>
        /// The remaining audio length - in seconds of speech - for the user to be enrolled
        /// </summary>
        [JsonProperty("remainingEnrollmentSpeechTime")]
        public double RemainingEnrollmentSpeechSeconds { get; set; }
    }
}
