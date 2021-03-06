﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json.Conversion;
using Jayrock.Json;

namespace TopTourMiddleOffice.Containers.Transfers
{
    public class TransferBooking
    {
        private TransferVariant _transferVariant;

        [JsonMemberName("variant")]
        public TransferVariant TransferVariant
        {
            get { return _transferVariant; }
            set { _transferVariant = value; }
        }

        private string _transactionId;

        [JsonMemberName("transactionId")]
        public string TransactionId
        {
            get { return _transactionId; }
            set { _transactionId = value; }
        }

        private string _startDate;
        [JsonMemberName("startDate")]
        public string StartDate
        {
            get { return _startDate; }
            set { _startDate = value; }
        }

        private string _endDate;
        [JsonMemberName("endDate")]
        public string EndDate
        {
            get { return _endDate; }
            set { _endDate = value; }
        }

        private string[] _turists;
        [JsonMemberName("turists")]
        public string[] Turists
        {
            get { return _turists; }
            set { _turists = value; }
        }

        private string search_id;
        [JsonMemberName("search_id")]
        public string SearchId
        {
            get { return search_id; }
            set { search_id = value; }
        }
    }
}