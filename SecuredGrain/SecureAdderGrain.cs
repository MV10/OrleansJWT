using Orleans;
using Orleans.Concurrency;
using System.Threading.Tasks;

namespace SecuredGrain
{
    public interface ISecureAdderGrain 
        : IGrainWithIntegerKey
    {
        Task<SecuredResponse<int>> Add(int value1, int value2);
    }

    [StatelessWorker]
    public class SecureAdderGrain 
        : Grain, ISecureAdderGrain
    {
        public Task<SecuredResponse<int>> Add(int value1, int value2)
            => Task.FromResult(new SecuredResponse<int>
            {
                Result = value1 + value2
            });
    }
}
