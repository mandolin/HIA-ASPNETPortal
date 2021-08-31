namespace ASPNET.StarterKit.Portal
{
    public interface IDocumentItemDetails : IDocumentItem
    {
        byte[] Content { get; set; }
        string ContentType { get; set; }
    }
}