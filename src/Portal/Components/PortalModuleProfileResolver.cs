using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>解析当前部署环境允许的模块 Profile 与模块包集合。</zh-CN>
    ///   <en>Resolves the module Profile and package set allowed by the current deployment environment.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P20.3 的解析器只读取 Web.config / 环境 JSON appSettings，不读取数据库设置表，也不提供在线启停能力。它表达“当前部署允许哪些能力进入门户”，模块状态表随后再表达“允许范围内是否启用”。</zh-CN>
    ///   <en>The P20.3 resolver reads only Web.config / environment JSON appSettings. It does not read the database setting table or provide online switching. It represents which capabilities this deployment allows into the Portal; the module state table then represents enablement inside that allowed scope.</en>
    /// </lang>
    /// </remarks>
    public static class PortalModuleProfileResolver
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>未进入当前 Profile 时写入诊断原因的稳定前缀。</zh-CN>
        ///   <en>Stable reason prefix used when a package is not allowed by the current Profile.</en>
        /// </lang>
        /// </summary>
        public const string NotAllowedReasonPrefix = "ModuleProfile.NotAllowed:";

        private const string CoreOnlyProfile = "CoreOnly";
        private const string ActiveProfileKey = "Portal.ModuleProfiles.Active";
        private const string EnabledPackagesKey = "Portal.ModulePackages.Enabled";
        private const string ProfilePackagesSuffix = ".Packages";
        private const string ProfileIncludesSuffix = ".Includes";
        private const string ProfileKeyPrefix = "Portal.ModuleProfiles.";

        private static readonly Regex StableNamePattern = new Regex(
            @"^[A-Za-z][A-Za-z0-9_.-]{0,99}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly IDictionary<string, string> DefaultProfilePackages =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "EnterpriseBase", "HIA.EmployeeProfileConfirm,HIA.EmployeeProfileCorrectionRequest" },
                { "BusinessWorkflow", "HIA.BusinessApplicationRequest" },
                { "LegacyContent", "Legacy.Announcements,Legacy.Contacts,Legacy.Discussion,Legacy.Document,Legacy.Events,Legacy.HtmlModule,Legacy.ImageModule,Legacy.Links,Legacy.QuickLinks,Legacy.XmlModule" },
                { "DevProbe", "HIA.ModuleProbe" }
            };

        private static readonly IDictionary<string, string> DefaultProfileIncludes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "BusinessWorkflow", "EnterpriseBase" }
            };

        private static readonly IDictionary<string, string> LegacyPackagesBySource =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "DesktopModules/Announcements.ascx", "Legacy.Announcements" },
                { "DesktopModules/Contacts.ascx", "Legacy.Contacts" },
                { "DesktopModules/Discussion.ascx", "Legacy.Discussion" },
                { "DesktopModules/Document.ascx", "Legacy.Document" },
                { "DesktopModules/Events.ascx", "Legacy.Events" },
                { "DesktopModules/HtmlModule.ascx", "Legacy.HtmlModule" },
                { "DesktopModules/ImageModule.ascx", "Legacy.ImageModule" },
                { "DesktopModules/Links.ascx", "Legacy.Links" },
                { "DesktopModules/QuickLinks.ascx", "Legacy.QuickLinks" },
                { "DesktopModules/XmlModule.ascx", "Legacy.XmlModule" }
            };

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析当前 Profile 快照。</zh-CN>
        ///   <en>Resolves the current Profile snapshot.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l zh-CN="当前 HTTP 上下文；本阶段仅保留参数用于后续诊断扩展。" en="Current HTTP context; kept in this stage for later diagnostic extension." />
        /// </param>
        /// <returns>
        /// <l zh-CN="不可变的 Profile 快照。" en="Immutable Profile snapshot." />
        /// </returns>
        public static PortalModuleProfileSnapshot Resolve(HttpContext context = null)
        {
            var invalidEntries = new List<string>();
            string configuredActiveProfile = ConfigurationManager.AppSettings[ActiveProfileKey];
            string activeProfile = NormalizeStableName(configuredActiveProfile, CoreOnlyProfile, invalidEntries, "active profile");

            var allowedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddProfileAndIncludes(
                activeProfile,
                allowedPackages,
                invalidEntries,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            AddPackageList(ConfigurationManager.AppSettings[EnabledPackagesKey], allowedPackages, invalidEntries, EnabledPackagesKey);

            return new PortalModuleProfileSnapshot(
                activeProfile,
                allowedPackages.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList(),
                invalidEntries);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断一个已规范化桌面入口是否属于 Core 能力。</zh-CN>
        ///   <en>Determines whether a normalized desktop entry belongs to Core capability.</en>
        /// </lang>
        /// </summary>
        /// <param name="normalizedDesktopSource">
        /// <l zh-CN="已去掉 ~/ 的桌面控件路径。" en="Desktop control path without ~/." />
        /// </param>
        /// <returns>
        /// <l zh-CN="属于 Core 时为 true。" en="True when the entry belongs to Core." />
        /// </returns>
        public static bool IsCoreDesktopSource(string normalizedDesktopSource)
        {
            string source = NormalizeSource(normalizedDesktopSource);
            return string.Equals(source, "DesktopModules/SignIn.ascx", StringComparison.OrdinalIgnoreCase) ||
                   source.StartsWith("Admin/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>尝试将旧模块入口映射为内置虚拟 package id。</zh-CN>
        ///   <en>Attempts to map a legacy module entry to a built-in virtual package id.</en>
        /// </lang>
        /// </summary>
        /// <param name="normalizedDesktopSource">
        /// <l zh-CN="已规范化桌面入口。" en="Normalized desktop entry." />
        /// </param>
        /// <param name="packageId">
        /// <l zh-CN="成功时返回虚拟 package id。" en="Virtual package id when successful." />
        /// </param>
        /// <returns>
        /// <l zh-CN="找到映射时为 true。" en="True when a mapping is found." />
        /// </returns>
        public static bool TryGetLegacyPackageId(string normalizedDesktopSource, out string packageId)
        {
            return LegacyPackagesBySource.TryGetValue(NormalizeSource(normalizedDesktopSource), out packageId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取 Legacy 虚拟包映射的只读快照。</zh-CN>
        ///   <en>Gets a read-only snapshot of the Legacy virtual package mappings.</en>
        /// </lang>
        /// </summary>
        public static IDictionary<string, string> GetLegacyPackageMappings()
        {
            return new Dictionary<string, string>(LegacyPackagesBySource, StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> ReadProfileIncludes(string profileName, IList<string> invalidEntries)
        {
            string key = ProfileKeyPrefix + profileName + ProfileIncludesSuffix;
            string configured = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(configured) && DefaultProfileIncludes.ContainsKey(profileName))
            {
                configured = DefaultProfileIncludes[profileName];
            }

            var includes = new List<string>();
            foreach (string include in SplitCsv(configured))
            {
                string normalized = NormalizeStableName(include, string.Empty, invalidEntries, key);
                if (!string.IsNullOrEmpty(normalized) &&
                    !string.Equals(normalized, profileName, StringComparison.OrdinalIgnoreCase))
                {
                    includes.Add(normalized);
                }
            }

            return includes;
        }

        private static void AddProfileAndIncludes(
            string profileName,
            ISet<string> allowedPackages,
            IList<string> invalidEntries,
            ISet<string> visitedProfiles,
            ISet<string> expandingProfiles)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                return;
            }

            if (visitedProfiles.Contains(profileName))
            {
                return;
            }

            if (!expandingProfiles.Add(profileName))
            {
                invalidEntries.Add("profile include cycle=" + profileName);
                return;
            }

            AddProfilePackages(profileName, allowedPackages, invalidEntries);
            foreach (string includedProfile in ReadProfileIncludes(profileName, invalidEntries))
            {
                AddProfileAndIncludes(includedProfile, allowedPackages, invalidEntries, visitedProfiles, expandingProfiles);
            }

            expandingProfiles.Remove(profileName);
            visitedProfiles.Add(profileName);
        }

        private static void AddProfilePackages(
            string profileName,
            ISet<string> allowedPackages,
            IList<string> invalidEntries)
        {
            string key = ProfileKeyPrefix + profileName + ProfilePackagesSuffix;
            string configured = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(configured) && DefaultProfilePackages.ContainsKey(profileName))
            {
                configured = DefaultProfilePackages[profileName];
            }

            AddPackageList(configured, allowedPackages, invalidEntries, key);
        }

        private static void AddPackageList(
            string configured,
            ISet<string> allowedPackages,
            IList<string> invalidEntries,
            string sourceKey)
        {
            foreach (string packageId in SplitCsv(configured))
            {
                string normalized = NormalizeStableName(packageId, string.Empty, invalidEntries, sourceKey);
                if (!string.IsNullOrEmpty(normalized))
                {
                    allowedPackages.Add(normalized);
                }
            }
        }

        private static string NormalizeStableName(
            string candidate,
            string fallback,
            IList<string> invalidEntries,
            string sourceName)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return fallback;
            }

            string trimmed = candidate.Trim();
            if (StableNamePattern.IsMatch(trimmed))
            {
                return trimmed;
            }

            invalidEntries.Add(sourceName + "=" + trimmed);
            return fallback;
        }

        private static IEnumerable<string> SplitCsv(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new string[0];
            }

            return value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0);
        }

        private static string NormalizeSource(string source)
        {
            return (source ?? string.Empty).Trim().TrimStart('~', '/').Replace('\\', '/');
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>一次模块 Profile 解析的不可变快照。</zh-CN>
    ///   <en>Immutable snapshot for one module Profile resolution.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalModuleProfileSnapshot
    {
        private readonly HashSet<string> allowedPackageSet;

        internal PortalModuleProfileSnapshot(
            string activeProfile,
            IList<string> allowedPackageIds,
            IList<string> invalidEntries)
        {
            ActiveProfile = activeProfile ?? string.Empty;
            AllowedPackageIds = new List<string>(allowedPackageIds ?? new List<string>()).AsReadOnly();
            InvalidEntries = new List<string>(invalidEntries ?? new List<string>()).AsReadOnly();
            allowedPackageSet = new HashSet<string>(AllowedPackageIds, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前活动 Profile 名称。</zh-CN>
        ///   <en>Current active Profile name.</en>
        /// </lang>
        /// </summary>
        public string ActiveProfile { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前 Profile 允许的 package id 列表。</zh-CN>
        ///   <en>Package id list allowed by the current Profile.</en>
        /// </lang>
        /// </summary>
        public IList<string> AllowedPackageIds { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析时发现的非法 Profile、include 或 package id 条目。</zh-CN>
        ///   <en>Invalid Profile, include, or package id entries found during resolution.</en>
        /// </lang>
        /// </summary>
        public IList<string> InvalidEntries { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断 package id 是否允许进入当前部署 Profile。</zh-CN>
        ///   <en>Determines whether a package id is allowed by the current deployment Profile.</en>
        /// </lang>
        /// </summary>
        /// <param name="packageId">
        /// <l zh-CN="待判断 package id。" en="Package id to check." />
        /// </param>
        /// <returns>
        /// <l zh-CN="允许时为 true。" en="True when allowed." />
        /// </returns>
        public bool IsPackageAllowed(string packageId)
        {
            return !string.IsNullOrWhiteSpace(packageId) && allowedPackageSet.Contains(packageId.Trim());
        }
    }
}
