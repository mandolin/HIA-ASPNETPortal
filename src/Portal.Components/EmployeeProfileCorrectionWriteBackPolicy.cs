namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料更正回写的纯函数策略：判定字段是否可自动回写，并把建议值映射到员工保存请求。</zh-CN>
    ///   <en>Pure-function policy for employee-profile correction write-back: it decides whether a field can be applied automatically and maps the proposed value onto an employee save request.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本策略不访问数据库、不依赖会话或权限，只做字段名与值的判定与映射，因此可被单测完整覆盖。设计要点：① 字段名必须由服务端二次校验，不信任调用方传入值；② 只有能直接映射到 <see cref="EmployeeSaveRequest"/> 的低敏字段才允许自动回写；③ 需要另行解析且可能造成错误指派的字段必须显式标记为"需人工处理"，避免"审核通过但未生效"的静默失败。</zh-CN>
    ///   <en>This policy touches no database and depends on no session or permission; it only judges field names and maps values, so unit tests can cover it completely. Design points: (1) field names must be re-validated server-side and caller-supplied values are not trusted; (2) only low-sensitivity fields that map directly onto <see cref="EmployeeSaveRequest"/> may be applied automatically; (3) fields that need extra resolution and could cause a wrong assignment must be explicitly marked as requiring manual handling, so approval never silently fails to take effect.</en>
    /// </lang>
    /// </remarks>
    public static class EmployeeProfileCorrectionWriteBackPolicy
    {
        /// <summary><lang><zh-CN>可自动回写的字段：员工显示名。</zh-CN><en>Auto-appliable field: employee display name.</en></lang></summary>
        public const string FieldDisplayName = "DisplayName";

        /// <summary><lang><zh-CN>可自动回写的字段：员工偏好称呼。</zh-CN><en>Auto-appliable field: employee preferred name.</en></lang></summary>
        public const string FieldPreferredName = "PreferredName";

        /// <summary><lang><zh-CN>可自动回写的字段：工作邮箱。</zh-CN><en>Auto-appliable field: work email.</en></lang></summary>
        public const string FieldWorkEmail = "WorkEmail";

        /// <summary>
        /// <lang>
        ///   <zh-CN>已知但**不可**自动回写的字段：组织显示名。它需要映射为 <see cref="EmployeeSaveRequest.OrganizationUnitId"/>，按显示名反查组织存在重名歧义，自动回写可能造成错误指派，故需人工处理。</zh-CN>
        ///   <en>A known field that must **not** be applied automatically: the organization display name. It must map to <see cref="EmployeeSaveRequest.OrganizationUnitId"/>, and resolving an organization by display name is ambiguous when names repeat, so automatic write-back could assign the wrong unit and this field requires manual handling.</en>
        /// </lang>
        /// </summary>
        public const string FieldOrganizationDisplayName = "OrganizationDisplayName";

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断字段名是否为已知更正字段。</zh-CN>
        ///   <en>Determines whether a field name is a known correction field.</en>
        /// </lang>
        /// </summary>
        /// <param name="fieldName"><l><zh-CN>字段名；空白返回 <c>false</c>。</zh-CN><en>The field name; blank values return <c>false</c>.</en></l></param>
        /// <returns><l><zh-CN>已知字段为 <c>true</c>。</zh-CN><en><c>true</c> for a known field.</en></l></returns>
        public static bool IsKnownField(string fieldName)
        {
            return string.Equals(fieldName, FieldDisplayName, System.StringComparison.Ordinal) ||
                   string.Equals(fieldName, FieldPreferredName, System.StringComparison.Ordinal) ||
                   string.Equals(fieldName, FieldWorkEmail, System.StringComparison.Ordinal) ||
                   string.Equals(fieldName, FieldOrganizationDisplayName, System.StringComparison.Ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断字段是否允许自动回写（服务端白名单）。</zh-CN>
        ///   <en>Determines whether a field may be applied automatically (server-side allow list).</en>
        /// </lang>
        /// </summary>
        /// <param name="fieldName"><l><zh-CN>字段名。</zh-CN><en>The field name.</en></l></param>
        /// <returns><l><zh-CN>可自动回写为 <c>true</c>；未知字段或需人工处理字段为 <c>false</c>。</zh-CN><en><c>true</c> when it can be applied automatically; <c>false</c> for unknown or manually handled fields.</en></l></returns>
        public static bool CanAutoApply(string fieldName)
        {
            return string.Equals(fieldName, FieldDisplayName, System.StringComparison.Ordinal) ||
                   string.Equals(fieldName, FieldPreferredName, System.StringComparison.Ordinal) ||
                   string.Equals(fieldName, FieldWorkEmail, System.StringComparison.Ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断已知字段是否必须人工处理（不自动回写）。</zh-CN>
        ///   <en>Determines whether a known field must be handled manually (not applied automatically).</en>
        /// </lang>
        /// </summary>
        /// <param name="fieldName"><l><zh-CN>字段名。</zh-CN><en>The field name.</en></l></param>
        /// <returns><l><zh-CN>需人工处理为 <c>true</c>；可自动回写或未知字段为 <c>false</c>。</zh-CN><en><c>true</c> when manual handling is required; <c>false</c> for auto-appliable or unknown fields.</en></l></returns>
        public static bool RequiresManualHandling(string fieldName)
        {
            return IsKnownField(fieldName) && !CanAutoApply(fieldName);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把建议值写入员工保存请求的对应属性；未知字段、需人工处理字段或空建议值一律拒绝。</zh-CN>
        ///   <en>Writes the proposed value into the matching property of an employee save request; unknown fields, manually handled fields, and empty proposed values are all rejected.</en>
        /// </lang>
        /// </summary>
        /// <param name="request"><l><zh-CN>员工保存请求；为空引用时返回 <c>false</c>。</zh-CN><en>The employee save request; a null reference returns <c>false</c>.</en></l></param>
        /// <param name="fieldName"><l><zh-CN>字段名；必须落在自动回写白名单内。</zh-CN><en>The field name; it must fall inside the automatic write-back allow list.</en></l></param>
        /// <param name="proposedValue"><l><zh-CN>建议值；空白表示"无实际变更"，返回 <c>false</c>。</zh-CN><en>The proposed value; blank means "no real change" and returns <c>false</c>.</en></l></param>
        /// <returns><l><zh-CN>成功写入为 <c>true</c>；否则 <c>false</c>。</zh-CN><en><c>true</c> when the value was written; otherwise <c>false</c>.</en></l></returns>
        public static bool TryApply(EmployeeSaveRequest request, string fieldName, string proposedValue)
        {
            // <lang>
            //   <zh-CN>请求为空、字段不在白名单或建议值为空白时拒绝：空白建议值等价于"无变更"，写入它只会制造无意义的主数据更新。</zh-CN>
            //   <en>Reject a null request, a field outside the allow list, or a blank proposed value: a blank value means "no change" and writing it would only create a meaningless master-data update.</en>
            // </lang>
            if (request == null || !CanAutoApply(fieldName) || string.IsNullOrWhiteSpace(proposedValue))
            {
                return false;
            }

            string value = proposedValue.Trim();

            if (string.Equals(fieldName, FieldDisplayName, System.StringComparison.Ordinal))
            {
                request.DisplayName = value;
                return true;
            }

            if (string.Equals(fieldName, FieldPreferredName, System.StringComparison.Ordinal))
            {
                request.PreferredName = value;
                return true;
            }

            request.WorkEmail = value;
            return true;
        }
    }
}
