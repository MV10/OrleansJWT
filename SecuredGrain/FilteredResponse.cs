
namespace SecuredGrain
{
    //public interface IFilteredResponse
    //{
    //    int Result { get; set; }
    //    string Message { get; set; }
    //}

    public class FilteredResponse //: IFilteredResponse
    {
        public int Result { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
