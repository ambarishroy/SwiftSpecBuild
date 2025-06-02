
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SwiftSpecBuild.Models;

namespace SwiftSpecBuild.Services
{
    public class UnitTestGenerator
    {
        private readonly string _testProjectRoot;

        public UnitTestGenerator(string testProjectRoot)
        {
            _testProjectRoot = testProjectRoot;
        }

        public void GenerateUnitTests(List<ParsedEndpoint> endpoints)
        {
            string testControllersPath = Path.Combine(_testProjectRoot, "Controllers");
            Directory.CreateDirectory(testControllersPath);
            // Clean up 
            foreach (var file in Directory.GetFiles(testControllersPath, "*ControllerTests.cs"))
            {
                try { File.Delete(file); } catch {  }
            }

            foreach (var file in Directory.GetFiles(testControllersPath, "*.cs"))
            {
                int retries = 3;
                while (retries-- > 0)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        break;
                    }
                    catch (IOException)
                    {
                        if (retries == 0) throw;
                        System.Threading.Thread.Sleep(300);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        if (retries == 0) throw;
                        System.Threading.Thread.Sleep(300);
                    }
                }
            }

            foreach (var ep in endpoints)
            {
                string className = ToPascal(ep.OperationId);
                string modelName = className + "Model";
                string testFileContent = GenerateUnitTest(className, modelName, ep);
                string testFilePath = Path.Combine(testControllersPath, className + "ControllerTests.cs");
                File.WriteAllText(testFilePath, testFileContent);
            }
        }

        private string GenerateUnitTest(string className, string modelName, ParsedEndpoint ep)
        {
            var paramAssignments = ep.Parameters.Select(p =>
                $"string {p.Key} = \"sample-{p.Key}\";");

            var modelAssignments = ep.RequestBody.Select(p =>
                $"{p.Key} = \"sample-{p.Key}\"");

            string paramVars = string.Join(Environment.NewLine + "        ", paramAssignments);
            string modelInit = modelAssignments.Any()
                ? $"var model = new {modelName} {{ {string.Join(", ", modelAssignments)} }};"
                : $"var model = new {modelName}();";

            var paramNames = string.Join(", ", ep.Parameters.Keys);
            string callParams = string.IsNullOrEmpty(paramNames) ? "model" : paramNames + ", model";

            string getTest = ep.HttpMethod == "GET"
                ? $@"
    [Fact]
    public void {className}_Get_ReturnsView()
    {{
        var controller = new {className}Controller();
        {paramVars}
        var result = controller.{className}({paramNames});
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult);
    }}"
                : "";

            return $@"
using Xunit;
using Microsoft.AspNetCore.Mvc;
using GeneratedWebApp.Controllers;
using GeneratedWebApp.Models;

namespace GeneratedWebApp.Controllers.Tests
{{
    public class {className}ControllerTests
    {{
    {getTest}
    
        [Fact]
        public void {className}_InvalidModelState_ReturnsViewWithModel()
        {{
            var controller = new {className}Controller();
            controller.ModelState.AddModelError(""Key"", ""Error"");
            {paramVars}
            {modelInit}
            var result = controller.{className}({callParams});
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }}
    
        [Fact]
        public void {className}_ValidModel_ShowsMessageInView()
        {{
            var controller = new {className}Controller();
            {paramVars}
            {modelInit}
            var result = controller.{className}({callParams});
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(controller.ViewBag.Message);
        }}
    }}
}}";
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
