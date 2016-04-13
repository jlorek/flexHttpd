using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace FlexHttpd
{
    public class FlexServer // : IBackgroundTask
    {
        private const int BufferSize = 8192;

        private StreamSocketListener _listener;

        public Dictionary<string, Func<FlexRequest, Task<FlexResponse>>> Get { get; } = new Dictionary<string, Func<FlexRequest, Task<FlexResponse>>>();

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

            FlexRequest request = FlexRequest.TryParse(requestStr);
            if (request == null)
            {
                //await socket.CancelIOAsync();
                return;
            }

            FlexResponse response = await ProcessRequest(request);
            using (IOutputStream output = socket.OutputStream)
            {
                using (Stream responseStream = output.AsStreamForWrite())
                {
                    await response.WriteToStream(responseStream);
                    await responseStream.FlushAsync();
                }
            }

            Debug.WriteLine(requestStr);
        }

        private async Task<FlexResponse> ProcessRequest(FlexRequest request)
        {
            if (request.Method == "GET")
            {
                Func<FlexRequest, Task<FlexResponse>> processor;
                if (Get.TryGetValue(request.Url, out processor))
                {
                    FlexResponse response = await processor(request);
                    return response;
                }

                //foreach (var kvp in Get)
                //{
                //    //var result = MatchQueryUrl(kvp.Key, request.Url);

                //    if (Regex.IsMatch(request.Url, kvp.Key))
                //    {
                //        FlexResponse response = await kvp.Value(request);
                //        return response;
                //    }

                //    // /?(?<myID>[^/]+)/?

                //    //string fo = "/index/show/{id}";
                //    //string rx = "(?<id>[^/]+)";

                //    //string fo2 = "/index?action={action}&id={id}";
                //    //string rx2 = "(?<id>[^&/]+)";
                //}
            }

            return new FlexResponse(FlexHttpStatus.NotFound);
        }

        private Dictionary<string, string> MatchQueryUrl(string route, string query)
        {
            route = "^" + route;

            return null;
        }
    }
}