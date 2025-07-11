﻿using System.IO;
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
                //clean up
            foreach (var file in Directory.GetFiles(uiTestDir, "UITesting*.cs"))
            {
                try { File.Delete(file); } catch {  }
            }
            string filePath = Path.Combine(uiTestDir, "UITesting.cs");

            var testCode = GenerateTestClass(endpoints);
            int retries = 3;
            while (retries-- > 0)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                        File.Delete(filePath);
                    }
                    File.WriteAllText(filePath, testCode);
                    break;
                }
                catch (IOException)
                {
                    if (retries == 0) throw;
                    System.Threading.Thread.Sleep(300);
                }
            }



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
                "using OpenQA.Selenium.Support.UI;",
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
                    lines.Add($"            _driver.FindElement(By.Name(\"{SanitizeName(field)}\"))" +
                              $".SendKeys(\"sample-{SanitizeName(field)}\");");
                }
                lines.Add("            _driver.FindElement(By.CssSelector(\"button[type='submit']\")).Click();");
                lines.Add("            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_driver, TimeSpan.FromSeconds(5));");
                lines.Add("            var message = wait.Until(driver => driver.FindElement(By.ClassName(\"alert\")));");
                lines.Add("            Assert.NotNull(message);");
                lines.Add("        }");
            }

            lines.Add("    }");
            lines.Add("}");

            return string.Join("\n", lines);
        }

        private string ToPascal(string input)
        {
            return string.Join("",
                input.Split(new[] { '_', '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1))
            );
        }
        private string SanitizeName(string raw)
        {
            return raw.Replace("-", "_").Replace(" ", "_");
        }

    }
}
