using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace identity_web_api_core.Services
{
    public interface IMailService
    {
        Task SendEmailAsync(string ToEmail, string Subject, string content);

    }

    public class SendGridMailService : IMailService
    {
      private  IConfiguration _configuration;
        public SendGridMailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmailAsync(string ToEmail, string Subject, string content)
        {
 
            var apiKey = _configuration["SendGridApiKey"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("nicolasbahindwa@outlook.com", "JWT API DEMO");
          //  var subject = "Sending with SendGrid is Fun";
            var to = new EmailAddress(ToEmail);
          //  var plainTextContent = "and easy to do anywhere, even with C#";
         //   var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, Subject, content, content);
            var response = await client.SendEmailAsync(msg);

            // throw new NotImplementedException();
        }
    }
}
