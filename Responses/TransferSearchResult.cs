using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TopTourMiddleOffice.Containers.Transfers;

using Jayrock.Json.Conversion;
using Jayrock.Json;

namespace TopTourMiddleOffice.Responses
{
    public class TransferSearchResult: _Response
    {
        private string _searchId;
        [JsonMemberName("search_id")]
        public string SearchId
        {
          get { return _searchId; }
          set { _searchId = value; }
        }

        private TransferVariant[] _variants;
        [JsonMemberName("variants")]
        public TransferVariant[] Variants
        {
            get { return _variants; }
            set { _variants = value; }
        }

        //private string _currencyCode;

        //public string CurrencyCode
        //{
        //    get { return _currencyCode; }
        //    set { _currencyCode = value; }
        //}  
    }
}