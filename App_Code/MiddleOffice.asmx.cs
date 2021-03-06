﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Configuration;

using TopTourMiddleOffice.ParamsContainers;
using TopTourMiddleOffice.Responses;
using TopTourMiddleOffice.Containers.Transfers;
using TopTourMiddleOffice.Containers.CarRent;
using TopTourMiddleOffice.Containers.Visa;
using TopTourMiddleOffice.Containers.Inurance;
using TopTourMiddleOffice.Containers.Excursions;
using TopTourMiddleOffice.Containers.Hotels;
using TopTourMiddleOffice.Helpers;
using TopTourMiddleOffice.MasterTour;

using System.ServiceModel;
using System.ServiceModel.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services.Protocols;

using Megatec.MasterTour.DataAccess;
using Megatec.MasterTour.BusinessRules;


using System.Data.SqlClient;
using System.Data;

using TopTourMiddleOffice.Exceptions;


namespace TopTourMiddleOffice
{
    [ServiceContractAttribute(Namespace = "http://schemas.myservice.com")]


    /// <summary>
    /// Summary description for Service1
    /// </summary>
    [WebService(Namespace = "http://clickandtravel.ru/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]

    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class MiddleOffice : System.Web.Services.WebService
    {
        public  MiddleOffice()
            : base()
        {
            //Manager.ConnectionString = ConfigurationManager.AppSettings["MasterTourConnectionString"];
            //  AppDomainManager.
            var sci = new Megatec.Connection.Initializer.SimpleConnectionStringInitiaizer();

            var cs = new Megatec.Connection.ConnectionConfiguration.ManualConnectionConfiguration(ConfigurationManager.AppSettings["ConnectionString"]);

            Megatec.Connection.ConnectionManager.InitializeConnectionStrings(sci, cs, false);
        }

        public static Dogovor GetDogovor(string code)
        {
            Dogovors dogs = new Dogovors(new DataCache());
            dogs.RowFilter = "dg_code='" + code + "'";
            dogs.Fill();

            if (dogs.Count == 0)
                throw new CatmoException("Не найдена путевка с номером " + code.Trim() + "!", ErrorCodes.AnotherException);

            Dogovor dog = dogs[0];

            if (dog.TurDate < System.Convert.ToDateTime("1900-01-01"))
                throw new CatmoException("Путевка с номером " + code.Trim() + " аннулирована!",  ErrorCodes.AnotherException);

            if (dog.TurDate < DateTime.Today)
                throw new CatmoException("По путевке " + code.Trim() + " уже наступила дата заезда!",  ErrorCodes.AnotherException);

            if ((dog.Price - dog.Payed) <= 0)
                throw new CatmoException("Путевка с номером " + code.Trim() + " уже оплачена!", ErrorCodes.AnotherException);

            if ((dog.OrderStatusKey == 2) || (dog.OrderStatusKey == 4))
                throw new CatmoException("Статус путевки " + dog.Code + " запрещает оплату",  ErrorCodes.AnotherException);

            return dog;
        }
        
        public static bool TryBonusPayment(string dogovorCode, long transactionId, int summ)
        {
            Dogovor dog = GetDogovor(dogovorCode);

            if (!IsPidUsed(dog, transactionId.ToString()))
                return CreatePay_UPayment(dog, transactionId.ToString(), summ);
            else
                throw new CatmoException("Trans_id already used", ErrorCodes.AnotherException);
        }


        public static bool IsPidUsed(Dogovor dogovor, string pId)
        {
            SqlConnection myConnection = new SqlConnection(Manager.ConnectionString);
            myConnection.Open();
            try
            {
                string findingNum =  "bs_" + pId + "" + dogovor.Key;
                SqlCommand com = new SqlCommand();
                com.Connection = myConnection;
                com.CommandText = "select count(*) from fin_documents where dc_outnum like '%" + findingNum + "%'";
                int num = Convert.ToInt32(com.ExecuteScalar());
                return num > 0;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog("Result - Ошибка при выполнении поиска документа " + pId + "\n" + ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                myConnection.Close();
            }
            return false;
        }

        //проведение оплаты бонусами
        private static bool CreatePay_UPayment(Dogovor dogovor, string pId, double summ)
        {
            SqlConnection myConnection = new SqlConnection(ConfigurationManager.AppSettings["ConnectionString"]);
            myConnection.Open();
            try
            {
                int error = 0;
                SqlCommand com = new SqlCommand("FIN_VZT_ADD_INCPAYMENT", myConnection);

                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.Add(new SqlParameter("@p_dtDate", DateTime.Today.ToString("yyyy-MM-dd")));
                com.Parameters.Add(new SqlParameter("@p_nFirm", 8637));     //Клик
                com.Parameters.Add(new SqlParameter("@p_nBank", 134));      //Сбербанк
                com.Parameters.Add(new SqlParameter("@p_nSum", summ)); //сумма в валюте ПС
                com.Parameters.Add(new SqlParameter("@p_sRate", "рб")); //код валюты ПС
                com.Parameters.Add(new SqlParameter("@p_nTurSum", summ));//сумма в евро
                com.Parameters.Add(new SqlParameter("@p_sTurRate", dogovor.RateCode));   //евро
                com.Parameters.Add(new SqlParameter("@p_sReceipt", "bs_" + pId+""+dogovor.Key));
                com.Parameters.Add(new SqlParameter("@p_sDogovor", dogovor.Code));
                com.Parameters.Add(new SqlParameter("@p_sComment", "Проводка по оплате бонусами. " + pId));
                com.Parameters.Add(new SqlParameter("@p_nError", error));
                com.CommandTimeout = 3000;
                com.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteToLog("Result - ошибка при создании проводок: " + ex.Message + "\n" + ex.StackTrace);
                return false;
            }
            finally
            {
                myConnection.Close();
            }
            return true;
        }

        [WebMethod]
         public NewDogovorResponse CreateNewDogovor(int[] bookIds, UserInfo info, string text, string login, PacketType type)
        {
            Dogovor dog = MtHelper.SaveNewDogovor(bookIds, info, login, type);

            if (text != "")
            {
                Logger.WriteToLog("try to add comment");
                Histories hsts = new Histories(new Megatec.Common.BusinessRules.Base.DataContainer());
                History hst = hsts.NewRow();
                hst.Text = text;
                hst.DogovorCode = dog.Code;
                hst.Mode = "MTM";
                hst.MessEnabled = 1;
                hst.UserKey = login;
                hsts.Add(hst);
                hsts.DataContainer.Update();
                hst.DataContainer.Update();
                Logger.WriteToLog("after add comment");
            }

            KeyValuePair<string, decimal>[] _courses = MtHelper.GetCourses(MtHelper.rate_codes, dog.Rate.ISOCode, DateTime.Today);

            return new NewDogovorResponse()
            {
                DogovorCode = dog.Code,
                PayDate = DateTime.Today.AddDays(1),
                Prices = MtHelper.ApplyCourses(Convert.ToDecimal(dog.Price), _courses),
            };
        }

        [WebMethod]  //annulate by user
        public int AnnulateDogovor(string userEmail, string dogovorCode, int[] services)
        {
            //получаем заказ по номеру
            var dog = MtHelper.GetDogovor(dogovorCode);

            dog.DogovorLists.Fill();

            if (userEmail != dog.MainMenEMail)
                throw new Exception("dogovor " + dogovorCode + " for user " + userEmail + " not found");

            //!!!!!
            //dogovorCode = MtHelper.GetDogovor(dogovorCode).Code; //для тестов подставляем определенный номер

            try
            {
                if (services.Length > 0)
                    foreach (int book_id in services)
                        MtHelper.AnnulateService(dogovorCode, book_id);
                else
                    MtHelper.AnnulateDogovor(dogovorCode);
            }
            catch (Exception ex)
            {
                Logger.WriteToLog("AnnulateDogovor exc: " + ex.Message + ex.StackTrace);
                throw ex;
            }

            return 1;
        }

        [WebMethod]  //dogovors by user
        public DogovorHeader[] GetDogovorsByUser(string email, int user_id)
        {
            Dogovors dogs = MtHelper.GetUserDogovors(email, user_id);

            DogovorHeader[] res = new DogovorHeader[dogs.Count];

            DateTime minDate = new DateTime(2014,1,1);

            for (int i = 0; i < dogs.Count; i++)
            {
                Dogovor dog = dogs[i];
                //получить курсы
                KeyValuePair<string, decimal>[] _courses = MtHelper.GetCourses(MtHelper.rate_codes, dog.Rate.ISOCode, dog.CreateDate);

                res[i] = new DogovorHeader()
                {
                    DogovorCode = dog.Code,
                    DogovorStatus = dog.OrderStatusKey,
                    PaidSumm = MtHelper.ApplyCourses(Convert.ToDecimal(dog.Payed), _courses),
                    TotalPrices = MtHelper.ApplyCourses(Convert.ToDecimal(dog.Price), _courses),
                    TourDate = dog.TurDate,
                    UpDate = dog.ConfirmedDate,
                    Services = GetDogovorServices(dog),
                    DocumentsCount = MtHelper.GetDogovorDocuments(dog.Code).Length,
                    CommentsCount = GetDogovorMessages(email, dog.Code, minDate).Length
                };
            }

            return res;
        }

        private ServiceSimpleInfo[] GetDogovorServices(Dogovor dog)
        {
            dog.DogovorLists.Fill(); //загрузить услуги
            ServiceSimpleInfo[] services_array = new ServiceSimpleInfo[dog.DogovorLists.Count];
            int j = 0;
            foreach (DogovorList dl in dog.DogovorLists)
            {
                int bookId = 0;
                try
                {
                    bookId = Convert.ToInt32(dl.Comment);
                }
                catch (Exception) { }

                services_array[j++] = new ServiceSimpleInfo()
                {
                    BookId = bookId,
                    Status = dl.ControlKey,
                    Title = dl.Name
                };
            }
            return services_array;
        }

        [WebMethod] //dogovor info
        public DogovorInfo GetDogovorInfo(string userEmail, string dogovorCode)
        {
            try
            {
                //получаем заказ по номеру
                var dog = MtHelper.GetDogovor(dogovorCode);

                dog.DogovorLists.Fill();

                if (userEmail != dog.MainMenEMail)
                    throw new Exception("dogovor " + dogovorCode + " for user " + userEmail + " not found");

                //узнаем курсы валют
                KeyValuePair<string, decimal>[] _courses = MtHelper.GetCourses(MtHelper.rate_codes, dog.Rate.ISOCode, dog.CreateDate);

                //создаем массив по количеству услуг
                ServiceInfo[] services = new ServiceInfo[dog.DogovorLists.Count];

                int cnt = 0;
                foreach (DogovorList dl in dog.DogovorLists) //заполняем массив услуг
                {
                    int bookId = 0;
                    try
                    {
                        bookId = Convert.ToInt32(dl.Comment);
                    }
                    catch (Exception) { }

                    services[cnt++] = new ServiceInfo()
                    {
                        BookId = bookId,
                        Day = dl.Day,
                        Ndays = dl.NDays,
                        Prices = MtHelper.ApplyCourses(Convert.ToDecimal(dl.Brutto), _courses),
                        ServiceClass = dl.ServiceKey,
                        Status = dl.ControlKey,
                        Title = dl.Name
                    };
                }

                //получаем информацию о туристах
                dog.Turists.Fill();
                TuristContainer[] turists = new TuristContainer[dog.Turists.Count];

                cnt = 0;

                foreach (Turist ts in dog.Turists) //заполняем массив услуг
                {
                    int tsId = cnt;
                    try
                    {
                        tsId = Convert.ToInt32(ts.PostIndex);
                    }
                    catch (Exception) { }

                    turists[cnt++] = new TuristContainer()
                    {
                        Id = tsId,
                        Name = ts.NameLat,
                        FirstName = ts.FNameLat,
                        BirthDate = ts.Birthday,
                        Citizenship = ts.Citizen,
                        PassportDate = ts.PasportDateEnd,
                        PassportNum = ts.PasportType + ts.PasportNum,
                        Sex = ts.RealSex + 1
                    };
                }

                return new DogovorInfo()
                {
                    Turists = turists,

                    DogovorStatus = dog.OrderStatusKey,

                    PaidSumm = MtHelper.ApplyCourses(Convert.ToDecimal(dog.Payed), _courses),

                    TotalPrices = MtHelper.ApplyCourses(Convert.ToDecimal(dog.Price), _courses),

                    TourDate = dog.TurDate,

                    PayDate = dog.CreateDate.AddMinutes(15),

                    UpDate = dog.ConfirmedDate,

                    Messages = GetDogovorMessages(userEmail, dog.Code, Convert.ToDateTime("2013-10-10")),

                    Services = services,

                    Documents = MtHelper.GetDogovorDocuments(dog.Code)
                };
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(ex.Message + " " + ex.Source + " " + ex.StackTrace);
                throw ex;

            }
        }

        [WebMethod]
        public Responses.DogovorMessage[] GetDogovorMessages(string email, string dogovorCode, DateTime minDate)
        {
            Dogovor dog = MtHelper.GetDogovor(dogovorCode);

            if (email != dog.MainMenEMail)
                throw new Exception("dogovor " + dogovorCode + " for user " + email + " not found");

            return MtHelper.GetDogovorMessages(dog.Key, minDate);
        }

        [WebMethod]
        public int SendMessage(string email, string dogovorCode, string text)
        {
            Dogovor dog = MtHelper.GetDogovor(dogovorCode);

            if (email != dog.MainMenEMail)
                throw new Exception("dogovor " + dogovorCode + " for user " + email + " not found");
            
            Mail.SendMail(dog.Owner.Mailbox, text, "Покупатель", dogovorCode);

            return MtHelper.SaveDogovorMessage(dog.Code, text);
        }

        [WebMethod]
        public int AddService(string dogovorCode, int bookId, PacketType type)
        {
            try
            {
                //получаем заказ по номеру
                Dogovor dog = MtHelper.GetDogovor(dogovorCode);
                return MtHelper.AddServiceToDogovor(dog, bookId, type);
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(ex.Message + " " + ex.Source + " " + ex.StackTrace);
                throw ex;
            }
        }
    }
}