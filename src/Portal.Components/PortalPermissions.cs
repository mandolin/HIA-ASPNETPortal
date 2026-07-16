using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户细粒度权限的稳定键名集合。
    ///
    /// English: Stable key set for fine-grained Portal permissions.
    /// </summary>
    /// <remarks>
    /// 中文：P5.3 第一版只把权限定义放在代码和文档中；数据库只保存角色到这些键名的映射。
    /// 新增或重命名键名属于安全契约变更，必须同步迁移脚本、ADR 和回归清单。
    ///
    /// English: P5.3 keeps permission definitions in code and documentation; the database stores only role-to-key
    /// mappings. Adding or renaming keys is a security-contract change and must update migration scripts, ADRs, and
    /// regression checklists together.
    /// </remarks>
    public static class PortalPermissionKeys
    {
        /// <summary>中文：查看普通系统设置。English: View regular system settings.</summary>
        public const string SettingsView = "Settings.View";

        /// <summary>中文：编辑普通系统设置。English: Edit regular system settings.</summary>
        public const string SettingsEdit = "Settings.Edit";

        /// <summary>中文：查看敏感系统设置状态。English: View sensitive system-setting status.</summary>
        public const string SettingsSensitiveView = "Settings.SensitiveView";

        /// <summary>中文：查看系统健康状态。English: View system health status.</summary>
        public const string OpsHealthView = "Ops.Health.View";

        /// <summary>中文：查看诊断日志列表。English: View diagnostics log list.</summary>
        public const string OpsDiagnosticsView = "Ops.Diagnostics.View";

        /// <summary>中文：查看诊断日志详情。English: View diagnostics log details.</summary>
        public const string OpsDiagnosticsDetail = "Ops.Diagnostics.Detail";

        /// <summary>中文：查看运营审计。English: View operation audits.</summary>
        public const string AuditOperationView = "Audit.Operation.View";

        /// <summary>中文：查看用户后台。English: View user administration.</summary>
        public const string AdminUsersView = "Admin.Users.View";

        /// <summary>中文：编辑用户资料和注册审核。English: Edit user profile and registration-review state.</summary>
        public const string AdminUsersEdit = "Admin.Users.Edit";

        /// <summary>中文：重置用户密码。English: Reset user credentials.</summary>
        public const string AdminUsersResetPassword = "Admin.Users.ResetPassword";

        /// <summary>中文：编辑角色和角色成员关系。English: Edit roles and role memberships.</summary>
        public const string AdminRolesEdit = "Admin.Roles.Edit";

        /// <summary>中文：查看主题设置。English: View theme settings.</summary>
        public const string ThemeView = "Theme.View";

        /// <summary>中文：编辑主题设置和 Tab 覆盖。English: Edit theme settings and Tab overrides.</summary>
        public const string ThemeEdit = "Theme.Edit";

        /// <summary>中文：查看模块包目录。English: View the module package catalog.</summary>
        public const string ModuleCatalogView = "Module.Catalog.View";

        /// <summary>中文：启停模块包。English: Enable or disable module packages.</summary>
        public const string ModuleCatalogEdit = "Module.Catalog.Edit";

        /// <summary>中文：编辑模块定义。English: Edit module definitions.</summary>
        public const string ModuleDefinitionEdit = "Module.Definition.Edit";

        /// <summary>中文：编辑门户 Tab。English: Edit Portal Tabs.</summary>
        public const string PortalTabsEdit = "Portal.Tabs.Edit";

        /// <summary>中文：编辑门户模块实例和布局。English: Edit Portal module instances and layout.</summary>
        public const string PortalModulesEdit = "Portal.Modules.Edit";

        /// <summary>中文：编辑原始 HTML 内容。English: Edit raw HTML content.</summary>
        public const string ContentRawHtmlEdit = "Content.RawHtml.Edit";

        /// <summary>中文：管理上传内容策略。English: Manage uploaded-content policy.</summary>
        public const string ContentUploadManage = "Content.Upload.Manage";
    }

    /// <summary>
    /// 中文：权限定义元数据。
    ///
    /// English: Permission definition metadata.
    /// </summary>
    public sealed class PortalPermissionDefinition
    {
        /// <summary>
        /// 中文：创建权限定义。
        ///
        /// English: Creates a permission definition.
        /// </summary>
        /// <param name="key">中文：稳定权限键名。English: Stable permission key.</param>
        /// <param name="category">中文：权限分组。English: Permission category.</param>
        /// <param name="description">中文：面向维护者的权限说明。English: Maintainer-facing permission description.</param>
        public PortalPermissionDefinition(string key, string category, string description)
        {
            Key = key;
            Category = category;
            Description = description;
        }

        /// <summary>中文：稳定权限键名。English: Stable permission key.</summary>
        public string Key { get; private set; }

        /// <summary>中文：权限分组。English: Permission category.</summary>
        public string Category { get; private set; }

        /// <summary>中文：面向维护者的权限说明。English: Maintainer-facing permission description.</summary>
        public string Description { get; private set; }
    }

    /// <summary>
    /// 中文：门户权限定义注册表。
    ///
    /// English: Registry of Portal permission definitions.
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
        /// 中文：获取全部已定义权限。
        ///
        /// English: Gets every defined permission.
        /// </summary>
        public static IEnumerable<PortalPermissionDefinition> Definitions
        {
            get { return DefinitionArray; }
        }

        /// <summary>
        /// 中文：判断权限键名是否已在注册表中定义。
        ///
        /// English: Determines whether a permission key is defined in the registry.
        /// </summary>
        /// <param name="key">中文：待检查权限键名。English: Permission key to check.</param>
        /// <returns>中文：已定义时为 <c>true</c>。English: <c>true</c> when defined.</returns>
        public static bool IsDefined(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && KnownKeys.Contains(key.Trim());
        }

        /// <summary>
        /// 中文：规范化并校验权限键名。
        ///
        /// English: Normalizes and validates a permission key.
        /// </summary>
        /// <param name="key">中文：待规范化权限键名。English: Permission key to normalize.</param>
        /// <returns>中文：规范化权限键名。English: Normalized permission key.</returns>
        /// <exception cref="ArgumentException">中文：权限键名不存在于注册表时引发。English: Thrown when the key is not registered.</exception>
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
        /// 中文：规范化权限键集合，并拒绝未定义键名。
        ///
        /// English: Normalizes a permission-key collection and rejects undefined keys.
        /// </summary>
        /// <param name="keys">中文：待规范化权限键集合。English: Permission keys to normalize.</param>
        /// <returns>中文：去空白、去重后的权限键数组。English: Trimmed and deduplicated permission-key array.</returns>
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
