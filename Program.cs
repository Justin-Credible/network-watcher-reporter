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
                FlushLogs();
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
                FlushLogs();
                return;
            }

            Log($"About to report new device...");

            var pushOverApiUrl = ConfigurationSettings.AppSettings["pushover_api_url"];
            var pushOverToken = ConfigurationSettings.AppSettings["pushover_token"];
            var pushOverUser = ConfigurationSettings.AppSettings["pushover_user"];

            if (String.IsNullOrEmpty(pushOverApiUrl) || String.IsNullOrEmpty(pushOverToken) || String.IsNullOrEmpty(pushOverUser))
            {
                Log("Exiting; PushOver.net API URL, token, or user ID not set in app.config.");
                FlushLogs();
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

            FlushLogs();
        }

        private static List<string> _logs = new List<string>();

        private static void Log(string message)
        {
            var logEntry = "[" + DateTime.Now.ToString() + "] " + message;

            Console.WriteLine(logEntry);
            _logs.Add(logEntry);
        }

        private static void FlushLogs()
        {
            if (_logs.Count == 0)
            {
                return;
            }

            // Network watcher will spawn multiple instances of this program
            // and we don't want to throw an exception if the log file is being
            // writen by another instance. We'll retry the write to disk 5 times.

            var maxAttempts = 5;

            for (var i = 0; i < maxAttempts; i++)
            {
                try
                {
                    var path = ConfigurationSettings.AppSettings["log-path"];

                    if (String.IsNullOrEmpty(path))
                        path = "C:\\network-watcher-reporter.log";

                    var logFileContents = String.Join(Environment.NewLine, _logs);
                    File.AppendAllText(path, logFileContents);

                    _logs.Clear();

                    // We're good; bail out.
                    break;
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"An error occurred while attempting to write log file during attempt {i+1}/{maxAttempts}: {exception.Message}");
                    System.Threading.Thread.Sleep(1);
                }

                // Keep the console open so we can examine the log content.
                if (i == maxAttempts-1)
                {
                    Console.WriteLine($"Unable to write to disk after {maxAttempts} attempts, giving up! Press any key to continue.");
                    Console.ReadKey();
                }
            }
        }
    }
}
