namespace ASPNET.StarterKit.Portal
{
    public interface IUserItem
    {
        int UserId { get; set; }
        string Email { get; set; }
        string Password { get; set; }
        string Name { get; set; }
    }
}