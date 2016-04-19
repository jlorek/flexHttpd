using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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

        private StreamSocketListener _listener;

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
        }

        private async Task<FlexResponse> ProcessRequest(FlexRequest request)
        {
            Func<FlexRequest, Task<FlexResponse>> processor = null;

            if (request.Method == "GET")
            {
                Get.TryGetValue(request.Url, out processor);
            }
            else if (request.Method == "POST")
            {
                Post.TryGetValue(request.Url, out processor);
            }
            else if (request.Method == "PUT")
            {
                Put.TryGetValue(request.Url, out processor);
            }
            else if (request.Method == "DELETE")
            {
                Delete.TryGetValue(request.Url, out processor);
            }

            if (processor != null)
            {
                FlexResponse response = await processor(request);
                return response;
            }

            return DefaultResponse;
        }
    }
}