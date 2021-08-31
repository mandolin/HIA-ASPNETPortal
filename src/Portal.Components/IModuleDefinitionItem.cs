namespace ASPNET.StarterKit.Portal
{
    public interface IModuleDefinitionItem
    {
        string FriendlyName { get; set; }

        string MobileSourceFile { get; set; }

        string DesktopSourceFile { get; set; }

        int ModuleDefId { get; set; }
    }
}