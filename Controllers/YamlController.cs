using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwiftSpecBuild.Models;
using SwiftSpecBuild.Services;

namespace SwiftSpecBuild.Controllers
{
    [Authorize]
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
            var tempFilePath = Path.GetTempFileName();

            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            var existingFilePath = Path.GetTempFileName();
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
                var endpoints = YamlParser.ExtractCrudEndpoints(tempFilePath);
                // TODO: Generate web app (stubbed for now)
                bool appGenerated = endpoints.Count > 0;
                if (appGenerated)
                {
                    // Upload the new YAML to S3 (overwrite)
                    using var uploadStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = s3Key,
                        InputStream = uploadStream,
                        ContentType = "application/x-yaml"
                    };
                    await s3.PutObjectAsync(putRequest);

                    TempData["Message"] = "New endpoints parsed and YAML uploaded.";
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
    }
}
