<%@ Page Language="c#" AutoEventWireup="True" MasterPageFile="../Default.master" %>

<%@ OutputCache Duration="36000" VaryByParam="none" %>
<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>
<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table width="500" border="0">
        <tbody>
            <tr>
                <td class="Normal">
                    <br />
                    <br />
                    <br />
                    <br />
                    <span class="Head">
                        <%=lang.Admin_EditAccessDenied_EditAccessDenied%></span>
                    <br />
                    <br />
                    <hr noshade="noshade" size="1" />
                    <br />
                    <%=lang.Admin_AccessDenied_DeniedAbout%>
                    <br />
                    <br />
                    <a href="<%=Global.GetApplicationPath(Request)%>/DesktopDefault.aspx">
                        <%=lang.Admin_AccessDenied_ReturnToHome%></a>
                </td>
            </tr>
        </tbody>
    </table>
</asp:Content>
