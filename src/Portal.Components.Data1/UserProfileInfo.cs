namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户用户资料扩展只读视图的默认实现。
    ///
    /// English: Default implementation of the Portal user-profile extension read-only view.
    /// </summary>
    public sealed class UserProfileInfo : IUserProfileInfo
    {
        /// <summary>
        /// 中文：创建用户资料扩展只读视图。
        ///
        /// English: Creates a user-profile extension read-only view.
        /// </summary>
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
