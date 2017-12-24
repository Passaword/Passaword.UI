using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Passaword.Constants;
using Passaword.Storage;
using Passaword.UI.Attributes;
using Passaword.UI.Models;
using Passaword.Utils;

namespace Passaword.UI.Controllers
{
    [Authorize]
    [SecurityHeaders]
    public class HomeController : Controller
    {
        private readonly ISecretContextService _secretContextService;
        private readonly ISecretStore _secretStore;
        private readonly IConfiguration _config;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ISecretContextService secretContextService, 
            ISecretStore secretStore, 
            IConfiguration config, 
            ILogger<HomeController> logger)
        {
            _secretContextService = secretContextService;
            _secretStore = secretStore;
            _config = config;
            _logger = logger;
        }

        public async Task<IActionResult> Index(bool created = false, string deleted = null)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email != null)
            {
                var mySecrets = await _secretStore.FindAsync(q => q.CreatedBy == email);

                if (created)
                {
                    var url = _config["Passaword:SecretUrl"].Replace("{key}", HttpUtility.UrlEncode(mySecrets.OrderByDescending(q=>q.CreatedDate).FirstOrDefault()?.Id ?? ""));
                    ViewBag.CreatedUrl = url;
                }

                if (!string.IsNullOrEmpty(deleted)) ViewBag.DeletedKey = deleted;

                return View(mySecrets);
            }
            return View();
        }

        [HttpGet]
        [Route("~/create")]
        public IActionResult Create()
        {
            var model = new SecretInputModel();
            return View(model);
        }

        [HttpPost]
        [Route("~/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SecretInputModel model)
        {
            if (ModelState.IsValid)
            {
                var email = User.FindFirstValue(ClaimTypes.Email);

                string secretId = null;
                using (var encryptContext = _secretContextService.CreateEncryptionContext(User))
                {
                    encryptContext.Secret.CreatedBy = email;
                    encryptContext.Secret.CreatedByProvider = "Google";
                    encryptContext.InputData.Add(UserInputConstants.Secret, model.Secret);
                    encryptContext.InputData.Add(UserInputConstants.Passphrase, model.Passphrase);
                    encryptContext.InputData.Add(UserInputConstants.CustomMessage, model.CustomMessage);
                    if (!string.IsNullOrEmpty(model.Email)) encryptContext.InputData.Add(UserInputConstants.EmailAddress, model.Email);
                    if (!string.IsNullOrEmpty(model.IpRegex)) encryptContext.InputData.Add(UserInputConstants.IpRegex, model.IpRegex);
                    if (!model.SendEmail) encryptContext.InputData.Add(UserInputConstants.DoNotSendEmail, true);
                    if (model.UserEmailMustMatch) encryptContext.InputData.Add(UserInputConstants.ForceAuthentication, true);
                    encryptContext.InputData.Add(UserInputConstants.Expiry, model.Expiry);
                    encryptContext.SecretProperties.Add(new SecretProperty(SecretProperties.OwnerEmail) { Data = email });

                    secretId = await encryptContext.EncryptSecretAsync();
                }
                
                return RedirectToAction("Index", new {created=true});
            }
            return View(model);
        }

        [HttpPost]
        [Route("~/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string k)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            var secret = await _secretStore.GetAsync(k);
            if (!string.IsNullOrEmpty(email) && secret?.CreatedBy == email)
            {
                await _secretStore.DeleteAsync(k);
            }

            return RedirectToAction("Index", new{deleted = k});
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("~/secret")]
        public async Task<IActionResult> Secret(string k)
        {
            using (var decryptContext = _secretContextService.CreateDecryptionContext(User))
            {
                try
                {
                    var result = await decryptContext.PreProcessAsync(k);
                    if (result.IsValid)
                    {
                        var model = new SecretRetrieveModel()
                        {
                            Id = k
                        };
                        return View(model);
                    }
                    else
                    {
                        return View("NoSecret");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    return View("NoSecret");
                }
            }
        }

        [HttpPost]
        [Route("~/secret")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecryptSecret(SecretRetrieveModel model)
        {
            using (var decryptContext = _secretContextService.CreateDecryptionContext(User))
            {
                decryptContext.InputData.Add(UserInputConstants.Passphrase, model.Passphrase);
                decryptContext.InputData.Add(UserInputConstants.IpAddress, Request.GetIpAddress());

                var result = await decryptContext.PreProcessAsync(model.Id);
                if (result.IsValid)
                {
                    var decrypted = await decryptContext.DecryptSecretAsync(model.Id);
                    if (decrypted == null)
                    {
                        return new BadRequestResult();
                    }
                    return Json(decrypted);
                }
                else
                {
                    return new BadRequestResult();
                }
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
