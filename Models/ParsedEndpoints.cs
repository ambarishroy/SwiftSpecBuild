namespace SwiftSpecBuild.Models
{
    public class ParsedEndpoint
    {
        public string Path { get; set; }
        public string HttpMethod { get; set; }
        public string OperationId { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Endpoint { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public Dictionary<string, string> RequestBody { get; set; }
        public Dictionary<string, string> ResponseBody { get; set; }
    }
}
