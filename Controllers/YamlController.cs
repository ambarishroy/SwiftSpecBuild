using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwiftSpecBuild.Models;

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
        public async Task<IActionResult> Upload(YAMLUploadViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("User.Identity.IsAuthenticated: " + User.Identity.IsAuthenticated);
            Console.WriteLine("User.Identity.Name: " + User.Identity.Name);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Save to S3
            TempData["Message"] = "YAML file uploaded successfully.";
            return RedirectToAction("Upload");
        }
    }
}
