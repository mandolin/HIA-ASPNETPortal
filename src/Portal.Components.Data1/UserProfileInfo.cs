namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>门户用户资料扩展只读视图的默认实现。</zh-CN>
    ///     <en>Default implementation of the Portal user-profile extension read-only view.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该对象把旧用户表和新用户资料扩展表的查询结果合并为只读视图，供后台用户管理和企业身份相关页面展示。
    ///       它不包含口令、Cookie、安全版本或角色列表；认证、授权和会话失效判断应继续通过专门服务完成。
    ///     </zh-CN>
    ///     <en>
    ///       This object merges query results from the legacy user table and the newer user-profile extension table
    ///       into a read-only view for user administration and enterprise identity pages. It does not contain
    ///       passwords, cookies, security versions, or role lists; authentication, authorization, and session
    ///       invalidation remain owned by dedicated services.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public sealed class UserProfileInfo : IUserProfileInfo
    {
        /// <summary>
        ///   <lang>
        ///     <zh-CN>创建用户资料扩展只读视图。</zh-CN>
        ///     <en>Creates a user-profile extension read-only view.</en>
        ///   </lang>
        /// </summary>
        /// <param name="userId">
        ///   <l>
        ///     <zh-CN>门户用户主键。</zh-CN>
        ///     <en>Portal user primary key.</en>
        ///   </l>
        /// </param>
        /// <param name="legacyName">
        ///   <l>
        ///     <zh-CN>旧用户表中的原始名称，用于兼容显示和迁移核对。</zh-CN>
        ///     <en>Original name from the legacy user table, used for compatibility display and migration review.</en>
        ///   </l>
        /// </param>
        /// <param name="loginName">
        ///   <l>
        ///     <zh-CN>当前登录名或账号名；展示层输出前仍需编码。</zh-CN>
        ///     <en>Current login or account name; presentation code must still encode it before output.</en>
        ///   </l>
        /// </param>
        /// <param name="displayName">
        ///   <l>
        ///     <zh-CN>用户显示名称，优先服务企业门户界面展示。</zh-CN>
        ///     <en>User display name, primarily used by the enterprise portal UI.</en>
        ///   </l>
        /// </param>
        /// <param name="nickname">
        ///   <l>
        ///     <zh-CN>用户昵称或短名称；为空时由页面决定回退显示。</zh-CN>
        ///     <en>User nickname or short name; pages decide fallback display when empty.</en>
        ///   </l>
        /// </param>
        /// <param name="preferredEmail">
        ///   <l>
        ///     <zh-CN>首选联系邮箱；不是认证凭据。</zh-CN>
        ///     <en>Preferred contact email; this is not an authentication credential.</en>
        ///   </l>
        /// </param>
        /// <param name="status">
        ///   <l>
        ///     <zh-CN>用户资料状态稳定字符串。</zh-CN>
        ///     <en>Stable user-profile status string.</en>
        ///   </l>
        /// </param>
        /// <param name="statusReason">
        ///   <l>
        ///     <zh-CN>状态原因或备注；显示前需编码，且不应包含敏感值。</zh-CN>
        ///     <en>Status reason or note; encode before display and do not include secrets.</en>
        ///   </l>
        /// </param>
        /// <param name="isAvailable">
        ///   <l>
        ///     <zh-CN>当前资料是否可用于页面展示或后台选择。</zh-CN>
        ///     <en>Whether the profile is currently available for page display or administration selection.</en>
        ///   </l>
        /// </param>
        /// <param name="source">
        ///   <l>
        ///     <zh-CN>资料来源标识，用于区分旧表、扩展表或扩展同步来源。</zh-CN>
        ///     <en>Profile source marker used to distinguish legacy table, extension table, or extended synchronization sources.</en>
        ///   </l>
        /// </param>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>构造函数统一把可空文本归一化为空字符串，减少 Web Forms 控件绑定时的空值判断噪声。</zh-CN>
        ///     <en>The constructor normalizes nullable text to empty strings to reduce null-check noise in Web Forms control binding.</en>
        ///   </lang>
        /// </remarks>
        public UserProfileInfo(
            int userId,
            string legacyName,
            string loginName,
            string displayName,
            string nickname,
            string preferredEmail,
            string status,
            string statusReason,
            bool isAvailable,
            string source)
        {
            UserId = userId;
            LegacyName = legacyName ?? string.Empty;
            LoginName = loginName ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Nickname = nickname ?? string.Empty;
            PreferredEmail = preferredEmail ?? string.Empty;
            Status = status ?? string.Empty;
            StatusReason = statusReason ?? string.Empty;
            IsAvailable = isAvailable;
            Source = source ?? string.Empty;
        }

        /// <inheritdoc />
        public int UserId { get; private set; }

        /// <inheritdoc />
        public string LegacyName { get; private set; }

        /// <inheritdoc />
        public string LoginName { get; private set; }

        /// <inheritdoc />
        public string DisplayName { get; private set; }

        /// <inheritdoc />
        public string Nickname { get; private set; }

        /// <inheritdoc />
        public string PreferredEmail { get; private set; }

        /// <inheritdoc />
        public string Status { get; private set; }

        /// <inheritdoc />
        public string StatusReason { get; private set; }

        /// <inheritdoc />
        public bool IsAvailable { get; private set; }

        /// <inheritdoc />
        public string Source { get; private set; }
    }
}
