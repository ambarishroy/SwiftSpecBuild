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
            string modelsPath = Path.Combine(_basePath, "Models");
            string controllersPath = Path.Combine(_basePath, "Controllers");
            string viewsPath = Path.Combine(_basePath, "Views");

            Directory.CreateDirectory(modelsPath);
            Directory.CreateDirectory(controllersPath);
            Directory.CreateDirectory(viewsPath);

            foreach (var ep in endpoints)
            {
                string modelName = $"{ep.OperationId}Model";
                string controllerName = $"{ep.OperationId}Controller";
                string viewFolder = Path.Combine(viewsPath, ep.OperationId);
                string viewPath = Path.Combine(viewFolder, $"{ep.OperationId}.cshtml");

                Directory.CreateDirectory(viewFolder);

                // 1. Model
                File.WriteAllText(Path.Combine(modelsPath, $"{modelName}.cs"), GenerateModel(modelName, ep.RequestBody));

                // 2. Controller
                File.WriteAllText(Path.Combine(controllersPath, $"{controllerName}.cs"), GenerateController(controllerName, modelName, ep));

                // 3. View
                File.WriteAllText(viewPath, GenerateView(modelName, ep));
            }
            AddProjectMetadataFiles();
            // Create zip
            string zipPath = Path.Combine(_basePath, "../GeneratedWebApp.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(_basePath, zipPath);

            return zipPath;
        }
        private void AddProjectMetadataFiles()
        {
            string csproj = @$"{_basePath}/GeneratedWebApp.csproj";
            string startup = @$"{_basePath}/Startup.cs";
            string program = @$"{_basePath}/Program.cs";
            string homeController = @$"{_basePath}/Controllers/HomeController.cs";

            Directory.CreateDirectory(Path.Combine(_basePath, "Controllers"));

            File.WriteAllText(csproj, """
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
""");

            File.WriteAllText(startup, """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

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

            File.WriteAllText(program, """
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
""");

            File.WriteAllText(homeController, """
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return Content("Welcome to your generated .NET MVC Web App!");
    }
}
""");
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
                lines.Add($"    public {prop.Value} {prop.Key} {{ get; set; }}");
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
                "{",
                $"    public IActionResult {ep.OperationId}()",
                "    {",
                "        return View();",
                "    }"
            };

            if (ep.HttpMethod == "POST")
            {
                lines.AddRange(new[]
                {
                    "",
                    $"    [HttpPost]",
                    $"    public IActionResult {ep.OperationId}({modelName} model)",
                    "    {",
                    "        if (ModelState.IsValid)",
                    "        {",
                    "            // Handle POST logic",
                    "            return RedirectToAction(\"Success\");",
                    "        }",
                    "        return View(model);",
                    "    }"
                });
            }

            lines.Add("}");
            return string.Join(Environment.NewLine, lines);
        }
        //
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

            foreach (var prop in ep.RequestBody)
            {
                lines.Add("    <div class=\"form-group\">");
                lines.Add($"        <label for=\"{prop.Key}\">{prop.Key}</label>");
                lines.Add($"        <input type=\"text\" name=\"{prop.Key}\" class=\"form-control\" required />");
                lines.Add("    </div>");
            }

            lines.Add("    <button type=\"submit\" class=\"btn btn-primary\">Submit</button>");
            lines.Add("</form>");

            return string.Join(Environment.NewLine, lines);
        }

    }
}
