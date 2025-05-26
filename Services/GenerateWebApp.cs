using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
            var modelsPath = Path.Combine(_basePath, "Models");
            var controllersPath = Path.Combine(_basePath, "Controllers");
            var viewsPath = Path.Combine(_basePath, "Views");
            var sharedPath = Path.Combine(viewsPath, "Shared");
            var homePath = Path.Combine(viewsPath, "Home");
            var wwwrootPath = Path.Combine(_basePath, "wwwroot");

            Directory.CreateDirectory(modelsPath);
            Directory.CreateDirectory(controllersPath);
            Directory.CreateDirectory(viewsPath);
            Directory.CreateDirectory(sharedPath);
            Directory.CreateDirectory(homePath);
            Directory.CreateDirectory(wwwrootPath);

            foreach (var ep in endpoints)
            {
                string modelName = $"{ep.OperationId}Model";
                string controllerName = $"{ep.OperationId}Controller";
                string viewFolder = Path.Combine(viewsPath, ep.OperationId);
                Directory.CreateDirectory(viewFolder);

                File.WriteAllText(Path.Combine(modelsPath, $"{modelName}.cs"), GenerateModel(modelName, ep.RequestBody));
                File.WriteAllText(Path.Combine(controllersPath, $"{controllerName}.cs"), GenerateController(controllerName, modelName, ep));
                File.WriteAllText(Path.Combine(viewFolder, $"{ep.OperationId}.cshtml"), GenerateView(modelName, ep));
            }

            // Shared views and home
            File.WriteAllText(Path.Combine(sharedPath, "_Layout.cshtml"), "<!DOCTYPE html><html><body>@RenderBody()</body></html>");
            File.WriteAllText(Path.Combine(sharedPath, "_ViewStart.cshtml"), "@{ Layout = \"_Layout.cshtml\"; }");
            File.WriteAllText(Path.Combine(homePath, "Success.cshtml"), "<h2>Action completed successfully.</h2>");

            // Home controller
            File.WriteAllText(Path.Combine(controllersPath, "HomeController.cs"), """
using Microsoft.AspNetCore.Mvc;
public class HomeController : Controller
{
    public IActionResult Index() => Content("Welcome to your generated web app!");
    public IActionResult Success() => View();
}
""");

            // Program.cs and Startup.cs
            File.WriteAllText(Path.Combine(_basePath, "Program.cs"), """
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args) =>
        CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
""");

            File.WriteAllText(Path.Combine(_basePath, "Startup.cs"), """
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
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });
    }
}
""");

            File.WriteAllText(Path.Combine(_basePath, "appsettings.json"), "{ }");

            File.WriteAllText(Path.Combine(_basePath, "GeneratedWebApp.csproj"), """
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
""");

            // Zip it
            string zipPath = Path.Combine(_basePath, "../GeneratedWebApp.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(_basePath, zipPath);

            return zipPath;
        }

        private string GenerateModel(string modelName, Dictionary<string, string> props)
        {
            var lines = new List<string>
            {
                "using System.ComponentModel.DataAnnotations;",
                "",
                $"public class {modelName}",
                "{"
            };
            foreach (var prop in props)
            {
                lines.Add("    [Required]");
                lines.Add($"    public {MapYamlTypeToCSharp(prop.Value)} {prop.Key} {{ get; set; }}");
            }
            lines.Add("}");
            return string.Join(Environment.NewLine, lines);
        }

        private string GenerateController(string controllerName, string modelName, ParsedEndpoint ep)
        {
            var lines = new List<string>
            {
                "using Microsoft.AspNetCore.Mvc;",
                "",
                $"public class {controllerName} : Controller",
                "{"
            };

            // GET method with parameters
            if (ep.HttpMethod == "GET")
            {
                string paramList = string.Join(", ", ep.Parameters.Select(p => $"{MapYamlTypeToCSharp(p.Value)} {p.Key}"));
                foreach (var param in ep.Parameters)
                    lines.Add($"    public IActionResult {ep.OperationId}({paramList})");
                lines.Add("    {");
                foreach (var param in ep.Parameters)
                    lines.Add($"        ViewBag.{param.Key} = {param.Key};");
                lines.Add("        return View();");
                lines.Add("    }");
            }

            // POST method
            if (ep.HttpMethod == "POST")
            {
                lines.AddRange(new[]
                {
                    $"    [HttpGet]",
                    $"    public IActionResult {ep.OperationId}() => View();",
                    "",
                    $"    [HttpPost]",
                    $"    public IActionResult {ep.OperationId}({modelName} model)",
                    "    {",
                    "        if (ModelState.IsValid)",
                    "        {",
                    "            return RedirectToAction(\"Success\", \"Home\");",
                    "        }",
                    "        return View(model);",
                    "    }"
                });
            }

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

            if (ep.HttpMethod == "GET")
            {
                foreach (var param in ep.Parameters)
                {
                    lines.Add("    <div class=\"form-group\">");
                    lines.Add($"        <label for=\"{param.Key}\">{param.Key}</label>");
                    lines.Add($"        <input type=\"text\" name=\"{param.Key}\" class=\"form-control\" value=\"@ViewBag.{param.Key}\" />");
                    lines.Add("    </div>");
                }
                lines.Add("    <button type=\"submit\" class=\"btn btn-primary\">Search</button>");
            }
            else
            {
                foreach (var prop in ep.RequestBody)
                {
                    lines.Add("    <div class=\"form-group\">");
                    lines.Add($"        <label for=\"{prop.Key}\">{prop.Key}</label>");
                    lines.Add($"        <input type=\"text\" name=\"{prop.Key}\" class=\"form-control\" required />");
                    lines.Add("    </div>");
                }
                lines.Add("    <button type=\"submit\" class=\"btn btn-primary\">Submit</button>");
            }

            lines.Add("</form>");
            return string.Join(Environment.NewLine, lines);
        }

        private string MapYamlTypeToCSharp(string yamlType)
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
