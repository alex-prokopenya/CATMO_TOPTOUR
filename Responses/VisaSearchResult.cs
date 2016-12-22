using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TopTourMiddleOffice.Containers.Visa;
using Jayrock.Json.Conversion;
namespace TopTourMiddleOffice.Responses
{
    public class VisaSearchResult
    {
        private string _searchId;

        [JsonMemberName("search_id")]
        public string SearchId
        {
            get { return _searchId; }
            set { _searchId = value; }
        }

        //private string _currencyCode;

        //public string CurrencyCode
        //{
        //    get { return _currencyCode; }
        //    set { _currencyCode = value; }
        //}

        private VisaDetails _visaDetails;

        [JsonMemberName("visa_details")]
        public VisaDetails VisaDetails
        {
            get { return _visaDetails; }
            set { _visaDetails = value; }
        }
    }
}