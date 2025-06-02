using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SwiftSpecBuild.Models;

namespace SwiftSpecBuild.Services
{
    public class GenerateWebApp
    {
        private readonly string _basePath;

        public GenerateWebApp(string basePath)
        {
            _basePath = basePath;
        }

        public string GenerateAndZip(List<ParsedEndpoint> endpoints)
        {
            string modelsPath = Path.Combine(_basePath, "Models");
            string controllersPath = Path.Combine(_basePath, "Controllers");
            string viewsPath = Path.Combine(_basePath, "Views");
            string sharedPath = Path.Combine(viewsPath, "Shared");
            string homePath = Path.Combine(viewsPath, "Home");
            string wwwrootPath = Path.Combine(_basePath, "wwwroot");
            string rootPath = Path.GetDirectoryName(_basePath)!;
            string testProjectRoot = Path.Combine(Path.GetDirectoryName(_basePath)!, "GeneratedWebApp.Tests");
            Directory.CreateDirectory(testProjectRoot);
            string uiTestProjectRoot = Path.Combine(Path.GetDirectoryName(_basePath)!, "GeneratedWebApp.UITests");
            Directory.CreateDirectory(uiTestProjectRoot);

            string slnPath = Path.Combine(_basePath, "GeneratedWebApp.sln");
            string testsPath = Path.Combine(testProjectRoot, "Controllers");


            Directory.CreateDirectory(modelsPath);
            Directory.CreateDirectory(controllersPath);
            Directory.CreateDirectory(viewsPath);
            Directory.CreateDirectory(sharedPath);
            Directory.CreateDirectory(homePath);
            Directory.CreateDirectory(wwwrootPath);
            
            Directory.CreateDirectory(testsPath);

            foreach (var ep in endpoints)
            {
                string className = ToPascal(ep.OperationId);
                string modelName = className + "Model";
                string controllerName = className + "Controller";
                string viewFolder = Path.Combine(viewsPath, className);
                Directory.CreateDirectory(viewFolder);

                File.WriteAllText(Path.Combine(modelsPath, modelName + ".cs"), GenerateModel(modelName, ep));
                File.WriteAllText(Path.Combine(controllersPath, controllerName + ".cs"), GenerateController(className, modelName, ep));
                File.WriteAllText(Path.Combine(viewFolder, className + ".cshtml"), GenerateView(modelName, ep, className));
                

            }
            new UnitTestGenerator(testProjectRoot).GenerateUnitTests(endpoints);
            new UITestGenerator(uiTestProjectRoot).GenerateUITests(endpoints);

            string propsDir = Path.Combine(_basePath, "Properties");
            Directory.CreateDirectory(propsDir);

            File.WriteAllText(Path.Combine(propsDir, "launchSettings.json"),
            $@"
            {{
              ""profiles"": {{
                ""GeneratedWebApp"": {{
                  ""commandName"": ""Project"",
                  ""dotnetRunMessages"": true,
                  ""launchBrowser"": true,
                  ""applicationUrl"": ""https://localhost:7010;http://localhost:5010"",
                  ""environmentVariables"": {{
                    ""ASPNETCORE_ENVIRONMENT"": ""Development""
                  }}
                }}
              }}
            }}");

            File.WriteAllText(Path.Combine(testProjectRoot, "GeneratedWebApp.Tests.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <IsPackable>false</IsPackable>
                <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
                <PackageReference Include="xunit" Version="2.9.2" />
                <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
                <PackageReference Include="xunit.assert" Version="2.9.2" />
                <PackageReference Include="xunit.extensibility.core" Version="2.9.2" />
                <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.15" />
              </ItemGroup>
              <ItemGroup>
                <ProjectReference Include="../GeneratedWebApp/GeneratedWebApp.csproj" />
              </ItemGroup>
            </Project>
            """);


            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore \"{slnPath}\"",
                WorkingDirectory = Path.GetDirectoryName(slnPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit();

            File.WriteAllText(Path.Combine(uiTestProjectRoot, "GeneratedWebApp.UITests.csproj"), @"
            <Project Sdk=""Microsoft.NET.Sdk"">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.12.0"" />
                <PackageReference Include=""xunit"" Version=""2.9.2"" />
                <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.4.5"" />
                <PackageReference Include=""Selenium.WebDriver"" Version=""4.19.0"" />
                <PackageReference Include=""Selenium.WebDriver.ChromeDriver"" Version=""136.0.0"" />
              </ItemGroup>
            </Project>
            ");

            var methodMap = endpoints
             .GroupBy(e => e.HttpMethod.ToUpper())
             .ToDictionary(
                 g => g.Key,
                g => g.Select(e => ToPascal(e.OperationId)).OrderBy(x => x).ToList()
             );

            var layoutLines = new List<string>
            {
                "<!DOCTYPE html>",
                "<html lang=\"en\">",
                "<head>",
                "    <meta charset=\"utf-8\" />",
                "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />",
                "    <title>@ViewData[\"Title\"] - GeneratedWebApp</title>",
                "    <link href=\"https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css\" rel=\"stylesheet\" />",
                "</head>",
                "<body>",
                "    <nav class=\"navbar navbar-expand-lg navbar-dark bg-primary shadow-sm\">",
                "        <div class=\"container-fluid\">",
                "            <a class=\"navbar-brand\" href=\"/\">Generated WebApp</a>",
                "            <button class=\"navbar-toggler\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#navbarNav\">",
                "                <span class=\"navbar-toggler-icon\"></span>",
                "            </button>",
                "            <div class=\"collapse navbar-collapse\" id=\"navbarNav\">",
                "                <ul class=\"navbar-nav\">"
            };
            
                        // build dropdowns
                        foreach (var method in methodMap.Keys.OrderBy(m => m))
                        {
                            layoutLines.Add($"                    <li class=\"nav-item dropdown\">");
                            layoutLines.Add($"                        <a class=\"nav-link dropdown-toggle\" href=\"#\" id=\"{method}\" role=\"button\" data-bs-toggle=\"dropdown\" aria-expanded=\"false\">{method}</a>");
                            layoutLines.Add($"                        <ul class=\"dropdown-menu\" aria-labelledby=\"{method}\">");
                            foreach (var op in methodMap[method])
                            {
                                layoutLines.Add($"                            <li><a class=\"dropdown-item\" href=\"/{op}/{op}\">{op}</a></li>");
                            }
                            layoutLines.Add("                        </ul>");
                            layoutLines.Add("                    </li>");
                        }
            
                        layoutLines.AddRange(new[]
                        {
                "                </ul>",
                "            </div>",
                "        </div>",
                "    </nav>",
                "    <main role=\"main\" class=\"pb-5\">",
                "        @RenderBody()",
                "    </main>",
                "    <footer class=\"footer bg-light border-top py-3\">",
                "        <div class=\"container text-center\">",
                "            <span class=\"text-muted\">© @DateTime.Now.Year SwiftSpecBuild. All rights reserved.</span>",
                "        </div>",
                "    </footer>",
                "    <script src=\"https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js\"></script>",
                "</body>",
                "</html>"
            });
            
            File.WriteAllText(Path.Combine(sharedPath, "_Layout.cshtml"), string.Join(Environment.NewLine, layoutLines));





            File.WriteAllText(Path.Combine(viewsPath, "_ViewImports.cshtml"),
              """
             @using Microsoft.AspNetCore.Mvc.Infrastructure
             @inject IActionDescriptorCollectionProvider ActionDescriptorCollectionProvider
             """);

            File.WriteAllText(Path.Combine(homePath, "Success.cshtml"),
             """
             @{
                 ViewData["Title"] = "Submission Successful";
             }

             <div class="container py-5">
                 <div class="row justify-content-center">
                     <div class="col-md-8">
                         <div class="alert alert-success shadow-lg text-center">
                             <h4 class="alert-heading">🎉 Submission Successful!</h4>
                             <p class="mb-3">Your request has been processed successfully.</p>
                             <hr>
                             <a href="/" class="btn btn-outline-primary">Go Back to Home</a>
                         </div>
                     </div>
                 </div>
             </div>
             """);


            File.WriteAllText(Path.Combine(controllersPath, "HomeController.cs"),
                """
                using Microsoft.AspNetCore.Mvc;
                public class HomeController : Controller
                {
                    public IActionResult Index() => Content("Welcome to your generated web app!");
                    public IActionResult Success() => View();
                }
                """);

            File.WriteAllText(Path.Combine(_basePath, "Program.cs"),
                """
                using Microsoft.AspNetCore.Hosting;
                using Microsoft.Extensions.Hosting;

                public class Program
                {
                    public static void Main(string[] args) =>
                        CreateHostBuilder(args).Build().Run();

                    public static IHostBuilder CreateHostBuilder(string[] args) =>
                        Host.CreateDefaultBuilder(args)
                            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
                }
                """);

            File.WriteAllText(Path.Combine(_basePath, "Startup.cs"),
                """
                using Microsoft.AspNetCore.Builder;
                using Microsoft.AspNetCore.Hosting;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                public class Startup
                {
                    public void ConfigureServices(IServiceCollection services) =>
                        services.AddControllersWithViews();

                    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
                    {
                        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
                        else app.UseExceptionHandler("/Home/Error");

                        app.UseStaticFiles();
                        app.UseRouting();
                        app.UseAuthorization();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
                        });
                    }
                }
                """);

            File.WriteAllText(Path.Combine(_basePath, "appsettings.json"), "{ }");

            File.WriteAllText(Path.Combine(_basePath, "GeneratedWebApp.csproj"),
                     @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
                      <PropertyGroup>
                        <TargetFramework>net8.0</TargetFramework>
                        <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
                        <Nullable>enable</Nullable>
                        <ImplicitUsings>enable</ImplicitUsings>
                      </PropertyGroup>
                    </Project>");
            // string slnPath = Path.Combine(Path.GetDirectoryName(_basePath)!, "GeneratedWebApp.sln");


            File.WriteAllText(slnPath,
             """
             Microsoft Visual Studio Solution File, Format Version 12.00
             # Visual Studio Version 17
             VisualStudioVersion = 17.8.33428.342
             MinimumVisualStudioVersion = 10.0.40219.1
             Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "GeneratedWebApp", "GeneratedWebApp.csproj", "{11111111-1111-1111-1111-111111111111}"
             EndProject
             Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "GeneratedWebApp.Tests", "../GeneratedWebApp.Tests/GeneratedWebApp.Tests.csproj", "{22222222-2222-2222-2222-222222222222}"
             EndProject
             Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "GeneratedWebApp.UITests", "../GeneratedWebApp.UITests/GeneratedWebApp.UITests.csproj", "{33333333-3333-3333-3333-333333333333}"
             EndProject
             Global
                 GlobalSection(SolutionConfigurationPlatforms) = preSolution
                     Debug|Any CPU = Debug|Any CPU
                     Release|Any CPU = Release|Any CPU
                 EndGlobalSection
                 GlobalSection(ProjectConfigurationPlatforms) = postSolution
                     {11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                     {11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
                     {22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                     {22222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU
                     {33333333-3333-3333-3333-333333333333}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                     {33333333-3333-3333-3333-333333333333}.Debug|Any CPU.Build.0 = Debug|Any CPU
                 EndGlobalSection
                 GlobalSection(SolutionProperties) = preSolution
                     HideSolutionNode = FALSE
                 EndGlobalSection
             EndGlobal
             """);


            
            string zipPath = Path.Combine(rootPath, "GeneratedWebAppBundle.zip");

            // Temp folder
            string bundleRoot = Path.Combine(rootPath, "BundleTemp");
            if (Directory.Exists(bundleRoot)) Directory.Delete(bundleRoot, true);
            Directory.CreateDirectory(bundleRoot);

            // Copying webapp. unitTest and UITest in same folder
            CopyDirectory(_basePath, Path.Combine(bundleRoot, "GeneratedWebApp"));
            CopyDirectory(Path.Combine(rootPath, "GeneratedWebApp.Tests"), Path.Combine(bundleRoot, "GeneratedWebApp.Tests"));
            CopyDirectory(Path.Combine(rootPath, "GeneratedWebApp.UITests"), Path.Combine(bundleRoot, "GeneratedWebApp.UITests"));


            // Zipping
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(bundleRoot, zipPath);

            // Clean up
            Directory.Delete(bundleRoot, true);

            return zipPath;

        }
        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);

            foreach (var dir in Directory.GetDirectories(sourceDir))
                CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }

        private string GenerateModel(string modelName, ParsedEndpoint ep)
        {
            var lines = new List<string>
            {
                "using System.ComponentModel.DataAnnotations;",
                "",
                "namespace GeneratedWebApp.Models",
                "{",
                $"public class {modelName}",
                "{"
            };

            foreach (var p in ep.Parameters)
                lines.Add($"    [Required] public {MapType(p.Value)} {p.Key} {{ get; set; }}");

            foreach (var p in ep.RequestBody)
                lines.Add($"    [Required] public {MapType(p.Value)} {p.Key} {{ get; set; }}");

            lines.Add("}");
            lines.Add("}");
            return string.Join(Environment.NewLine, lines);
        }

        private string GenerateController(string className, string modelName, ParsedEndpoint ep)
        {
            var lines = new List<string>
            {
                "using GeneratedWebApp.Models;",
                "using Microsoft.AspNetCore.Mvc;",
                "using System.Net.Http;",
                "using System.Text;",
                "using System.Text.Json;",
                "",
                "namespace GeneratedWebApp.Controllers",
                "{",
                $"public class {className}Controller : Controller",
                "{"
            };

            string action = className;
            string paramList = string.Join(", ", ep.Parameters.Select(p => $"{MapType(p.Value)} {p.Key}"));

            // View loader httpget
            lines.Add("    [HttpGet]");
            lines.Add($"    public IActionResult {action}({paramList})");
            lines.Add("    {");
            foreach (var p in ep.Parameters.Keys)
                lines.Add($"        ViewBag.{p} = {p};");
            lines.Add("        return View();");
            lines.Add("    }");

            // handle form httpPost
            string allParams = string.IsNullOrEmpty(paramList) ? $"{modelName} model" : $"{paramList}, {modelName} model";
            lines.Add("    [HttpPost]");
            lines.Add($"    public IActionResult {action}({allParams})");
            lines.Add("    {");
            lines.Add("        if (ModelState.IsValid)");
            lines.Add("        {");
            lines.Add("            using var client = new HttpClient();");
            lines.Add($"            var apiUrl = \"{ep.Endpoint}\";");
            lines.Add("            var json = JsonSerializer.Serialize(model);");
            lines.Add("            var content = new StringContent(json, Encoding.UTF8, \"application/json\");");
            lines.Add("            var response = client.PostAsync(apiUrl, content).Result;");
            lines.Add("            if (response.IsSuccessStatusCode)");
            lines.Add("            {");
            lines.Add("                ViewBag.Message = \"API call succeeded.\";");
            lines.Add("            }");
            lines.Add("            else");
            lines.Add("            {");
            lines.Add("                var errorContent = response.Content.ReadAsStringAsync().Result;");
            lines.Add("                ViewBag.Message = $\"API call failed with status {response.StatusCode}: {errorContent}\";");
            lines.Add("            }");
            lines.Add("        }");
            lines.Add("        return View(model);");
            lines.Add("    }");


            lines.Add("}");
            lines.Add("}");
            return string.Join(Environment.NewLine, lines);
        }


        private string GenerateView(string modelName, ParsedEndpoint ep, string className)
        {
            string action = className;
            var lines = new List<string>
    {
        $"@model GeneratedWebApp.Models.{modelName}",
        "@{",
        "    Layout = \"~/Views/Shared/_Layout.cshtml\";",
        $"    ViewData[\"Title\"] = \"{ep.Summary}\";",
        "}",
        "",
        "<div class=\"container py-5\">",
        "    <div class=\"row justify-content-center\">",
        "        <div class=\"col-lg-8 col-md-10\">",
        "            <div class=\"card border-0 shadow\">",
        "                <div class=\"card-header bg-primary text-white\">",
        $"                    <h3 class=\"mb-0\">{ep.Summary}</h3>",
        "                </div>",
        "                <div class=\"card-body\">",
        $"                    <p class=\"mb-4 text-muted\">{ep.Description}</p>",

        "                    @if (ViewBag.Message != null)",
        "                    {",
        "                        var isSuccess = ViewBag.Message.ToString().Contains(\"succeeded\");",
        "                        <div class=\"alert @(isSuccess ? \"alert-success\" : \"alert-danger\")\" role=\"alert\">",
        "                            @ViewBag.Message",
        "                        </div>",
        "                    }",

        $"                    <form method=\"post\" asp-action=\"{action}\" novalidate>",
    };

            foreach (var p in ep.Parameters)
            {
                lines.Add("                        <div class=\"mb-3\">");
                lines.Add($"                            <label for=\"{p.Key}\" class=\"form-label\">{p.Key}</label>");
                lines.Add($"                            <input type=\"text\" name=\"{p.Key}\" value=\"@ViewBag.{p.Key}\" class=\"form-control\" id=\"{p.Key}\" placeholder=\"Enter {p.Key}\" required>");
                lines.Add("                        </div>");
            }

            foreach (var f in ep.RequestBody)
            {
                lines.Add("                        <div class=\"mb-3\">");
                lines.Add($"                            <label for=\"{f.Key}\" class=\"form-label\">{f.Key}</label>");
                lines.Add($"                            <input type=\"text\" name=\"{f.Key}\" class=\"form-control\" id=\"{f.Key}\" placeholder=\"Enter {f.Key}\" required>");
                lines.Add("                        </div>");
            }

            lines.Add("                        <div class=\"d-grid\">");
            lines.Add("                            <button type=\"submit\" class=\"btn btn-success btn-lg\">Submit</button>");
            lines.Add("                        </div>");
            lines.Add("                    </form>");
            lines.Add("                </div>");
            lines.Add("            </div>");
            lines.Add("        </div>");
            lines.Add("    </div>");
            lines.Add("</div>");

            return string.Join(Environment.NewLine, lines);
        }



        private string MapType(string type)
        {
            return type switch
            {
                "integer" => "int",
                "number" => "float",
                "boolean" => "bool",
                _ => "string"
            };
        }

        private string ToPascal(string input)
        {
            return string.Join("",
                input.Split(new[] { '_', '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1))
            );
        }

    }
}
