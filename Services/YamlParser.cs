using YamlDotNet.RepresentationModel;

namespace SwiftSpecBuild.Services
{
    public class YamlParser
    {
        public class Endpoint
        {
            public string Path { get; set; }
            public string Method { get; set; }
        }
        public static List<Endpoint> ExtractCrudEndpoints(string yamlFilePath)
        {
            var endpoints = new List<Endpoint>();
            using var reader = new StreamReader(yamlFilePath);
            var yaml = new YamlStream();
            yaml.Load(reader);

            var root = (YamlMappingNode)yaml.Documents[0].RootNode;
            if (root.Children.TryGetValue("paths", out var pathsNodeRaw) && pathsNodeRaw is YamlMappingNode pathsNode)
            {
                foreach (var pathEntry in pathsNode.Children)
                {
                    var path = ((YamlScalarNode)pathEntry.Key).Value;
                    if (pathEntry.Value is YamlMappingNode methodsNode)
                    {
                        foreach (var methodEntry in methodsNode.Children)
                        {
                            var method = ((YamlScalarNode)methodEntry.Key).Value?.ToUpperInvariant();
                            if (method is "GET" or "POST" or "PUT" or "DELETE")
                            {
                                endpoints.Add(new Endpoint { Path = path, Method = method });
                            }
                        }
                    }
                }
            }
            return endpoints;
        }
    }
}
