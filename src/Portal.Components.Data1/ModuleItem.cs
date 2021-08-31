using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("PortalCfg_Modules")]
    public class ModuleItem : IModuleItem
    {
        public virtual ICollection<ModuleSettingItem> Settings { get; set; }

        #region IModuleItem Members

        public int? ModuleOrder { get; set; }
        public string ModuleTitle { get; set; }
        public string PaneName { get; set; }

        [Key]
        public int ModuleId { get; set; }

        public int? ModuleDefId { get; set; }
        public string EditRoles { get; set; }
        public int? CacheTimeout { get; set; }
        public bool? ShowMobile { get; set; }
        public int? TabId { get; set; }

        #endregion
    }
}