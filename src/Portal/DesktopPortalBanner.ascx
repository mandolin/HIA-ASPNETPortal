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
<%-- #todo 默认模板banner区域的改造：样式改造等（待细化） --%>
<table width="100%" cellspacing="0" class="HeadBg" border="0">
    <!-- 顶部横幅 -->
    <tr valign="top">
        <td colspan="3" class="SiteLink" background="<%= Global.GetApplicationPath(Request) %>/images/bars.gif" align="right">
            <!-- 个性化欢迎消息 -->
            <asp:Label ID="WelcomeMessage" ForeColor="#eeeeee" runat="server" />
            <!-- 分隔符 -->
            <span class="Accent">|</span>
            <!-- Portal 主页链接 --><%-- #todo Localization --%>
            <a href="<%= Global.GetApplicationPath(Request) %>/DesktopDefault.aspx" class="SiteLink">Portal Home</a>
            <!-- 分隔符 -->
            <span class="Accent">|</span>
            <!-- Portal 文档链接（示例链接，根据实际需求替换） --><%-- #todo Localization --%>
            <a href="<%= Global.GetApplicationPath(Request) %>/admin/NotImplemented.aspx" class="SiteLink">Portal Documentation</a>
            <!-- 登出链接 -->
            <%= LogoffLink %>
            &nbsp;&nbsp;
        </td>
    </tr>
    <tr>
        <!-- 左侧空白 -->
        <td width="10" rowspan="2">&nbsp;</td>
        <!-- 门户站点名称 -->
        <td height="40">
            <asp:Label ID="SiteName" CssClass="SiteTitle" EnableViewState="false" runat="server" />
        </td>
        <!-- 右侧 Logo 区域 -->
        <td align="center" rowspan="2">
            <!-- ASP.NET Logo 或其他 Logo -->
        </td>
    </tr>
    <tr>
        <!-- 选项卡导航 -->
        <td>
            <asp:DataList ID="Tabs" CssClass="OtherTabsBg" RepeatDirection="Horizontal" ItemStyle-Height="25" SelectedItemStyle-CssClass="TabBg" ItemStyle-BorderWidth="1" EnableViewState="false" runat="server">
                <ItemTemplate>
                    <!-- 选项卡链接，根据实际需求替换 href 和显示文本 -->
                    &nbsp;<a href='<%= Global.GetApplicationPath(Request) %>/DesktopDefault.aspx?tabindex=<%# Container.ItemIndex %>&tabid=<%# ((ITabItem) Container.DataItem).TabId %>' class="OtherTabs"><%# ((ITabItem) Container.DataItem).TabName %></a>&nbsp;
                </ItemTemplate>
                <SelectedItemTemplate>
                    <!-- 选中状态的选项卡 -->
                    &nbsp;<span class="SelectedTab"><%# ((ITabItem) Container.DataItem).TabName %></span>&nbsp;
                </SelectedItemTemplate>
            </asp:DataList>
        </td>
    </tr>
</table>