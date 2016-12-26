using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json.Conversion;
using Jayrock.Json;

namespace TopTourMiddleOffice.Containers.Tours
{
    public class TourBooking
    {
        private TourVariant _tourVariant;

        [JsonMemberName("variant")]
        public TourVariant TourVariant
        {
            get { return _tourVariant; }
            set { _tourVariant = value; }
        }

        private string[] _turists;
        [JsonMemberName("turists")]
        public string[] Turists
        {
            get { return _turists; }
            set { _turists = value; }
        }

    }
}