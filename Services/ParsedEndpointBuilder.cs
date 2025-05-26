using System;
using System.Collections.Generic;
using System.IO;
using SwiftSpecBuild.Models;
using YamlDotNet.RepresentationModel;

namespace SwiftSpecBuild.Services
{
    public class ParsedEndpointBuilder
    {
        public static List<ParsedEndpoint> FromYaml(string yamlFilePath)
        {
            var endpoints = new List<ParsedEndpoint>();

            var yamlContent = File.ReadAllText(yamlFilePath);
            var yaml = new YamlStream();
            yaml.Load(new StringReader(yamlContent));

            var root = (YamlMappingNode)yaml.Documents[0].RootNode;
            if (!root.Children.TryGetValue("paths", out var pathsRaw) || pathsRaw is not YamlMappingNode pathsNode)
                return endpoints;

            foreach (var pathEntry in pathsNode.Children)
            {
                var path = ((YamlScalarNode)pathEntry.Key).Value;
                if (pathEntry.Value is not YamlMappingNode methodsNode) continue;

                foreach (var methodEntry in methodsNode.Children)
                {
                    var method = ((YamlScalarNode)methodEntry.Key).Value?.ToUpperInvariant();
                    if (method is not ("GET" or "POST" or "PUT" or "DELETE" or "PATCH")) continue;

                    var operationId = $"{method.ToLower()}_{path.Trim('/').Replace("/", "_").Replace("{", "").Replace("}", "")}";

                    var parsedEndpoint = new ParsedEndpoint
                    {
                        Path = path,
                        HttpMethod = method,
                        OperationId = operationId,
                        RequestBody = new Dictionary<string, string>(),
                        ResponseBody = new Dictionary<string, string>(),
                        Parameters = new Dictionary<string, string>(),
                        Summary = "",
                        Description = ""
                    };

                    if (methodEntry.Value is not YamlMappingNode methodDetail) continue;
                    if (methodDetail.Children.TryGetValue("summary", out var summaryNode))
                        parsedEndpoint.Summary = ((YamlScalarNode)summaryNode).Value;

                    if (methodDetail.Children.TryGetValue("description", out var descNode))
                        parsedEndpoint.Description = ((YamlScalarNode)descNode).Value;
                    // Parse parameters
                    if (methodDetail.Children.TryGetValue("parameters", out var parametersNode) &&
                        parametersNode is YamlSequenceNode parametersList)
                    {
                        foreach (YamlMappingNode param in parametersList)
                        {
                            if (param.Children.TryGetValue("name", out var nameNode) &&
                                param.Children.TryGetValue("schema", out var schemaNode) &&
                                schemaNode is YamlMappingNode schemaMap &&
                                schemaMap.Children.TryGetValue("type", out var typeNode))
                            {
                                var name = ((YamlScalarNode)nameNode).Value;
                                var type = ((YamlScalarNode)typeNode).Value;
                                parsedEndpoint.Parameters[name] = MapYamlTypeToCSharp(type);
                            }
                        }
                    }

                    // Parse requestBody
                    if (methodDetail.Children.TryGetValue("requestBody", out var requestBodyNode) &&
                        requestBodyNode is YamlMappingNode requestBodyMap &&
                        requestBodyMap.Children.TryGetValue("content", out var contentNode) &&
                        contentNode is YamlMappingNode contentMap &&
                        contentMap.Children.TryGetValue("application/json", out var appJsonNode) &&
                        appJsonNode is YamlMappingNode jsonNode &&
                        jsonNode.Children.TryGetValue("schema", out var schemaNode2) &&
                        schemaNode2 is YamlMappingNode schemaMap2 &&
                        schemaMap2.Children.TryGetValue("properties", out var propertiesNode) &&
                        propertiesNode is YamlMappingNode propertiesMap)
                    {
                        foreach (var prop in propertiesMap.Children)
                        {
                            var propName = ((YamlScalarNode)prop.Key).Value;
                            var propType = "string";

                            if (prop.Value is YamlMappingNode valNode &&
                                valNode.Children.TryGetValue("type", out var typeNode))
                            {
                                propType = ((YamlScalarNode)typeNode).Value;
                            }

                            parsedEndpoint.RequestBody[propName] = MapYamlTypeToCSharp(propType);
                        }
                    }

                    // Parse responses > 200 > content > application/json > schema > properties
                    if (methodDetail.Children.TryGetValue("responses", out var responsesNode) &&
                        responsesNode is YamlMappingNode responsesMap &&
                        responsesMap.Children.TryGetValue("200", out var response200Node) &&
                        response200Node is YamlMappingNode response200Map &&
                        response200Map.Children.TryGetValue("content", out var responseContentNode) &&
                        responseContentNode is YamlMappingNode responseContentMap &&
                        responseContentMap.Children.TryGetValue("application/json", out var responseJsonNode) &&
                        responseJsonNode is YamlMappingNode responseJsonMap &&
                        responseJsonMap.Children.TryGetValue("schema", out var responseSchemaNode) &&
                        responseSchemaNode is YamlMappingNode responseSchemaMap &&
                        responseSchemaMap.Children.TryGetValue("properties", out var responsePropertiesNode) &&
                        responsePropertiesNode is YamlMappingNode responsePropertiesMap)
                    {
                        foreach (var respProp in responsePropertiesMap.Children)
                        {
                            var propName = ((YamlScalarNode)respProp.Key).Value;
                            var propType = "string";

                            if (respProp.Value is YamlMappingNode valNode &&
                                valNode.Children.TryGetValue("type", out var typeNode))
                            {
                                propType = ((YamlScalarNode)typeNode).Value;
                            }

                            parsedEndpoint.ResponseBody[propName] = MapYamlTypeToCSharp(propType);
                        }
                    }

                    endpoints.Add(parsedEndpoint);
                }
            }

            return endpoints;
        }

        private static string MapYamlTypeToCSharp(string yamlType)
        {
            return yamlType switch
            {
                "integer" => "int",
                "number" => "float",
                "boolean" => "bool",
                "string" => "string",
                _ => "string"
            };
        }
    }
}
