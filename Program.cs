using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace network_watcher_reporter
{
    class Program
    {
        static void Main(string[] args)
        {
            Log("******* Starting execution ************************************************************");

            if (args.Length != 5)
            {
                Log($"Exiting; expecting 5 arguments, but received {args.Length}.");
                return;
            }

            var deviceName = args[0];
            var macAddress = args[1];
            var userText = args[2];
            var adapterCompany = args[3];
            var ipAddress = args[4];

            Log($"Device Name     : {deviceName}");
            Log($"MAC Address     : {macAddress}");
            Log($"User Text       : {userText}");
            Log($"Adapter Company : {adapterCompany}");
            Log($"IP Address      : {ipAddress}");

            if (!String.IsNullOrEmpty(userText))
            {
                Log($"Exiting; device is assumed as known because user text was present, so no need to report.");
                return;
            }

            Log($"About to report new device...");

            var pushOverApiUrl = ConfigurationSettings.AppSettings["pushover_api_url"];
            var pushOverToken = ConfigurationSettings.AppSettings["pushover_token"];
            var pushOverUser = ConfigurationSettings.AppSettings["pushover_user"];

            if (String.IsNullOrEmpty(pushOverApiUrl) || String.IsNullOrEmpty(pushOverToken) || String.IsNullOrEmpty(pushOverUser))
            {
                Log("Exiting; PushOver.net API URL, token, or user ID not set in app.config.");
                return;
            }

            // Build the message body for the push notification.
            var message = WebUtility.UrlEncode($@"New Device Detected:
MAC Address: {macAddress}
IP Address: {ipAddress}
Name: {deviceName}
Company: {adapterCompany}");

            var request = HttpWebRequest.Create(pushOverApiUrl) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            var postData = $"token={pushOverToken}&user={pushOverUser}&message={message}";

            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentLength = byteArray.Length;

            var statusCode = -1;
            HttpWebResponse response = null;
            string responseBody = null;

            // curl -s --insecure --data "token=xxx&user=xxx&message=New Device Detected:%0AName: %device_name%%0AUser Text: %user_text%%0AAdapter Company: %adapter_company%%0AMAC: %mac_addr%%0AIP: %ip_addr%%0ADetect Count: %detect_count%" https://api.pushover.net/1/messages.json

            try
            {

                using (var stream = request.GetRequestStream())
                {
                    using (var streamWriter = new System.IO.StreamWriter(stream))
                    {
                        streamWriter.Write(postData);
                    }
                }

                response = request.GetResponse() as HttpWebResponse;
                statusCode = (int)response.StatusCode;

                using (var stream = response.GetResponseStream())
                {
                    using (var streamReader = new System.IO.StreamReader(stream))
                    {
                        responseBody = streamReader.ReadToEnd();
                    }
                }

                Log($"Request Complete: Status Code: {statusCode} / Body: {responseBody}");
            }
            catch (WebException exception)
            {
                Log($"WebException: {exception.Message} / Status Code: {statusCode} / Body: {responseBody}");
            }
            catch (Exception exception)
            {
                Log($"Exception: {exception.Message} / Status Code: {statusCode} / Body: {responseBody}");
            }
        }

        private static void Log(string message)
        {
            var path = ConfigurationSettings.AppSettings["log-path"];

            if (String.IsNullOrEmpty(path))
                path = "C:\\network-watcher-reporter.log";

            var logEntry = "[" + DateTime.Now.ToString() + "] " + message + Environment.NewLine;

            File.AppendAllText(path, logEntry);
            Console.Write(logEntry);
        }
    }
}
