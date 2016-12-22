namespace TopTourMiddleOffice.MasterTour
{
    using System;

    public class TempService
    {
        private string comment;
        private DateTime date;
        private ServiceDetails details;
        private int id;
        private string name;
        private int nDays;
        private int partnerKey;
        private double price;
        private double netto;
        private string rate;
        private int serviceClass;
        private string[] turists;

        public string Comment
        {
            get
            {
                return this.comment;
            }
            set
            {
                this.comment = value;
            }
        }

        public DateTime Date //дата начала ...
        {
            get
            {
                return this.date;
            }
            set
            {
                this.date = value;
            }
        }

        public ServiceDetails Details
        {
            get
            {
                return this.details;
            }
            set
            {
                this.details = value;
            }
        }

        public int Id //book_id для связи с фронтом
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }

        public string Name //название...
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        public int NDays //продолжительность ...
        {
            get
            {
                return this.nDays;
            }
            set
            {
                this.nDays = value;
            }
        }

        public int PartnerKey  //ссылка на партнера
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

        public double Price //стоимость ...
        {
            get
            {
                return this.price;
            }
            set
            {
                this.price = value;
            }
        }

        public double Netto //стоимость ...
        {
            get
            {
                return this.netto;
            }
            set
            {
                this.netto = value;
            }
        }

        public string Rate
        {
            get
            {
                return this.rate;
            }
            set
            {
                this.rate = value;
            }
        }

        public int ServiceClass // тип услуги (отель, билет....)
        {
            get
            {
                return this.serviceClass;
            }
            set
            {
                this.serviceClass = value;
            }
        }

        public string[] Turists //список туристов...
        {
            get
            {
                return this.turists;
            }
            set
            {
                this.turists = value;
            }
        }
    }
}

