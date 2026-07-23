using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户细粒度权限的稳定键名集合。</zh-CN>
    ///   <en>Stable key set for fine-grained Portal permissions.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P5.3 第一版只把权限定义放在代码和文档中；数据库只保存角色到这些键名的映射。 新增或重命名键名属于安全契约变更，必须同步迁移脚本、ADR 和回归清单。</zh-CN>
    ///   <en>P5.3 keeps permission definitions in code and documentation; the database stores only role-to-key mappings. Adding or renaming keys is a security-contract change and must update migration scripts, ADRs, and regression checklists together.</en>
    /// </lang>
    /// </remarks>
    public static class PortalPermissionKeys
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>查看普通系统设置。</zh-CN>
        ///   <en>View regular system settings.</en>
        /// </lang>
        /// </summary>
        public const string SettingsView = "Settings.View";

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑普通系统设置。</zh-CN>
        ///   <en>Edit regular system settings.</en>
        /// </lang>
        /// </summary>
        public const string SettingsEdit = "Settings.Edit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看敏感系统设置状态。</zh-CN>
        ///   <en>View sensitive system-setting status.</en>
        /// </lang>
        /// </summary>
        public const string SettingsSensitiveView = "Settings.SensitiveView";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看系统健康状态。</zh-CN>
        ///   <en>View system health status.</en>
        /// </lang>
        /// </summary>
        public const string OpsHealthView = "Ops.Health.View";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看诊断日志列表。</zh-CN>
        ///   <en>View diagnostics log list.</en>
        /// </lang>
        /// </summary>
        public const string OpsDiagnosticsView = "Ops.Diagnostics.View";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看诊断日志详情。</zh-CN>
        ///   <en>View diagnostics log details.</en>
        /// </lang>
        /// </summary>
        public const string OpsDiagnosticsDetail = "Ops.Diagnostics.Detail";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看运营审计。</zh-CN>
        ///   <en>View operation audits.</en>
        /// </lang>
        /// </summary>
        public const string AuditOperationView = "Audit.Operation.View";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看用户后台。</zh-CN>
        ///   <en>View user administration.</en>
        /// </lang>
        /// </summary>
        public const string AdminUsersView = "Admin.Users.View";

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑用户资料和注册审核。</zh-CN>
        ///   <en>Edit user profile and registration-review state.</en>
        /// </lang>
        /// </summary>
        public const string AdminUsersEdit = "Admin.Users.Edit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>重置用户密码。</zh-CN>
        ///   <en>Reset user credentials.</en>
        /// </lang>
        /// </summary>
        public const string AdminUsersResetPassword = "Admin.Users.ResetPassword";

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑角色和角色成员关系。</zh-CN>
        ///   <en>Edit roles and role memberships.</en>
        /// </lang>
        /// </summary>
        public const string AdminRolesEdit = "Admin.Roles.Edit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看员工、组织和账号绑定目录。</zh-CN>
        ///   <en>View employee, organization, and user-binding directories.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeDirectoryView = "EmployeeDirectory.View";

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑员工与组织主数据。</zh-CN>
        ///   <en>Edit employee and organization master data.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeDirectoryEdit = "EmployeeDirectory.Edit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定或解绑门户账号与员工。</zh-CN>
        ///   <en>Bind or unbind Portal users and employees.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeDirectoryBind = "EmployeeDirectory.Bind";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看员工资料确认模块。</zh-CN>
        ///   <en>View the employee-profile confirmation module.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileConfirmView = "EmployeeProfileConfirm.View";

        /// <summary>
        /// <lang>
        ///   <zh-CN>确认自己的员工资料。</zh-CN>
        ///   <en>Confirm one's own employee profile.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileConfirmConfirm = "EmployeeProfileConfirm.Confirm";

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理员工资料确认记录。</zh-CN>
        ///   <en>Administer employee-profile confirmation records.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileConfirmAdmin = "EmployeeProfileConfirm.Admin";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看员工资料更正请求模块。</zh-CN>
        ///   <en>View the employee-profile correction request module.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileCorrectionRequestView = "EmployeeProfileCorrectionRequest.View";

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交自己的员工资料更正请求。</zh-CN>
        ///   <en>Submit one's own employee-profile correction request.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileCorrectionRequestSubmit = "EmployeeProfileCorrectionRequest.Submit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核员工资料更正请求。</zh-CN>
        ///   <en>Review employee-profile correction requests.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileCorrectionRequestReview = "EmployeeProfileCorrectionRequest.Review";

        /// <summary>
        /// <lang>
        ///   <zh-CN>取消或关闭员工资料更正请求。</zh-CN>
        ///   <en>Cancel or close employee-profile correction requests.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileCorrectionRequestCancel = "EmployeeProfileCorrectionRequest.Cancel";

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理员工资料更正请求的旧聚合权限。</zh-CN>
        ///   <en>Legacy aggregate permission for employee-profile correction administration.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileCorrectionRequestAdmin = "EmployeeProfileCorrectionRequest.Admin";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看业务待办。</zh-CN>
        ///   <en>View business work items.</en>
        /// </lang>
        /// </summary>
        public const string BusinessWorkItemsView = "Business.WorkItems.View";

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理业务待办。</zh-CN>
        ///   <en>Handle business work items.</en>
        /// </lang>
        /// </summary>
        public const string BusinessWorkItemsHandle = "Business.WorkItems.Handle";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看和处理业务待办的旧聚合权限。</zh-CN>
        ///   <en>Legacy aggregate permission for viewing and handling business work items.</en>
        /// </lang>
        /// </summary>
        public const string BusinessWorkItemsAdmin = "Business.WorkItems.Admin";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看主题设置。</zh-CN>
        ///   <en>View theme settings.</en>
        /// </lang>
        /// </summary>
        public const string ThemeView = "Theme.View";

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑主题设置和 Tab 覆盖。</zh-CN>
        ///   <en>Edit theme settings and Tab overrides.</en>
        /// </lang>
        /// </summary>
        public const string ThemeEdit = "Theme.Edit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>查看模块包目录。</zh-CN>
        ///   <en>View the module package catalog.</en>
        /// </lang>
        /// </summary>
        public const string ModuleCatalogView = "Module.Catalog.View";

        /// <summary>
        /// <lang>
        ///   <zh-CN>启停模块包。</zh-CN>
        ///   <en>Enable or disable module packages.</en>
        /// </lang>
        /// </summary>
        public const string ModuleCatalogEdit = "Module.Catalog.Edit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑模块定义。</zh-CN>
        ///   <en>Edit module definitions.</en>
        /// </lang>
        /// </summary>
        public const string ModuleDefinitionEdit = "Module.Definition.Edit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑门户 Tab。</zh-CN>
        ///   <en>Edit Portal Tabs.</en>
        /// </lang>
        /// </summary>
        public const string PortalTabsEdit = "Portal.Tabs.Edit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑门户模块实例和布局。</zh-CN>
        ///   <en>Edit Portal module instances and layout.</en>
        /// </lang>
        /// </summary>
        public const string PortalModulesEdit = "Portal.Modules.Edit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑原始 HTML 内容。</zh-CN>
        ///   <en>Edit raw HTML content.</en>
        /// </lang>
        /// </summary>
        public const string ContentRawHtmlEdit = "Content.RawHtml.Edit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理上传内容策略。</zh-CN>
        ///   <en>Manage uploaded-content policy.</en>
        /// </lang>
        /// </summary>
        public const string ContentUploadManage = "Content.Upload.Manage";
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>权限定义元数据。</zh-CN>
    ///   <en>Permission definition metadata.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalPermissionDefinition
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建权限定义。</zh-CN>
        ///   <en>Creates a permission definition.</en>
        /// </lang>
        /// </summary>
        /// <param name="key">
        /// <l>
        ///   <zh-CN>稳定权限键名。</zh-CN>
        ///   <en>Stable permission key.</en>
        /// </l>
        /// </param>
        /// <param name="category">
        /// <l>
        ///   <zh-CN>权限分组。</zh-CN>
        ///   <en>Permission category.</en>
        /// </l>
        /// </param>
        /// <param name="description">
        /// <l>
        ///   <zh-CN>面向维护者的权限说明。</zh-CN>
        ///   <en>Maintainer-facing permission description.</en>
        /// </l>
        /// </param>
        public PortalPermissionDefinition(string key, string category, string description)
        {
            Key = key;
            Category = category;
            Description = description;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>稳定权限键名。</zh-CN>
        ///   <en>Stable permission key.</en>
        /// </lang>
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>权限分组。</zh-CN>
        ///   <en>Permission category.</en>
        /// </lang>
        /// </summary>
        public string Category { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>面向维护者的权限说明。</zh-CN>
        ///   <en>Maintainer-facing permission description.</en>
        /// </lang>
        /// </summary>
        public string Description { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>门户权限定义注册表。</zh-CN>
    ///   <en>Registry of Portal permission definitions.</en>
    /// </lang>
    /// </summary>
    public static class PortalPermissionRegistry
    {
        private static readonly PortalPermissionDefinition[] DefinitionArray =
        {
            new PortalPermissionDefinition(PortalPermissionKeys.SettingsView, "Settings", "查看普通系统设置。"),
            new PortalPermissionDefinition(PortalPermissionKeys.SettingsEdit, "Settings", "编辑普通系统设置。"),
            new PortalPermissionDefinition(PortalPermissionKeys.SettingsSensitiveView, "Settings", "查看敏感系统设置状态。"),
            new PortalPermissionDefinition(PortalPermissionKeys.OpsHealthView, "Operations", "查看系统健康状态。"),
            new PortalPermissionDefinition(PortalPermissionKeys.OpsDiagnosticsView, "Operations", "查看诊断日志列表。"),
            new PortalPermissionDefinition(PortalPermissionKeys.OpsDiagnosticsDetail, "Operations", "查看诊断日志详情。"),
            new PortalPermissionDefinition(PortalPermissionKeys.AuditOperationView, "Audit", "查看运营审计。"),
            new PortalPermissionDefinition(PortalPermissionKeys.AdminUsersView, "Administration", "查看用户后台。"),
            new PortalPermissionDefinition(PortalPermissionKeys.AdminUsersEdit, "Administration", "编辑用户资料和注册审核。"),
            new PortalPermissionDefinition(PortalPermissionKeys.AdminUsersResetPassword, "Administration", "重置用户密码。"),
            new PortalPermissionDefinition(PortalPermissionKeys.AdminRolesEdit, "Administration", "编辑角色和角色成员关系。"),
            new PortalPermissionDefinition(PortalPermissionKeys.EmployeeDirectoryView, "EnterpriseDirectory", "查看员工、组织和账号绑定目录。"),
            new PortalPermissionDefinition(PortalPermissionKeys.EmployeeDirectoryEdit, "EnterpriseDirectory", "编辑员工与组织主数据。"),
            new PortalPermissionDefinition(PortalPermissionKeys.EmployeeDirectoryBind, "EnterpriseDirectory", "绑定或解绑门户账号与员工。"),
            new PortalPermissionDefinition(PortalPermissionKeys.EmployeeProfileConfirmView, "Business.EmployeeProfileConfirm", "查看员工资料确认模块。"),
            new PortalPermissionDefinition(PortalPermissionKeys.EmployeeProfileConfirmConfirm, "Business.EmployeeProfileConfirm", "确认自己的员工资料。"),
            new PortalPermissionDefinition(PortalPermissionKeys.EmployeeProfileConfirmAdmin, "Business.EmployeeProfileConfirm", "管理员查看和处理员工资料确认记录。"),
            new PortalPermissionDefinition(PortalPermissionKeys.EmployeeProfileCorrectionRequestView, "Business.EmployeeProfileCorrectionRequest", "查看员工资料更正请求模块。"),
            new PortalPermissionDefinition(PortalPermissionKeys.EmployeeProfileCorrectionRequestSubmit, "Business.EmployeeProfileCorrectionRequest", "提交自己的员工资料更正请求。"),
            new PortalPermissionDefinition(PortalPermissionKeys.EmployeeProfileCorrectionRequestReview, "Business.EmployeeProfileCorrectionRequest", "审核员工资料更正请求。"),
            new PortalPermissionDefinition(PortalPermissionKeys.EmployeeProfileCorrectionRequestCancel, "Business.EmployeeProfileCorrectionRequest", "取消或关闭员工资料更正请求。"),
            new PortalPermissionDefinition(PortalPermissionKeys.EmployeeProfileCorrectionRequestAdmin, "Business.EmployeeProfileCorrectionRequest", "员工资料更正请求旧聚合管理权限。"),
            new PortalPermissionDefinition(PortalPermissionKeys.BusinessWorkItemsView, "Business.WorkItems", "查看业务待办。"),
            new PortalPermissionDefinition(PortalPermissionKeys.BusinessWorkItemsHandle, "Business.WorkItems", "处理业务待办。"),
            new PortalPermissionDefinition(PortalPermissionKeys.BusinessWorkItemsAdmin, "Business.WorkItems", "业务待办旧聚合管理权限。"),
            new PortalPermissionDefinition(PortalPermissionKeys.ThemeView, "Theme", "查看主题设置。"),
            new PortalPermissionDefinition(PortalPermissionKeys.ThemeEdit, "Theme", "编辑主题设置和 Tab 覆盖。"),
            new PortalPermissionDefinition(PortalPermissionKeys.ModuleCatalogView, "Module", "查看模块包目录。"),
            new PortalPermissionDefinition(PortalPermissionKeys.ModuleCatalogEdit, "Module", "启停模块包。"),
            new PortalPermissionDefinition(PortalPermissionKeys.ModuleDefinitionEdit, "Module", "编辑模块定义。"),
            new PortalPermissionDefinition(PortalPermissionKeys.PortalTabsEdit, "PortalStructure", "编辑门户 Tab。"),
            new PortalPermissionDefinition(PortalPermissionKeys.PortalModulesEdit, "PortalStructure", "编辑门户模块实例和布局。"),
            new PortalPermissionDefinition(PortalPermissionKeys.ContentRawHtmlEdit, "Content", "编辑原始 HTML 内容。"),
            new PortalPermissionDefinition(PortalPermissionKeys.ContentUploadManage, "Content", "管理上传内容策略。")
        };

        private static readonly HashSet<string> KnownKeys = new HashSet<string>(
            DefinitionArray.Select(definition => definition.Key),
            StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取全部已定义权限。</zh-CN>
        ///   <en>Gets every defined permission.</en>
        /// </lang>
        /// </summary>
        public static IEnumerable<PortalPermissionDefinition> Definitions
        {
            get { return DefinitionArray; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断权限键名是否已在注册表中定义。</zh-CN>
        ///   <en>Determines whether a permission key is defined in the registry.</en>
        /// </lang>
        /// </summary>
        /// <param name="key">
        /// <l>
        ///   <zh-CN>待检查权限键名。</zh-CN>
        ///   <en>Permission key to check.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已定义时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when defined.</en>
        /// </l>
        /// </returns>
        public static bool IsDefined(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && KnownKeys.Contains(key.Trim());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>规范化并校验权限键名。</zh-CN>
        ///   <en>Normalizes and validates a permission key.</en>
        /// </lang>
        /// </summary>
        /// <param name="key">
        /// <l>
        ///   <zh-CN>待规范化权限键名。</zh-CN>
        ///   <en>Permission key to normalize.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>规范化权限键名。</zh-CN>
        ///   <en>Normalized permission key.</en>
        /// </l>
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <l>
        ///   <zh-CN>权限键名不存在于注册表时引发。</zh-CN>
        ///   <en>Thrown when the key is not registered.</en>
        /// </l>
        /// </exception>
        public static string NormalizeDefinedKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Permission key is required.", "key");
            }

            string normalized = key.Trim();
            if (!KnownKeys.Contains(normalized))
            {
                throw new ArgumentException("Permission key is not defined: " + normalized, "key");
            }

            return DefinitionArray
                .First(definition => string.Equals(definition.Key, normalized, StringComparison.OrdinalIgnoreCase))
                .Key;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>规范化权限键集合，并拒绝未定义键名。</zh-CN>
        ///   <en>Normalizes a permission-key collection and rejects undefined keys.</en>
        /// </lang>
        /// </summary>
        /// <param name="keys">
        /// <l>
        ///   <zh-CN>待规范化权限键集合。</zh-CN>
        ///   <en>Permission keys to normalize.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>去空白、去重后的权限键数组。</zh-CN>
        ///   <en>Trimmed and deduplicated permission-key array.</en>
        /// </l>
        /// </returns>
        public static string[] NormalizeDefinedKeys(IEnumerable<string> keys)
        {
            if (keys == null)
            {
                return new string[0];
            }

            return keys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(NormalizeDefinedKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
