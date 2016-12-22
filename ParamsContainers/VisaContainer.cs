namespace TravelSoftMiddleOffice.ParamsContainers
{
    using Jayrock.Json;
    using Jayrock.Json.Conversion;
    using System;

    public class VisaContainer
    {
        private int _code;
        private int _partner;
        private int _price;
        private string _rate;
        private int _sv_key;

        public VisaContainer()
        {
        }

        public VisaContainer(JsonObject inp)
        {
            if (inp != null)
            {
                try
                {
                    this._sv_key = Convert.ToInt32(inp["sv_key"]);
                    this._partner = Convert.ToInt32(inp["partner"]);
                    this._price = Convert.ToInt32(inp["price"]);
                    this._code = Convert.ToInt32(inp["code"]);
                    this._rate = inp["rate"].ToString();
                }
                catch (Exception)
                {
                    throw new Exception("cann't parse VisaContainer object from " + inp.ToString());
                }
            }
        }

        [JsonMemberName("code")]
        public int Code
        {
            get
            {
                return this._code;
            }
            set
            {
                this._code = value;
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

        [JsonMemberName("price")]
        public int Price
        {
            get
            {
                return this._price;
            }
            set
            {
                this._price = value;
            }
        }

        [JsonMemberName("rate")]
        public string Rate
        {
            get
            {
                return this._rate;
            }
            set
            {
                this._rate = value;
            }
        }

        [JsonMemberName("sv_key")]
        public int Sv_key
        {
            get
            {
                return this._sv_key;
            }
            set
            {
                this._sv_key = value;
            }
        }
    }
}

