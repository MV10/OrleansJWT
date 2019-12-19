using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Orleans;
using Orleans.Runtime;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SecuredGrain
{
    public class AccessTokenValidationFilter
        : IIncomingGrainCallFilter
    {
        private readonly IMemoryCache memoryCache;
        private readonly IDiscoveryCache discoveryCache;
        private readonly IHttpClientFactory httpClientFactory;

        public AccessTokenValidationFilter(
            IMemoryCache memoryCache,
            IDiscoveryCache discoveryCache,
            IHttpClientFactory httpClientFactory)
        {
            this.memoryCache = memoryCache;
            this.discoveryCache = discoveryCache;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            var securedResponseType = GetSecuredResponseType();

            if(securedResponseType == null)
            {
                await context.Invoke();
                return;
            }

            context.Result = Activator.CreateInstance(securedResponseType);

            try
            {
                var accessToken = (string)RequestContext.Get("Bearer");
                if (string.IsNullOrWhiteSpace(accessToken))
                    throw new Exception("Unauthorized (bearer token missing in RequestContext)");

                await ThrowIfTokenInvalid(accessToken);

                await context.Invoke();
                SetSecuredResponse(true, "Authorized");
            }
            catch (Exception ex)
            {
                SetSecuredResponse(false, $"{ex.GetType().Name}: {ex.Message}");
            }

            Type GetSecuredResponseType()
            {
                // always a Task in Orleans but not always generic
                var task = context.ImplementationMethod.ReturnType;
                if (!task.IsGenericType) return null;
                if (!task.GetGenericTypeDefinition().Equals(typeof(Task<>))) return null;
                // assume nothing goofy like Task<string, SecuredResponse<>> etc.
                var taskArg = task.GetGenericArguments()[0];
                if (!taskArg.IsGenericType) return null;
                if (!taskArg.GetGenericTypeDefinition().Equals(typeof(SecuredResponse<>))) return null;
                return taskArg;
            }

            void SetSecuredResponse(bool success, string message)
            {
                ((ISecuredResponseValidation)context.Result).Success = success;
                ((ISecuredResponseValidation)context.Result).Message = message;
            }
        }

        private string GetCacheKey(JwtSecurityToken jwt)
            => $"accesstokensid:{jwt.Subject}";

        private async Task ThrowIfTokenInvalid(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadToken(accessToken) as JwtSecurityToken;

            try
            {
                if (jwt.ValidTo <= DateTime.Now)
                    throw new Exception("Unauthorized (token expired)");

                if (!jwt.Audiences.Any(a => a.Equals("api")))
                    throw new Exception("Unauthorized (api scope required)");

                if (!jwt.Header.ContainsKey("typ")
                    || !jwt.Header["typ"].Equals("at+jwt"))
                    throw new Exception("Unauthorized (wrong token type)");
            }
            catch
            {
                var key = GetCacheKey(jwt);
                memoryCache.Remove(key);
                throw;
            }

            if (!IsKnownToken(jwt))
                await VerifyAndCacheToken(jwt);
        }

        private bool IsKnownToken(JwtSecurityToken jwt)
        {
            var key = GetCacheKey(jwt);
            if (!memoryCache.TryGetValue(key, out var cachedValue)) return false;
            if (!cachedValue.Equals(jwt.RawData))
            {
                memoryCache.Remove(key);
                return false;
            }
            return true;
        }

        private async Task VerifyAndCacheToken(JwtSecurityToken jwt)
        {
            var discovery = await discoveryCache.GetAsync();
            if (discovery.IsError)
                throw new Exception("Unauthorized (authority discovery failed)");

            var client = httpClientFactory.CreateClient();

            // this user/pass combo is specific to the IdentityServer demo authority,
            // and the use of scope as the username is generally specific to IdentityServer
            client.SetBasicAuthenticationOAuth("api", "secret");

            var tokenResponse = await client.IntrospectTokenAsync(
                new TokenIntrospectionRequest
                { 
                    Address = discovery.IntrospectionEndpoint,
                    ClientId = "interactive.confidential",
                    ClientSecret = "secret",
                    Token = jwt.RawData
                });
            if (tokenResponse.IsError)
                throw new Exception($"Unauthorized (introspection error: {tokenResponse.Error})");

            if (!tokenResponse.IsActive)
                throw new Exception("Unauthorized (token deactivated by authority)");

            var key = GetCacheKey(jwt);
            memoryCache.Set(key, jwt.RawData);
        }
    }
}
