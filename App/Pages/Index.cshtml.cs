using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Orleans;
using Orleans.Runtime;
using SecuredGrain;

namespace App.Pages
{
    public class IndexModel : PageModel
    {
        private readonly Random random = new Random();
        private readonly IClusterClient clusterClient;

        public IndexModel(IClusterClient clusterClient)
        {
            this.clusterClient = clusterClient;
        }

        public string TokenExpiration;
        public string ApiResult;
        public string FilterResult;

        public async Task OnGet()
        {
            ApiResult = "(click Call API button)";
            FilterResult = ApiResult;
            TokenExpiration = await HttpContext.GetTokenAsync("expires_at") ?? "(no expiration claim)";
        }

        public IActionResult OnGetLogin()
        {
            var authProps = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(15),
                RedirectUri = Url.Content("~/")
            };
            return Challenge(authProps, "oidc");
        }

        public async Task OnGetLogout()
        {
            var authProps = new AuthenticationProperties
            {
                RedirectUri = Url.Content("~/")
            };
            await HttpContext.SignOutAsync("Cookies");
            await HttpContext.SignOutAsync("oidc", authProps);
        }

        public async Task OnGetCallAPI()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            RequestContext.Set("Bearer", accessToken);

            var adder = clusterClient.GetGrain<ISecureAdderGrain>(0);

            int v1 = random.Next(1, 100);
            int v2 = random.Next(1, 100);
            var result = await adder.Add(v1, v2);

            ApiResult = $"Add({v1}, {v2}) = {result.Result}";
            FilterResult = result.Message ?? "(no message)";
        }
    }
}
