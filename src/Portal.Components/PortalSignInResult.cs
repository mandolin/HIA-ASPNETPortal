namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：表示一次门户登录校验的结果。
    ///
    /// English: Represents the result of one Portal sign-in validation.
    /// </summary>
    /// <remarks>
    /// 中文：此对象只携带会话签发和审计需要的非敏感状态；不得加入密码、摘要、盐、哈希或 Cookie 文本。
    ///
    /// English: This object carries only non-sensitive state needed for issuing a session and writing audits; passwords,
    /// digests, salts, hashes, or cookie text must never be added to it.
    /// </remarks>
    public sealed class PortalSignInResult
    {
        /// <summary>
        /// 中文：创建登录校验结果。
        ///
        /// English: Creates a sign-in validation result.
        /// </summary>
        /// <param name="succeeded">中文：登录校验是否成功。English: Whether sign-in validation succeeded.</param>
        /// <param name="userId">中文：成功登录的用户标识；失败时为 <c>0</c>。English: Identifier of the signed-in user; <c>0</c> on failure.</param>
        /// <param name="userName">中文：成功登录的用户名称；失败时为空。English: User name on success; empty on failure.</param>
        /// <param name="securityVersion">中文：用户当前安全版本。English: Current user security version.</param>
        /// <param name="upgradedLegacyCredential">中文：本次登录是否把旧 MD5 凭据升级为强哈希。English: Whether this sign-in upgraded a legacy MD5 credential to a strong hash.</param>
        /// <param name="requiresReset">中文：用户凭据是否要求先重置。English: Whether the user credential requires a reset before sign-in.</param>
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
        /// 中文：登录校验是否成功。
        ///
        /// English: Whether sign-in validation succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// 中文：成功登录的用户标识；失败时为 <c>0</c>。
        ///
        /// English: Identifier of the signed-in user; <c>0</c> on failure.
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// 中文：成功登录的用户名称；失败时为空。
        ///
        /// English: User name on success; empty on failure.
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// 中文：用户当前安全版本，会写入身份票据和角色 Cookie。
        ///
        /// English: Current user security version, written to both the authentication ticket and the role cookie.
        /// </summary>
        public long SecurityVersion { get; private set; }

        /// <summary>
        /// 中文：本次登录是否把旧 MD5 凭据升级为强哈希。
        ///
        /// English: Whether this sign-in upgraded a legacy MD5 credential to a strong hash.
        /// </summary>
        public bool UpgradedLegacyCredential { get; private set; }

        /// <summary>
        /// 中文：用户凭据是否要求先重置；为 <c>true</c> 时不得签发登录票据。
        ///
        /// English: Whether the user credential requires a reset first; when <c>true</c>, no sign-in ticket may be issued.
        /// </summary>
        public bool RequiresReset { get; private set; }

        /// <summary>
        /// 中文：创建一个通用失败结果，不暴露账号是否存在或密码状态。
        ///
        /// English: Creates a generic failure result without exposing whether the account exists or its password state.
        /// </summary>
        /// <returns>中文：失败结果。English: Failure result.</returns>
        public static PortalSignInResult Failed()
        {
            return new PortalSignInResult(false, 0, string.Empty, 0, false, false);
        }
    }
}
