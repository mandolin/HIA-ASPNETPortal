using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>主导航两级分组的展示视图模型：把「已完成授权过滤的页签集合」与 `PortalNavigationGroupAlias` 的封闭词表
    ///   结合，产出「分组 → 组内页签」以及「未分组平铺页签」的只读视图，供 `DesktopPortalBanner.ascx` 绑定渲染。
    ///   本类不读数据库、不读请求上下文、不调用 `PortalSecurity.IsInRoles` 或任何权限判定；分组只决定展示顺序与折叠归属，
    ///   **绝不隐藏任何已授权页签**，也不改变 `PortalCfg_Tabs` 表结构（A 级硬边界）。</zh-CN>
    ///   <en>Presentation view model for two-level main-navigation groups: combines the already authorization-filtered tab
    ///   collection with the closed vocabulary in `PortalNavigationGroupAlias`, producing a read-only "group → member tabs"
    ///   view plus flat ungrouped tabs for `DesktopPortalBanner.ascx` to bind. This class reads no database or request
    ///   context and calls neither `PortalSecurity.IsInRoles` nor any permission check; groups decide only presentation
    ///   order and collapse ownership, **never hide an authorized tab**, and never change the `PortalCfg_Tabs` schema
    ///   (class-A hard boundary).</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>与 `PortalNavigationGroupAlias` 的分工：后者是词表与配置覆盖的**数据契约**（fail-closed），本类是
    ///   **渲染数据装配**（保留每个页签在最终授权集合中的显示下标，因为 `PortalDesktopTabUrl` 的 `tabindex` 用它；
    ///   分组重排不得改变它，否则页面内匿名登录模块的注入启发式会漂移）。</zh-CN>
    ///   <en>Division of labour with `PortalNavigationGroupAlias`: the latter is the **data contract** for the vocabulary and
    ///   configuration overrides (fail-closed); this class **assembles rendering data**, preserving each tab's display index
    ///   in the final authorized collection because `PortalDesktopTabUrl` uses it for `tabindex`. Group reordering must not
    ///   change that index, or the in-page anonymous-login injection heuristic drifts.</en>
    /// </lang>
    /// </remarks>
    public static class PortalNavigationGroupView
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>组内一个页签的展示项：保留原始 `ITabItem`（渲染层据此取本地化名与地址）、最终集合中的显示下标、
        ///   选中标记，以及入口 gate 已判定的禁用态与原因码。禁用原因码只供工具提示，不成为可见文案。</zh-CN>
        ///   <en>One tab's presentation item inside a group: keeps the original `ITabItem` (the renderer takes the localized
        ///   name and URL from it), the display index in the final collection, the selected flag, and the disabled state and
        ///   reason code already decided by the entry gate. The reason code feeds a tooltip only and never becomes visible copy.</en>
        /// </lang>
        /// </summary>
        public sealed class GroupTabView
        {
            /// <summary><lang><zh-CN>原始页签；渲染层用它解析中英显示名与页签 ID。</zh-CN><en>Original tab; the renderer resolves localized names and the tab id from it.</en></lang></summary>
            public ITabItem Tab { get; private set; }

            /// <summary><lang><zh-CN>页签在最终授权集合中的显示下标，用于 `tabindex`；分组不改变它。</zh-CN><en>Display index in the final authorized collection, used for `tabindex`; grouping never changes it.</en></lang></summary>
            public int DisplayIndex { get; private set; }

            /// <summary><lang><zh-CN>是否为当前活动页签；为 true 时渲染成非链接的选中态。</zh-CN><en>Whether this is the active tab; when true it renders as a non-link selected state.</en></lang></summary>
            public bool IsSelected { get; private set; }

            /// <summary><lang><zh-CN>入口 gate 是否把该页签标为禁用（仅管理员可见禁用态）。</zh-CN><en>Whether the entry gate marked this tab disabled (the disabled state is visible to administrators only).</en></lang></summary>
            public bool IsBlocked { get; private set; }

            /// <summary><lang><zh-CN>禁用原因码；仅供 `title` 工具提示，不进入可见文本。</zh-CN><en>Disabled reason code; for the `title` tooltip only, never visible text.</en></lang></summary>
            public string BlockedReason { get; private set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>构造不可变展示项；原因码为空时统一为空串，避免模板绑定出现 `null` 属性。</zh-CN>
            ///   <en>Build an immutable presentation item; a blank reason is normalized to an empty string so template binding never sees a `null` attribute.</en>
            /// </lang>
            /// </summary>
            /// <param name="tab"><l zh-CN="原始页签。" en="Original tab." /></param>
            /// <param name="displayIndex"><l zh-CN="最终集合中的显示下标。" en="Display index in the final collection." /></param>
            /// <param name="isSelected"><l zh-CN="是否活动页签。" en="Whether it is the active tab." /></param>
            /// <param name="isBlocked"><l zh-CN="是否被 gate 标为禁用。" en="Whether the gate marked it disabled." /></param>
            /// <param name="blockedReason"><l zh-CN="禁用原因码，可为 null。" en="Disabled reason code, may be null." /></param>
            public GroupTabView(ITabItem tab, int displayIndex, bool isSelected, bool isBlocked, string blockedReason)
            {
                Tab = tab;
                DisplayIndex = displayIndex;
                IsSelected = isSelected;
                IsBlocked = isBlocked;
                BlockedReason = blockedReason ?? string.Empty;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>一个展示分组：稳定键、已本地化的显示名、供 `aria-controls` 使用的稳定容器 id，以及组内页签。
        ///   只渲染至少有一个页签的分组；空分组由 `NavigationView.EmptySectionCount` 计数留证，不输出空白 disclosure 按钮。</zh-CN>
        ///   <en>One presentation group: stable key, localized label, a stable container id for `aria-controls`, and member tabs.
        ///   Only groups with at least one tab are rendered; empty groups are counted in `NavigationView.EmptySectionCount`
        ///   as evidence rather than emitting blank disclosure buttons.</en>
        /// </lang>
        /// </summary>
        public sealed class GroupSectionView
        {
            /// <summary><lang><zh-CN>分组稳定键（词表键，非配置文本）。</zh-CN><en>Stable group key (vocabulary key, not configuration text).</en></lang></summary>
            public string GroupKey { get; private set; }

            /// <summary><lang><zh-CN>按界面文化解析后的分组显示名。</zh-CN><en>Group label resolved for the UI culture.</en></lang></summary>
            public string DisplayName { get; private set; }

            /// <summary><lang><zh-CN>组内链接容器的稳定 HTML id，供 disclosure 按钮 `aria-controls` 指向。</zh-CN><en>Stable HTML id of the in-group link container, targeted by the disclosure button's `aria-controls`.</en></lang></summary>
            public string ContainerId { get; private set; }

            /// <summary><lang><zh-CN>组内页签数，用于分组标题的计数徽标。</zh-CN><en>Member tab count, used for the group header count badge.</en></lang></summary>
            public int TabCount { get { return Tabs.Count; } }

            /// <summary><lang><zh-CN>组内页签只读集合，顺序沿用最终授权集合的顺序。</zh-CN><en>Read-only member tabs, ordered as in the final authorized collection.</en></lang></summary>
            public IList<GroupTabView> Tabs { get; private set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>构造不可变分组；成员集合冻结，防止模板或调用方在绑定过程中改写。</zh-CN>
            ///   <en>Build an immutable group; the member collection is frozen so the template or caller cannot mutate it during binding.</en>
            /// </lang>
            /// </summary>
            /// <param name="groupKey"><l zh-CN="分组稳定键。" en="Stable group key." /></param>
            /// <param name="displayName"><l zh-CN="已本地化的显示名。" en="Localized label." /></param>
            /// <param name="containerId"><l zh-CN="组内容器稳定 id。" en="Stable in-group container id." /></param>
            /// <param name="tabs"><l zh-CN="组内页签序列。" en="Member tab sequence." /></param>
            public GroupSectionView(string groupKey, string displayName, string containerId, IEnumerable<GroupTabView> tabs)
            {
                GroupKey = groupKey;
                DisplayName = displayName;
                ContainerId = containerId;
                Tabs = new ReadOnlyCollection<GroupTabView>((tabs ?? Enumerable.Empty<GroupTabView>()).ToList());
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>一次导航渲染的完整只读视图。配置非法（`Degraded`）时调用方改为一级平铺，所有页签仍可达；
        ///   未分组页签始终平铺输出，绝不因为不属于任何词表分组而被隐藏。</zh-CN>
        ///   <en>Complete read-only view for one navigation render. On invalid configuration (`Degraded`) the caller falls
        ///   back to flat navigation with every tab still reachable; ungrouped tabs are always rendered flat and are never
        ///   hidden for lacking a vocabulary group.</en>
        /// </lang>
        /// </summary>
        public sealed class NavigationView
        {
            /// <summary><lang><zh-CN>非空的展示分组，按词表顺序排列。</zh-CN><en>Non-empty presentation groups in vocabulary order.</en></lang></summary>
            public IList<GroupSectionView> Sections { get; private set; }

            /// <summary><lang><zh-CN>不属于任何分组的页签，按原顺序平铺展示。</zh-CN><en>Tabs belonging to no group, rendered flat in original order.</en></lang></summary>
            public IList<GroupTabView> UngroupedTabs { get; private set; }

            /// <summary><lang><zh-CN>别名配置非法、已 fail-closed 回退默认词表时为 true；调用方据此降级为一级平铺。</zh-CN><en>True when alias configuration was invalid and fail-closed fallback to defaults occurred; the caller degrades to flat navigation.</en></lang></summary>
            public bool Degraded { get; private set; }

            /// <summary><lang><zh-CN>被过滤掉的空分组数量，留作证据（不影响任何页签可见性）。</zh-CN><en>Number of filtered-out empty groups, kept as evidence; it never affects tab visibility.</en></lang></summary>
            public int EmptySectionCount { get; private set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>构造不可变视图；分组与未分组集合均冻结。</zh-CN>
            ///   <en>Build an immutable view; both the group and ungrouped collections are frozen.</en>
            /// </lang>
            /// </summary>
            /// <param name="sections"><l zh-CN="非空分组序列。" en="Non-empty group sequence." /></param>
            /// <param name="ungroupedTabs"><l zh-CN="未分组页签序列。" en="Ungrouped tab sequence." /></param>
            /// <param name="degraded"><l zh-CN="是否因配置非法而降级。" en="Whether invalid configuration caused degradation." /></param>
            /// <param name="emptySectionCount"><l zh-CN="被过滤的空分组数。" en="Count of filtered empty groups." /></param>
            public NavigationView(IEnumerable<GroupSectionView> sections, IEnumerable<GroupTabView> ungroupedTabs, bool degraded, int emptySectionCount)
            {
                Sections = new ReadOnlyCollection<GroupSectionView>((sections ?? Enumerable.Empty<GroupSectionView>()).ToList());
                UngroupedTabs = new ReadOnlyCollection<GroupTabView>((ungroupedTabs ?? Enumerable.Empty<GroupTabView>()).ToList());
                Degraded = degraded;
                EmptySectionCount = emptySectionCount;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>容器 id 前缀；与主题作用域类同名族，值是静态常量，不含请求数据。</zh-CN>
        ///   <en>Container id prefix, from the same family as the theme scope classes; it is a static constant with no request data.</en>
        /// </lang>
        /// </summary>
        public const string ContainerIdPrefix = "portal-nav-group-";

        /// <summary>
        /// <lang>
        ///   <zh-CN>从已授权页签集合构造两级分组视图。授权过滤必须在此**之前**由调用方完成；本方法只按
        ///   `NavigationKey` 查封闭词表与部署别名覆盖，不改变页签集合、不做权限判定。</zh-CN>
        ///   <en>Build the two-level grouped view from an already authorized tab collection. Authorization filtering must be
        ///   completed by the caller **before** this call; this method only looks up `NavigationKey` in the closed vocabulary
        ///   and deployment alias overrides, never changing the collection or deciding permissions.</en>
        /// </lang>
        /// </summary>
        /// <param name="authorizedTabs"><l zh-CN="已完成角色过滤与入口 gate 判定的页签集合；为 null 时按空集合处理。" en="Tabs that already passed role filtering and the entry gate; null is treated as empty." /></param>
        /// <param name="activeTabId"><l zh-CN="当前活动页签 ID，用于选中态。" en="Current active tab id, used for the selected state." /></param>
        /// <param name="configuration"><l zh-CN="部署别名覆盖文本，可为 null。" en="Deployment alias override text, may be null." /></param>
        /// <param name="blockedReasons"><l zh-CN="入口 gate 已判定的禁用原因码（按 TabId 索引），可为 null。" en="Disabled reason codes already decided by the entry gate (keyed by TabId), may be null." /></param>
        /// <param name="culture"><l zh-CN="界面文化；为 null 时取当前线程界面文化。" en="UI culture; null uses the current thread UI culture." /></param>
        /// <returns><l zh-CN="只读分组视图，含未分组平铺页签与降级标记。" en="Read-only grouped view with flat ungrouped tabs and a degradation flag." /></returns>
        public static NavigationView Build(
            IList<ITabItem> authorizedTabs,
            int activeTabId,
            string configuration,
            IDictionary<int, string> blockedReasons,
            CultureInfo culture)
        {
            // <lang>
            //   <zh-CN>输入集合只为 null 时归一为空列表；不去重、不排序、不删除任何项 —— 去重会静默丢掉
            //   共享稳定键的页签，排序会改变 `tabindex` 语义，二者都违反「绝不隐藏页签」的边界。</zh-CN>
            //   <en>Normalize only a null input to an empty list; never de-duplicate, sort, or remove an item. De-duplication
            //   would silently drop tabs sharing a stable key and sorting would change `tabindex` semantics, both violating
            //   the "never hide a tab" boundary.</en>
            // </lang>
            IList<ITabItem> safeTabs = authorizedTabs ?? new List<ITabItem>();

            // <lang>
            //   <zh-CN>原因码字典同样只做 null 归一；它已是授权阶段的结果，本方法只读不判定。</zh-CN>
            //   <en>The reason-code dictionary is only null-normalized; it is already an authorization-stage result, read but never decided here.</en>
            // </lang>
            IDictionary<int, string> safeReasons = blockedReasons ?? new Dictionary<int, string>();

            // <lang>
            //   <zh-CN>调用 P290 的权威 API 取得分组骨架与降级标记：组顺序来自封闭词表，不依赖数据库排序；
            //   `degraded` 为 true 表示配置被整份拒绝、已回退默认词表，渲染层据此降级为一级平铺。</zh-CN>
            //   <en>Call the authoritative P290 API for the group skeleton and degradation flag: group order comes from the
            //   closed vocabulary rather than database ordering; a true `degraded` means the configuration was rejected
            //   wholesale and defaults were used, so the renderer falls back to flat navigation.</en>
            // </lang>
            IList<string> keys = safeTabs.Select(tab => tab == null ? string.Empty : (tab.NavigationKey ?? string.Empty)).ToList();
            IList<string> ungroupedKeys;
            bool degraded;
            IList<KeyValuePair<PortalNavigationGroupAlias.GroupDefinition, IList<string>>> skeleton =
                PortalNavigationGroupAlias.BuildGroupedView(keys, configuration, out ungroupedKeys, out degraded);

            // <lang>
            //   <zh-CN>从骨架反建「稳定键 → 分组定义」映射。`BuildGroupedView` 已处理别名覆盖，故这里不需要
            //   再解析配置文本，避免两处口径分叉；同键重复出现时以骨架最后一次归属为准（词表成员互斥，正常不冲突）。</zh-CN>
            //   <en>Rebuild a "stable key → group definition" map from the skeleton. `BuildGroupedView` already applied alias
            //   overrides, so parsing the configuration again here would create a second interpretation. For a duplicated key
            //   the skeleton's last assignment wins (vocabulary members are disjoint, so this normally cannot collide).</en>
            // </lang>
            Dictionary<string, PortalNavigationGroupAlias.GroupDefinition> keyToGroup =
                new Dictionary<string, PortalNavigationGroupAlias.GroupDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<PortalNavigationGroupAlias.GroupDefinition, IList<string>> pair in skeleton)
            {
                foreach (string memberKey in pair.Value)
                {
                    keyToGroup[memberKey] = pair.Key;
                }
            }

            // <lang>
            //   <zh-CN>按骨架顺序为每个分组预建可变成员桶；桶与骨架一一对应，最后再统一冻结成只读视图。</zh-CN>
            //   <en>Pre-create a mutable member bucket per group in skeleton order; buckets map one-to-one to the skeleton and are frozen into read-only views at the end.</en>
            // </lang>
            List<PortalNavigationGroupAlias.GroupDefinition> definitions = new List<PortalNavigationGroupAlias.GroupDefinition>();
            Dictionary<string, List<GroupTabView>> buckets = new Dictionary<string, List<GroupTabView>>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<PortalNavigationGroupAlias.GroupDefinition, IList<string>> pair in skeleton)
            {
                definitions.Add(pair.Key);
                buckets[pair.Key.GroupKey] = new List<GroupTabView>();
            }

            // <lang>
            //   <zh-CN>未分组页签单独收集，按原顺序平铺；它们多为历史 `Test.*` 页签或尚未填写稳定键的模块页签，
            //   分组机制不得把它们藏起来（A 级硬边界）。</zh-CN>
            //   <en>Ungrouped tabs are collected separately and rendered flat in original order; they are mostly legacy
            //   `Test.*` tabs or module tabs without a stable key yet, and grouping must never hide them (class-A boundary).</en>
            // </lang>
            List<GroupTabView> ungrouped = new List<GroupTabView>();

            // <lang>
            //   <zh-CN>逐页签装配：下标用循环变量（即最终授权集合中的原始位置），而不是分组后的组内位置；
            //   这样 `BuildTabUrl` 产出的 `tabindex` 与改造前逐字节一致。</zh-CN>
            //   <en>Assemble tab by tab: the index comes from the loop variable (the original position in the final authorized
            //   collection), not the position after grouping, so `BuildTabUrl` produces a `tabindex` byte-for-byte identical to before.</en>
            // </lang>
            for (int displayIndex = 0; displayIndex < safeTabs.Count; displayIndex++)
            {
                ITabItem tab = safeTabs[displayIndex];
                if (tab == null)
                {
                    continue;
                }

                // <lang>
                //   <zh-CN>选中态只按 TabId 比对；禁用态与原因码直接取授权阶段的结果，本方法不重新判定权限。</zh-CN>
                //   <en>The selected state compares TabId only; the disabled state and reason code come straight from the
                //   authorization stage, with no permission decision repeated here.</en>
                // </lang>
                bool isSelected = tab.TabId == activeTabId;
                string reason;
                bool isBlocked = safeReasons.TryGetValue(tab.TabId, out reason);
                GroupTabView item = new GroupTabView(tab, displayIndex, isSelected, isBlocked, isBlocked ? reason : string.Empty);

                // <lang>
                //   <zh-CN>空稳定键一律进平铺区；非空键查映射，查不到同样进平铺区，绝不丢弃。</zh-CN>
                //   <en>A blank stable key always goes to the flat area; a non-blank key is looked up and an unknown key
                //   likewise goes to the flat area, so nothing is ever dropped.</en>
                // </lang>
                string navigationKey = (tab.NavigationKey ?? string.Empty).Trim();
                PortalNavigationGroupAlias.GroupDefinition definition;
                if (navigationKey.Length > 0 && keyToGroup.TryGetValue(navigationKey, out definition))
                {
                    buckets[definition.GroupKey].Add(item);
                }
                else
                {
                    ungrouped.Add(item);
                }
            }

            // <lang>
            //   <zh-CN>界面文化缺省取当前线程值，与 `GroupDefinition.GetDisplayName` 的口径一致。</zh-CN>
            //   <en>A missing culture falls back to the current thread value, matching `GroupDefinition.GetDisplayName`.</en>
            // </lang>
            CultureInfo effectiveCulture = culture ?? CultureInfo.CurrentUICulture;

            // <lang>
            //   <zh-CN>只输出有成员的分组；空组计数留证但不渲染，避免匿名态出现多个点了却什么都没有的 disclosure 按钮。
            //   这与原型状态①「保留空组标题」有偏差，理由与边界见 `works/W-anp-P291.md`（据实纠正，不虚报已实现 8 态）。</zh-CN>
            //   <en>Only groups with members are emitted; empty groups are counted as evidence but not rendered, so anonymous
            //   sessions do not see several disclosure buttons that reveal nothing. This deviates from prototype state 1
            //   ("keep empty group headers"); rationale and boundary are recorded in `works/W-anp-P291.md` (honest correction,
            //   no claim that all eight prototype states are implemented).</en>
            // </lang>
            List<GroupSectionView> sections = new List<GroupSectionView>();
            int emptySectionCount = 0;
            foreach (PortalNavigationGroupAlias.GroupDefinition definition in definitions)
            {
                List<GroupTabView> members = buckets[definition.GroupKey];
                if (members.Count == 0)
                {
                    emptySectionCount++;
                    continue;
                }

                // <lang>
                //   <zh-CN>容器 id 由静态前缀 + 归一化后的词表键构成，只保留 ASCII 字母数字，绝不拼接请求参数；
                //   同一页只挂一个 Banner，故 id 稳定且唯一，可供 `aria-controls` 精确指向。</zh-CN>
                //   <en>The container id is a static prefix plus a normalized vocabulary key, keeping ASCII alphanumerics only
                //   and never concatenating request parameters. A page hosts exactly one Banner, so the id is stable and unique
                //   and `aria-controls` can target it precisely.</en>
                // </lang>
                string containerId = ContainerIdPrefix + NormalizeIdPart(definition.GroupKey);
                sections.Add(new GroupSectionView(
                    definition.GroupKey,
                    definition.GetDisplayName(effectiveCulture),
                    containerId,
                    members));
            }

            return new NavigationView(sections, ungrouped, degraded, emptySectionCount);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把词表键归一为小写 ASCII 字母数字串，用于 HTML id；非法字符以 `-` 替代，空结果回退 `group`。
        ///   这是纯展示用途的字符串整形，不改变分组归属或任何配置语义。</zh-CN>
        ///   <en>Normalize a vocabulary key into a lowercase ASCII-alphanumeric string for an HTML id; invalid characters
        ///   become `-` and an empty result falls back to `group`. This is presentation-only string shaping and changes
        ///   neither group membership nor any configuration semantics.</en>
        /// </lang>
        /// </summary>
        /// <param name="groupKey"><l zh-CN="分组稳定键。" en="Stable group key." /></param>
        /// <returns><l zh-CN="可用于 HTML id 的片段。" en="Fragment usable in an HTML id." /></returns>
        private static string NormalizeIdPart(string groupKey)
        {
            // <lang>
            //   <zh-CN>逐字符过滤，避免正则回溯与本地化大小写差异；`char.ToLowerInvariant` 保证跨机器一致。</zh-CN>
            //   <en>Filter character by character, avoiding regex backtracking and locale-dependent casing; `char.ToLowerInvariant` keeps it machine-independent.</en>
            // </lang>
            string source = groupKey ?? string.Empty;
            char[] buffer = new char[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                char current = char.ToLowerInvariant(source[index]);
                bool isAsciiLetter = (current >= 'a' && current <= 'z');
                bool isAsciiDigit = (current >= '0' && current <= '9');
                buffer[index] = (isAsciiLetter || isAsciiDigit) ? current : '-';
            }

            // <lang>
            //   <zh-CN>全非法字符时给稳定回退值，避免产出空 id 让 `aria-controls` 指向不存在的元素。</zh-CN>
            //   <en>When every character is invalid, use a stable fallback so `aria-controls` never points at a missing element.</en>
            // </lang>
            string normalized = new string(buffer).Trim('-');
            return normalized.Length > 0 ? normalized : "group";
        }
    }
}
