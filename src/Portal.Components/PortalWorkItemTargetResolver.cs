using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>一次待办目标解析的结果：要么给出承载业务对象的页签，要么给出降级原因码。</zh-CN>
    ///   <en>Outcome of one work-item target resolution: either the tab that hosts the business object, or a degradation reason code.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>原因码是**非敏感运维事实**（用于审计统计与排障），不含物理路径、连接串或异常细节，因此可以进入审计正文。调用方据 <see cref="IsResolved"/> 决定呈现链接还是降级提示。</zh-CN>
    ///   <en>A reason code is a non-sensitive operational fact (for audit counting and diagnosis) and carries no physical path, connection string, or exception detail, so it may enter audit text. Callers use <see cref="IsResolved"/> to choose between rendering a link and rendering the degraded hint.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalWorkItemTargetResolution
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建一次解析结果。</zh-CN>
        ///   <en>Creates one resolution outcome.</en>
        /// </lang>
        /// </summary>
        /// <param name="tab">
        /// <l>
        ///   <zh-CN>已通过角色检查的承载页签；未解析时为 <c>null</c>。</zh-CN>
        ///   <en>The hosting tab that passed the role check, or <c>null</c> when unresolved.</en>
        /// </l>
        /// </param>
        /// <param name="reasonCode">
        /// <l>
        ///   <zh-CN>未解析时的原因码；已解析时为空串。</zh-CN>
        ///   <en>The reason code when unresolved, or an empty string when resolved.</en>
        /// </l>
        /// </param>
        internal PortalWorkItemTargetResolution(ITabItem tab, string reasonCode)
        {
            Tab = tab;
            ReasonCode = reasonCode ?? string.Empty;
        }

        /// <summary><lang><zh-CN>承载业务对象的页签；未解析时为 <c>null</c>。</zh-CN><en>The tab hosting the business object, or <c>null</c> when unresolved.</en></lang></summary>
        public ITabItem Tab { get; private set; }

        /// <summary><lang><zh-CN>未解析原因码；已解析时为空串。取值集合见 <see cref="PortalWorkItemTargetResolver"/> 的常量。</zh-CN><en>The unresolved reason code, or an empty string when resolved; the value set is defined by the constants on <see cref="PortalWorkItemTargetResolver"/>.</en></lang></summary>
        public string ReasonCode { get; private set; }

        /// <summary><lang><zh-CN>是否解析到可导航页签。为 <c>false</c> 时调用方必须降级呈现，不得凭业务类型猜测地址。</zh-CN><en>Whether a navigable tab was resolved. When <c>false</c> the caller must render the degraded form and must not guess a URL from the business kind.</en></lang></summary>
        public bool IsResolved
        {
            get { return Tab != null; }
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>待办目标解析所需的最小数据端口：只暴露反查链路真正用到的六个操作。</zh-CN>
    ///   <en>Minimal data port required by work-item target resolution: it exposes only the six operations the reverse lookup actually uses.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>既有数据门面（模块定义、模块实例、页签）成员众多，直接依赖它们会迫使调用方与测试实现大量无关成员。本端口按接口隔离原则只声明反查所需的最小面，使解析逻辑可在不接触 HTTP 上下文、数据库与文件系统的前提下被完整验证；实现方仍复用既有门面，不新增数据访问。</zh-CN>
    ///   <en>The existing facades (module definitions, module instances, tabs) carry many members, so depending on them directly would force callers and tests to implement a large amount of unrelated surface. Following interface segregation, this port declares only the minimum the reverse lookup needs, letting the resolution logic be fully verified without touching HTTP context, database, or file system; implementations still reuse the existing facades and add no data access.</en>
    /// </lang>
    /// </remarks>
    public interface IPortalModuleTargetSource
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>按模块包标识读取其已验证的桌面用户控件入口。</zh-CN>
        ///   <en>Reads the validated desktop user-control entry of a module package by package identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="packageId"><l><zh-CN>模块包标识，形如 <c>HIA.EnterpriseCapabilityWorkbench</c>。</zh-CN><en>The module package identifier, such as <c>HIA.EnterpriseCapabilityWorkbench</c>.</en></l></param>
        /// <returns><l><zh-CN>桌面入口虚拟路径；包不存在或未声明时为空串。</zh-CN><en>The desktop entry virtual path, or an empty string when the package is absent or declares none.</en></l></returns>
        string GetDesktopEntry(string packageId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前缓存中的全部模块定义。</zh-CN>
        ///   <en>Reads all module definitions from the current cache.</en>
        /// </lang>
        /// </summary>
        /// <returns><l><zh-CN>模块定义集合，可能为空但不应为 <c>null</c>。</zh-CN><en>The module-definition collection, which may be empty but should not be <c>null</c>.</en></l></returns>
        IEnumerable<IModuleDefinitionItem> GetModuleDefinitions();

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取引用指定模块定义的模块实例标识。</zh-CN>
        ///   <en>Reads the module-instance identifiers that reference the specified module definition.</en>
        /// </lang>
        /// </summary>
        /// <param name="definitionId"><l><zh-CN>模块定义标识。</zh-CN><en>The module-definition identifier.</en></l></param>
        /// <returns><l><zh-CN>模块实例标识集合，可能为空。</zh-CN><en>The module-instance identifier collection, which may be empty.</en></l></returns>
        IEnumerable<int> GetModuleIdsByDefinitionId(int definitionId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识查找模块实例。</zh-CN>
        ///   <en>Finds a module instance by identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId"><l><zh-CN>模块实例标识。</zh-CN><en>The module-instance identifier.</en></l></param>
        /// <returns><l><zh-CN>匹配的模块实例；不存在时为 <c>null</c>。</zh-CN><en>The matching module instance, or <c>null</c> when absent.</en></l></returns>
        IModuleItem FindModuleById(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识查找页签。</zh-CN>
        ///   <en>Finds a tab by identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId"><l><zh-CN>页签标识。</zh-CN><en>The tab identifier.</en></l></param>
        /// <returns><l><zh-CN>匹配的页签；不存在时为 <c>null</c>。</zh-CN><en>The matching tab, or <c>null</c> when absent.</en></l></returns>
        ITabItem FindTabById(int tabId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求是否满足给定的旧门户分号角色串。</zh-CN>
        ///   <en>Determines whether the current request satisfies the given legacy semicolon-delimited role string.</en>
        /// </lang>
        /// </summary>
        /// <param name="accessRoles"><l><zh-CN>页签的访问角色串。</zh-CN><en>The tab's access-role string.</en></l></param>
        /// <returns><l><zh-CN>满足时为 <c>true</c>；无请求上下文时调用方应返回 <c>false</c>（fail-closed）。</zh-CN><en><c>true</c> when satisfied; with no request context the implementation should return <c>false</c> (fail-closed).</en></l></returns>
        bool IsInRoles(string accessRoles);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>把待办的业务类型反查为普通用户在**当次请求**下有权限访问的承载页签。</zh-CN>
    ///   <en>Resolves a work item's business kind into the hosting tab that the ordinary user may access in the current request.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>反查链：业务类型 → 能力键 → 主责模块包 → 已验证桌面入口 → 模块定义 → 模块实例 → 承载页签 → **角色检查**。链路每一环都可能是"部署未挂载""注册缺失"或"当前用户无权"，因此每一环都返回独立原因码而不是抛错或猜测地址；只有全链通过才产出可导航目标。**不扩权**：解析只读取既有注册与角色事实，不授予任何权限，也不改写模块或页签配置。</zh-CN>
    ///   <en>Reverse lookup chain: business kind to capability key to primary module package to validated desktop entry to module definition to module instance to hosting tab to a role check. Every link may fail because the deployment has not mounted the module, the registration is missing, or the current user lacks access, so each link returns its own reason code instead of throwing or guessing a URL; only a fully passing chain yields a navigable target. No privilege is widened: resolution reads existing registration and role facts, grants nothing, and rewrites no module or tab configuration.</en>
    /// </lang>
    /// </remarks>
    public static class PortalWorkItemTargetResolver
    {
        /// <summary><lang><zh-CN>原因码：业务类型不在已知词表内，按降级处理且不臆造地址。</zh-CN><en>Reason code: the business kind is not in the known vocabulary, so it degrades and no URL is invented.</en></lang></summary>
        public const string ReasonUnknownBusinessKind = "UnknownBusinessKind";

        /// <summary><lang><zh-CN>原因码：能力键在权威词表中不存在，说明注册与词表已经不一致。</zh-CN><en>Reason code: the capability key does not exist in the authority registry, meaning registration and vocabulary have drifted apart.</en></lang></summary>
        public const string ReasonUnknownCapability = "UnknownCapability";

        /// <summary><lang><zh-CN>原因码：能力定义没有主责模块包，无法继续反查。</zh-CN><en>Reason code: the capability definition declares no primary module package, so the lookup cannot continue.</en></lang></summary>
        public const string ReasonNoPrimaryModule = "NoPrimaryModule";

        /// <summary><lang><zh-CN>原因码：主责包没有已验证的桌面入口（未部署该包或清单未声明）。</zh-CN><en>Reason code: the primary package has no validated desktop entry because the package is not deployed or its manifest declares none.</en></lang></summary>
        public const string ReasonNoDesktopEntry = "NoDesktopEntry";

        /// <summary><lang><zh-CN>原因码：没有模块定义引用该桌面入口（部署包尚未注册定义）。</zh-CN><en>Reason code: no module definition references that desktop entry because the deployed package has not had its definition registered.</en></lang></summary>
        public const string ReasonNoModuleDefinition = "NoModuleDefinition";

        /// <summary><lang><zh-CN>原因码：定义存在但没有归属页签的模块实例（定义未挂载到任何页面）。</zh-CN><en>Reason code: the definition exists but has no module instance owning a tab, meaning it is mounted on no page.</en></lang></summary>
        public const string ReasonNoModuleInstance = "NoModuleInstance";

        /// <summary><lang><zh-CN>原因码：实例存在，但其承载页签缺失或当前用户无权访问该页签。两者归为同一原因码，因为**对用户而言结论相同**（不可导航），区别只体现在运维排查顺序上。</zh-CN><en>Reason code: instances exist, yet their hosting tab is missing or the current user may not access it. Both cases share one code because the conclusion is identical for the user (not navigable) and they differ only in the order an operator investigates.</en></lang></summary>
        public const string ReasonNoAccessibleTab = "NoAccessibleTab";

        /// <summary><lang><zh-CN>原因码：解析所需的数据端口缺失，属调用方编程错误；此时一律降级，不尝试绕过。</zh-CN><en>Reason code: the data port required for resolution is missing, which is a caller programming error; resolution degrades rather than attempting a workaround.</en></lang></summary>
        public const string ReasonSourceUnavailable = "SourceUnavailable";

        /// <summary><lang><zh-CN>原因码：读取注册事实时发生意外失败（数据访问或部署清单异常）。由调用方在捕获后归类，使"读不出注册事实"与"确实未注册"在审计里可区分。</zh-CN><en>Reason code: an unexpected failure occurred while reading registration facts (a data-access or deployment-manifest exception). The caller classifies it after catching so the audit can distinguish "registration facts unreadable" from "genuinely not registered".</en></lang></summary>
        public const string ReasonResolutionFailed = "ResolutionFailed";

        /// <summary>
        /// <lang>
        ///   <zh-CN>把业务类型键映射为能力键；未知类型返回空串。</zh-CN>
        ///   <en>Maps a business-kind key to a capability key, returning an empty string for unknown kinds.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此处只做"分类归属"这一步：能力到模块包、模块包到桌面入口分别由权威能力词表与已验证部署清单提供，因此本方法**不复制任何路径字面量**，部署包换目录时无需改这里。</zh-CN>
        ///   <en>This step only performs classification: capability to module package and module package to desktop entry come from the authority capability registry and the validated deployment manifests respectively, so this method **duplicates no path literal** and needs no change when a package moves.</en>
        /// </lang>
        /// </remarks>
        /// <param name="businessKind"><l><zh-CN>稳定的业务对象类型键，取自 <see cref="PortalWorkItemBusinessKinds"/>。</zh-CN><en>The stable business-object kind key from <see cref="PortalWorkItemBusinessKinds"/>.</en></l></param>
        /// <returns><l><zh-CN>能力键；未知或空白类型为空串。</zh-CN><en>The capability key, or an empty string for an unknown or blank kind.</en></l></returns>
        public static string GetCapabilityId(string businessKind)
        {
            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.CollaborationItem, StringComparison.Ordinal))
            {
                return PortalCapabilityRegistry.Collaboration;
            }

            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.BusinessApplication, StringComparison.Ordinal))
            {
                return PortalCapabilityRegistry.ApplicationRequest;
            }

            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.EmployeeProfileCorrectionRequest, StringComparison.Ordinal))
            {
                return PortalCapabilityRegistry.EmployeeProfileCorrectionRequest;
            }

            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.LeaveRequest, StringComparison.Ordinal))
            {
                return PortalCapabilityRegistry.LeaveRequest;
            }

            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.ExpenseReimbursement, StringComparison.Ordinal))
            {
                return PortalCapabilityRegistry.ExpenseReimbursement;
            }

            return string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析待办业务对象的前台可达页签。</zh-CN>
        ///   <en>Resolves the front-end reachable tab of the business object behind a work item.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>多实例取舍（P77.2 裁定 `D1`）：在**通过角色检查**的承载页签中取 <c>TabOrder</c> 最小者；<c>TabOrder</c> 为空视为最大（排在有值者之后），并列时保留枚举先后，使结果可复现。**先过滤权限再排序**是刻意的：若先按顺序取第一个，会把"顺序最靠前但无权访问"的页签当成结论，进而把用户送到 403。</zh-CN>
        ///   <en>Multi-instance selection (decision D1 from P77.2): among hosting tabs that **pass the role check**, take the smallest <c>TabOrder</c>; a null <c>TabOrder</c> counts as largest (sorted after those with a value), and ties keep enumeration order so the result is reproducible. Filtering before ordering is deliberate: taking the first by order would treat a low-order tab the user cannot access as the answer and send the user to a 403.</en>
        /// </lang>
        /// </remarks>
        /// <param name="businessKind"><l><zh-CN>待办的业务对象类型键。</zh-CN><en>The work item's business-object kind key.</en></l></param>
        /// <param name="source"><l><zh-CN>反查数据端口；为 <c>null</c> 时直接降级。</zh-CN><en>The reverse-lookup data port; a <c>null</c> value degrades immediately.</en></l></param>
        /// <returns><l><zh-CN>解析结果，区分"已解析"与各降级原因。</zh-CN><en>The resolution outcome, distinguishing success from each degradation reason.</en></l></returns>
        public static PortalWorkItemTargetResolution Resolve(string businessKind, IPortalModuleTargetSource source)
        {
            // <lang>
            //   <zh-CN>端口缺失属调用方错误：降级而不是抛错，避免一个装配问题把整块待办变成错误页。</zh-CN>
            //   <en>A missing port is a caller error: degrade instead of throwing so one wiring problem does not turn the whole to-do block into an error page.</en>
            // </lang>
            if (source == null)
            {
                return new PortalWorkItemTargetResolution(null, ReasonSourceUnavailable);
            }

            string capabilityId = GetCapabilityId(businessKind);
            if (string.IsNullOrEmpty(capabilityId))
            {
                return new PortalWorkItemTargetResolution(null, ReasonUnknownBusinessKind);
            }

            PortalCapabilityDefinition capability;
            if (!PortalCapabilityRegistry.TryGet(capabilityId, out capability) || capability == null)
            {
                return new PortalWorkItemTargetResolution(null, ReasonUnknownCapability);
            }

            if (string.IsNullOrEmpty(capability.PrimaryModuleId))
            {
                return new PortalWorkItemTargetResolution(null, ReasonNoPrimaryModule);
            }

            string desktopEntry = source.GetDesktopEntry(capability.PrimaryModuleId);
            if (string.IsNullOrEmpty(desktopEntry))
            {
                return new PortalWorkItemTargetResolution(null, ReasonNoDesktopEntry);
            }

            // <lang>
            //   <zh-CN>与模块目录页同一匹配口径（不区分大小写的已验证入口精确匹配），避免两处对"是否已注册"得出不同结论。</zh-CN>
            //   <en>Same matching rule as the module-catalog page (case-insensitive exact match on the validated entry) so the two places cannot disagree about whether the package is registered.</en>
            // </lang>
            IEnumerable<IModuleDefinitionItem> definitions = source.GetModuleDefinitions() ??
                Enumerable.Empty<IModuleDefinitionItem>();
            IModuleDefinitionItem definition = definitions.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.DesktopSourceFile, desktopEntry, StringComparison.OrdinalIgnoreCase));

            if (definition == null)
            {
                return new PortalWorkItemTargetResolution(null, ReasonNoModuleDefinition);
            }

            IEnumerable<int> moduleIds = source.GetModuleIdsByDefinitionId(definition.ModuleDefId) ??
                Enumerable.Empty<int>();

            ITabItem accessibleTab = null;
            int bestTabOrder = int.MaxValue;
            bool anyInstanceOwningTab = false;

            foreach (int moduleId in moduleIds)
            {
                IModuleItem module = source.FindModuleById(moduleId);
                if (module == null || !module.TabId.HasValue)
                {
                    continue;
                }

                anyInstanceOwningTab = true;

                ITabItem tab = source.FindTabById(module.TabId.Value);
                if (tab == null || !source.IsInRoles(tab.AccessRoles))
                {
                    continue;
                }

                int tabOrder = tab.TabOrder.HasValue ? tab.TabOrder.Value : int.MaxValue;
                if (accessibleTab == null || tabOrder < bestTabOrder)
                {
                    accessibleTab = tab;
                    bestTabOrder = tabOrder;
                }
            }

            if (accessibleTab == null)
            {
                return new PortalWorkItemTargetResolution(
                    null,
                    anyInstanceOwningTab ? ReasonNoAccessibleTab : ReasonNoModuleInstance);
            }

            return new PortalWorkItemTargetResolution(accessibleTab, string.Empty);
        }
    }
}
