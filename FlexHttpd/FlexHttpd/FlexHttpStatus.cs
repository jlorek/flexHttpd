namespace FlexHttpd
{
    public class FlexHttpStatus
    {
        public static FlexHttpStatus Ok { get; } = new FlexHttpStatus(200, "OK");

        public static FlexHttpStatus BadRequest { get; } = new FlexHttpStatus(400, "Bad Request");

        public static FlexHttpStatus NotFound { get; } = new FlexHttpStatus(404, "Not Found");

        public static FlexHttpStatus InternalServerError { get; } = new FlexHttpStatus(500, "Internal Server Error");

        public static FlexHttpStatus NotImplemented { get; } = new FlexHttpStatus(501, "Not Implemented");

        public int Code { get; private set; }

        public string Name { get; private set; }

        public FlexHttpStatus(int code, string name)
        {
            Code = code;
            Name = name;
        }
    }
}
