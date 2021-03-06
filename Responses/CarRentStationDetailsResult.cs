﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TopTourMiddleOffice.Containers.CarRent;

namespace TopTourMiddleOffice.Responses
{
    public class CarRentStationDetailsResult:_Response
    {
        private CarRentStationDetails _stationDetails;

        public CarRentStationDetails StationDetails
        {
            get { return _stationDetails; }
            set { _stationDetails = value; }
        }
    }
}