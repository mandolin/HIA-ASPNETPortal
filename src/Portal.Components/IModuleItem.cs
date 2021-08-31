namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   This class encapsulates the basic attributes of a Module, and is used
    ///   by the administration pages when manipulating modules.
    /// </summary>
    public interface IModuleItem
    {
        int? ModuleOrder { get; set; }
        string ModuleTitle { get; set; }
        string PaneName { get; set; }
        int ModuleId { get; set; }
        int? ModuleDefId { get; set; }

        string EditRoles { get; set; }
        int? CacheTimeout { get; set; }

        bool? ShowMobile { get; set; }
        int? TabId { get; set; }
    }
}