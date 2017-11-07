using System.Net.Mail;

namespace iPhonePickupChecker
{
    /// <summary>
    /// gmail sending
    /// </summary>
    public class Gmail
    {
        public string GmailId = "";
        public string GmailPassword = "";
        public string[] To = null;
        public string Subject = "";
        public string Body = "";
        public bool IsHtml = false;
        public string From
        {
            get
            {
                if (GmailId.Contains("@"))
                    return GmailId;
                return GmailId + "@gmail.com";
            }
        }

        public void Send()
        {
            MailMessage mail = new MailMessage();
            SmtpClient smtp = new SmtpClient("smtp.gmail.com");

            mail.From = new MailAddress(From);
            foreach (string t in To)
                mail.To.Add(t);

            mail.Subject = Subject;
            mail.Body = Body;
            mail.IsBodyHtml = IsHtml;

            smtp.Port = 587;
            smtp.Credentials = new System.Net.NetworkCredential(From, GmailPassword);
            smtp.EnableSsl = true;

            smtp.Send(mail);
        }
    }
}