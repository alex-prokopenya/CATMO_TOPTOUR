using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json.Conversion;
using Jayrock.Json;

namespace TopTourMiddleOffice.Containers.Transfers
{
    public class TransferVariant
    {
        private KeyValuePair<string, decimal>[] _prices;
        private string _date;
        private string _info;
        private string _service_id;
        private string _title;

        [JsonMemberName("date")]
        public string Date
        {
            get
            {
                return this._date;
            }
            set
            {
                this._date = value;
            }
        }

        [JsonMemberName("info")]
        public string Info
        {
            get
            {
                return this._info;
            }
            set
            {
                this._info = value;
            }
        }

        [JsonMemberName("price")]
        public JsonObject Price
        {
            get
            {
                JsonObject pr = new JsonObject();

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

        //private int _detailsId;
        //[JsonMemberName("id")]
        //public int DetailsId
        //{
        //    get { return _detailsId; }
        //    set { _detailsId = value; }
        //}

        //private short _carsCount;
        //[JsonMemberName("cars_count")]
        //public short CarsCount
        //{
        //    get { return _carsCount; }
        //    set { _carsCount = value; }
        //}


        [JsonMemberName("service_id")]
        public string ServiceId
        {
            get
            {
                return this._service_id;
            }
            set
            {
                this._service_id = value;
            }
        }

        [JsonMemberName("title")]
        public string Title
        {
            get { return this._title; }
            set { this._title = value; }
        }
    }
}