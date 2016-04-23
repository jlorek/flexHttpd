using System.Collections.Generic;

namespace FlexHttpd
{
    public class FlexResponse
    {
        public Dictionary<string, string> Headers { get; protected set; } = new Dictionary<string, string>
        {
            {"Cache-Control", "no-cache"}
        };

        public string ContentType { get; set; } = "text/html";

        public FlexHttpStatus Status { get; protected set; }

        public string Body { get; set; } = string.Empty;

        public FlexResponse(FlexHttpStatus status)
        {
            Status = status;
        }
    }
}
