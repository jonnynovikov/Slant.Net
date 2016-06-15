using Slant.Net.Http.Response;

namespace Slant.Net.Http.Serialization
{
    public interface IDeserializer
    {
        T Deserialize<T>(IRestResponse response) where T : class;
    }
}