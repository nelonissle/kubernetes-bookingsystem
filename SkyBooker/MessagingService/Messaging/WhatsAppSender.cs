using System;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MessagingService.Messaging
{
    public class WhatsAppSender
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly bool _enableWhatsAppMessages;
        private readonly ILogger<WhatsAppSender> _logger;
        private readonly string _whatsAppFrom;

        public WhatsAppSender(IConfiguration configuration, ILogger<WhatsAppSender> logger)
        {
            _logger = logger;

              // Read all required values from Windows environment variables:
            _accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNTSID")
                ?? throw new Exception("TWILIO_ACCOUNTSID is not set in the environment.");
            _authToken  = Environment.GetEnvironmentVariable("TWILIO_AUTHTOKEN")
                ?? throw new Exception("TWILIO_AUTHTOKEN is not set in the environment.");
            _whatsAppFrom = Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER")
                ?? throw new Exception("TWILIO_PHONE_NUMBER is not set in the environment.");

            // Default to 'true' if not found in config
            _enableWhatsAppMessages = configuration.GetValue<bool>("MessagingService:EnableWhatsAppMessages", true);

            if (string.IsNullOrWhiteSpace(_accountSid) || string.IsNullOrWhiteSpace(_authToken))
            {
                throw new Exception("Twilio credentials are missing. Please set them as environment variables or in configuration.");
            }

            // Initialize Twilio client
            TwilioClient.Init(_accountSid, _authToken);
        }

        // This method sends a plain text WhatsApp message.
        public void SendWhatsAppMessage(string to, string passengerName, string flightId, int ticketCount)
        {
            if (!_enableWhatsAppMessages)
            {
                _logger.LogInformation("WhatsApp messages are disabled.");
                return;
            }

            try
            {
                string messageBody = $"Hello {passengerName}, your booking for flight {flightId} " +
                                     $"with {ticketCount} ticket(s) has been confirmed.";

                var messageOptions = new CreateMessageOptions(new PhoneNumber($"whatsapp:{to}"))
                {
                    From = new PhoneNumber($"whatsapp:{_whatsAppFrom}"),
                    Body = messageBody
                };

                var msg = MessageResource.Create(messageOptions);
                _logger.LogInformation("WhatsApp message sent successfully. SID: {MessageSid}", msg.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message.");
            }
        }
    }
}
