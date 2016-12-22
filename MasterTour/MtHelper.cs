using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

using Megatec.MasterTour.BusinessRules;
using Megatec.MasterTour.DataAccess;

using System.Data.Sql;
using System.Data.SqlClient;
using TopTourMiddleOffice.Exceptions;
using TopTourMiddleOffice.Responses;
using TopTourMiddleOffice.Store;
using TopTourMiddleOffice.Helpers;
using TopTourMiddleOffice.ParamsContainers;
using TopTourMiddleOffice.Containers.Hotels;
using TopTourMiddleOffice.Containers.Excursions;
using TopTourMiddleOffice.Containers.Transfers;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Jayrock.Json.Conversion;
using Jayrock.Json;
using Megatec.Common.BusinessRules.Base;
using TravelSoftMiddleOffice.Containers.Excursion;
using TravelSoftMiddleOffice.ParamsContainers;


namespace TopTourMiddleOffice.MasterTour
{
    public enum PacketType
    {
        Default = 0,
        Russia = 1,
        Europe = 2,
        Other = 3
    }

    public class MtHelper
    {

        #region PrivateFields rate_codes, rate_courses

        public static string[] rate_codes = new string[] { "RUB", "USD", "EUR", "BYN" };

        private KeyValuePair<string, decimal>[] rate_courses;

        #endregion

        private static int serviceAnnulateStatusKey = 13; //ключ статуса аннулированной услуги
        private static int dogovorAnnulateStatusKey = 22; //ключ статуса аннулированной путевки
        //private static string salt_key = "laypistrubezkoi"; //secretkey для подписи ссылки на документ
        private static string BaseRate = "BYN";

        private static int PacketKey(PacketType type)
        {
            try
            {
                switch (type)
                {
                    case PacketType.Default:
                        return Convert.ToInt32(ConfigurationManager.AppSettings["PacketKeyDefault"]);

                    case PacketType.Russia:
                        return Convert.ToInt32(ConfigurationManager.AppSettings["PacketKeyRussia"]);

                    case PacketType.Europe:
                        return Convert.ToInt32(ConfigurationManager.AppSettings["PacketKeyEurope"]);

                    case PacketType.Other:
                        return Convert.ToInt32(ConfigurationManager.AppSettings["PacketKeyOther"]);

                    default:
                        return Convert.ToInt32(ConfigurationManager.AppSettings["PacketKeyDefault"]);
                }
            }
            catch
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["PacketKeyDefault"]);
            }

        }

        private static int PacketKeyDefault
        {
            get
            {
                try
                {
                    return Convert.ToInt32(ConfigurationManager.AppSettings["PacketKeyDefault"]);
                }
                catch
                {
                    throw new Exception("Value 'PacketKeyDefault' not found in web.config.");
                }
            }
        }

        private static ServiceDetails GetServiceDetails(int serviceClass, string variantCode, DateTime date, PacketType type, string title = "")
        {
            int packetKey = PacketKey(type);

            switch (serviceClass)
            {
                case Service.Transfert:
                    {
                        char[] separator = new char[] { '_' };
                        string[] strArray2 = variantCode.Split(separator);
                        int num5 = Convert.ToInt32(strArray2[0]);
                        int num6 = Convert.ToInt32(strArray2[1]);
                        Transfert transferByName = GetTransferByName(title);
                        return new ServiceDetails { PacketKey = packetKey, PartnerKey = num5, Code = transferByName.Key, City = transferByName.CityKey, Country = transferByName.Country.Key, SubCode1 = num6, SubCode2 = 0 };
                    }

                case Service.Hotel:
                    //2045_ve_2444_29
                    var hotelParts = variantCode.Split('_');
                    int hotelId = Convert.ToInt32(hotelParts[0]);

                    Hotels hts = new Hotels(new Megatec.Common.BusinessRules.Base.DataContainer());
                    hts.RowFilter = "hd_key = " + hotelId;
                    hts.Fill();

                    int pKey = 1;
                    Costs csts = new Costs(new Megatec.Common.BusinessRules.Base.DataContainer());
                    csts.RowFilter = "cs_code = " + hotelId + " and cs_subcode1 = " + Convert.ToInt32(hotelParts[2]);
                    csts.RowFilter += " and cs_subcode2 = " + Convert.ToInt32(hotelParts[3]) + " and cs_svkey = " + Service.Hotel;
                    csts.RowFilter += " and cs_pkkey = " + packetKey + " and  cs_date <= '" + date.ToString("yyyy-MM-dd") + "' and cs_dateend >= '" + date.ToString("yyyy-MM-dd") + "'";
                    csts.Fill();

                    if (csts.Count > 0)
                        pKey = csts[0].PartnerKey;

                    if (hts.Count > 0)
                        return new ServiceDetails() {
                            PacketKey = packetKey,
                            PartnerKey = pKey,
                            Code = hotelId,
                            City = hts[0].CityKey,
                            Country = hts[0].CountryKey,
                            SubCode1 = Convert.ToInt32(hotelParts[2]),
                            SubCode2 = Convert.ToInt32(hotelParts[3])
                        };
                    else
                        return null;
            }

            return null;
        }

        private static ServiceDetails GetServiceDetails(ExcursionBooking ex)
        {

            Excursions excs = new Excursions(new Megatec.Common.BusinessRules.Base.DataContainer());
            excs.RowFilter = String.Format("ed_cnkey = {0} and ed_ctkey = {1} and ed_name = '{2}'", ex.Country, ex.City,
                ex.Name);
            excs.Fill();

            if (excs.Count > 0)
                return new ServiceDetails()
                {
                    PacketKey = PacketKeyDefault,
                    PartnerKey = ex.Partner,
                    Code = excs[0].Key,
                    City = ex.City,
                    Country = ex.Country,
                    SubCode1 = ex.TypeTransport,
                    SubCode2 = 0
                };
            else
            {
                Excursion excursion = excs.NewRow();

                excursion.CountryKey = ex.Country;
                excursion.CityKey = ex.City;
                excursion.Name = ex.Name;
                excursion.NameLat = ex.Name;

                excs.Add(excursion);
                excs.DataContainer.Update();

                return new ServiceDetails()
                {
                    PacketKey = PacketKeyDefault,
                    PartnerKey = ex.Partner,
                    Code = excursion.Key,
                    City = ex.City,
                    Country = ex.Country,
                    SubCode1 = ex.TypeTransport,
                    SubCode2 = 0
                };
            }


        }

        private static Transfert GetTransferByName(string title)
        {
            if (title.Length > 100)
            {
                title = title.Substring(100);
            }
            Transferts transferts = new Transferts(new DataContainer())
            {
                RowFilter = "tf_name = '" + title + "' and tf_ctkey = 630"
            };
            transferts.Fill();
            if (transferts.Count > 0)
            {
                Logger.WriteToLog("transfer exists");
                return transferts[0];
            }
            Logger.WriteToLog("new transfer added");
            Transfert dataRow = transferts.NewRow();
            dataRow.Name = title;
            dataRow.NameLat = title;
            dataRow.CityKey = 630;
            transferts.Add(dataRow);
            transferts.DataContainer.Update();
            return dataRow;
        }

        private static decimal GetCourse(string rate1, string rate2, DateTime date)
        {
            RealCourses courses = new RealCourses(new DataContainer());
            object[] args = new object[] { rate1, rate2, date.ToString("yyyy-MM-dd"), date.AddDays(-14.0).ToString("yyyy-MM-dd") };
            string message = string.Format("RC_RCOD2 = (select top 1  ra_code from dbo.Rates where RA_ISOCode = '{0}') and RC_RCOD1 = (select top 1  ra_code from dbo.Rates where RA_ISOCode = '{1}') and RC_DATEBEG <='{2}' and RC_DATEBEG > '{3}'", args);
            courses.RowFilter = message;
            Logger.WriteToLog(message);
            courses.Sort = "RC_DATEBEG desc";
            courses.Fill();
            if (courses.Count > 0)
            {
                return Convert.ToDecimal(courses[0].Course);
            }
            courses = new RealCourses(new DataContainer());
            object[] objArray2 = new object[] { rate1, rate2, date.ToString("yyyy-MM-dd"), date.AddDays(-14.0).ToString("yyyy-MM-dd") };
            string str2 = string.Format("RC_RCOD1 = (select top 1  ra_code from dbo.Rates where RA_ISOCode = '{0}') and RC_RCOD2 = (select top 1  ra_code from dbo.Rates where RA_ISOCode = '{1}') and RC_DATEBEG <='{2}' and RC_DATEBEG > '{3}'", objArray2);
            courses.RowFilter = str2;
            Logger.WriteToLog(str2);
            courses.Sort = "RC_DATEBEG desc";
            courses.Fill();
            if (courses.Count > 0)
            {
                return (decimal.One / Convert.ToDecimal(courses[0].Course));
            }
            if ((rate1 != BaseRate) && (rate2 != BaseRate))
            {
                return (GetCourse(BaseRate, rate1, date) / GetCourse(BaseRate, rate2, date));
            }
            string[] textArray1 = new string[] { "Course not founded for date and rates ", rate1, " ", rate2, " ", date.ToString() };
            throw new CatmoException(string.Concat(textArray1));
        }

        public static KeyValuePair<string, decimal>[] GetCourses(string[] iso_codes, string base_rate, DateTime date)
        {
            //check redis cache
            string key_for_redis = "courses_" + base_rate + "b" + iso_codes.Aggregate((a, b) => a + "," + b) + "d" + date.ToString();

            KeyValuePair<string, decimal>[] res = new KeyValuePair<string, decimal>[iso_codes.Length];

            string cache = RedisHelper.GetString(key_for_redis);

            if ((cache != null) && (cache.Length > 0))
            {
                try
                {
                    var pairs = cache.Split(';');

                    var kvps = pairs.Select<string, KeyValuePair<string, decimal>>(x =>
                    {
                        string[] arr = x.Split('=');
                        return new KeyValuePair<string, decimal>(arr[0], Convert.ToDecimal(arr[1]));
                    }).ToArray();

                    if (kvps.Length == res.Length)
                    {
                        Logger.WriteToLog("from redis");
                        return kvps;
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteToLog(ex.Message + ex.StackTrace);
                }
            }
            //string str1 = "";
            //get data from MasterTour
            int cnt = 0;
            foreach (string code in iso_codes)
            {
               
                if (code != base_rate)
                    res[cnt++] = new KeyValuePair<string, decimal>(code, getCourse(code, base_rate, date));
                else
                    res[cnt++] = new KeyValuePair<string, decimal>(code, 1M);
            }
            Logger.WriteToLog("from mt");
            //конвертируем массив в строку
            var str = res.Select(kvp => String.Format("{0}={1}", kvp.Key, kvp.Value));
            string value_for_redis = string.Join(";", str);
            //save_to redis
            RedisHelper.SetString(key_for_redis, value_for_redis);

            return res;
        }

        private static decimal getCourse(string rate1, string rate2, DateTime date)
        {
            RealCourses rcs = new RealCourses(new Megatec.Common.BusinessRules.Base.DataContainer());
            //click1.dbo.
            //DateTime date1 = Convert.ToDateTime(date);
            string filter = String.Format("RC_RCOD2 = (select top 1  ra_code from Rates where RA_ISOCode = '{0}') and " +
                                          "RC_RCOD1 = (select top 1  ra_code from Rates where RA_ISOCode = '{1}') and " +
                                          "RC_DATEBEG <='{2}' and RC_DATEBEG > '{3}'", rate1, rate2, date.ToString("yyyy-MM-dd"), date.AddDays(-14).ToString("yyyy-MM-dd"));
            rcs.RowFilter = filter;

            Logger.WriteToLog(filter);
            rcs.Sort = "RC_DATEBEG desc";

            rcs.Fill();

            if (rcs.Count > 0)
                return Convert.ToDecimal(rcs[0].Course);

            else
            {
                rcs = new RealCourses(new Megatec.Common.BusinessRules.Base.DataContainer());

                string filter2 = String.Format("RC_RCOD1 = (select top 1  ra_code from Rates where RA_ISOCode = '{0}') and " +
                                                    "RC_RCOD2 = (select top 1  ra_code from Rates where RA_ISOCode = '{1}') and " +
                                                    "RC_DATEBEG <='{2}' and RC_DATEBEG > '{3}'", rate1, rate2, date.ToString("yyyy-MM-dd"), date.AddDays(-14).ToString("yyyy-MM-dd"));
                rcs.RowFilter = filter2;
                Logger.WriteToLog(filter2);
                rcs.Sort = "RC_DATEBEG desc";

                rcs.Fill();
                if (rcs.Count > 0)
                    return  1 / Convert.ToDecimal(rcs[0].Course);
            }

            throw new CatmoException("Course not founded for date and rates "+ rate1+ " " + rate2 + " " + date.ToString());
        }




        //применяет пачку курсов к цене
        public static KeyValuePair<string, decimal>[] ApplyCourses(decimal price, KeyValuePair<string, decimal>[] courses)
        {
            price = Math.Round(price, 2);

            KeyValuePair<string, decimal>[] res = new KeyValuePair<string, decimal>[courses.Length];

            for (int i = 0; i < courses.Length; i++)
            {

                if (courses[i].Key != BaseRate)
                    res[i] = new KeyValuePair<string, decimal>(courses[i].Key, Math.Round(price / courses[i].Value));
                else
                    res[i] = new KeyValuePair<string, decimal>(courses[i].Key, Math.Round(price / courses[i].Value, 2));
                //res[i] = new KeyValuePair<string, decimal>(courses[i].Key, Math.Round((price+ (decimal)0.5)/courses[i].Value, 2));
                Logger.WriteToLog(string.Concat(new object[] { "before apply =", price, " course =", courses[i].Value, " val = ", courses[i].Key, " summ= ", res[i].Value, " ", res[i].Key }));
            }

            return res;
        }


        // не используется
        //public static Dogovor SaveNewDogovor(int[] bookIds, UserInfo userInfo, PacketType type)
        //{
        //    Dogovors dogs = new Dogovors(new Megatec.Common.BusinessRules.Base.DataContainer());
        //    Dogovor dog = dogs.NewRow();
        //    try
        //    {
        //        DupUsers dups = new DupUsers(new Megatec.Common.BusinessRules.Base.DataContainer());
        //        dups.RowFilter = "us_id='" + AntiInject(userInfo.UserLogin) + "'";
        //        dups.Fill();

        //        List<TempService> tempServices = GetBookedServices(bookIds, type);
        //        Rates rates = new Rates(new DataContainer())
        //        {
        //            RowFilter = "RA_ISOCode = '" + tempServices[0].Rate + "'"
        //        };

        //        dog.CountryKey = tempServices[0].Details.Country;// !!! ГОРОД ПЕРВОЙ УСЛУГИ
        //        dog.CityKey = tempServices[0].Details.City;      // !!! СТРАНА ПЕРВОЙ УСЛУГИ
        //        dog.TurDate = DateTime.Today.AddDays(380);
        //        dog.NDays = 1;
        //        dog.MainMenEMail = userInfo.Email;
        //        dog.MainMenPhone = userInfo.Phone;
        //        dog.TourKey = PacketKey(type);     //*NETTO for agents (SPO)
        //        dog.PartnerKey = dups[0].PartnerKey;  //!!!!покупатель
        //        dog.DupUserKey = dups[0].Key;  //!!!!

        //        dog.CreatorKey = Convert.ToInt32(ConfigurationManager.AppSettings["CreatorKey"]);
        //        dog.OwnerKey = dog.CreatorKey;
        //        dog.RateCode = rates[0].Code;
        //        dog.PaymentDate = DateTime.Now.AddMinutes(45);
        //        dogs.Add(dog);

        //        dogs.DataContainer.Update();

        //        List<string> tempTuristsIds = new List<string>();

        //        foreach (TempService tfl in tempServices)
        //            tempTuristsIds.AddRange(tfl.Turists);

        //        TempTurist[] tempTurists = GetTurists(tempTuristsIds.ToArray());

        //        Dictionary<int, List<string>> serviceToTuristLink = SaveNewServices(dog.DogovorLists, tempServices); //возвращает ссылки на туристов
        //        SaveNewTurists(dog, tempTurists, serviceToTuristLink);

        //        dog.CalculateCost();
        //        MyCalculateCost(dog);

        //        dog.NMen = (short)dog.Turists.Count;
        //        dog.DataContainer.Update();

        //        SqlConnection conn = new SqlConnection(Manager.ConnectionString);
        //        conn.Open();
        //        SqlCommand com = conn.CreateCommand();
        //        com.CommandText = "update tbl_dogovor set dg_creator=" + Convert.ToInt32(ConfigurationManager.AppSettings["CreatorKey"]) + ", dg_owner=" + 100130 + ", dg_filialkey = (select top 1 us_prkey from userlist where us_key = " + 100130 + ") where dg_code='" + dog.Code + "'";
        //        com.ExecuteNonQuery();
        //        conn.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteToLog(ex.Message + " " + ex.StackTrace);
        //        throw ex;
        //    }

        //    return dog;
        //}

        public static Dogovor SaveNewDogovor(int[] bookIds, UserInfo userInfo, string login, PacketType type)
        {
            Logger.WriteToLog(Manager.ConnectionString);
            Dogovors dogovors = new Dogovors(new DataContainer());
            Dogovor dataRow = dogovors.NewRow();
            try
            {
                List<TempService> bookedServices = GetBookedServices(bookIds, type);

                Rates rates = new Rates(new DataContainer())
                {
                    RowFilter = "RA_ISOCode = '" + bookedServices[0].Rate + "'"
                };
                rates.Fill();
                dataRow.CountryKey = bookedServices[0].Details.Country;
                dataRow.CityKey = bookedServices[0].Details.City;
                dataRow.TurDate = DateTime.Today.AddDays(380.0);
                dataRow.NDays = 1;
                dataRow.TourKey = PacketKey(type);
                dataRow.PartnerKey = 0;
                if (login != "")
                {
                    DupUsers users = new DupUsers(new DataContainer())
                    {
                        RowFilter = "us_id='" + AntiInject(login) + "'"
                    };
                    users.Fill();
                    if (users.Count == 0)
                    {
                        throw new Exception("not found user with login " + login);
                    }
                    dataRow.PartnerKey = users[0].PartnerKey;
                    dataRow.DupUserKey = users[0].Key;
                }
                dataRow.CreatorKey = Convert.ToInt32(ConfigurationManager.AppSettings["CreatorKey"]);
                dataRow.OwnerKey = dataRow.CreatorKey;
                dataRow.RateCode = rates[0].Code;
                dataRow.PaymentDate = DateTime.Now.AddMinutes(45.0);
                dogovors.Add(dataRow);
                dogovors.DataContainer.Update();
                List<string> list2 = new List<string>();
                foreach (TempService service in bookedServices)
                {
                    list2.AddRange(service.Turists);
                }
                TempTurist[] turists = GetTurists(list2.ToArray());
                Dictionary<int, List<string>> serviceToTurist = SaveNewServices(dataRow.DogovorLists, bookedServices);
                SaveNewTurists(dataRow, turists, serviceToTurist);
                dataRow.CalculateCost();
                dataRow.NMen = (short)dataRow.Turists.Count;
                dataRow.DataContainer.Update();
                SqlConnection connection = new SqlConnection(Manager.ConnectionString);
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                object[] objArray1 = new object[] { "update tbl_dogovor set DG_MAINMENEMAIL='", userInfo.Email, "' , DG_MAINMENPHONE='", userInfo.Phone, "' , dg_creator=", dataRow.CreatorKey, ", dg_owner=", dataRow.CreatorKey, ", dg_filialkey = (select top 1 us_prkey from userlist where us_key = ", dataRow.CreatorKey, ") where dg_code='", dataRow.Code, "'" };
                command.CommandText = string.Concat(objArray1);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception exception)
            {
                Logger.WriteToLog(exception.Message + " " + exception.StackTrace);
                throw exception;
            }
            return dataRow;
        }

        private static List<TempService> GetBookedServices(int[] bookIds, PacketType type)
        {
            //получить услуги по бук айди
            List<TempService> tempServices = GetHotels(bookIds, type);
            tempServices.AddRange(GetTransfers(bookIds, type));
            tempServices.AddRange(GetExcursions(bookIds));
            return tempServices;
        }

        public static void SaveNewTurists(Dogovor dogovor, TempTurist[] tsts, Dictionary<int, List<string>> serviceToTurist)
        {
            TuristServices tServices = new TuristServices(new Megatec.Common.BusinessRules.Base.DataContainer());   //берем объект TuristServices
            int num3;
            List<Turist> list = new List<Turist>();

            foreach (TempTurist iTst in tsts) //если добавляются новые туристы
            {
                Turist tst = dogovor.Turists.NewRow();          // создаем новый объект "турист"
                tst.NameRus = iTst.Name;                        // проставляем имя
                tst.NameLat = iTst.Name;
                tst.FNameRus = iTst.FName;                      // проставляем Фамилию
                tst.FNameLat = iTst.FName;
                tst.SNameRus = "";                              // проставляем отчество
                tst.SNameLat = "";
                tst.Birthday = iTst.BirthDate;                  //дату рождения
                tst.CreatorKey = Convert.ToInt32(ConfigurationManager.AppSettings["CreatorKey"]);                        //создатель - Он-лайн

                tst.PasportDateEnd = iTst.PasspDate;

                if (iTst.PasspNum.Length > 2)
                {
                    tst.PasportNum = iTst.PasspNum.Substring(2);      //номер и ...
                    tst.PasportType = iTst.PasspNum.Substring(0, 2);     //... серия паспорта
                }
                
                tst.DogovorCode = dogovor.Code;              //код путевки
                tst.DogovorKey = dogovor.Key;                //ключ путевки
                tst.PostIndex = iTst.Id;


                tst.Citizen = iTst.Citizen;
                //if (iTst.Citizen.Length > 2)
                //    tst.Citizen = iTst.Citizen.Substring(2, 2);          //код гражданства туриста
                //else
                //    tst.Citizen = iTst.Citizen;

                if (iTst.Gender == 1)                  //пол туриста
                {
                    tst.RealSex = Turist.RealSex_Female;
                    if (tst.Age > 14)                        //ребенок или взрослый в зависимости от возраста
                        tst.Sex = Turist.Sex_Female;
                    else
                        tst.Sex = Turist.Sex_Child;
                }
                else
                {
                    tst.RealSex = Turist.RealSex_Male;
                    if (tst.Age > 14)
                        tst.Sex = Turist.Sex_Male;
                    else
                        tst.Sex = Turist.Sex_Child;
                }
                dogovor.Turists.Add(tst);                              //Добавляем к туристам в путевке 
                dogovor.Turists.DataContainer.Update();                    //Сохраняем изменения
                list.Add(tst);

                foreach (DogovorList dl in dogovor.DogovorLists)       //Просматриваем услуги в путевке
                {
                   if((serviceToTurist.ContainsKey(dl.Key)) && (serviceToTurist[dl.Key].Contains(iTst.Id)))
                   {
                            dl.NMen += 1;                               //увеличиваем кол-во туристов на услуге
                            TuristService ts = tServices.NewRow();      //садим туриста на услугу
                            ts.Turist = tst;
                            ts.DogovorList = dl;
                            tServices.Add(ts);
                            tServices.DataContainer.Update();          //сохраняем изменения
                   }
                }
                dogovor.DogovorLists.DataContainer.Update();                //сохраняем изменения в услугах
            }
            for (int i = 0; i < tsts.Length; i = num3 + 1)
            {
                TempTurist turist3 = tsts[i];
                Turist mtTurist = list[i];
                if ((turist3.Visa != null) && (turist3.Visa.Sv_key > 0))
                {
                    AddVisaFoTurist(turist3, mtTurist, dogovor.DogovorLists);
                }
                num3 = i;
            }
            if (tsts.Length == 0) // если просто нужно привязать старых туристов к новой услуге
            {
                dogovor.Turists.Fill();
                dogovor.DogovorLists.Fill();

                foreach (DogovorList dl in dogovor.DogovorLists)
                { 
                        if(serviceToTurist.ContainsKey(dl.Key))
                            foreach (Turist tst in dogovor.Turists)
                            {
                                if (serviceToTurist[dl.Key].Contains(tst.PostIndex))
                                {
                                    dl.NMen += 1;                               //увеличиваем кол-во туристов на услуге
                                    TuristService ts = tServices.NewRow();      //садим туриста на услугу
                                    ts.Turist = tst;
                                    ts.DogovorList = dl;
                                    tServices.Add(ts);
                                    tServices.DataContainer.Update();
                                    dl.DataContainer.Update();
                                }
                            }
                }
            }
        }

        private static double CalcNetto(double brutto, int serviceKey, int partnerKey)
        {
            return 0.0;
        }

        public static Dictionary<int, List<string>> SaveNewServices(DogovorLists dls, List<TempService> services)
        {
            Dictionary<int, List<string>> result = new Dictionary<int, List<string>>();

            foreach (TempService srvc in services)                //По одной создаем услуги
            {
                DogovorList dl = dls.NewRow();                    //создаем объект

                DateTime date = srvc.Date;
                if (dl.Dogovor.TurDate > date)		//корректируем даты тура в путевке
                {
                    dl.Dogovor.TurDate = date;
                    dl.Dogovor.DataContainer.Update();
                }

                string[] parts = srvc.Name.Split('_');

                dl.NMen = 0;                                        //обнуляем кол-во туристов
             
                dl.ServiceKey = srvc.ServiceClass;                  //ставим тип услуги

                dl.SubCode1 = srvc.Details.SubCode1;              //..привязываем к ислуге в справочнике
                dl.SubCode2 = srvc.Details.SubCode2;                                    //..
                dl.TurDate = dl.Dogovor.TurDate;                    //копируем дату тура
                dl.TourKey = dl.Dogovor.TourKey;                    //ключ тура
                dl.PacketKey  = srvc.Details.PacketKey;                  //пакет
                dl.CreatorKey = dl.Dogovor.CreatorKey;              //копируем ключ создателя
                dl.OwnerKey   = dl.Dogovor.OwnerKey;                  //копируем ключ создателя
                //dl.DateBegin = System.Convert.ToDateTime(srvc.date);     //ставим дату начала услуги
                dl.PartnerKey = srvc.Details.PartnerKey;                    //ставим поставщика услуги
                dl.CountryKey = srvc.Details.Country;      //копируем страну
                dl.CityKey = srvc.Details.City;            //копируем город
                dl.Name = srvc.Name;
                dl.Code = srvc.Details.Code;//;AddServiceToServiceList(srvc);
                dl.Comment = srvc.Id.ToString();

                dl.BuildName();
                dl.CalculateCost(dl.NDays, date, dl.NDays);               

                dl.Brutto = srvc.Price;//ставим брутто
                dl.FormulaBrutto = ((dl.Brutto.ToString()).Contains(".") || (dl.Brutto.ToString()).Contains(",")) ? (dl.Brutto.ToString()).Replace(".", ",") : (dl.Brutto + ",00"); //копируем брутто в "formula"
    
                //double netto = CalcNetto(dl.Brutto, dl.ServiceKey, srvc.PartnerKey); //расчет нетто //
                dl.Netto = srvc.Netto;
                dl.FormulaNetto = ((dl.Netto.ToString()).Contains(".") || (dl.Netto.ToString()).Contains(",")) ? (dl.Netto.ToString()).Replace(".", ",") : (dl.Netto + ",00"); //копируем брутто в "formula"
				

                if (srvc.NDays > 0)
                {
                    dl.DateEnd = date.AddDays(srvc.NDays);

                    if (dl.ServiceKey != Service.Hotel)
                    {
                         dl.NDays = Convert.ToInt16(srvc.NDays);
                        // dl.Name += ", " + dl.NDays + " дней";
                    }
                    else
                        dl.NDays = Convert.ToInt16(srvc.NDays);
                }
                else
                    dl.DateEnd = date; //проставляем дату окончания услуги


                if (dl.DateEnd > dl.Dogovor.DateEnd)	//корректируем дату окончания тура в путевке
                {
                    dl.Dogovor.NDays += (short)(dl.DateEnd - dl.Dogovor.DateEnd).Days;
                    dl.Dogovor.DataContainer.Update();
                }

                dl.Day = (short)((date - dl.Dogovor.TurDate).Days + 1);	    //порядковый день
                dl.DataContainer.Update();                                 	//сохраняем изменения
                dls.Add(dl);                                           	    //добавляем в набор услуг
                dls.DataContainer.Update();

                List<string> tList = new List<string>();
                tList.AddRange(srvc.Turists);

                result.Add(dl.Key, tList);

                if ((srvc.Comment != null) && (srvc.Comment.Length > 0))
                {
                    AddCommentForDogovor(dl.Name + ":" + srvc.Comment, "", dl.Dogovor);
                }
            }

            foreach (DogovorList dl in dls)
            {
                dl.DateBegin = dl.Dogovor.TurDate.AddDays(dl.Day - 1);
                dl.DataContainer.Update();
            }

            return result;
        }

        private static void MyCalculateCost(Dogovor dog)                             //Расчитываем стоимость
        {
            MyCalculateCost(dog, "");
        }

        private static void MyCalculateCost(Dogovor dog, string promo)                             //Расчитываем стоимость
        {
            bool have_ins = false;

            int promo_disc = 0;

            dog.DogovorLists.Fill();
            foreach (DogovorList dl in dog.DogovorLists)                      //По всем услугам в путевке
            {
                try
                {
                    if ((dl.FormulaBrutto != "") && (dl.FormulaBrutto.IndexOf(",") > 0))                                 //если брутто услуги 0
                    {
                        dl.Brutto = Math.Round(System.Convert.ToDouble(dl.FormulaBrutto) * (100 - promo_disc)) / 100;    //проставляем брутто из поля "Formula"

                        dog.Price += dl.Brutto;
                        dl.DataContainer.Update();                                    //сохраняем изменения
                    }

                    if ((dl.FormulaNetto != "") && (dl.FormulaNetto.IndexOf(",") > 0))                                 //если брутто услуги 0
                    {
                        dl.Netto = System.Convert.ToDouble(dl.FormulaNetto);      //проставляем брутто из поля "Formula"
                        dl.DataContainer.Update();                                    //сохраняем изменения
                    }
                    dog.DataContainer.Update();

                    have_ins = have_ins || ((dl.ServiceKey == 1118) && (dl.Brutto != Math.Round(dl.Brutto)));
                }
                catch (Exception)
                {
                    //throw new Exception(ex.Message);
                }
            }

            if (!have_ins)
                dog.Price = Math.Round(dog.Price);

            //ерунда какая-то если одна услуга экскурсия не проставляет ее стоимость
            if ((dog.Price == 0) && (dog.DogovorLists.Count == 1))
            {
                dog.Price = dog.DogovorLists[0].Brutto;
                dog.DataContainer.Update();
            }
        }

        private static int AddServiceToServiceList(TempService srvc)
        {
            if (srvc.Name.Length > 50)
                srvc.Name = srvc.Name.Substring(0, 50);

            ServiceLists svs = new ServiceLists(new Megatec.Common.BusinessRules.Base.DataContainer());
            ServiceList sv = svs.NewRow();
            sv.Name = srvc.Name;
            sv.NameLat = srvc.Id.ToString();
            sv.ServiceKey = srvc.ServiceClass;
            svs.Add(sv);
            svs.DataContainer.Update();

            return sv.Key;
        }

        private static void AddVisaFoTurist(TempTurist turist, Turist mtTurist, DogovorLists dls)
        {
            DogovorList dataRow = dls.NewRow();
            dataRow.DateBegin = dataRow.Dogovor.TurDate;
            dataRow.Day = 1;
            dataRow.DateEnd = dataRow.Dogovor.DateEnd;
            dataRow.NDays = dataRow.Dogovor.NDays;
            dataRow.NMen = 1;
            dataRow.ServiceKey = turist.Visa.Sv_key;
            dataRow.SubCode1 = 0;
            dataRow.SubCode2 = 0;
            dataRow.TurDate = dataRow.Dogovor.TurDate;
            dataRow.TourKey = dataRow.Dogovor.TourKey;
            dataRow.PacketKey = dataRow.Dogovor.TourKey;
            dataRow.CreatorKey = dataRow.Dogovor.CreatorKey;
            dataRow.OwnerKey = dataRow.Dogovor.OwnerKey;
            dataRow.Name = "Виза для " + turist.Name + " " + turist.FName;
            dataRow.Code = turist.Visa.Code;
            dataRow.Brutto = turist.Visa.Price;
            dataRow.FormulaBrutto = ((dataRow.Brutto.ToString()).Contains(".") || (dataRow.Brutto.ToString()).Contains(",")) ? (dataRow.Brutto.ToString()).Replace(".", ",") : (dataRow.Brutto + ",00");
            dataRow.CountryKey = dataRow.Dogovor.CountryKey;
            dataRow.CityKey = dataRow.Dogovor.CityKey;
            dataRow.BuildName();
            dataRow.PartnerKey = turist.Visa.Partner;
            dataRow.DataContainer.Update();
            dls.Add(dataRow);
            dls.DataContainer.Update();
            TuristServices services = new TuristServices(new DataContainer());
            TuristService service = services.NewRow();
            service.Turist = mtTurist;
            service.DogovorList = dataRow;
            services.Add(service);
            services.DataContainer.Update();
        }

        public static List<TempService> GetFlights(int[] bookIds)
        {
            try
            {
                var partnerKeys = new Dictionary<string,int>();
                partnerKeys.Add("aw_", 7965);
                partnerKeys.Add("tk_", 9215);
                partnerKeys.Add("pb_", 8797);
                partnerKeys.Add("vt_", 7993);

                string ids = string.Join(",", bookIds);

                //получить список сообщений
                SqlConnection con = new SqlConnection(Manager.ConnectionString);
                con.Open();

                SqlCommand com = new SqlCommand(String.Format("select book_id,[ft_id],[ft_ticketid],[ft_route],[ft_date],[ft_price],[ft_turists] from [CATSE_Flights], [CATSE_book_id] where [ft_id] = service_id and book_id in(" + ids + ") and service_type='CATSE_Flights'"), con);

                SqlDataReader reader = com.ExecuteReader();

                List<TempService> tempList = new List<TempService>();

                while (reader.Read())
                {
                    string bookTurists = reader["ft_turists"].ToString();

                    string route = Convert.ToString(reader["ft_route"]).Trim();

                    string ticketId = reader["ft_ticketid"].ToString().Trim();

                    if (ticketId.Contains("@@@"))
                        ticketId = ticketId.Substring(0, ticketId.IndexOf("@@@"));

                    tempList.Add(new TempService()
                    {
                        Date = Convert.ToDateTime(reader["ft_date"]),
                        Id = Convert.ToInt32(reader["book_id"]),
                        Name = ticketId + "/" + route,
                        Price = Convert.ToInt32(reader["ft_price"]),
                        Turists = bookTurists.Split(','),
                        NDays = 0,
                        PartnerKey = partnerKeys[reader["ft_ticketid"].ToString().Substring(0,3)], // подставить партнера
                        ServiceClass = 1118
                    });
                }
                reader.Close();
                con.Close();

                return tempList;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(ex.Message + " " + ex.StackTrace);
                throw ex;
            }
        }

        public static List<TempService> GetHotels(int[] bookIds, PacketType type) //выгружаем отели
        {
            var prefixToKey = new Dictionary<string, int>();
            prefixToKey.Add("ve_", 7993 );

            try
            {
                Logger.WriteToLog("in get hotels");

                string ids = string.Join(",", bookIds);

                //получить список сообщений
                SqlConnection con = new SqlConnection(Manager.ConnectionString);
                con.Open();

                SqlCommand com = new SqlCommand(String.Format("select book_id, ht_hash from [CATSE_hotels], [CATSE_book_id] where [ht_id] = service_id and book_id in(" + ids + ") and service_type='CATSE_hotels'"), con);

                SqlDataReader reader = com.ExecuteReader();

                List<TempService> tempList = new List<TempService>();

                while (reader.Read())
                {
                    string hash = reader["ht_hash"].ToString();
                    HotelBooking[] htlBooks = JsonConvert.Import<HotelBooking[]>(hash);

                    int bookId = Convert.ToInt32(reader["book_id"]);

                    foreach (HotelBooking hb in htlBooks)
                    {
                        Logger.WriteToLog( "бронь отеля " + hb.PartnerPrefix + hb.PartnerBookId);

                        tempList.Add(new TempService()
                        {
                            Details = GetServiceDetails(Service.Hotel, hb.PartnerBookId, Convert.ToDateTime(hb.DateBegin), type),
                            Date = Convert.ToDateTime(hb.DateBegin),
                            Name = hb.PartnerBookId,
                            Id = bookId,
                            NDays = hb.NightsCnt,
                            PartnerKey = 1051,
                            Rate = hb.Prices[0].Key,
                            Price = Math.Round(Convert.ToDouble(hb.Prices[0].Value), 2),
                            ServiceClass = Service.Hotel,
                            Turists = string.Join(",", hb.Turists).Split(',')
                        });
                    }
                }

                Logger.WriteToLog("end get hotels");

                return tempList;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(ex.Message + " " + ex.StackTrace);
                throw ex;
            }
        }

        private static int GetPartnerByExcursion(int excId)
        {
            return 0;
        }

        public static List<TempService> GetExcursions(int[] bookIds) //выгружаем отели
        {
            var prefixToKey = new Dictionary<string, int>();

            try
            {
                Logger.WriteToLog("in get exc");

                string ids = string.Join(",", bookIds);

                //получить список сообщений
                SqlConnection con = new SqlConnection(Manager.ConnectionString);
                con.Open();

                SqlCommand com = new SqlCommand(String.Format("select book_id, ex_hash from [CATSE_excursions], [CATSE_book_id] where [ex_id] = service_id and book_id in(" + ids + ") and service_type='CATSE_excursions'"), con);

                SqlDataReader reader = com.ExecuteReader();

                List<TempService> tempList = new List<TempService>();

                while (reader.Read())
                {
                    string hash = reader["ex_hash"].ToString();
                    ExcursionBooking exBook = JsonConvert.Import<ExcursionBooking>(hash);

                    int bookId = Convert.ToInt32(reader["book_id"]);

                    Logger.WriteToLog("бронь экскурсии " + exBook.Name);

                    tempList.Add(new TempService()
                    {
                        Details = GetServiceDetails(exBook),
                        Date = exBook.Date,
                        Name = exBook.Name,
                        Id = bookId,
                        NDays = 1,
                        PartnerKey = 1051,
                        Price = Math.Round(Convert.ToDouble(exBook.Prices[0].Value), 2),
                        Netto = Math.Round(Convert.ToDouble(exBook.Nettos[0].Value), 2),
                        Rate = exBook.Prices[0].Key,
                        ServiceClass = Service.Excursion,
                        Turists = string.Join(",", exBook.Turists).Split(','),
                        Comment = exBook.Comment
                    });

                    //Logger.WriteToLog("бронь экскурсии прошла успешно");
                }

                return tempList;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(ex.Message + " " + ex.StackTrace);
                throw ex;
            }
        }

        public static List<TempService> GetTransfers(int[] bookIds, PacketType type) //выгружаем отели
        {
            List<TempService> list2;
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            try
            {
                Logger.WriteToLog("in get trf");
                string str = string.Join<int>(",", bookIds);
                //SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
                SqlConnection connection = new SqlConnection(Manager.ConnectionString);
                connection.Open();
                SqlDataReader reader = new SqlCommand(string.Format("select book_id, tr_hash from [CATSE_transfers], [CATSE_book_id] where [tr_id] = service_id and book_id in(" + str + ") and service_type='CATSE_transfers'", new object[0]), connection).ExecuteReader();
                List<TempService> list = new List<TempService>();
                while (reader.Read())
                {
                    TransferBooking booking = JsonConvert.Import<TransferBooking>(reader["tr_hash"].ToString());
                    int num = Convert.ToInt32(reader["book_id"]);
                    TempService item = new TempService
                    {
                        Details = GetServiceDetails(2, booking.TransferVariant.ServiceId, DateTime.MinValue, type, booking.TransferVariant.Title),
                        Date = Convert.ToDateTime(booking.TransferVariant.Date),
                        Name = "Трансфер:://Минск/" + booking.TransferVariant.Title,
                        Id = num,
                        NDays = 1,
                        PartnerKey = 1,
                        ServiceClass = 2,
                        Rate = booking.TransferVariant.Prices[0].Key,
                        Price = Math.Round(Convert.ToDouble(booking.TransferVariant.Prices[0].Value), 2)
                    };
                    char[] separator = new char[] { ',' };
                    item.Turists = string.Join(",", booking.Turists).Split(separator);
                    item.Comment = booking.TransferVariant.Info;
                    list.Add(item);
                }
                list2 = list;

                Logger.WriteToLog("end get trf");
            }
            catch (Exception exception)
            {
                Logger.WriteToLog(exception.Message + " " + exception.StackTrace);
                throw exception;
            }
            return list2;

            //var prefixToKey = new Dictionary<string, int>();

            //try
            //{
            //    Logger.WriteToLog("in get trf");

            //    string ids = string.Join(",", bookIds);

            //    //получить список сообщений
            //    SqlConnection con = new SqlConnection(Manager.ConnectionString);
            //    con.Open();

            //    SqlCommand com = new SqlCommand(String.Format("select book_id, tr_hash from [CATSE_transfers], [CATSE_book_id] where [tr_id] = service_id and book_id in(" + ids + ") and service_type='CATSE_transfers'"), con);

            //    SqlDataReader reader = com.ExecuteReader();

            //    List<TempService> tempList = new List<TempService>();

            //    while (reader.Read())
            //    {
            //        string hash = reader["tr_hash"].ToString();
            //        var trBook = JsonConvert.Import<TransferBooking>(hash);

            //        int bookId = Convert.ToInt32(reader["book_id"]);

            //        Logger.WriteToLog("бронь трансфера " + trBook.TransactionId);

            //        tempList.Add(new TempService()
            //        {
            //            Details = GetServiceDetails(2, trBook.TransferVariant.ServiceId, DateTime.MinValue, type, trBook.TransferVariant.Title),
            //            Date = Convert.ToDateTime(trBook.StartDate),
            //            Name = "Трансфер:://Минск/" + trBook.TransferVariant.Title,
            //            Id = bookId,
            //            NDays = 1,
            //            //NDays = (Convert.ToDateTime(trBook.EndDate) - Convert.ToDateTime( trBook.StartDate )).Days,
            //            PartnerKey = 1, //проставить IWAY
            //            Rate = trBook.TransferVariant.Prices[0].Key,
            //            Price = Math.Round(Convert.ToDouble(trBook.TransferVariant.Prices[0].Value), 2),
            //            ServiceClass = 1133, //проставить ТРАНСФЕР
            //            Turists = string.Join(",", trBook.Turists).Split(','),
            //            Comment = trBook.TransferVariant.Info
            //        });
            //    }

            //    return tempList;
            //}
            //catch (Exception ex)
            //{
            //    Logger.WriteToLog(ex.Message + " " + ex.StackTrace);
            //    throw ex;
            //}
        }

        public static TempTurist[] GetTurists(string[] turistsIds)
        {
            try
            {
                string ids = "-1";
                foreach (string ft_id in turistsIds) ids += "," + ft_id;
                //получить список сообщений
                SqlConnection con = new SqlConnection(Manager.ConnectionString);
                con.Open();

                SqlCommand com = new SqlCommand(String.Format("select [ts_name] ,[ts_fname] ,[ts_gender] ,[ts_id] ,[ts_passport] ,[ts_passportdate] ,[ts_birthdate] ,[ts_citizenship] from [CATSE_Turists] where [ts_id] in ({0})", ids), con);

                SqlDataReader reader = com.ExecuteReader();

                List<TempTurist> tempList = new List<TempTurist>();

                while (reader.Read())
                {
                    tempList.Add(new TempTurist()
                    {
                        BirthDate = Convert.ToDateTime( reader["ts_birthdate"]),
                        Citizen = Convert.ToString(reader["ts_citizenship"]).Trim(),
                        FName = Convert.ToString(reader["ts_fname"]).Trim(),
                        Name = Convert.ToString(reader["ts_name"]).Trim(),
                        PasspDate = Convert.ToDateTime(reader["ts_passportdate"]),
                        PasspNum = Convert.ToString(reader["ts_passport"]).Trim(),
                        Id = Convert.ToString(reader["ts_id"]).Trim(),
                        Gender = Convert.ToInt32(reader["ts_gender"]),
                    });
                }
                reader.Close();
                con.Close();

                return tempList.ToArray();
             }
            catch (Exception ex)
            {

                Logger.WriteToLog(ex.Message + " " + ex.StackTrace);
                throw ex;
            }
        }

        public static void AddCommentForDogovor(string text, string login, Dogovor dog)
        {
            Logger.WriteToLog("try to add comment");
            Histories histories = new Histories(new DataContainer());
            History dataRow = histories.NewRow();
            dataRow.Text = text;
            dataRow.DogovorCode = dog.Code;
            dataRow.Mode = "MTM";
            dataRow.MessEnabled = 1;
            dataRow.UserKey = login;
            histories.Add(dataRow);
            histories.DataContainer.Update();
            dataRow.DataContainer.Update();
            Logger.WriteToLog("after add comment");
        }

        public static int AddServiceToDogovor(Dogovor dogovor, int bookId, PacketType type)
        {
            try
            {
                dogovor.DogovorLists.Fill();
                dogovor.Turists.Fill();

               //проверяем, есть ли уже услуга в заказе
                if (ContainsBookId(dogovor.DogovorLists, bookId)) return 0;

                //получаем список услуг
                List<TempService> tempServices = GetBookedServices(new int[]{bookId}, type);

                //собираем список с id новых туристов
                List<string> tempTuristsIds = new List<string>();

                foreach (TempService tfl in tempServices)
                {
                    for (int i = 0; i < tfl.Turists.Length; i++)                    //есть ли новый для брони турист 
                        if (!ContainsTuristId(dogovor.Turists, tfl.Turists[i])) tempTuristsIds.Add(tfl.Turists[i]);// = "-1"; 

                    //tempTuristsIds.AddRange(tfl.Turists);
                }

                TempTurist[] tempTurists = GetTurists(tempTuristsIds.ToArray());

                Dictionary<int, List<string>> serviceToTurist =  SaveNewServices(dogovor.DogovorLists, tempServices);

                SaveNewTurists(dogovor, tempTurists, serviceToTurist);

                dogovor.CalculateCost();
                MyCalculateCost(dogovor);

                dogovor.NMen = (short)dogovor.Turists.Count;
                dogovor.DataContainer.Update();
                return 1;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(ex.Message + " " + ex.StackTrace);
            }

            return 0;
        }

        private static bool ContainsBookId(DogovorLists dls, int bookId)
        {
            foreach (DogovorList dl in dls)
            { 
                try
                {
                    if (Convert.ToInt32(dl.Comment) == bookId)  return true;
                }
                catch(Exception){}

            }
            return false;
        }

        private static bool ContainsTuristId(Turists tsts, string turistId)
        {
            foreach (Turist tst in tsts)
            {
                try
                {
                    if ((tst.PostIndex) == turistId) return true;
                }
                catch (Exception) { }

            }
            return false;
        }

        //изменяет статус услуги на "запрос на аннуляцию"
        public static void AnnulateService(string dogovorCode, int bookId)
        {
            DogovorLists dls = new DogovorLists(new Megatec.Common.BusinessRules.Base.DataContainer());
            dls.RowFilter = "dl_dgcod='" + dogovorCode + "' and DL_comment='" + bookId+"'";

            dls.Fill();

            if (dls.Count > 0)
            {
                dls[0].SetStatus(serviceAnnulateStatusKey);

                if (dls[0].Dogovor.Payed == 0)
                {
                    Dogovor dog = dls[0].Dogovor;

                    DeleteService(dls[0].Key);

                    dog.CalculateCost();

                    MyCalculateCost(dog);

                }
                else
                    dls[0].DataContainer.Update();
            }
            else
                throw new CatmoException("service not found", ErrorCodes.ServiceNotFound);
        }

        private static void DeleteService(int serviceKey)
        {
            SqlConnection con = new SqlConnection(Manager.ConnectionString);
            con.Open();
            SqlCommand com = new SqlCommand(String.Format("delete from tbl_dogovorlist where dl_key={0}", serviceKey), con);

            com.ExecuteNonQuery();

            con.Close();
        }

        //изменяет статус путевки на "запрос на аннуляцию"
        public static void AnnulateDogovor(string dogovorCode)
        {
            Dogovor dog = GetDogovor(dogovorCode);

            Mail.SendMail(dog.Owner.Mailbox, "Пользователь запросил аннуляцию путевки " + dogovorCode, "Аннуляция", dogovorCode);

            dog.SetStatus(dogovorAnnulateStatusKey);
            dog.DataContainer.Update();
        }

        //забирает путевки созданные пользователем
        public static Dogovors GetUserDogovors(string mail, int userkey)
        {
            Dogovors dogs = new Dogovors(new Megatec.Common.BusinessRules.Base.DataContainer());

            string filter = ""; //создаем пустой фильтр

            if (mail.Length > 0) //если пришел и-мэйл, добавим его к фильтру
                filter = " (DG_MAINMENEMAIL like '" + mail + "') ";

            if (userkey > 0) //если пришел юзер-айди, добавим его к фильтру
            {
                filter = filter.Length > 0 ? filter + " or " : ""; // если в фильтре уже есть условие добавим "или"

                filter += "(dg_dupuserkey = " + userkey + ")";
            }

            dogs.RowFilter = filter;   //применим фильтр
            dogs.Fill();               //комит

            return dogs;    //отдаем найденные путевки
        }

        //получить путевку
        public static Dogovor GetDogovor(string dg_code)
        {
          //  dg_code = "TPA3103012";

            Dogovors dogs = new Dogovors(new Megatec.Common.BusinessRules.Base.DataContainer()); 
            dogs.RowFilter = "dg_code = '"+dg_code+"'"; //ищем закакз по номеру

            dogs.Fill(); //комит

            if (dogs.Count > 0) //если заказ нашелся, отдаем его
                return dogs[0];
            else                //иначе
                throw new CatmoException("dogovor not found", ErrorCodes.DogovorNotFound); //генерируем эксепшн
        }

        //получить документы прикрепленные к путевке
        public static Responses.Document[] GetDogovorDocuments(string dg_code)
        {
            Dogovor dog = GetDogovor(dg_code);

            //получить список документов
            SqlConnection con = new SqlConnection(Manager.ConnectionString);
            con.Open();

            string query = String.Format(@"SELECT
                         FR_Name
                        ,FR_Guid
                    FROM ts_FileRepository
                    WHERE ((FR_Type = 0 and FR_AssociatedKey = {0})  OR (FR_Type = 1 and FR_AssociatedKey={1})) AND FR_ForWeb = 1", dog.TourKey, dog.Key);

            SqlCommand com = new SqlCommand(query, con);

            SqlDataReader reader = com.ExecuteReader();

            List<Responses.Document> tempList = new List<Responses.Document>();

            while (reader.Read())
            {
                tempList.Add(new Responses.Document()
                {
                    Title = reader["FR_Name"].ToString(),
                    Guid = reader["FR_Guid"].ToString()
                });
            }
            reader.Close();
            con.Close();

            return tempList.ToArray();
        }

        //получить сообщения прикрепленные к путевке
        public static Responses.DogovorMessage[] GetDogovorMessages(int dg_key, DateTime minDate)
        {
            //получить список сообщений
            SqlConnection con = new SqlConnection(Manager.ConnectionString);
            con.Open();

            SqlCommand com = new SqlCommand(String.Format("select [DM_IsOutgoing], [DM_CreateDate], [DM_Text] from [DogovorMessages] where [DM_DGKEY] = '{0}' and DM_CreateDate > '{1}' and dm_PROCESSED = 0  and DM_PRKEY is null", dg_key, minDate.ToString("yyyy-MM-dd HH:mm:ss")), con);

            SqlDataReader reader = com.ExecuteReader();

            List<Responses.DogovorMessage> tempList = new List<Responses.DogovorMessage>();

            while (reader.Read())
            {
                tempList.Add(new Responses.DogovorMessage()
                {
                    Date = Convert.ToDateTime(reader["DM_CreateDate"]), 
                    InOut = Convert.ToInt32(reader["DM_IsOutgoing"]),
                    Text = reader["DM_Text"].ToString()
                });
            }
            reader.Close();
            con.Close();

            return tempList.ToArray();
        }

        public static int SaveDogovorMessage(string code, string text)
        {
            Dogovor dog = GetDogovor(code);

            SqlConnection con = new SqlConnection(Manager.ConnectionString);
            con.Open();

            SqlCommand com = new SqlCommand(String.Format("insert into [DogovorMessages](DM_TypeCode, [DM_DGKey], DM_Text, [DM_IsOutgoing], DM_Remark) values ({0},{1},'{2}','{3}', '')", 0, dog.Key, AntiInject(text),0), con);

            try
            {
                com.ExecuteNonQuery();
            }
            catch (Exception)
            {
                con.Close();
                return 0;
            }

            con.Close();
            return 1;
        }

        private static string AntiInject(string inp)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("'", "`");
            pairs.Add("--", "- -");
            pairs.Add(" drop ", " dr op ");
            pairs.Add("insert", "inse rt");
            pairs.Add("select", "sel ect");
            pairs.Add("delete", "dele te");
            pairs.Add("update", "up date");

            foreach (string key in pairs.Keys)
                inp = Regex.Replace(inp, key, pairs[key], RegexOptions.IgnoreCase);

            return inp;
        }

        private static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }
    }
}