using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("PortalCfg_ModuleDefinitions")]
    public class ModuleDefinitionItem : IModuleDefinitionItem
    {
        #region IModuleDefinitionItem Members

        public string FriendlyName { get; set; }

        public string MobileSourceFile { get; set; }

        public string DesktopSourceFile { get; set; }

        [Key]
        public int ModuleDefId { get; set; }

        #endregion
    }
}