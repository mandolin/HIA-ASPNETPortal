namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>密码策略校验的拒绝原因。策略层只返回该原因码，面向用户的文案由表现层按当前语言映射；复杂度类提示所需的长度与类别数由表现层从 <see cref="PortalPasswordPolicy.GetEffectiveOptions"/> 取得后拼装。</zh-CN>
    ///   <en>Rejection reason for password-policy validation. The policy layer returns only this code, and the presentation layer maps it to user-facing text for the current language; the length and category counts needed by the complexity prompt are assembled by the presentation layer from <see cref="PortalPasswordPolicy.GetEffectiveOptions"/>.</en>
    /// </lang>
    /// </summary>
    public enum PortalPasswordRejection
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>未拒绝：密码满足当前策略。</zh-CN>
        ///   <en>Not rejected: the password satisfies the current policy.</en>
        /// </lang>
        /// </summary>
        None = 0,

        /// <summary>
        /// <lang>
        ///   <zh-CN>长度不足或字符类别数不足。</zh-CN>
        ///   <en>The length is too short or too few character categories are covered.</en>
        /// </lang>
        /// </summary>
        BelowComplexityPolicy,

        /// <summary>
        /// <lang>
        ///   <zh-CN>密码过于常见或容易猜测，命中弱口令字典。</zh-CN>
        ///   <en>The password is too common or easy to guess and matches the weak-password dictionary.</en>
        /// </lang>
        /// </summary>
        WeakDictionaryMatch,

        /// <summary>
        /// <lang>
        ///   <zh-CN>密码包含用户名、邮箱、员工号、显示名等账号相关信息。</zh-CN>
        ///   <en>The password contains account-related context such as the user name, email, employee code, or display name.</en>
        /// </lang>
        /// </summary>
        ContainsAccountContextTerm,
    }
}
