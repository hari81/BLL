using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace BLL.Administration
{
    public class EmailManager
    {
        private SharedContext _context;
        private readonly string FROM_EMAIL_ADDRESS = "info@tracktreads.com";
        private readonly string REPLY_TO_EMAIL_ADDRESS = "info@tracktreads.com";
        public EmailManager()
        {
            this._context = new SharedContext();
        }

        public bool SendEmail(string sendToEmailAddress, string subject, string body, bool appendHeaderAndFooter)
        {
            string email = "";
            if (appendHeaderAndFooter)
            {
                email = @"
                                <p>Hi " + sendToEmailAddress + @",</p>
                                " + body + @"
                                <p>Best Regards,</p>
                                <p>TrackTreads Support Team</p>
                                <a href='http://www.tracktreads.com'>
                                <img class='logo' src='http://www.tracktreads.com/wp-content/uploads/2017/05/TrackTreads-Logo-Small.png'>
                                </a>";
            } else
            {
                email = body;
            }
            return SendEmail(FROM_EMAIL_ADDRESS, REPLY_TO_EMAIL_ADDRESS, subject + " - TrackTreads", sendToEmailAddress, email, MailPriority.Normal);
        }

        public bool SendEmail(string emailFromAddress, string emailReply, string emailSubject, string emailTo, string emailBody, MailPriority priority)
        {
            bool emailSent = false;
            try
            {
                SmtpClient client = new SmtpClient(_context.APPLICATION_LU_CONFIG.Where(c => c.variable_key == "SmtpServer").Select(c => c.value_key).FirstOrDefault());
                MailAddress from = new MailAddress(emailFromAddress);
                MailAddress to = new MailAddress(emailTo);
                MailMessage Message = new MailMessage(from, to);
                Message.Subject = emailSubject;
                Message.Headers.Add("Reply-To", emailReply);
                Message.IsBodyHtml = true;
                Message.BodyEncoding = System.Text.Encoding.GetEncoding("UTF-8");
                Message.Priority = priority;
                Message.Body = emailBody;
                client.Send(Message);
                Message.Dispose();
                emailSent = true;
            }
            catch (Exception ex)
            {
                emailSent = false;
            }
            return emailSent;
        }

        public bool SendEmailWithAttachment(string sentbyUserName, string emailTo, string subject, string body, bool appendHeaderAndFooter, System.IO.MemoryStream ms,string sentToUserName, string emailFromAddress)
        {
            bool emailSent = false;

            string email = "";
            if (appendHeaderAndFooter)
            {
                email = @"
                                <p>Hi " + sentToUserName + @",</p>
                                " + body + @"
                                <p>Best Regards,</p>
                                <p>"  + sentbyUserName + @"</p>
                                <a href='http://www.tracktreads.com'>
                                <img class='logo' src='http://www.tracktreads.com/wp-content/uploads/2017/05/TrackTreads-Logo-Small.png'>
                                </a>";
            }
            else
            {
                email = body;
            }

            try
            {
                System.Net.Mime.ContentType ct = new System.Net.Mime.ContentType(System.Net.Mime.MediaTypeNames.Application.Pdf);
                Attachment report = new Attachment(ms, ct);
                report.ContentDisposition.FileName = "Rope Shovel Inspection Report - TrackTreads.pdf";

                SmtpClient client = new SmtpClient(_context.APPLICATION_LU_CONFIG.Where(c => c.variable_key == "SmtpServer").Select(c => c.value_key).FirstOrDefault());
                MailAddress from = new MailAddress(emailFromAddress);
                MailAddress to = new MailAddress(emailTo);
                MailMessage Message = new MailMessage(from, to);
                Message.Subject = subject;
                Message.Headers.Add("Reply-To", emailFromAddress);
                Message.IsBodyHtml = true;
                Message.BodyEncoding = System.Text.Encoding.GetEncoding("UTF-8");
                Message.Priority = MailPriority.High;
                Message.Body = email;
                Message.Attachments.Add(report);

                client.Send(Message);
                Message.Dispose();
                emailSent = true;
            }
            catch (Exception ex)
            {
                emailSent = false;
            }
            return emailSent;
        }
    }
}