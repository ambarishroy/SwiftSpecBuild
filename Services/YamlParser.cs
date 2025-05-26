using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SwiftSpecBuild.Services
{
    public class YamlParser
    {
        public class Endpoint
        {
            public string Path { get; set; }
            public string Method { get; set; }
            public string Summary { get; set; }
            public string Description { get; set; }
        }

        public static List<Endpoint> ExtractCrudEndpoints(string yamlFilePath)
        {
            var endpoints = new List<Endpoint>();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var yamlContent = File.ReadAllText(yamlFilePath);
            var root = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);

            if (!root.ContainsKey("paths")) return endpoints;

            var parser = new YamlStream();
            parser.Load(new StringReader(yamlContent));

            var rootNode = (YamlMappingNode)parser.Documents[0].RootNode;
            if (!rootNode.Children.TryGetValue("paths", out var pathsRaw) || pathsRaw is not YamlMappingNode pathsNode)
                return endpoints;

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
                            var endpoint = new Endpoint
                            {
                                Path = path,
                                Method = method,
                                Summary = "",
                                Description = ""
                            };

                            if (methodEntry.Value is YamlMappingNode methodDetail)
                            {
                                if (methodDetail.Children.TryGetValue("summary", out var summaryNode))
                                    endpoint.Summary = ((YamlScalarNode)summaryNode).Value;

                                if (methodDetail.Children.TryGetValue("description", out var descNode))
                                    endpoint.Description = ((YamlScalarNode)descNode).Value;
                            }

                            endpoints.Add(endpoint);
                        }
                    }
                }
            }

            return endpoints;
        }
    }
}
