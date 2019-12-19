
namespace SecuredGrain
{
    public interface ISecuredResponseValidation
    {
        bool Success { get; set; }
        string Message { get; set; }
    }

    public class SecuredResponse<TResult>
        : ISecuredResponseValidation
    {
        public TResult Result { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
