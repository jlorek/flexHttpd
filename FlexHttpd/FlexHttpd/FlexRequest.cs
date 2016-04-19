using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FlexHttpd
{
    public class FlexRequest
    {
        Dictionary<string, string> Header = new Dictionary<string, string>();

        public string Method { get; set; }

        public string Url { get; set; }

        public string Body { get; set; }

        public string Protocol { get; private set; }

        public Dictionary<string, string> QueryParameters { get; private set; } = new Dictionary<string, string>();

        public static FlexRequest TryParse(string requestRaw)
        {
            try
            {
                FlexRequest httpRequest = new FlexRequest();

                string[] linesRaw = requestRaw.Split(new[] { "\r\n" }, StringSplitOptions.None);

                string[] request = linesRaw[0].Split(' ');

                httpRequest.Method = request[0].ToUpperInvariant();

                string completeQuery = request[1];
                string[] queryString = completeQuery.Split('?');

                httpRequest.Url = queryString[0];
                if (queryString.Length > 1)
                {
                    httpRequest.QueryParameters = ParseQueryString(queryString[1]);
                }

                httpRequest.Protocol = request[2];

                for (int i = 1; i < linesRaw.Length; ++i)
                {
                    string line = linesRaw[i];

                    // content begins
                    if (String.IsNullOrEmpty(line))
                    {
                        StringBuilder contentBuilder = new StringBuilder();
                        for (int j = i + 1; j < linesRaw.Length; ++j)
                        {
                            contentBuilder.AppendLine(linesRaw[j]);
                        }
                        httpRequest.Body = contentBuilder.ToString();
                        break;
                    }

                    // header
                    int separater = line.IndexOf(":");
                    string key = line.Substring(0, separater);
                    string value = line.Substring(separater + 2);
                    httpRequest.Header[key] = value;
                }

                return httpRequest;
            }
            catch
            {
                // error while parsing
                return null;
            }
        }

        private static Dictionary<string, string> ParseQueryString(string queryString)
        {
            Dictionary<string, string> queryParameters = new Dictionary<string, string>();

            var matches = Regex.Matches(queryString, @"&?([^\=]+)=([^&]+)");
            foreach (Match match in matches)
            {
                //byteFAKK
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                queryParameters[key] = value;
            }

            return queryParameters;
        }
    }
}
