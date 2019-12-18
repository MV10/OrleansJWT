using Orleans;
using Orleans.Runtime;
using System;
using System.Net.Http;
using System.Threading.Tasks;

// https://dotnet.github.io/orleans/Documentation/grains/interceptors.html

namespace SecuredGrain
{
    public class AccessTokenValidationFilter
        : IIncomingGrainCallFilter
    {
        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1#use-ihttpclientfactory-in-a-console-app
        // adds to System.Net.Http

        // add IdentityModel

        private readonly IHttpClientFactory httpClientFactory;

        public AccessTokenValidationFilter(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            // remember orleans itself uses grains too, check for the desired grain type or return type
            //if (!(context.Grain is SecureAdderGrain))
            if(!IsTargetReturnType())
            {
                await context.Invoke();
                return;
            }

            context.Result = new FilteredResponse();

            try
            {
                var accessToken = (string)RequestContext.Get("Bearer");
                if (string.IsNullOrWhiteSpace(accessToken))
                    throw new Exception("Unauthorized (bearer token missing in RequestContext)");

                // TODO validate token (would also be cached normally)

                await context.Invoke();
                SetMessage("Success!");
            }
            catch (Exception ex)
            {
                SetMessage($"Exception: {ex.Message}");
            }

            // captures the "context" argument
            bool IsTargetReturnType()
            => context.ImplementationMethod.ReturnType.Equals(typeof(Task<FilteredResponse>));

            void SetMessage(string message)
            => ((FilteredResponse)context.Result).Message = message;
        }
    }
}
