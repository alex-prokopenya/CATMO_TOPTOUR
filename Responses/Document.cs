using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json;
using Jayrock.Json.Conversion;

namespace TopTourMiddleOffice.Responses
{
    public class Document
    {
        private string _title;
        [JsonMemberName("title")]
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }


        private string _guid;
        [JsonMemberName("guid")]
        public string Guid
        {
            get { return _guid; }
            set { _guid = value; }
        }


        //private DateTime updated;
        //[JsonIgnore]
        //public DateTime Updated
        //{
        //    get { return updated; }
        //    set { updated = value; }
        //}


        //[JsonMemberName("up_date")]
        //public string UpdatedString
        //{
        //    get { return this.updated.ToString("dd.MM.yyyy HH:mm:ss"); }
        //    set { }
        //}
    }
}