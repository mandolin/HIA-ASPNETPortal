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
                <asp:Label ID="WelcomeMessage" CssClass="portal-welcome" runat="server" />
                <a href="<%= Global.GetApplicationPath(Request) %>/DesktopDefault.aspx" class="SiteLink portal-toplink">Portal Home</a>
                <a href="<%= Global.GetApplicationPath(Request) %>/admin/NotImplemented.aspx" class="SiteLink portal-toplink">Portal Documentation</a>
                <%= LogoffLink %>
            </div>
        </div>

        <div class="portal-nav-row">
            <asp:DataList
                ID="Tabs"
                CssClass="portal-tabs OtherTabsBg"
                RepeatDirection="Horizontal"
                RepeatLayout="Flow"
                EnableViewState="false"
                runat="server">
                <ItemTemplate>
                    <a href='<%= Global.GetApplicationPath(Request) %>/DesktopDefault.aspx?tabindex=<%# Container.ItemIndex %>&tabid=<%# ((ITabItem) Container.DataItem).TabId %>' class="portal-tab OtherTabs"><%# ((ITabItem) Container.DataItem).TabName %></a>
                </ItemTemplate>
                <SelectedItemTemplate>
                    <span class="portal-tab portal-tab-selected SelectedTab"><%# ((ITabItem) Container.DataItem).TabName %></span>
                </SelectedItemTemplate>
            </asp:DataList>
        </div>
    </div>
</div>
