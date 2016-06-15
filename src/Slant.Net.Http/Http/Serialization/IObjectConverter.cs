namespace Slant.Net.Http.Serialization
{
    public interface IObjectConverter
    {
        ISerializer Serializer { get; set; }

        IDeserializer Deserializer { get; set; }
    }
}