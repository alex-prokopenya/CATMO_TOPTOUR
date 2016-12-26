using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json.Conversion;
using Jayrock.Json;

namespace TopTourMiddleOffice.Containers.Tours
{
    public class TourVariant
    {
        private KeyValuePair<string, decimal>[] _prices;

        [JsonMemberName("price")]
        public JsonObject Price
        {
            get
            {
                JsonObject pr = new JsonObject();

                if (_prices != null)
                    foreach (KeyValuePair<string, decimal> val in _prices)
                        pr.Add(val.Key, val.Value);

                return pr;
            }
            set
            {
                List<KeyValuePair<string, decimal>> prices = new List<KeyValuePair<string, decimal>>();

                JsonObject vl = value;
                foreach (string name in vl.Names)
                    prices.Add(new KeyValuePair<string, decimal>(name, Convert.ToDecimal(vl[name])));

                _prices = prices.ToArray();
            }
        }

        [JsonIgnore]
        public KeyValuePair<string, decimal>[] Prices
        {
            get { return _prices; }
            set { _prices = value; }
        }

        private string _date;
        [JsonMemberName("date")]
        public string Date
        {
            get { return _date; }
            set { _date = value; }
        }

        private string _title;
        [JsonMemberName("title")]
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        private int _provider_id;
        [JsonMemberName("provider_id")]
        public int ProviderId
        {
            get { return _provider_id; }
            set { _provider_id = value; }
        }

        private string _service_id;
        [JsonMemberName("service_id")]
        public string ServiceId
        {
            get { return _service_id; }
            set { _service_id = value; }
        }

        private string _info;
        [JsonMemberName("info")]
        public string Info
        {
            get { return _info; }
            set { _info = value; }
        }
    }
}