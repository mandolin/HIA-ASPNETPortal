<%@ Control CodeBehind="DesktopPortalBanner.ascx.cs" Language="c#" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.DesktopPortalBanner" %>
<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%--

   The DesktopPortalBanner User Control is responsible for displaying the standard Portal
   banner at the top of each .aspx page.

   The DesktopPortalBanner uses the Portal Configuration System to obtain a list of the
   portal's sitename and tab settings. It then render's this content into the page.

--%>
<table width="100%" cellspacing="0" class="HeadBg" border="0">
    <tr valign="top">
        <td colspan="3" class="SiteLink" background="<%= Global.GetApplicationPath(Request) %>/images/bars.gif" align="right">
            <asp:label id="WelcomeMessage" forecolor="#eeeeee" runat="server" />
            <a href="<%= Global.GetApplicationPath(Request) %>/DesktopDefault.aspx" class="SiteLink">Portal Home</a>
            <span class="Accent">
                |</span>
            <a href="<%= Global.GetApplicationPath(Request) %>/admin/NotImplemented.aspx" class="SiteLink">Portal Documentation</a>
            <%= LogoffLink %>
            &nbsp;&nbsp;
        </td>
    </tr>
    <tr>
        <td width="10" rowspan="2">
            &nbsp;
        </td>
        <td height="40">
            <asp:label id="siteName" CssClass="SiteTitle" EnableViewState="false" runat="server" />
        </td>
        <td align="center" rowspan="2">
            <!--ASP.NET Logo was here//-->
        </td>
    </tr>
    <tr>
        <td>
            <asp:datalist id="tabs" cssclass="OtherTabsBg" repeatdirection="horizontal" ItemStyle-Height="25" SelectedItemStyle-CssClass="TabBg" ItemStyle-BorderWidth="1" EnableViewState="false" runat="server">
                <ItemTemplate>
                    &nbsp;<a href='<%= Global.GetApplicationPath(Request) %>/DesktopDefault.aspx?tabindex=<%# Container.ItemIndex %>&tabid=<%# ((ITabItem) Container.DataItem).TabId %>' class="OtherTabs"><%# ((ITabItem) Container.DataItem).TabName %></a>&nbsp;
                </ItemTemplate>
                <SelectedItemTemplate>
                    &nbsp;<span class="SelectedTab"><%# ((ITabItem) Container.DataItem).TabName %></span>&nbsp;
                </SelectedItemTemplate>
            </asp:datalist>
        </td>
    </tr>
</table>