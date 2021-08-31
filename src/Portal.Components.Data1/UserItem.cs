using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("Portal_Users")]
    public class UserItem : IUserItem
    {
        public virtual ICollection<RoleItem> Roles { get; set; }

        #region IUserItem Members

        [Key]
        public int UserId { get; set; }

        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }

        #endregion
    }
}