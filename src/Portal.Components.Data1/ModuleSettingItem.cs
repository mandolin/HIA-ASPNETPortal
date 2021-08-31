using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("PortalCfg_ModuleSettings")]
    public class ModuleSettingItem
    {
        public int ModuleId { get; set; }

        public string SettingName { get; set; }
        public string SettingText { get; set; }

        [Key]
        public int ModuleSettingId { get; set; }
    }
}