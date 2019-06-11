using System.Diagnostics.Contracts;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using Shared.Printing;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace BillingFunctions
{
    public static class NotifyInvoiceFunc
    {
        /// <summary>
        /// Test numbers for Twilio service: https://www.twilio.com/docs/iam/test-credentials?code-sample=code-create-a-message&code-language=C%23&code-sdk-version=5.x#test-sms-messages
        /// </summary>
        /// <param name="notificationRequest"></param>
        /// <param name="email"></param>
        /// <param name="sms"></param>
        /// <param name="log"></param>
        [FunctionName("NotifyInvoiceFunc")]
        public static void Run(
            [QueueTrigger("invoice-notification-request")]InvoiceNotificationRequest notificationRequest,
            [SendGrid(ApiKey = "SendGridApiKey")] out SendGridMessage email,
            [TwilioSms(AccountSidSetting = "TwilioAccountSid", AuthTokenSetting = "TwilioAuthToken", From = "+15005550006")] out CreateMessageOptions sms,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {notificationRequest}");

            email = CreateEmail(notificationRequest);
            sms = CreateSMS(notificationRequest);
        }

        private static SendGridMessage CreateEmail(InvoiceNotificationRequest request)
        {
            var email = new SendGridMessage();

            email.AddTo("asc-lab@altkom.pl");
            email.AddContent("text/html", $"You have new invoice {request.InvoiceForNotification.InvoiceNumber} for {request.InvoiceForNotification.TotalCost.ToString()}.");
            email.SetFrom(new EmailAddress("asc-lab@altkom.pl"));
            email.SetSubject($"New Invoice - {request.InvoiceForNotification.InvoiceNumber}");

            return email;
        }

        private static CreateMessageOptions CreateSMS(InvoiceNotificationRequest request)
        {
            return new CreateMessageOptions(new PhoneNumber("+15005550006"))
            {
                Body = $"You have new invoice {request.InvoiceForNotification.InvoiceNumber} for {request.InvoiceForNotification.TotalCost.ToString()}."
            };
        }
    }
}

namespace BillingFunctionsModified
{
    public static class NotifyInvoiceFunc
    {
        private static BillingFunctionsStubs.SendGridMessage CreateEmail_NoBugs(InvoiceNotificationRequest request)
        {
            var email = new BillingFunctionsStubs.SendGridMessage();

            email.AddTo("asc-lab@altkom.pl");
            email.AddContent("text/html", $"You have new invoice {request.InvoiceForNotification.InvoiceNumber} for {request.InvoiceForNotification.TotalCost.ToString()}.");
            email.SetFrom(new BillingFunctionsStubs.EmailAddress("asc-lab@altkom.pl"));
            email.SetSubject($"New Invoice - {request.InvoiceForNotification.InvoiceNumber}");

            //Contract.Assert(email.To == "asc-lab@altkom.pl");
            //Contract.Assert(email.From.Email == "asc-lab@altkom.pl");
            //Contract.Assert(email.Subject == $"New Invoice - {request.InvoiceForNotification.InvoiceNumber}");
            Contract.Assert(email.Content == $"You have new invoice {request.InvoiceForNotification.InvoiceNumber} for {request.InvoiceForNotification.TotalCost.ToString()}.");

            return email;
        }

        private static BillingFunctionsStubs.SendGridMessage CreateEmail_Bugged(InvoiceNotificationRequest request)
        {
            var email = new BillingFunctionsStubs.SendGridMessage();

            email.AddTo("asc-lab@altkom.pl");
            email.AddContent("text/html", $"You have new invoice {request.InvoiceForNotification.InvoiceNumber} for {request.InvoiceForNotification.TotalCost.ToString()}.");
            email.SetFrom(new BillingFunctionsStubs.EmailAddress("asc-lab@altkom.pl"));
            email.SetSubject($"New Invoice - {request.InvoiceForNotification.InvoiceNumber}");

            //Contract.Assert(email.To != "asc-lab@altkom.pl");
            //Contract.Assert(email.From.Email != "asc-lab@altkom.pl");
            //Contract.Assert(email.Subject != $"New Invoice - {request.InvoiceForNotification.InvoiceNumber}");
            Contract.Assert(email.Content != $"You have new invoice {request.InvoiceForNotification.InvoiceNumber} for {request.InvoiceForNotification.TotalCost.ToString()}.");

            return email;
        }

        private static BillingFunctionsStubs.CreateMessageOptions CreateSMS_NoBugs(InvoiceNotificationRequest request)
        {
            BillingFunctionsStubs.CreateMessageOptions createMessageOptions = new BillingFunctionsStubs.CreateMessageOptions(new BillingFunctionsStubs.PhoneNumber("+15005550006"))
            {
                Body = $"You have new invoice {request.InvoiceForNotification.InvoiceNumber} for {request.InvoiceForNotification.TotalCost.ToString()}."
            };

            Contract.Assert(createMessageOptions.Body == $"You have new invoice {request.InvoiceForNotification.InvoiceNumber} for {request.InvoiceForNotification.TotalCost.ToString()}.");
            //Contract.Assert(createMessageOptions.To.Number == "+15005550006");

            return createMessageOptions;
        }

        private static BillingFunctionsStubs.CreateMessageOptions CreateSMS_Bugged(InvoiceNotificationRequest request)
        {
            BillingFunctionsStubs.CreateMessageOptions createMessageOptions = new BillingFunctionsStubs.CreateMessageOptions(new BillingFunctionsStubs.PhoneNumber("+15005550006"))
            {
                Body = $"You have new invoice {request.InvoiceForNotification.InvoiceNumber} for {request.InvoiceForNotification.TotalCost.ToString()}."
            };

            Contract.Assert(createMessageOptions.Body != $"You have new invoice {request.InvoiceForNotification.InvoiceNumber} for {request.InvoiceForNotification.TotalCost.ToString()}.");
            //Contract.Assert(createMessageOptions.To.Number != "+15005550006");

            return createMessageOptions;
        }
    }
}

namespace BillingFunctionsStubs
{
    class CreateMessageOptions
    {
        public CreateMessageOptions(PhoneNumber to)
        {
            this.To = to;
        }

        public PhoneNumber To;
        public string Body;
    }

    class PhoneNumber
    {
        public PhoneNumber(string number)
        {
            this.Number = number;
        }

        public string Number;
    }

    class SendGridMessage
    {
        public string To; // this should be a list
        public string Content; // dictionary with ContentType?
        public string ContentType;
        public EmailAddress From;
        public string Subject;

        public void AddTo(string email)
        {
            To = email;
        }

        public void AddContent(string t, string content)
        {
            ContentType = t;
            Content = content;
        }

        public void SetFrom(EmailAddress email)
        {
            From = email;
        }

        public void SetSubject(string s)
        {
            Subject = s;
        }
    }

    class EmailAddress
    {
        public string Email { get; set; }

        public EmailAddress(string e)
        {
            Email = e;
        }
    }
}