﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TopTourMiddleOffice.Responses
{
    public class InsuranceCalculateResult: _Response
    {
        private KeyValuePair<string, decimal>[] _prices;

        public KeyValuePair<string, decimal>[] Prices
        {
            get { return _prices; }
            set { _prices = value; }
        }
    }
}