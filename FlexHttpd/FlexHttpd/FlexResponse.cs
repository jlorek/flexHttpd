using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FlexHttpd
{
    public class FlexResponse
    {
        public string ContentType { get; set; } = "text/html";

        public FlexHttpStatus Status { get; protected set; }

        public string Body { get; set; } = string.Empty;

        public FlexResponse(FlexHttpStatus status)
        {
            Status = status;
        }

        public async Task WriteToStream(Stream response)
        {
            byte[] bodyArray = Encoding.UTF8.GetBytes(Body);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"HTTP/1.1 {Status.Code} {Status.Name}");
            builder.AppendLine($"Content-Type: {ContentType}; charset=utf-8");
            builder.AppendLine($"Content-Length: {bodyArray.Length}");
            builder.AppendLine("Connection: close");
            builder.AppendLine();

            byte[] headerArray = Encoding.UTF8.GetBytes(builder.ToString());
            await response.WriteAsync(headerArray, 0, headerArray.Length);

            if (bodyArray.Length > 0)
            {
                await response.WriteAsync(bodyArray, 0, bodyArray.Length);
            }
        }
    }
}
