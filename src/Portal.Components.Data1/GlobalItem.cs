using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("PortalCfg_Globals")]
    public class GlobalItem : IGlobalItem
    {
        #region IGlobalItem Members

        [Key]
        public int PortalId { get; set; }

        public string PortalName { get; set; }
        public bool? AlwaysShowEditButton { get; set; }

        #endregion
    }
}