<%@ Page language="c#" CodeBehind="DesktopDefault.aspx.cs" AutoEventWireup="True" 
        Inherits="ASPNET.StarterKit.Portal.DesktopDefault"
        MasterPageFile="Default.master" %>
<%--

   The DesktopDefault.aspx page is used to load and populate each Portal View.  It accomplishes
   this by reading the layout configuration of the portal from the Portal Configuration
   system, and then using this information to dynamically instantiate portal modules
   (each implemented as an ASP.NET User Control), and then inject them into the page.

--%>

<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table cellspacing="0" cellpadding="4" width="100%" border="0">
        <tbody>
            <tr valign="top" height="*">
                <td width="5">
                    &nbsp;
                </td>
                <td id="LeftPane" width="170" runat="server" visible="false">
                </td>
                <td width="1">
                </td>
                <td id="ContentPane" width="*" runat="server" visible="false">
                </td>
                <td id="RightPane" width="230" runat="server" visible="false">
                </td>
                <td width="10">
                    &nbsp;
                </td>
            </tr>
        </tbody>
    </table>
</asp:Content>