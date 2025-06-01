using System.IO;
using System.Linq;
using System.Collections.Generic;
using SwiftSpecBuild.Models;

namespace SwiftSpecBuild.Services
{
    public class UITestGenerator
    {
        private readonly string _testProjectRoot;

        public UITestGenerator(string testProjectRoot)
        {
            _testProjectRoot = testProjectRoot;
        }

        public void GenerateUITests(List<ParsedEndpoint> endpoints)
        {
            string uiTestDir = Path.Combine(_testProjectRoot, "UITests");
            Directory.CreateDirectory(uiTestDir);
            string filePath = Path.Combine(uiTestDir, "UITesting.cs");

            var testCode = GenerateTestClass(endpoints);
            File.WriteAllText(filePath, testCode);

            
        }

        private string GenerateTestClass(List<ParsedEndpoint> endpoints)
        {
            var lines = new List<string>
            {
                "using System;",
                "using System.IO;",
                "using System.Collections.Generic;",
                "using System.Text.Json;",
                "using OpenQA.Selenium;",
                "using OpenQA.Selenium.Chrome;",
                "using Xunit;",
                "",
                "namespace GeneratedWebApp.UITests",
                "{",
                "    public class UITesting : IDisposable",
                "    {",
                "        private readonly IWebDriver _driver;",
                "        private readonly string _baseUrl;",
                "",
                "        public UITesting()",
                "        {",
                "            var options = new ChromeOptions();",
                "            options.AcceptInsecureCertificates = true;",
                "            _driver = new ChromeDriver(options);",
                "            _baseUrl = \"https://localhost:7010\";",              
                "        }",
                "",
                "        public void Dispose() => _driver.Quit();"
            };

            foreach (var ep in endpoints)
            {
                string testName = ToPascal(ep.OperationId) + "_UITest";
                string urlPath = "/" + ToPascal(ep.OperationId) + "/" + ToPascal(ep.OperationId);
                var parameters = ep.Parameters.Keys.Concat(ep.RequestBody.Keys).ToList();

                lines.Add("");
                lines.Add("        [Fact]");
                lines.Add($"        public void {testName}()");
                lines.Add("        {");
                lines.Add($"            _driver.Navigate().GoToUrl($\"{{_baseUrl}}{urlPath}\");");
                foreach (var field in parameters)
                {
                    lines.Add($"            _driver.FindElement(By.Name(\"{field}\"))" +
                              $".SendKeys(\"sample-{field}\");");
                }
                lines.Add("            _driver.FindElement(By.CssSelector(\"button[type='submit']\")).Click();");
                lines.Add("            var message = _driver.FindElement(By.ClassName(\"alert\"));");
                lines.Add("            Assert.NotNull(message);");
                lines.Add("        }");
            }

            lines.Add("    }");
            lines.Add("}");

            return string.Join("\n", lines);
        }

        private string ToPascal(string input)
        {
            return string.Join("", input.Split(new[] { '_', '-', '/' }, System.StringSplitOptions.RemoveEmptyEntries)
                                        .Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1)));
        }
    }
}
