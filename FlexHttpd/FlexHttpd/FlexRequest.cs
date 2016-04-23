using System.Collections.Generic;

namespace FlexHttpd
{
    public class FlexRequest
    {
        public string Method { get; protected set; }

        public string Url { get; protected set; }

        public string Protocol { get; protected set; }

        public Dictionary<string, string> Headers { get; protected set; } = new Dictionary<string, string>();

        public Dictionary<string, string> QueryParameters { get; protected set; } = new Dictionary<string, string>();

        public string Body { get; set; }

        public FlexRequest(string method, string url, string protocol)
        {
            Method = method;
            Url = url;
            Protocol = protocol;
        }
    }
}
