<%@ Page Language="c#" AutoEventWireup="True" MasterPageFile="../Default.master" %>

<%@ OutputCache Duration="600" VaryByParam="title" %>
<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<script runat="server">

    /// <summary>
    ///   The Page_Load event on this Page is used to obtain the title
    ///   of the fictious content item.
    /// </summary>
    /// <remarks>
    ///   This page is the target for the fictious links in the sample data.
    /// </remarks>
    private void Page_Load(Object sender, EventArgs e)
    {
        if (Request.Params["title"] != null)
        {
            title.InnerHtml = Request.Params["title"];
        }
    }

</script>
<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table width="500" border="0">
        <tbody>
            <tr>
                <td class="Normal">
                    <br />
                    <br />
                    <br />
                    <br />
                    <span class="Head" id="title" runat="server">Linked Content Not Provided</span>
                    <br />
                    <br />
                    <hr noshade="noshade" size="1" />
                    <br />
                    The link you clicked was provided as a part of the sample data for the <b>ASP.NET Portal
                        Starter Kit</b>. The content for this link is not provided as part of the sample
                    application.
                    <br />
                    <br />
                    <a href="<%=Global.GetApplicationPath(Request)%>/DesktopDefault.aspx">Return to ASP.NET
                        Portal Starter Kit Home</a>
                </td>
            </tr>
        </tbody>
    </table>
</asp:Content>
