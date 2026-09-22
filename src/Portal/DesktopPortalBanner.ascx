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
                <a href="<%= Global.GetApplicationPath(Request) %>/admin/NotImplemented.aspx" class="SiteLink portal-toplink"><%= lang.PortalBanner_Documentation %></a>
                <%= LogoffLink %>
            </div>
        </div>

        <div class="portal-nav-row">
            <%--
                <lang>
                    <zh-CN>Tabs 数据源来自门户配置；每个导航 URL 由应用路径、索引和 TabId 组合，实际可见性与访问权限仍由服务器控制。</zh-CN>
                    <en>The Tabs data source comes from portal configuration; each navigation URL combines the application path, index, and TabId, while visibility and access remain server-controlled.</en>
                </lang>
            --%>
            <asp:DataList
                ID="Tabs"
                CssClass="portal-tabs OtherTabsBg"
                RepeatDirection="Horizontal"
                RepeatLayout="Flow"
                EnableViewState="false"
                OnItemDataBound="Tabs_ItemDataBound"
                runat="server">
                <ItemTemplate>
                    <%--
                        <lang>
                            <zh-CN>普通 Tab 项输出服务器绑定的名称与 URL；模板只呈现导航，不把绑定值当作客户端权限判断。URL 改由 code-behind 方法生成，避免在服务器控件属性中使用 &lt;%= %&gt; 代码块（Web Forms 不允许）。每项同时输出一个默认隐藏的禁用态元素，供 Tab 门控在管理员可见时替换显示并携带阻断原因；默认隐藏使其在门控关闭时完全不输出 HTML。</zh-CN>
                            <en>A regular tab item renders the server-bound name and URL; the template presents navigation and does not treat bound values as client-side authorization decisions. The URL is now produced by a code-behind method because a &lt;%= %&gt; code block is not allowed inside a server-control attribute in Web Forms. Each item also emits a hidden disabled element that the tab gate can reveal for administrators together with the blocked reason; because it defaults to hidden it emits no HTML at all while the gate is off.</en>
                        </lang>
                    --%>
                    <asp:HyperLink ID="TabLink" runat="server" CssClass="portal-tab OtherTabs"
                        NavigateUrl='<%# BuildTabUrl(Container.ItemIndex, (ITabItem) Container.DataItem) %>'
                        Text='<%# ((ITabItem) Container.DataItem).TabName %>' />
                    <asp:Label ID="TabDisabled" runat="server" Visible="false" CssClass="portal-tab portal-tab-disabled" />
                </ItemTemplate>
                <SelectedItemTemplate>
                    <span class="portal-tab portal-tab-selected SelectedTab"><%# ((ITabItem) Container.DataItem).TabName %></span>
                </SelectedItemTemplate>
            </asp:DataList>
        </div>
    </div>
</div>
