using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TopTourMiddleOffice.Containers.CarRent;
using Jayrock.Json.Conversion;

namespace TopTourMiddleOffice.Responses
{
    public class CarRentExtrasResult: _Response
    {
        private CarRentExtra[] _extras;
        [JsonMemberName("extras")]
        public CarRentExtra[] Extras
        {
          get { return _extras; }
          set { _extras = value; }
        }
    }
}