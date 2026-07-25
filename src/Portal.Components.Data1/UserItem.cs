using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧门户用户表的 Entity Framework 投影。</zh-CN>
    ///     <en>Entity Framework projection for the legacy portal user table.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型映射 <c>Portal_Users</c>，保留早期 Email、Name 和 Password 字段。认证服务会处理
    ///       新旧口令格式、安全版本和资料扩展，直接使用实体字段时不得绕过认证服务。
    ///     </zh-CN>
    ///     <en>
    ///       This type maps <c>Portal_Users</c> and preserves the early Email, Name, and Password fields.
    ///       The authentication service handles new and legacy password formats, security versions, and
    ///       profile extensions; direct entity-field use must not bypass that service.
    ///     </en>
    ///   </lang>
    /// </remarks>
    [Table("Portal_Users")]
    public class UserItem : IUserItem
    {
        /// <summary>
        ///   <l zh-CN="用户拥有的角色导航集合，由 EF 旧成员关系映射填充。" en="Role navigation collection populated by the legacy EF membership mapping." />
        /// </summary>
        public virtual ICollection<RoleItem> Roles { get; set; }

        #region IUserItem Members

        /// <summary>
        ///   <l zh-CN="用户主键。" en="User primary key." />
        /// </summary>
        [Key]
        public int UserId { get; set; }

        /// <summary>
        ///   <l zh-CN="用户邮箱或历史登录名字段；当前登录标识解析需经过认证服务。" en="User email or historical login-name field; current login identifier resolution must go through the authentication service." />
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///   <l zh-CN="用户口令哈希或历史兼容口令字段；不得写入明文。" en="User password hash or historical compatibility password field; plaintext must never be written here." />
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///   <l zh-CN="用户显示名或旧用户名字段。" en="User display name or legacy user-name field." />
        /// </summary>
        public string Name { get; set; }

        #endregion
    }
}
