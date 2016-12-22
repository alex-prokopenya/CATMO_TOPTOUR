namespace TopTourMiddleOffice.MasterTour
{
    using System;

    public class ServiceDetails
    {
        private int city;
        private int code;
        private int country;
        private int packetKey;
        private int partnerKey;
        private int subCode1;
        private int subCode2;

        public int City
        {
            get
            {
                return this.city;
            }
            set
            {
                this.city = value;
            }
        }

        public int Code
        {
            get
            {
                return this.code;
            }
            set
            {
                this.code = value;
            }
        }

        public int Country
        {
            get
            {
                return this.country;
            }
            set
            {
                this.country = value;
            }
        }

        public int PacketKey
        {
            get
            {
                return this.packetKey;
            }
            set
            {
                this.packetKey = value;
            }
        }

        public int PartnerKey
        {
            get
            {
                return this.partnerKey;
            }
            set
            {
                this.partnerKey = value;
            }
        }

        public int SubCode1
        {
            get
            {
                return this.subCode1;
            }
            set
            {
                this.subCode1 = value;
            }
        }

        public int SubCode2
        {
            get
            {
                return this.subCode2;
            }
            set
            {
                this.subCode2 = value;
            }
        }
    }
}

