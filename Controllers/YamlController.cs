using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwiftSpecBuild.Models;
using SwiftSpecBuild.Services;

namespace SwiftSpecBuild.Controllers
{
    [AllowAnonymous]
    public class YamlController : Controller
    {
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(YAMLUploadViewModel model, [FromServices] S3Client s3Client, [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            System.Diagnostics.Debug.WriteLine("User.Identity.IsAuthenticated: " + User.Identity.IsAuthenticated);
            Console.WriteLine("User.Identity.Name: " + User.Identity.Name);
            
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Registration");
               
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var idToken = httpContextAccessor.HttpContext.Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(idToken))
            {
                TempData["Message"] = "Auth token missing. Please log in again.";
                return RedirectToAction("Login", "Registration");
            }

            var s3 = await s3Client.CreateS3ClientFromTokenAsync(idToken);

            var bucketName = "yaml-uploads";
            var fileName = Path.GetFileName(model.File.FileName);
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ??
                           User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value ?? "anonymous";
                         
            var s3Key = $"{userEmail}/{fileName}";
            var tempFilePath = Path.GetRandomFileName();

            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            // Validate YAML format
            try
            {
                using var reader = new StreamReader(tempFilePath);
                var yaml = new YamlDotNet.RepresentationModel.YamlStream();
                yaml.Load(reader);
            }
            catch (Exception ex)
            {
                System.IO.File.Delete(tempFilePath);
                TempData["Message"] = $"Invalid YAML format: {ex.Message}";
                return RedirectToAction("Upload");
            }


            var existingFilePath = Path.GetRandomFileName();
            bool existsInS3 = false;

            try
            {
                var response = await s3.GetObjectAsync(bucketName, s3Key);
                using (var responseStream = response.ResponseStream)
                using (var fs = new FileStream(existingFilePath, FileMode.Create))
                {
                    await responseStream.CopyToAsync(fs);
                }
                existsInS3 = true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                existsInS3 = false;
            }

            bool isNewOrChanged = !existsInS3 || FindDifferences.AreDifferent(tempFilePath, existingFilePath);

            if (isNewOrChanged)
            {
                // Parse endpoints
               
                var newParsedEndpoints = ParsedEndpointBuilder.FromYaml(tempFilePath);
                var oldParsedEndpoints = existsInS3 ? ParsedEndpointBuilder.FromYaml(existingFilePath) : new List<ParsedEndpoint>();
                var (added, removed, modified) = EndpointDiff.GetEndpointChanges(oldParsedEndpoints, newParsedEndpoints);


                var outputPath = Path.Combine(Path.GetTempPath(), $"GeneratedWebApp_{Guid.NewGuid()}");

                var generator = new GenerateWebApp(outputPath);
                

                var changesToGenerate = added.Concat(modified).ToList();
                foreach (var ep in removed)
                {
                    var name = ToPascal(ep.OperationId);
                    var models = Path.Combine(outputPath, "Models", $"{name}Model.cs");
                    var controller = Path.Combine(outputPath, "Controllers", $"{name}Controller.cs");
                    var view = Path.Combine(outputPath, "Views", name, $"{name}.cshtml");
                    var unitTest = Path.Combine(outputPath, "GeneratedWebApp.Tests", "Controllers", $"{name}ControllerTests.cs");

                    var files = new[] { models, controller, view, unitTest };
                    foreach (var f in files)
                        if (System.IO.File.Exists(f)) System.IO.File.Delete(f);

                    // Delete view folder if empty
                    var viewFolder = Path.Combine(outputPath, "Views", name);
                    if (Directory.Exists(viewFolder) && !Directory.EnumerateFileSystemEntries(viewFolder).Any())
                    {
                        Directory.Delete(viewFolder, true);
                    }
                }


                string zipFilePath = generator.GenerateAndZip(changesToGenerate);


                bool appGenerated = changesToGenerate.Count > 0; 
                if (appGenerated)
                {
                    
                    // overwrite yaml to s3
                    using var uploadStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = s3Key,
                        InputStream = uploadStream,
                        ContentType = "application/x-yaml"
                    };
                    await s3.PutObjectAsync(putRequest);

                    // Upload generated ZIP to S3
                    var zipS3Key = $"{userEmail}/GeneratedWebApp.zip";
                    using (var zipStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
                    {
                        var putZipRequest = new PutObjectRequest
                        {
                            BucketName = bucketName,
                            Key = zipS3Key,
                            InputStream = zipStream,
                            ContentType = "application/zip"
                        };
                        await s3.PutObjectAsync(putZipRequest);
                    }

                    TempData["Message"] = "YAML uploaded and project generation complete!";
                    return PhysicalFile(zipFilePath, "application/zip", "GeneratedWebApp.zip");
                }
                else
                {
                    TempData["Message"] = "YAML parsed but no valid endpoints found. Upload skipped.";
                }
            }
            else
            {
                TempData["Message"] = "No changes detected in YAML. Skipping upload.";
            }
            System.IO.File.Delete(tempFilePath);
            if (System.IO.File.Exists(existingFilePath)) System.IO.File.Delete(existingFilePath);
            return RedirectToAction("Upload");
        }
        private static string ToPascal(string input)
        {
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(input).Replace("-", "").Replace("_", "").Replace(" ", "");
        }

    }
}
