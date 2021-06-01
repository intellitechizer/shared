using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Hosting;
using MimeKit;
using Repository;
using UserModel;
using Utility;

namespace BusinessLayer.Common
{
    public class MailHelper
    {
        TaleemIndiaDBEntities db = new TaleemIndiaDBEntities();
        private static readonly HttpRequest request = HttpContext.Current.Request;
        string senderEmail = ConfigurationManager.AppSettings["SenderEmail"].ToString();
        string senderEmailPassword = ConfigurationManager.AppSettings["SenderEmailPassword"].ToString();
        string hostName = ConfigurationManager.AppSettings["HostName"].ToString();
        string siteUrl = ConfigurationManager.AppSettings["SiteUrl"].ToString();
        int portNo = Convert.ToInt16(ConfigurationManager.AppSettings["PortNo"]);
        bool isSSL = Convert.ToBoolean(ConfigurationManager.AppSettings["IsSSL"]);
        bool useDefaultCredentials = Convert.ToBoolean(ConfigurationManager.AppSettings["UseDefaultCredentials"]);

        string baseUrl = request.Url.Scheme + "://" + request.Url.Authority;
        public bool GetAdminForgotPassword(string email)
        {
            bool status = false;
            string password, userId;
            using (TaleemIndiaDBEntities db = new TaleemIndiaDBEntities())
            {
                var data = db.Logins.Where(x => x.EmailId == email).FirstOrDefault();
                userId = data.UserName + "/" + data.EmailId + "/" + data.MobileNo;
                password = data.Password;
                if (data == null)
                {
                    status = false;
                }
                else
                {
                    MailMessage mail = new MailMessage();
                    SmtpClient SmtpServer = new SmtpClient(hostName);
                    SmtpServer.Port = portNo;
                    SmtpServer.Credentials = new NetworkCredential(senderEmail, senderEmailPassword);
                    SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
                    mail.From = new MailAddress(senderEmail);
                    mail.To.Add(email);
                    mail.Subject = "Admin Login Credentials details: ";
                    mail.Body = "Your's login Credentials : <hr/><br/> UserName: <b> " + userId + "</b><br/>" + "Password:<b> " + password + "</b><br/><br/>Baitul Ma'arif <br/>Khairabad (U.P.)";
                    mail.IsBodyHtml = true;
                    SmtpServer.EnableSsl = false;
                    SmtpServer.Send(mail);
                    status = true;
                }

            }
            return status;
        }

        public bool FatwaQuestionEmail(DarulIftaFatwaModel model, string templateFile, HttpPostedFileBase pfb)
        {
            bool result = false;
            string filePath = HostingEnvironment.ApplicationPhysicalPath + "App_Data" + Path.DirectorySeparatorChar.ToString() + "Templates" + Path.DirectorySeparatorChar.ToString() + templateFile;

            var bodyBuilder = new BodyBuilder();
            using (StreamReader streamReader = File.OpenText(filePath))
            {
                bodyBuilder.HtmlBody = streamReader.ReadToEnd();
            }
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{image}", baseUrl);
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{fatwaId}", model.FatwaId.ToString());
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{title}", model.Title);
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{question}", model.Question);
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{mainCategory}", model.MainCategory);
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{category}", model.Category);
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{language}", model.Language);
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{year}", DateTime.UtcNow.Year.ToString());
            string FromEmail = senderEmail;
            MailMessage message = new MailMessage();
            SmtpClient smtp = new SmtpClient();
            message.From = new MailAddress(FromEmail);
            message.To.Add(model.Email);
            message.Subject = model.Title;
            message.IsBodyHtml = true;
            message.Body = bodyBuilder.HtmlBody;
            if (pfb.ContentLength > 0)
            {
                message.Attachments.Add(new Attachment(pfb.InputStream, pfb.FileName));
            }
            smtp.Port = portNo;
            smtp.Host = hostName;
            smtp.EnableSsl = isSSL;
            smtp.UseDefaultCredentials = useDefaultCredentials;
            smtp.Credentials = new NetworkCredential(FromEmail, senderEmailPassword);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.Send(message);
            result = true;
            return result;
        }

        public bool FatwaReplyEmail(string emailId, long fatwaId, string title, string question, string answer, string templateFile)
        {
            bool result = true;
            string filePath = HostingEnvironment.ApplicationPhysicalPath + "App_Data" + Path.DirectorySeparatorChar.ToString() + "Templates" + Path.DirectorySeparatorChar.ToString() + templateFile;

            var bodyBuilder = new BodyBuilder();
            using (StreamReader streamReader = File.OpenText(filePath))
            {
                bodyBuilder.HtmlBody = streamReader.ReadToEnd();
            }
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{image}", baseUrl);
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{fatwaId}", fatwaId.ToString());
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{title}", title);
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{question}", question);
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{answer}", answer);
            bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace("{year}", DateTime.UtcNow.Year.ToString());
            string FromEmail = senderEmail;
            MailMessage message = new MailMessage();
            SmtpClient smtp = new SmtpClient();
            message.From = new MailAddress(FromEmail);
            message.To.Add(emailId);
            message.Subject = title;
            message.IsBodyHtml = true;
            message.Body = bodyBuilder.HtmlBody;
            smtp.Port = portNo;
            smtp.Host = hostName;
            smtp.EnableSsl = isSSL;
            smtp.UseDefaultCredentials = useDefaultCredentials;
            smtp.Credentials = new NetworkCredential(FromEmail, senderEmailPassword);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.Send(message);
            result = true;

            return result;
        }

        public void sentMailNotification(string subject, string bodyMessage)
        {
            string[] emailList = db.SubscriberDetails.Select(x => x.Email).ToArray();
            foreach (var emailId in emailList)
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(hostName);
                SmtpServer.Port = portNo;
                SmtpServer.Credentials = new NetworkCredential(senderEmail, senderEmailPassword);
                SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
                mail.From = new MailAddress(senderEmail);
                //foreach (var emailId in emailList)
                //{
                //    mail.To.Add(new MailAddress(emailId));
                //}
                mail.To.Add(emailId);
                mail.Subject = subject;
                mail.Body = bodyMessage;
                mail.IsBodyHtml = true;
                SmtpServer.EnableSsl = isSSL;
                SmtpServer.Send(mail);
            }
        }

        public void sentMailMember(string toMail, string subject, string bodyMessage)
        {
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient(hostName);
            SmtpServer.Port = portNo;
            SmtpServer.Credentials = new NetworkCredential(senderEmail, senderEmailPassword);
            SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
            mail.From = new MailAddress(senderEmail);
            mail.To.Add(toMail);
            mail.Subject = subject;
            mail.Body = bodyMessage;
            mail.IsBodyHtml = true;
            SmtpServer.EnableSsl = isSSL;
            SmtpServer.Send(mail);
        }

        public bool ComposeOrResend(List<string> mailsTo, string subject, string message, HttpFileCollection attachment)
        {
            foreach (var emailId in mailsTo)
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(hostName);
                SmtpServer.Port = portNo;
                SmtpServer.Credentials = new NetworkCredential(senderEmail, senderEmailPassword);
                SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
                mail.From = new MailAddress(senderEmail);
                mail.To.Add(emailId);
                mail.Subject = subject;
                mail.Body = message;
                mail.IsBodyHtml = true;
                if (attachment.Count > 0)
                {
                    for (int i = 0; i < attachment.Count; i++)
                    {
                        HttpPostedFileBase pfb = new HttpPostedFileWrapper(attachment[i]);
                        if (pfb != null && pfb.ContentLength > 0)
                        {
                            mail.Attachments.Add(new Attachment(pfb.InputStream, pfb.FileName));
                        }
                    }
                }
                SmtpServer.EnableSsl = isSSL;
                SmtpServer.Send(mail);
            }
            return true;
        }
    }
}