
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace SendSMSviaSkebby
{
    public static class SendSMSviaSkebby
    {
        [FunctionName("SendSMSviaSkebby")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            try
            {
                log.Info("C# HTTP trigger function processed a request.");

                string requestBody = new StreamReader(req.Body).ReadToEnd();
                dynamic data = JsonConvert.DeserializeObject(requestBody.ToString());

                string password = data.password;
                string sender = data.sender;
                string[] recipient = new String[] { };
                string message = data.message;
                string message_type = data.message_type;

                recipient = data.recipient.ToObject<string[]>();


                if (password != correct_password)
                {
                    return (ActionResult)new OkObjectResult("Bad request - Wrong Password");
                }

                String[] auth = authenticate("skebbyuser", "skebbypassword");

                SendSMS sendSMSRequest = new SendSMS();

                sendSMSRequest.message = message;
                sendSMSRequest.recipient = recipient;

                if (message_type == null)
                {
                    sendSMSRequest.message_type = "SI";
                }
                else
                {
                    sendSMSRequest.message_type = message_type;
                }

                if (sendSMSRequest.message_type == "GP" || sendSMSRequest.message_type == "TI")
                {
                    sendSMSRequest.sender = sender;
                }
                else
                {
                    sendSMSRequest.sender = "";
                }


                SMSSent smsSent = sendSMS(auth, sendSMSRequest);

                if ("OK".Equals(smsSent.result))
                {
                    log.Info("SMS successfully sent!");
                    return (ActionResult)new OkObjectResult("SMS successfully sent!");
                }

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult("Bad Request - " + ex);
            }

            return new BadRequestObjectResult("Bad Request");
        }

        public static String BASEURL = "https://api.skebby.it/API/v1.0/REST/";
        public static String correct_password = "pippo";
        //public static String MESSAGE_HIGH_QUALITY = "GP";
        //public static String MESSAGE_MEDIUM_QUALITY = "TI";
        //public static String MESSAGE_LOW_QUALITY = "SI";

        /**
         * Authenticates the user given it's username and
         * password. Returns a couple (user_key, session_key)
         */
        static String[] authenticate(String username, String password)
        {
            String[] auth = null;

            using (var wb = new WebClient())
            {
                var response = wb.DownloadString(BASEURL +
                                                 "login?username=" + username +
                                                 "&password=" + password);
                auth = response.Split(';');
            }

            return auth;
        }

        /**
         * Sends an SMS
         */
        static SMSSent sendSMS(String[] auth, SendSMS sendSMS)
        {
            using (var wb = new WebClient())
            {
                wb.Headers.Set(HttpRequestHeader.ContentType, "application/json");
                wb.Headers.Add("user_key", auth[0]);
                wb.Headers.Add("Session_key", auth[1]);

                String json = JsonConvert.SerializeObject(sendSMS);

                var sentSMSBody =
                    wb.UploadString(BASEURL + "sms", "POST", json);

                SMSSent sentSMSResponse =
                    JsonConvert.DeserializeObject<SMSSent>(sentSMSBody);

                return sentSMSResponse;
            }
        }

    }

    /**
     * This object is used to create an SMS message sending request.
     * The JSon object is then automatically created starting from an
     * instance of this class, using JSON.NET.
     */
    class SendSMS
    {
        /** The message body */
        public String message;

        /** The message type */
        public String message_type;

        /** The sender Alias (TPOA) */
        public String sender;

        /** Postpone the SMS message sending to the specified date */
        public DateTime? scheduled_delivery_time;

        /** The list of recipients */
        public String[] recipient;

        /** Should the API return the remaining credits? */
        public Boolean returnCredits = false;
    }

    /**
     * This class represents the API Response. It is automatically created starting
     * from the JSON object returned by the server, using GSon
     */
    class SMSSent
    {
        /** The result of the SMS message sending */
        public String result;

        /** The order ID of the SMS message sending */
        public String order_id;

        /** The actual number of sent SMS messages */
        public int total_sent;

        /** The remaining credits */
        public int remaining_credits;
    }

}
