using Orleans;
using Orleans.Concurrency;
using System.Threading.Tasks;

// client app gets API access token from IDS4
// token added to grain call Request Context
// filter validates access token or throws exception

// how to handle refresh token?

// not using IDS4-specific reference tokens

namespace SecuredGrain
{
    public interface ISecureAdderGrain 
        : IGrainWithIntegerKey
    {
        Task<FilteredResponse> Add(int value1, int value2);
    }

    [StatelessWorker]
    public class SecureAdderGrain 
        : Grain, ISecureAdderGrain
    {
        public Task<FilteredResponse> Add(int value1, int value2)
            => Task.FromResult(new FilteredResponse
            {
                Result = value1 + value2
            });
    }
}
