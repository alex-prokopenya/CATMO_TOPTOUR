using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json;
using Jayrock.Json.Conversion;
using TopTourMiddleOffice.MasterTour;
using TopTourMiddleOffice.Responses;
using TravelSoftMiddleOffice;

namespace TravelSoftMiddleOffice.Containers.Excursion
{
    public class ExcursionBooking
    {
        private DateTime _date;
        private int _country;
        private int _city;
        private string _name;
        private int _typeTransport;
        private int _partner;
        private string _comment;
        private KeyValuePair<string, decimal>[] _prices;
        private KeyValuePair<string, decimal>[] _nettos;
        private TuristContainer[] _infoTurists;
        private int[] _turists;


        public ExcursionBooking() { }

        public ExcursionBooking(JsonObject inp)
        {
            try
            {
                this._date = Convert.ToDateTime(inp["date"]);
                this._country = Convert.ToInt32(inp["country"]);
                this._city = Convert.ToInt32(inp["city"]);
                this._name = inp["name"].ToString();

                if (inp.Contains("typeTransport"))
                    this._typeTransport = Convert.ToInt32(inp["typeTransport"]);
                else
                    this._typeTransport = 22;

                if (inp.Contains("partner"))
                    this._partner = Convert.ToInt32(inp["partner"]);
                else
                    this._partner = 6126;

                if (inp.Contains("comment"))
                    this._comment = inp["comment"].ToString();
                else
                    this._comment = "";

                JsonArray arrTurists = inp["tourists"] as JsonArray;

                this._infoTurists = new TuristContainer[arrTurists.Length];

                for (int i = 0; i < arrTurists.Length; i++)
                    this._infoTurists[i] = new TuristContainer(arrTurists[i] as JsonObject);

                string rate = inp["rate"].ToString();
                decimal price = Convert.ToDecimal(inp["price"]);
                decimal netto = Convert.ToDecimal(inp["netto"]);

                var courses = MtHelper.GetCourses(MtHelper.rate_codes, rate, DateTime.Today);
                _prices = MtHelper.ApplyCourses(price, courses);
                _nettos = MtHelper.ApplyCourses(netto, courses);

            }
            catch (Exception)
            {
                throw new Exception("cann't parse Excursion from " + inp.ToString());
            }
        }


        [JsonMemberName("date")]
        public DateTime Date
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

        [JsonMemberName("country")]
        public int Country
        {
            get
            {
                return this._country;
            }
            set
            {
                this._country = value;
            }
        }

        [JsonMemberName("city")]
        public int City
        {
            get
            {
                return this._city;
            }
            set
            {
                this._city = value;
            }
        }

        [JsonMemberName("name")]
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }

        [JsonMemberName("typeTransport")]
        public int TypeTransport
        {
            get
            {
                return this._typeTransport;
            }
            set
            {
                this._typeTransport = value;
            }
        }

        [JsonMemberName("partner")]
        public int Partner
        {
            get
            {
                return this._partner;
            }
            set
            {
                this._partner = value;
            }
        }

        [JsonMemberName("comment")]
        public string Comment
        {
            get
            {
                return this._comment;
            }
            set
            {
                this._comment = value;
            }
        }

        [JsonMemberName("price")]
        public JsonObject Price
        {
            get
            {
                JsonObject obj2 = new JsonObject();

                if (this._prices != null)
                    obj2.Add(_prices[0].Key, _prices[0].Value);

                return obj2;
            }
            set
            {
                List<KeyValuePair<string, decimal>> list = new List<KeyValuePair<string, decimal>>();
                JsonObject obj2 = value;
                foreach (string str in obj2.Names)
                {
                    list.Add(new KeyValuePair<string, decimal>(str, Convert.ToDecimal(obj2[str])));
                }
                this._prices = list.ToArray();
            }
        }

        [JsonMemberName("netto")]
        public JsonObject Netto
        {
            get
            {
                JsonObject obj2 = new JsonObject();

                if (this._nettos != null)
                    obj2.Add(_nettos[0].Key, _nettos[0].Value);

                return obj2;
            }
            set
            {
                List<KeyValuePair<string, decimal>> list = new List<KeyValuePair<string, decimal>>();
                JsonObject obj2 = value;
                foreach (string str in obj2.Names)
                {
                    list.Add(new KeyValuePair<string, decimal>(str, Convert.ToDecimal(obj2[str])));
                }
                this._nettos = list.ToArray();
            }
        }

        [JsonIgnore]
        public KeyValuePair<string, decimal>[] Prices
        {
            get
            {
                return this._prices;
            }
            set
            {
                this._prices = value;
            }
        }

        [JsonIgnore]
        public KeyValuePair<string, decimal>[] Nettos
        {
            get
            {
                return this._nettos;
            }
            set
            {
                this._nettos = value;
            }
        }


        [JsonIgnore]
        public TuristContainer[] InfoTurists
        {
            get
            {
                return this._infoTurists;
            }
            set
            {
                this._infoTurists = value;
            }
        }

        [JsonMemberName("turists")]
        public int[] Turists
        {
            get
            {
                return this._turists;
            }
            set
            {
                this._turists = value;
            }
        }

    }
}