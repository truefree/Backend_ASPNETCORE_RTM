using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend_ASPNETCORE_RTM.Models.SpeakerRecognition
{
    public class WebServiceSettings
    {
        public string SubsKey { get; set; }
        public string CreateProfile { get; set; }
        public string GetAllProfile { get; set; }

        public string Locale { get; set; }
    }
}
