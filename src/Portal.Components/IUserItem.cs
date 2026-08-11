namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧门户用户项契约。</zh-CN>
    ///     <en>Contract for a legacy portal user item.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该接口用于旧用户表投影、后台用户管理和登录辅助流程。`Password` 字段是历史存储字段，
    ///       不能在新代码中作为明文口令或 UI 回显值使用；认证、哈希升级和会话失效由专门服务处理。
    ///     </zh-CN>
    ///     <en>
    ///       This interface is used for legacy user table projections, administration user management, and login helper flows.
    ///       The `Password` field is a historical storage field and must not be treated as a plain-text password or echoed in UI by new code; authentication, hash upgrades, and session invalidation are handled by dedicated services.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public interface IUserItem
    {
        /// <summary>
        ///   <l>
        ///     <zh-CN>用户的旧数据库主键。</zh-CN>
        ///     <en>Legacy database primary key of the user.</en>
        ///   </l>
        /// </summary>
        int UserId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>用户邮箱或旧登录名字段；实际登录标识解析由登录服务处理。</zh-CN>
        ///     <en>User email or legacy login-name field; actual login identifier resolution is handled by the sign-in service.</en>
        ///   </l>
        /// </summary>
        string Email { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>历史口令存储字段；只允许认证/迁移服务按安全策略读取或更新。</zh-CN>
        ///     <en>Historical password storage field; only authentication or migration services may read or update it according to security policy.</en>
        ///   </l>
        /// </summary>
        string Password { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>用户显示名或旧名称字段；不作为唯一身份或授权依据。</zh-CN>
        ///     <en>User display name or legacy name field; not used as a unique identity or authorization source.</en>
        ///   </l>
        /// </summary>
        string Name { get; set; }
    }
}
