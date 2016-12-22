using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace TopTourMiddleOffice.Helpers
{
    public static class Mail
    {
        private static string ServerSMTP = ConfigurationManager.AppSettings["ServerSMTP"];
        private static int Port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"]);
        private static string EmailAddress = ConfigurationManager.AppSettings["EmailAddress"];
        private static string LoginSMTP = ConfigurationManager.AppSettings["LoginSMTP"];
        private static string PasswordSMTP = ConfigurationManager.AppSettings["PasswordSMTP"];

        public static void SendMail(string mailto, string message, string theme, string dogcode = "")
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(EmailAddress);

                if (mailto.Contains(","))
                {
                    string[] addresses = mailto.Replace(" ", "").Split(',');

                    foreach (var address in addresses)
                    {
                        mail.To.Add(new MailAddress(address));
                    }
                }
                else
                {
                    mail.To.Add(new MailAddress(mailto.Trim()));
                }

                mail.Subject = String.Format("{0}: Получено новое сообщение с сайта {1}"
                    ,theme
                    , dogcode != "" ? " по путевке " + dogcode : "");
                mail.Body = message;
                mail.IsBodyHtml = true;
                //if (!string.IsNullOrEmpty(attachFile))
                //{
                //    mail.Attachments.Add(new Attachment(attachFile));

                //    // проверяем наличие картинок. Если есть - цепляем
                //    for (int i = 1; i < 5; i++)
                //    {
                //        var jpg = attachFile.Replace(".html", "_" + i + ".jpg");
                //        if (File.Exists(jpg))
                //        {
                //            mail.Attachments.Add(new Attachment(jpg));
                //        }
                //    }
                //}
                SmtpClient client = new SmtpClient();
                client.Host = ServerSMTP;
                client.Port = Port;
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(LoginSMTP, PasswordSMTP);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Send(mail);
                mail.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLog("Произошла ошибка при отправке сообщения" + e.Message);
            }
        }
    }
}