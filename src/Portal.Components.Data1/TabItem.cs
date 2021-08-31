using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("PortalCfg_Tabs")]
    public class TabItem : ITabItem
    {
        public ICollection<ModuleItem> Modules { get; set; }

        #region ITabItem Members

        public int? TabOrder { get; set; }
        public string TabName { get; set; }

        [Key]
        public int TabId { get; set; }

        public string AccessRoles { get; set; }
        public string MobileTabName { get; set; }
        public bool? ShowMobile { get; set; }

        #endregion
    }
}