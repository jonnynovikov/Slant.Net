namespace Slant.Net.Http.Serialization
{
    public interface ISerializer
    {
        string Serialize(object obj);

        string ContentType { get; set; }
    }
}