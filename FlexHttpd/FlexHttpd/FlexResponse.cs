using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FlexHttpd
{
    public class FlexResponse
    {
        public string Body { get; protected set; } = String.Empty;

        public FlexHttpStatus Status { get; protected set; }

        public FlexResponse(FlexHttpStatus status)
        {
            Status = status;
        }
        public FlexResponse(FlexHttpStatus status, string body)
        {
            Status = status;
            Body = body;
        }

        public async Task WriteToStream(Stream response)
        {
            byte[] bodyArray = Encoding.UTF8.GetBytes(Body);

            //MemoryStream bodyStream = new MemoryStream(bodyArray);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(String.Format("HTTP/1.1 {0} {1}", Status.Code, Status.Name));
            builder.AppendLine("Content-Type: text/html; charset=utf-8");
            builder.AppendLine("Content-Length: " + bodyArray.Length);
            builder.AppendLine("Connection: close");

            builder.AppendLine();

            //string header =
            //        "HTTP/1.1 200 OK\r\n" +
            //        "Content-Type: text/html; charset=utf-8\r\n" +
            //        "Content-Length: " + bodyArray.Length /*bodyStream.Length*/ + "\r\n" +
            //        "Connection: close\r\n" +
            //        "\r\n";

            byte[] headerArray = Encoding.UTF8.GetBytes(builder.ToString());

            await response.WriteAsync(headerArray, 0, headerArray.Length);

            if (bodyArray.Length > 0)
            {
                await response.WriteAsync(bodyArray, 0, bodyArray.Length);
            }

            //await bodyStream.CopyToAsync(response);
        }
    }
}
