<%@ Control CodeBehind="DesktopPortalBanner.ascx.cs" Language="c#" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.DesktopPortalBanner" %>
<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%--

   The DesktopPortalBanner User Control is responsible for displaying the standard Portal
   banner at the top of each .aspx page.

   The DesktopPortalBanner uses the Portal Configuration System to obtain a list of the
   portal's SiteName and tab settings. It then render's this content into the page.

   桌面门户横幅用户控件负责显示每个 .aspx 页面顶部的标准门户横幅。

   桌面门户横幅使用门户配置系统来获取门户的站点名称和tab设置列表。然后将内容呈现到页面中。

--%>
<div class="portal-header HeadBg">
    <div class="portal-header-inner">
        <div class="portal-brand-row">
            <div class="portal-brand-block">
                <asp:Label ID="SiteName" CssClass="SiteTitle" EnableViewState="false" runat="server" />
                <span class="portal-brand-subtitle">Enterprise Portal</span>
            </div>
            <div class="portal-userbar SiteLink">
                <%--
                    <lang>
                        <zh-CN>用户栏中的欢迎文本、注销链接和门户入口由服务器上下文生成；页面不自行推断当前身份或会话状态。</zh-CN>
                        <en>The server context generates the welcome text, logoff link, and portal entries in the user bar; the page does not infer identity or session state on its own.</en>
                    </lang>
                --%>
                <asp:Label ID="WelcomeMessage" CssClass="portal-welcome" runat="server" />
                <a href="<%= Global.GetApplicationPath(Request) %>/DesktopDefault.aspx" class="SiteLink portal-toplink">Portal Home</a>
                <a href="<%= Global.GetApplicationPath(Request) %>/admin/NotImplemented.aspx" class="SiteLink portal-toplink">Portal Documentation</a>
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
                runat="server">
                <ItemTemplate>
                    <%--
                        <lang>
                            <zh-CN>普通 Tab 项输出服务器绑定的名称和 ID；模板只呈现导航，不把绑定值当作客户端权限判断。</zh-CN>
                            <en>A regular tab item renders the server-bound name and ID; the template presents navigation and does not treat bound values as client-side authorization decisions.</en>
                        </lang>
                    --%>
                    <a href='<%= Global.GetApplicationPath(Request) %>/DesktopDefault.aspx?tabindex=<%# Container.ItemIndex %>&tabid=<%# ((ITabItem) Container.DataItem).TabId %>' class="portal-tab OtherTabs"><%# ((ITabItem) Container.DataItem).TabName %></a>
                </ItemTemplate>
                <SelectedItemTemplate>
                    <span class="portal-tab portal-tab-selected SelectedTab"><%# ((ITabItem) Container.DataItem).TabName %></span>
                </SelectedItemTemplate>
            </asp:DataList>
        </div>
    </div>
</div>
