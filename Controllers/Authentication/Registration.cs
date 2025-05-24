using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Xml.Linq;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.AspNetCore.Mvc;
using SwiftSpecBuild.Models.Authentication;

namespace SwiftSpecBuild.Controllers.Authentication
{
    public class Registration : Controller
    {
        private readonly string _clientId = "3qpa1otk5vqef734pq1g86f6a3";
        private readonly IAmazonCognitoIdentityProvider _provider;

        public Registration(IAmazonCognitoIdentityProvider provider)
        {
            _provider = new AmazonCognitoIdentityProviderClient(RegionEndpoint.EUWest1);
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpGet]
        public IActionResult ConfirmEmail()
        {
            return View();
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult>  Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            var signUpRequest = new SignUpRequest
            {
                ClientId = _clientId,
                Username = model.Email,
                Password = model.Password,
                UserAttributes = new List<AttributeType>
            {
                new AttributeType
                {
                    Name = "email",
                    Value = model.Email
                }
            }
            };
            try
            {
                var response = await _provider.SignUpAsync(signUpRequest);
                TempData["Message"] = "Registration successful! Please check your email and enter the confirmation code.";
                return RedirectToAction("ConfirmEmail");

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmEmail(ConfirmViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            var request = new ConfirmSignUpRequest
            {
                ClientId = _clientId,
                Username = model.Email,
                ConfirmationCode = model.Code
            };
            try
            {
                var result = await _provider.ConfirmSignUpAsync(request);
                TempData["Message"] = "Email confirmed successfully. You can now log in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            var request = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = _clientId,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", model.Email },
                    { "PASSWORD", model.Password }
                }
            };
            try
            {
                var response = await _provider.InitiateAuthAsync(request);            
                HttpContext.Response.Cookies.Append("AuthToken", response.AuthenticationResult.IdToken);     
                return RedirectToAction("Upload", "Yaml");
            }
            catch (NotAuthorizedException)
            {
                ModelState.AddModelError("", "Invalid credentials.");
            }
            catch (UserNotConfirmedException)
            {
                ModelState.AddModelError("", "Email not confirmed. Please verify your email.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var request = new ForgotPasswordRequest
            {
                ClientId = _clientId,
                Username = model.Email
            };

            try
            {
                await _provider.ForgotPasswordAsync(request);
                TempData["Message"] = "A verification code has been sent to your email.";
                return RedirectToAction("ResetPassword");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }

        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var request = new ConfirmForgotPasswordRequest
            {
                ClientId = _clientId,
                Username = model.Email,
                ConfirmationCode = model.Code,
                Password = model.NewPassword
            };

            try
            {
                await _provider.ConfirmForgotPasswordAsync(request);
                TempData["Message"] = "Password reset successfully. Please log in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }
    }
}
