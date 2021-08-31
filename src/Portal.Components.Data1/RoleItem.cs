using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("Portal_Roles")]
    public class RoleItem : IRoleItem
    {
        public virtual ICollection<UserItem> Users { get; set; }

        #region IRoleItem Members

        [Key]
        public int RoleId { get; set; }

        public int PortalId { get; set; }
        public string RoleName { get; set; }

        #endregion
    }
}