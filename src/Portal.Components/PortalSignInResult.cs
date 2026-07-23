namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>表示一次门户登录校验的结果。</zh-CN>
    ///   <en>Represents the result of one Portal sign-in validation.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此对象只携带会话签发和审计需要的非敏感状态；不得加入密码、摘要、盐、哈希或 Cookie 文本。</zh-CN>
    ///   <en>This object carries only non-sensitive state needed for issuing a session and writing audits; passwords, digests, salts, hashes, or cookie text must never be added to it.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalSignInResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建登录校验结果。</zh-CN>
        ///   <en>Creates a sign-in validation result.</en>
        /// </lang>
        /// </summary>
        /// <param name="succeeded">
        /// <l>
        ///   <zh-CN>登录校验是否成功。</zh-CN>
        ///   <en>Whether sign-in validation succeeded.</en>
        /// </l>
        /// </param>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>成功登录的用户标识；失败时为 <c>0</c>。</zh-CN>
        ///   <en>Identifier of the signed-in user; <c>0</c> on failure.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>成功登录的用户名称；失败时为空。</zh-CN>
        ///   <en>User name on success; empty on failure.</en>
        /// </l>
        /// </param>
        /// <param name="securityVersion">
        /// <l>
        ///   <zh-CN>用户当前安全版本。</zh-CN>
        ///   <en>Current user security version.</en>
        /// </l>
        /// </param>
        /// <param name="upgradedLegacyCredential">
        /// <l>
        ///   <zh-CN>本次登录是否把旧 MD5 凭据升级为强哈希。</zh-CN>
        ///   <en>Whether this sign-in upgraded a legacy MD5 credential to a strong hash.</en>
        /// </l>
        /// </param>
        /// <param name="requiresReset">
        /// <l>
        ///   <zh-CN>用户凭据是否要求先重置。</zh-CN>
        ///   <en>Whether the user credential requires a reset before sign-in.</en>
        /// </l>
        /// </param>
        public PortalSignInResult(
            bool succeeded,
            int userId,
            string userName,
            long securityVersion,
            bool upgradedLegacyCredential,
            bool requiresReset)
        {
            Succeeded = succeeded;
            UserId = userId;
            UserName = userName ?? string.Empty;
            SecurityVersion = securityVersion;
            UpgradedLegacyCredential = upgradedLegacyCredential;
            RequiresReset = requiresReset;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>登录校验是否成功。</zh-CN>
        ///   <en>Whether sign-in validation succeeded.</en>
        /// </lang>
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>成功登录的用户标识；失败时为 <c>0</c>。</zh-CN>
        ///   <en>Identifier of the signed-in user; <c>0</c> on failure.</en>
        /// </lang>
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>成功登录的用户名称；失败时为空。</zh-CN>
        ///   <en>User name on success; empty on failure.</en>
        /// </lang>
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户当前安全版本，会写入身份票据和角色 Cookie。</zh-CN>
        ///   <en>Current user security version, written to both the authentication ticket and the role cookie.</en>
        /// </lang>
        /// </summary>
        public long SecurityVersion { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>本次登录是否把旧 MD5 凭据升级为强哈希。</zh-CN>
        ///   <en>Whether this sign-in upgraded a legacy MD5 credential to a strong hash.</en>
        /// </lang>
        /// </summary>
        public bool UpgradedLegacyCredential { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户凭据是否要求先重置；为 <c>true</c> 时不得签发登录票据。</zh-CN>
        ///   <en>Whether the user credential requires a reset first; when <c>true</c>, no sign-in ticket may be issued.</en>
        /// </lang>
        /// </summary>
        public bool RequiresReset { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建一个通用失败结果，不暴露账号是否存在或密码状态。</zh-CN>
        ///   <en>Creates a generic failure result without exposing whether the account exists or its password state.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>失败结果。</zh-CN>
        ///   <en>Failure result.</en>
        /// </l>
        /// </returns>
        public static PortalSignInResult Failed()
        {
            return new PortalSignInResult(false, 0, string.Empty, 0, false, false);
        }
    }
}
