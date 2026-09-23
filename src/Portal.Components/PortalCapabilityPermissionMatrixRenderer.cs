using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>把权限定义与角色映射渲染为"能力层 → 权限分类 → 权限键"的只读矩阵（P45.5 Step4）。</zh-CN>
    ///   <en>Renders permission definitions and role mappings into a read-only matrix grouped by capability layer, permission category, and permission key (P45.5 Step4).</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本渲染器是**纯字符串生成**（不读 HttpContext、数据库或配置），文案通过 <c>localize</c> 委托由调用方注入，因此可被 Portal.Tests 覆盖。它**只输出语义类**，不内联颜色或尺寸；矩阵使用原生 table 结构，可在 IE9+ 与六套主题下按主题皮肤呈现。**只读**：不生成任何可写入控件。</zh-CN>
    ///   <en>This renderer is pure string generation (it reads no HttpContext, database, or configuration), and its copy is injected through the <c>localize</c> delegate so Portal.Tests can cover it. It emits semantic classes only and never inlines colors or sizes; the matrix uses a native table structure so IE9+ and all six themes can skin it. It is read-only and produces no writable control.</en>
    /// </lang>
    /// </remarks>
    public static class PortalCapabilityPermissionMatrixRenderer
    {
        // <lang>
        //   <zh-CN>能力层顺序固定为"基础 → 基础业务 → 平台"，与 P19.2 能力分层的推进顺序一致；候选层（BusinessCapability / Professional / Industry）当前无键，不会出现在矩阵中。</zh-CN>
        //   <en>Capability layers are ordered Foundation, Basic Business, then Platform, matching the progression in the P19.2 layering; the candidate layers (BusinessCapability / Professional / Industry) have no keys yet and therefore never appear.</en>
        // </lang>
        private static readonly string[] LayerOrder = { "Foundation", "BasicBusiness", "Platform" };

        // <lang>
        //   <zh-CN>权限分类到能力层的受控映射：矩阵按层分组的数据来源。**新增权限分类时必须同步本表**，否则该分类会落入默认层；`PortalCapabilityPermissionMatrixTests` 有契约测试守护"注册表中每个分类都已登记层归属"。</zh-CN>
        //   <en>Controlled mapping from permission category to capability layer, which is the data source for grouping the matrix. **A new permission category must be added here together with the definition**, otherwise it falls into the default layer; a contract test in `PortalCapabilityPermissionMatrixTests` guards that every registered category declares its layer.</en>
        // </lang>
        private static readonly Dictionary<string, string> CategoryLayerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // <lang><zh-CN>Foundation Core：门户运行、登录、治理与恢复的基础能力。</zh-CN><en>Foundation Core: portal runtime, sign-in, governance, and recovery foundations.</en></lang>
            { "Settings", "Foundation" },
            { "Operations", "Foundation" },
            { "Audit", "Foundation" },
            { "Administration", "Foundation" },

            // <lang><zh-CN>Enterprise Basic Business：人员与任职、参考数据、项目与工作协同。</zh-CN><en>Enterprise Basic Business: people and assignment, reference data, project and work collaboration.</en></lang>
            { "EnterpriseDirectory", "BasicBusiness" },
            { "Business.EmployeeProfileConfirm", "BasicBusiness" },
            { "Business.EmployeeProfileCorrectionRequest", "BasicBusiness" },
            { "Business.WorkItems", "BasicBusiness" },
            { "Business.Application", "BasicBusiness" },
            { "Business.Workflow", "BasicBusiness" },
            { "Business.Collaboration", "BasicBusiness" },

            // <lang><zh-CN>Platform Capability：本 Portal 自身的导航、主题、模块装配与内容治理。</zh-CN><en>Platform Capability: this portal's own navigation, theme, module assembly, and content governance.</en></lang>
            { "Theme", "Platform" },
            { "Module", "Platform" },
            { "PortalStructure", "Platform" },
            { "Content", "Platform" },

            // <lang><zh-CN>P45.5 分层键族自身：跨层 View 归 Platform（门户治理视图），其余按层后缀。</zh-CN><en>The P45.5 layered family itself: the cross-layer View belongs to Platform (a portal governance view) and the rest follow their layer suffix.</en></lang>
            { "EnterpriseCapability", "Platform" },
            { "EnterpriseCapability.Foundation", "Foundation" },
            { "EnterpriseCapability.BasicBusiness", "BasicBusiness" },
            { "EnterpriseCapability.Platform", "Platform" }
        };

        // <lang>
        //   <zh-CN>分类无登记层归属时使用的默认层；选择 Platform 是因为未登记分类多为门户呈现类，且该默认值会被契约测试发现（测试要求全部已知分类显式登记，不走默认值）。</zh-CN>
        //   <en>Default layer used when a category declares no layer; Platform is chosen because unregistered categories are usually presentation-related, and the contract test still surfaces the omission because it requires every known category to be registered explicitly.</en>
        // </lang>
        private const string DefaultLayer = "Platform";

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析权限分类所属的能力层。</zh-CN>
        ///   <en>Resolves the capability layer a permission category belongs to.</en>
        /// </lang>
        /// </summary>
        /// <param name="category">
        /// <l>
        ///   <zh-CN>权限定义中的分类值。</zh-CN>
        ///   <en>Category value from a permission definition.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>能力层标识；未登记时返回默认层。</zh-CN>
        ///   <en>Capability layer identifier, or the default layer when unregistered.</en>
        /// </l>
        /// </returns>
        public static string ResolveCapabilityLayer(string category)
        {
            string mapped;
            if (!string.IsNullOrWhiteSpace(category) && CategoryLayerMap.TryGetValue(category.Trim(), out mapped))
            {
                return mapped;
            }

            return DefaultLayer;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断权限分类是否已在受控映射中登记层归属。</zh-CN>
        ///   <en>Determines whether a permission category already declares its capability layer in the controlled mapping.</en>
        /// </lang>
        /// </summary>
        /// <param name="category">
        /// <l>
        ///   <zh-CN>权限定义中的分类值。</zh-CN>
        ///   <en>Category value from a permission definition.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已登记时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when registered.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>本方法供契约测试使用：它区分"显式登记"与"落入默认层"两种情形，使"新增权限分类忘记登记层归属"能被测试当场发现，而不是被默认值静默掩盖。</zh-CN>
        ///   <en>This method exists for the contract test: it distinguishes an explicit registration from a fallback to the default layer, so a newly added category that forgets to declare its layer is caught by the test instead of being silently masked by the default.</en>
        /// </lang>
        /// </remarks>
        public static bool IsCategoryLayerRegistered(string category)
        {
            return !string.IsNullOrWhiteSpace(category) && CategoryLayerMap.ContainsKey(category.Trim());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>渲染只读能力权限矩阵。</zh-CN>
        ///   <en>Renders the read-only capability permission matrix.</en>
        /// </lang>
        /// </summary>
        /// <param name="definitions">
        /// <l>
        ///   <zh-CN>权限定义（键与分类的唯一权威来源）；为空时返回空态提示。</zh-CN>
        ///   <en>Permission definitions, the single authority for keys and categories; blank input yields the empty-state hint.</en>
        /// </l>
        /// </param>
        /// <param name="grants">
        /// <l>
        ///   <zh-CN>角色到权限键的映射投影（来自只读查询）；为空表示无映射，仍渲染表头与未授权态。</zh-CN>
        ///   <en>Projection of role-to-key mappings from the read-only query; blank means no mapping while the header and un-granted cells are still rendered.</en>
        /// </l>
        /// </param>
        /// <param name="localize">
        /// <l>
        ///   <zh-CN>文案解析委托（由页面注入 lang.* 资源键）；为 null 时使用内建英文兜底。</zh-CN>
        ///   <en>Copy resolver injected by the page from lang.* resource keys; when null an internal English fallback is used.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>矩阵 HTML；无权限定义时返回单段空态提示。</zh-CN>
        ///   <en>Matrix HTML, or a single empty-state hint when no permission definition exists.</en>
        /// </l>
        /// </returns>
        public static string Render(
            IEnumerable<PortalPermissionDefinition> definitions,
            IEnumerable<RolePermissionEntry> grants,
            Func<string, string> localize)
        {
            // <lang>
            //   <zh-CN>统一文案入口：调用方未提供委托时退回内建英文兜底，保证渲染器独立可用且不抛异常。</zh-CN>
            //   <en>Single copy entry point: when the caller supplies no delegate, fall back to built-in English so the renderer stays independently usable and never throws.</en>
            // </lang>
            Func<string, string> text = localize ?? (key => key);

            // <lang>
            //   <zh-CN>定义集合是矩阵的唯一权威来源；无定义时输出空态而不是空表头。</zh-CN>
            //   <en>The definition collection is the sole authority for the matrix; without definitions, emit the empty state instead of an empty header.</en>
            // </lang>
            List<PortalPermissionDefinition> definitionList = definitions == null
                ? new List<PortalPermissionDefinition>()
                : definitions.Where(definition => definition != null).ToList();

            if (definitionList.Count == 0)
            {
                return "<p class=\"Normal portal-matrix-empty\">" + HttpUtility.HtmlEncode(text("EmptyDefinitions")) + "</p>";
            }

            // <lang>
            //   <zh-CN>映射集合按"角色名 + 权限键"建立查找表；同一组合出现多行时以后者为准（数据库主键已保证唯一，此处仅防御性处理）。</zh-CN>
            //   <en>Build a lookup keyed by role name plus permission key; when a pair repeats, the later row wins (the database primary key already guarantees uniqueness, so this is defensive only).</en>
            // </lang>
            Dictionary<string, RolePermissionEntry> grantLookup = new Dictionary<string, RolePermissionEntry>(StringComparer.OrdinalIgnoreCase);
            List<RolePermissionEntry> grantList = grants == null
                ? new List<RolePermissionEntry>()
                : grants.Where(grant => grant != null && !string.IsNullOrWhiteSpace(grant.RoleName) && !string.IsNullOrWhiteSpace(grant.PermissionKey)).ToList();

            foreach (RolePermissionEntry grant in grantList)
            {
                grantLookup[BuildCellKey(grant.RoleName, grant.PermissionKey)] = grant;
            }

            // <lang>
            //   <zh-CN>角色列取自映射中实际出现的角色名并按名称排序，保证同一数据每次呈现顺序一致；无映射时只有权限键列。**已知边界**：完全没有任何映射的角色不会出现在矩阵中（它没有任何权限事实可展示）；若将来需要"把零映射角色也列为空列"，应扩展只读数据源同时返回角色表全量角色，而不是在此推测角色清单。</zh-CN>
            //   <en>Role columns are the role names actually present in the mappings, ordered by name so identical data renders identically; with no mappings only the permission-key column is shown. **Known boundary**: a role with no mapping at all never appears, because it carries no permission fact to display. If zero-mapping roles must later appear as empty columns, extend the read-only data source to return every role from the role table instead of guessing role names here.</en>
            // </lang>
            List<string> roleColumns = grantList
                .Select(grant => grant.RoleName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // <lang>
            //   <zh-CN>表头先输出权限键列标题，再逐个输出角色列标题。</zh-CN>
            //   <en>The header emits the permission-key column title first, then one title per role column.</en>
            // </lang>
            StringBuilder builder = new StringBuilder();
            builder.Append("<table class=\"portal-capability-matrix\">");
            builder.Append("<thead><tr>");
            builder.Append("<th scope=\"col\">").Append(HttpUtility.HtmlEncode(text("ColumnPermissionKey"))).Append("</th>");

            foreach (string roleName in roleColumns)
            {
                builder.Append("<th scope=\"col\">").Append(HttpUtility.HtmlEncode(roleName)).Append("</th>");
            }

            builder.Append("</tr></thead><tbody>");

            // <lang>
            //   <zh-CN>按固定层顺序逐个渲染：层内按分类分组、分类内按键名排序，形成"层 → 分类 → 键"三级结构。</zh-CN>
            //   <en>Render layer by layer in the fixed order, grouping by category inside a layer and ordering by key inside a category, which yields the layer to category to key structure.</en>
            // </lang>
            foreach (string layer in LayerOrder)
            {
                List<PortalPermissionDefinition> layerDefinitions = definitionList
                    .Where(definition => string.Equals(ResolveCapabilityLayer(definition.Category), layer, StringComparison.Ordinal))
                    .ToList();

                if (layerDefinitions.Count == 0)
                {
                    continue;
                }

                builder.Append("<tr class=\"portal-capability-matrix-layer\"><th scope=\"colgroup\" colspan=\"")
                    .Append((roleColumns.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .Append("\">")
                    .Append(HttpUtility.HtmlEncode(text("Layer" + layer)))
                    .Append("</th></tr>");

                foreach (IGrouping<string, PortalPermissionDefinition> categoryGroup in layerDefinitions
                    .GroupBy(definition => definition.Category ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
                {
                    builder.Append("<tr class=\"portal-capability-matrix-category\"><th scope=\"colgroup\" colspan=\"")
                        .Append((roleColumns.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture))
                        .Append("\">")
                        .Append(HttpUtility.HtmlEncode(categoryGroup.Key))
                        .Append("</th></tr>");

                    foreach (PortalPermissionDefinition definition in categoryGroup.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        builder.Append("<tr class=\"portal-capability-matrix-row\"><td>")
                            .Append(HttpUtility.HtmlEncode(definition.Key))
                            .Append("</td>");

                        foreach (string roleName in roleColumns)
                        {
                            builder.Append(RenderCell(grantLookup, roleName, definition.Key, text));
                        }

                        builder.Append("</tr>");
                    }
                }
            }

            builder.Append("</tbody></table>");
            return builder.ToString();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>渲染单个单元格：已授权 / 未授权 / 已禁用三态。</zh-CN>
        ///   <en>Renders a single cell in one of three states: granted, not granted, or disabled.</en>
        /// </lang>
        /// </summary>
        /// <param name="grantLookup">
        /// <l>
        ///   <zh-CN>角色名与权限键到映射的查找表。</zh-CN>
        ///   <en>Lookup from role name and permission key to the mapping.</en>
        /// </l>
        /// </param>
        /// <param name="roleName">
        /// <l>
        ///   <zh-CN>当前角色列名。</zh-CN>
        ///   <en>Current role column name.</en>
        /// </l>
        /// </param>
        /// <param name="permissionKey">
        /// <l>
        ///   <zh-CN>当前权限键。</zh-CN>
        ///   <en>Current permission key.</en>
        /// </l>
        /// </param>
        /// <param name="text">
        /// <l>
        ///   <zh-CN>文案解析委托。</zh-CN>
        ///   <en>Copy resolver.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>单元格 HTML。</zh-CN>
        ///   <en>Cell HTML.</en>
        /// </l>
        /// </returns>
        private static string RenderCell(
            Dictionary<string, RolePermissionEntry> grantLookup,
            string roleName,
            string permissionKey,
            Func<string, string> text)
        {
            // <lang>
            //   <zh-CN>未找到映射：输出"未授权"态，与"已禁用"必须视觉可区分。</zh-CN>
            //   <en>No mapping found: emit the not-granted state, which must be visually distinguishable from disabled.</en>
            // </lang>
            RolePermissionEntry grant;
            if (!grantLookup.TryGetValue(BuildCellKey(roleName, permissionKey), out grant))
            {
                return "<td class=\"portal-capability-matrix-cell portal-capability-matrix-none\" title=\"" +
                    HttpUtility.HtmlAttributeEncode(text("StateNotGranted")) + "\">" +
                    HttpUtility.HtmlEncode(text("MarkNotGranted")) + "</td>";
            }

            // <lang>
            //   <zh-CN>映射存在且生效：输出"已授权"态。</zh-CN>
            //   <en>Mapping exists and is effective: emit the granted state.</en>
            // </lang>
            if (grant.IsEnabled)
            {
                return "<td class=\"portal-capability-matrix-cell portal-capability-matrix-granted\" title=\"" +
                    HttpUtility.HtmlAttributeEncode(text("StateGranted")) + "\">" +
                    HttpUtility.HtmlEncode(text("MarkGranted")) + "</td>";
            }

            // <lang>
            //   <zh-CN>映射存在但被禁用：输出"已禁用"态并带 aria-disabled，避免被读作"未授权"或可点击。</zh-CN>
            //   <en>Mapping exists but is disabled: emit the disabled state with aria-disabled so it is neither read as not-granted nor treated as clickable.</en>
            // </lang>
            return "<td class=\"portal-capability-matrix-cell portal-capability-matrix-disabled\" aria-disabled=\"true\" title=\"" +
                HttpUtility.HtmlAttributeEncode(text("StateDisabled")) + "\">" +
                HttpUtility.HtmlEncode(text("MarkDisabled")) + "</td>";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造单元格查找键（角色名 + 分隔符 + 权限键），分隔符使用不可见控制字符以降低与真实名称冲突的概率。</zh-CN>
        ///   <en>Builds the cell lookup key (role name plus separator plus permission key) using a control character so real names are unlikely to collide.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleName">
        /// <l>
        ///   <zh-CN>角色名。</zh-CN>
        ///   <en>Role name.</en>
        /// </l>
        /// </param>
        /// <param name="permissionKey">
        /// <l>
        ///   <zh-CN>权限键。</zh-CN>
        ///   <en>Permission key.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>查找键。</zh-CN>
        ///   <en>Lookup key.</en>
        /// </l>
        /// </returns>
        private static string BuildCellKey(string roleName, string permissionKey)
        {
            return (roleName ?? string.Empty).Trim() + "\u001f" + (permissionKey ?? string.Empty).Trim();
        }
    }
}
