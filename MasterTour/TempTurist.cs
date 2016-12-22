using TravelSoftMiddleOffice.ParamsContainers;

namespace TopTourMiddleOffice.MasterTour
{
    using System;
    using TopTourMiddleOffice.ParamsContainers;

    public class TempTurist
    {
        private DateTime birthDate;
        private string citizen;
        private string fName;
        private int gender;
        private string id;
        private string name;
        private DateTime passpDate;
        private string passpNum;
        private VisaContainer visa;

        public DateTime BirthDate
        {
            get
            {
                return this.birthDate;
            }
            set
            {
                this.birthDate = value;
            }
        }

        public string Citizen
        {
            get
            {
                return this.citizen;
            }
            set
            {
                this.citizen = value;
            }
        }

        public string FName
        {
            get
            {
                return this.fName;
            }
            set
            {
                this.fName = value;
            }
        }

        public int Gender
        {
            get
            {
                return this.gender;
            }
            set
            {
                this.gender = value;
            }
        }

        public string Id
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

        public string Name
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

        public DateTime PasspDate
        {
            get
            {
                return this.passpDate;
            }
            set
            {
                this.passpDate = value;
            }
        }

        public string PasspNum
        {
            get
            {
                return this.passpNum;
            }
            set
            {
                this.passpNum = value;
            }
        }

        public VisaContainer Visa
        {
            get
            {
                return this.visa;
            }
            set
            {
                this.visa = value;
            }
        }
    }
}

