using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TopTourMiddleOffice.Containers.CarRent;
using Jayrock.Json;
using Jayrock.Json.Conversion;

namespace TopTourMiddleOffice.Responses
{
    public class CarRentGetStationsResult: _Response
    {
        private CarRentStation[] _stations;
        [JsonMemberName("stations")]
        public CarRentStation[] Stations
        {
            get { return _stations; }
            set { _stations = value; }
        }
    }
}