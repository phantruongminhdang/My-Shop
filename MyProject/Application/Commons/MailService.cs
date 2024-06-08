using MailKit.Net.Smtp;
using MimeKit;

namespace Application.Commons
{
    public class MailService
    {
        public bool SendEmail(string toMail, string subject, string content)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress("minhdang12345c", "minhdang12345c@gmail.com"));
            email.To.Add(MailboxAddress.Parse(toMail));
            email.Subject = subject;

            var builder = new BodyBuilder();

            builder.HtmlBody = " <div style=\" background-color: #fff;\r\n  color: #333;\r\n " +
                " border-radius: 15px;\r\n  box-shadow: 0 0 20px rgba(0, 0, 0, 0.2);\r\n  overflow: hidden;\r\n  width: 80%;\r\n  max-width: 500px;\r\n " +
                " text-align: left; padding: 30px;\">" + content +
                "   <p style=\"margin-bottom: 10px;\r\n   " +
                " text-align: left;\">Trân trọng,</p>\r\n    <p style=\"font-weight: bold; color: #009c11;\">minhdang12345c</p>\r\n  " +
                "  <img class=\"logo\" style=\"max-width: 50%;\r\n    height: auto;\r\n    margin-bottom: 20px;\"\r\n    " +
                "  src=\"\"\r\n    " +
                "  alt=\"Logo minhdang12345c\">  <div style=\"margin-top: 20px;\r\n    text-align: left;\">\r\n    " +
                "  <p style=\"margin-bottom: 10px;\r\n text-align: left; margin: 5px 0; \"><strong>Liên hệ với chúng tôi để được hỗ trợ nhiều\r\n hơn</strong>" +
                "</p>\r\n <p style=\"margin-bottom: 10px;\r\n text-align: left; margin: 5px 0;\"><strong>Email:</strong> </p>\r\n " +
                " <p style=\"margin-bottom: 10px;\r\n      text-align: left; margin: 5px 0;\"><strong>Hotline:</strong> (024) 6680 5588</p>\r\n    </div>\r\n  </div>";
            email.Body = builder.ToMessageBody();
            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            smtp.Connect("smtp.gmail.com", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);
            smtp.Authenticate("minhdang12345c@gmail.com", "czzmzoqjjwvobugb");
            try
            {
                smtp.Send(email);
            }
            catch (SmtpCommandException ex)
            {
                return false;
            }
            smtp.Disconnect(true);
            return true;
        }


    }
}
