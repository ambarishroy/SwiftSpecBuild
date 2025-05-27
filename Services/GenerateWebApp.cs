using System;
using System.Collections.Generic;
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

            Directory.CreateDirectory(modelsPath);
            Directory.CreateDirectory(controllersPath);
            Directory.CreateDirectory(viewsPath);
            Directory.CreateDirectory(sharedPath);
            Directory.CreateDirectory(homePath);
            Directory.CreateDirectory(wwwrootPath);

            foreach (var ep in endpoints)
            {
                string className = ToPascal(ep.OperationId);
                string modelName = className + "Model";
                string controllerName = className + "Controller";
                string viewFolder = Path.Combine(viewsPath, className);
                Directory.CreateDirectory(viewFolder);

                File.WriteAllText(Path.Combine(modelsPath, modelName + ".cs"), GenerateModel(modelName, ep));
                File.WriteAllText(Path.Combine(controllersPath, controllerName + ".cs"), GenerateController(className, modelName, ep));
                File.WriteAllText(Path.Combine(viewFolder, className + ".cshtml"), GenerateView(modelName, ep));
            }

            File.WriteAllText(Path.Combine(sharedPath, "_Layout.cshtml"), "<!DOCTYPE html><html><body>@RenderBody()</body></html>");
            File.WriteAllText(Path.Combine(sharedPath, "_ViewStart.cshtml"), "@{ Layout = \"_Layout.cshtml\"; }");
            File.WriteAllText(Path.Combine(homePath, "Success.cshtml"), "<h2>Success</h2>");

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
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>");


            string zipPath = Path.Combine(_basePath, "../GeneratedWebApp.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(_basePath, zipPath);

            return zipPath;
        }

        private string GenerateModel(string modelName, ParsedEndpoint ep)
        {
            var lines = new List<string>
            {
                "using System.ComponentModel.DataAnnotations;",
                "",
                $"public class {modelName}",
                "{"
            };

            foreach (var p in ep.Parameters)
                lines.Add($"    [Required] public {MapType(p.Value)} {p.Key} {{ get; set; }}");

            foreach (var p in ep.RequestBody)
                lines.Add($"    [Required] public {MapType(p.Value)} {p.Key} {{ get; set; }}");

            lines.Add("}");
            return string.Join(Environment.NewLine, lines);
        }

        private string GenerateController(string className, string modelName, ParsedEndpoint ep)
        {
            var lines = new List<string>
            {
                "using Microsoft.AspNetCore.Mvc;",
                "",
                $"public class {className}Controller : Controller",
                "{"
            };

            string action = className;
            string paramList = string.Join(", ", ep.Parameters.Select(p => $"{MapType(p.Value)} {p.Key}"));

            // Always generate [HttpGet] view loader
            lines.Add("    [HttpGet]");
            lines.Add($"    public IActionResult {action}({paramList})");
            lines.Add("    {");
            foreach (var p in ep.Parameters.Keys)
                lines.Add($"        ViewBag.{p} = {p};");
            lines.Add("        return View();");
            lines.Add("    }");

            // Add method to handle the actual operation (simulate Submit)
            lines.Add("");
            string handlerAction = action + "Submit";
            string allParams = string.IsNullOrEmpty(paramList) ? $"{modelName} model" : $"{paramList}, {modelName} model";
            lines.Add($"    [HttpPost]");
            lines.Add($"    public IActionResult {handlerAction}({allParams})");
            lines.Add("    {");
            lines.Add("        if (ModelState.IsValid)");
            lines.Add("        {");
            lines.Add("            return RedirectToAction(\"Success\", \"Home\");");
            lines.Add("        }");
            lines.Add("        return View(model);");
            lines.Add("    }");

            lines.Add("}");
            return string.Join(Environment.NewLine, lines);
        }

        private string GenerateView(string modelName, ParsedEndpoint ep)
        {
            var lines = new List<string>
            {
                $"@model {modelName}",
                "",
                $"<h2>{ep.Summary}</h2>",
                $"<p>{ep.Description}</p>",
                "<form method=\"post\">"
            };

            foreach (var p in ep.Parameters)
            {
                lines.Add($"    <label>{p.Key}</label>");
                lines.Add($"    <input name=\"{p.Key}\" value=\"@ViewBag.{p.Key}\" class=\"form-control\" />");
            }

            foreach (var f in ep.RequestBody)
            {
                lines.Add($"    <label>{f.Key}</label>");
                lines.Add($"    <input name=\"{f.Key}\" class=\"form-control\" />");
            }

            lines.Add("    <button type=\"submit\" class=\"btn btn-primary\">Submit</button>");
            lines.Add("</form>");
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
            return string.Join("", input.Split(new[] { '_', '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1).ToLower()));
        }
    }
}
