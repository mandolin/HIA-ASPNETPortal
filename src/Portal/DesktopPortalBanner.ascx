<%@ Control CodeBehind="DesktopPortalBanner.ascx.cs" Language="c#" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.DesktopPortalBanner" %>
<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%--
    <lang>
        <zh-CN>DesktopPortalBanner 是每个桌面门户页面共享的页眉控件；站点名称、用户栏和 Tab 列表均来自服务器端门户配置与身份上下文，标记层只负责按旧 Web Forms 模板输出。</zh-CN>
        <en>DesktopPortalBanner is the shared header control for desktop portal pages; the site name, user bar, and tab list all come from server-side portal configuration and identity context, while markup only renders the legacy Web Forms template.</en>
    </lang>
--%>
<div class="portal-header HeadBg">
    <div class="portal-header-inner">
        <div class="portal-brand-row">
            <div class="portal-brand-block">
                <%--
                    <lang>
                        <zh-CN>品牌标识为随代码发布的静态资源（src/Portal/Images/portal-logo.png），不经数据库、不接受请求参数。
                        替代文本给出项目标识；品牌文字紧随其后，故该图不承担额外语义。</zh-CN>
                        <en>The brand mark is a static asset shipped with the code (src/Portal/Images/portal-logo.png); it comes from no
                        database and accepts no request parameter. Its alternative text names the project, while the brand text follows
                        immediately, so the image carries no additional meaning.</en>
                    </lang>
                --%>
                <img class="portal-logo" src="<%= Global.GetApplicationPath(Request) %>/Images/portal-logo.png" alt="HIA" />
                <asp:Label ID="SiteName" CssClass="SiteTitle" EnableViewState="false" runat="server" />
                <span class="portal-brand-subtitle"><%= lang.PortalBanner_Subtitle %></span>
            </div>
            <div class="portal-userbar SiteLink">
                <%--
                    <lang>
                        <zh-CN>用户栏中的欢迎文本、注销链接和门户入口由服务器上下文生成；页面不自行推断当前身份或会话状态。</zh-CN>
                        <en>The server context generates the welcome text, logoff link, and portal entries in the user bar; the page does not infer identity or session state on its own.</en>
                    </lang>
                --%>
                <asp:Label ID="WelcomeMessage" CssClass="portal-welcome" runat="server" />
                <a href="<%= Global.GetApplicationPath(Request) %>/DesktopDefault.aspx" class="SiteLink portal-toplink"><%= lang.PortalBanner_Home %></a>
                <%--
                <lang>
                  <zh-CN>原「门户文档」按钮指向 `/admin/NotImplemented.aspx`（未实现占位页），是公开导航里的一处死链，
                  已移除。文档入口应由部署侧提供真实文档地址后再挂回，避免把"未实现"页面暴露给使用者。</zh-CN>
                  <en>The former "Documentation" button pointed at `/admin/NotImplemented.aspx` — a not-implemented placeholder —
                  a dead link inside the public chrome, now removed. A documentation entry should be re-added only once the
                  deployment supplies a real documentation address, so that no "not implemented" page is ever exposed to users.</en>
                </lang>
                --%>
                <%= LogoffLink %>
            </div>
        </div>

        <div class="portal-nav-row">
            <%--
                <lang>
                    <zh-CN>Tabs 数据源来自门户配置；每个导航 URL 由应用路径、索引和 TabId 组合，实际可见性与访问权限仍由服务器控制。
                    分组配置非法时本 DataList 承担「一级平铺」降级：此时输出与两级分组改造前逐字节一致，是运行期回滚点。</zh-CN>
                    <en>The Tabs data source comes from portal configuration; each navigation URL combines the application path, index, and TabId, while visibility and access remain server-controlled.
                    When group configuration is invalid this DataList carries the flat-navigation degradation: its output is byte-for-byte identical to before the two-level change, serving as the runtime rollback point.</en>
                </lang>
            --%>
            <asp:DataList
                ID="Tabs"
                Visible="false"
                CssClass="portal-tabs OtherTabsBg"
                RepeatDirection="Horizontal"
                RepeatLayout="Flow"
                EnableViewState="false"
                OnItemDataBound="Tabs_ItemDataBound"
                runat="server">
                <ItemTemplate>
                    <%--
                        <lang>
                            <zh-CN>普通 Tab 项输出服务器绑定的名称与 URL；模板只呈现导航，不把绑定值当作客户端权限判断。URL 改由 code-behind 方法生成，避免在服务器控件属性中使用 &lt;%= %&gt; 代码块（Web Forms 不允许）。每项同时输出一个默认隐藏的禁用态元素，供 Tab 门控在管理员可见时替换显示并携带阻断原因；默认隐藏使其在门控关闭时完全不输出 HTML。
                            名称同样走 code-behind 方法（`BuildTabDisplayName`）：页签名已支持中/英/其它语言三列，直接绑定中文列会让英文界面出现反向混排。</zh-CN>
                            <en>A regular tab item renders the server-bound name and URL; the template presents navigation and does not treat bound values as client-side authorization decisions. The URL is now produced by a code-behind method because a &lt;%= %&gt; code block is not allowed inside a server-control attribute in Web Forms. Each item also emits a hidden disabled element that the tab gate can reveal for administrators together with the blocked reason; because it defaults to hidden it emits no HTML at all while the gate is off.
                            The name likewise goes through a code-behind method (`BuildTabDisplayName`): tab names now support Chinese / English / other-languages columns, so binding the Chinese column directly would produce reverse mixing in English UIs.</en>
                        </lang>
                    --%>
                    <asp:HyperLink ID="TabLink" runat="server" CssClass="portal-tab OtherTabs"
                        NavigateUrl='<%# BuildTabUrl(Container.ItemIndex, (ITabItem) Container.DataItem) %>'
                        Text='<%# BuildTabDisplayName((ITabItem) Container.DataItem) %>' />
                    <asp:Label ID="TabDisabled" runat="server" Visible="false" CssClass="portal-tab portal-tab-disabled" />
                </ItemTemplate>
                <SelectedItemTemplate>
                    <span class="portal-tab portal-tab-selected SelectedTab"><%# BuildTabDisplayName((ITabItem) Container.DataItem) %></span>
                </SelectedItemTemplate>
            </asp:DataList>

            <%--
                <lang>
                    <zh-CN>两级分组导航：分组标题是原生 button（disclosure，aria-expanded / aria-controls），组内仍是平铺的 .portal-tab 链接。
                    按 W3C WAI-ARIA APG，站点导航不用 menu / menubar role。分组只决定展示与折叠，不参与授权；权限仍由 AccessRoles 与入口 gate 逐页签判定。
                    无 JS 时所有分组以 aria-expanded="true" 输出且组内链接不加隐藏类，渐进增强保证页签始终可达；折叠与 Escape 回焦由外置脚本绑定（不用内联 onclick，避免可访问性门禁违规与脚本注入面）。</zh-CN>
                    <en>Two-level grouped navigation: each group header is a native button (disclosure with aria-expanded / aria-controls) and each group still contains flat .portal-tab links.
                    Per W3C WAI-ARIA APG, site navigation uses no menu / menubar role. Groups decide only presentation and collapse, never authorization; permissions stay with AccessRoles and the per-tab entry gate.
                    Without JavaScript every group is emitted with aria-expanded="true" and no hidden class on its links, so progressive enhancement keeps all tabs reachable; collapsing and Escape focus return are bound by an external script (no inline onclick, avoiding accessibility-gate violations and script-injection surface).</en>
                </lang>
            --%>
            <asp:PlaceHolder ID="GroupedNavigation" runat="server">
                <span class="portal-tabs OtherTabsBg portal-tabs-grouped">
                    <asp:Repeater ID="NavigationGroups" runat="server">
                        <ItemTemplate>
                            <div class="portal-nav-group">
                                <button type="button" class="portal-nav-group-toggle"
                                    aria-expanded="true" aria-controls='<%# Eval("ContainerId") %>'>
                                    <span class="portal-nav-group-caret">&#9662;</span><%# GetGroupDisplayName(Container.DataItem) %><span class="portal-nav-group-count"><%# Eval("TabCount") %></span>
                                </button>
                                <span id='<%# Eval("ContainerId") %>' class="portal-nav-group-tabs">
                                    <asp:Repeater ID="GroupTabs" runat="server" DataSource='<%# Eval("Tabs") %>' OnItemDataBound="GroupTab_ItemDataBound">
                                        <ItemTemplate>
                                            <asp:HyperLink ID="GroupTabLink" runat="server" CssClass="portal-tab OtherTabs"
                                                NavigateUrl='<%# BuildGroupTabUrl(Container.DataItem) %>'
                                                Text='<%# BuildGroupTabDisplayName(Container.DataItem) %>'
                                                Visible='<%# IsGroupTabLinkVisible(Container.DataItem) %>' />
                                            <asp:Label ID="GroupTabSelected" runat="server" CssClass="portal-tab portal-tab-selected SelectedTab"
                                                Text='<%# BuildGroupTabDisplayName(Container.DataItem) %>'
                                                Visible='<%# IsGroupTabSelected(Container.DataItem) %>' />
                                            <asp:Label ID="GroupTabDisabled" runat="server" CssClass="portal-tab portal-tab-disabled"
                                                Text='<%# BuildGroupTabDisplayName(Container.DataItem) %>'
                                                ToolTip='<%# BuildGroupTabTooltip(Container.DataItem) %>'
                                                Visible='<%# IsGroupTabBlocked(Container.DataItem) %>' />
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </span>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                    <%--
                        <lang>
                            <zh-CN>不属于任何词表分组的页签（历史 Test.* 页签或尚未填写稳定键的模块页签）平铺在此，无 disclosure 按钮，绝不隐藏。</zh-CN>
                            <en>Tabs belonging to no vocabulary group (legacy Test.* tabs or module tabs without a stable key yet) are rendered flat here with no disclosure button and are never hidden.</en>
                        </lang>
                    --%>
                    <asp:Repeater ID="UngroupedTabs" runat="server" OnItemDataBound="GroupTab_ItemDataBound">
                        <ItemTemplate>
                            <asp:HyperLink ID="GroupTabLink" runat="server" CssClass="portal-tab OtherTabs"
                                NavigateUrl='<%# BuildGroupTabUrl(Container.DataItem) %>'
                                Text='<%# BuildGroupTabDisplayName(Container.DataItem) %>'
                                Visible='<%# IsGroupTabLinkVisible(Container.DataItem) %>' />
                            <asp:Label ID="GroupTabSelected" runat="server" CssClass="portal-tab portal-tab-selected SelectedTab"
                                Text='<%# BuildGroupTabDisplayName(Container.DataItem) %>'
                                Visible='<%# IsGroupTabSelected(Container.DataItem) %>' />
                            <asp:Label ID="GroupTabDisabled" runat="server" CssClass="portal-tab portal-tab-disabled"
                                Text='<%# BuildGroupTabDisplayName(Container.DataItem) %>'
                                ToolTip='<%# BuildGroupTabTooltip(Container.DataItem) %>'
                                Visible='<%# IsGroupTabBlocked(Container.DataItem) %>' />
                        </ItemTemplate>
                    </asp:Repeater>
                </span>
            </asp:PlaceHolder>
            <script type="text/javascript" src="<%= Global.GetApplicationPath(Request) %>/Scripts/Navigation/PortalNavigationGroups.js"></script>
        </div>
    </div>
</div>
