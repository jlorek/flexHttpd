using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace FlexHttpd
{
    public class FlexServer
    {
        private const int BufferSize = 8192;

        public FlexResponse DefaultResponse { get; set; } = new FlexResponse(FlexHttpStatus.NotFound);

        public Dictionary<string, Func<FlexRequest, Task<FlexResponse>>> Get { get; } = new Dictionary<string, Func<FlexRequest, Task<FlexResponse>>>();
        public Dictionary<string, Func<FlexRequest, Task<FlexResponse>>> Post { get; } = new Dictionary<string, Func<FlexRequest, Task<FlexResponse>>>();
        public Dictionary<string, Func<FlexRequest, Task<FlexResponse>>> Put { get; } = new Dictionary<string, Func<FlexRequest, Task<FlexResponse>>>();
        public Dictionary<string, Func<FlexRequest, Task<FlexResponse>>> Delete { get; } = new Dictionary<string, Func<FlexRequest, Task<FlexResponse>>>();

        private readonly StreamSocketListener _listener;

        public FlexServer()
        {
            _listener = new StreamSocketListener();
            _listener.ConnectionReceived += ListenerConnectionReceived;
        }

        public async Task Start(uint localport)
        {
            await _listener.BindServiceNameAsync(Convert.ToString(localport));
        }

        private async void ListenerConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            StreamSocket socket = args.Socket;

            StringBuilder requestStrBuilder = new StringBuilder();

            using (IInputStream input = socket.InputStream)
            {
                byte[] data = new byte[BufferSize];
                IBuffer buffer = data.AsBuffer();
                uint dataRead = BufferSize;
                while (dataRead == BufferSize)
                {
                    await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                    requestStrBuilder.Append(Encoding.UTF8.GetString(data, 0, (int) buffer.Length));
                    dataRead = buffer.Length;
                }
            }

            string requestStr = requestStrBuilder.ToString();

            if (String.IsNullOrEmpty(requestStr))
            {
                return;
            }

            FlexRequest request = TryParse(requestStr);
            if (request == null)
            {
                return;
            }

            FlexResponse response = await ProcessRequest(request);
            using (IOutputStream output = socket.OutputStream)
            {
                using (Stream responseStream = output.AsStreamForWrite())
                {
                    await WriteResponse(response, responseStream);
                    await responseStream.FlushAsync();
                }
            }
        }

        private Func<FlexRequest, Task<FlexResponse>> FindRequestProcessor(FlexRequest request, Dictionary<string, Func<FlexRequest, Task<FlexResponse>>> processors)
        {
            Func<FlexRequest, Task<FlexResponse>> processor = null;

            if (!processors.TryGetValue(request.Url, out processor))
            {
                foreach (string route in processors.Keys)
                {
                    var parameters = TryMatchRoute(route, request.Url);
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            request.QueryParameters[parameter.Key] = parameter.Value;
                        }

                        return processors[route];
                    }
                }
            }

            return processor;
        }

        private async Task<FlexResponse> ProcessRequest(FlexRequest request)
        {
            Func<FlexRequest, Task<FlexResponse>> processor = null;

            if (request.Method == "GET")
            {
                processor = FindRequestProcessor(request, Get);
            }
            else if (request.Method == "POST")
            {
                processor = FindRequestProcessor(request, Post);
            }
            else if (request.Method == "PUT")
            {
                processor = FindRequestProcessor(request, Put);
            }
            else if (request.Method == "DELETE")
            {
                processor = FindRequestProcessor(request, Delete);
            }

            if (processor != null)
            {
                FlexResponse response = await processor(request);
                return response;
            }

            return DefaultResponse;
        }

        public static FlexRequest TryParse(string requestRaw)
        {
            try
            {
                string[] linesRaw = requestRaw.Split(new[] { "\r\n" }, StringSplitOptions.None);

                string[] request = linesRaw[0].Split(' ');
                string method = request[0].ToUpperInvariant();
                string protocol = request[2];

                string completeQuery = request[1];
                string[] query = completeQuery.Split('?');

                //string url = request[1];
                string url = query[0];

                FlexRequest httpRequest = new FlexRequest(method, url, protocol);

                if (query.Length > 1)
                {
                    var parameters = ParseQuery(query[1]);
                    foreach (var parameter in parameters)
                    {
                        httpRequest.QueryParameters[parameter.Key] = parameter.Value;
                    }
                }

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
                    int separater = line.IndexOf(":", StringComparison.Ordinal);
                    string key = line.Substring(0, separater);
                    string value = line.Substring(separater + 2);
                    httpRequest.Headers[key] = value;
                }

                return httpRequest;
            }
            catch
            {
                // error while parsing
                return null;
            }
        }

        private static readonly Regex RxQuery = new Regex(@"&?([^\=]+)=([^&]+)", RegexOptions.Compiled);

        private static Dictionary<string, string> ParseQuery(string query)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            //var matches = Regex.Matches(query, @"&?([^\=]+)=([^&]+)");
            MatchCollection matches = RxQuery.Matches(query);
            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                parameters[key] = value;
            }

            return parameters;
        }

        private static readonly Dictionary<string, Regex> RxRouteCache = new Dictionary<string, Regex>();

        private static Dictionary<string, string> TryMatchRoute(string route, string url)
        {
            try
            {
                Regex rxRoute;

                if (!RxRouteCache.TryGetValue(route, out rxRoute))
                {
                    // what about trailing ? (already removed by parse request?)
                    string routeExpression = "^" + Regex.Replace(route, @"{(\w+)}", @"(?<$1>[^/]+)") + "$";
                    rxRoute = new Regex(routeExpression, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    RxRouteCache[route] = rxRoute;
                }

                var match = rxRoute.Match(url);
                if (match.Success)
                {
                    Dictionary<string, string> parameters = new Dictionary<string, string>();

                    string[] groups = rxRoute.GetGroupNames();
                    for (int i = 1; i < groups.Length; ++i)
                    {
                        string key = groups[i];
                        string value = match.Groups[i].Value;
                        parameters[key] = value;
                    }

                    return parameters;
                }
            }
            catch (Exception ex)
            {
                
            }

            return null;
        }

        private async Task WriteResponse(FlexResponse response, Stream stream)
        {
            byte[] bodyArray = Encoding.UTF8.GetBytes(response.Body);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"HTTP/1.1 {response.Status.Code} {response.Status.Name}");

            foreach (var header in response.Headers)
            {
                builder.AppendLine($"{header.Key}: {header.Value}");
            }

            builder.AppendLine($"Content-Type: {response.ContentType}; charset=utf-8");
            builder.AppendLine($"Content-Length: {bodyArray.Length}");
            builder.AppendLine("Connection: close");
            builder.AppendLine();

            byte[] headerArray = Encoding.UTF8.GetBytes(builder.ToString());
            await stream.WriteAsync(headerArray, 0, headerArray.Length);

            if (bodyArray.Length > 0)
            {
                await stream.WriteAsync(bodyArray, 0, bodyArray.Length);
            }
        }
    }
}