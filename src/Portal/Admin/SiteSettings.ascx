<%@ Control Inherits="ASPNET.StarterKit.Portal.SiteSettings" CodeBehind="SiteSettings.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title runat="server" id=Title1 />
<table cellpadding="2" cellspacing="0" border="0">
    <tr>
        <td width="100" class="Normal">
            Site Title:
        </td>
        <td colspan="2" class="NormalTextBox">
            <asp:Textbox id="siteName" width="240" runat="server" />
        </td>
    </tr>
    <tr>
        <td class="Normal">
            Always show edit button?:
        </td>
        <td colspan="2" class="Normal">
            <asp:CheckBox id="showEdit" runat="server" />
        </td>
    </tr>
    <tr>
        <td>
            &nbsp;
        </td>
        <td colspan="2">
            <asp:LinkButton id="applyBtn" class="CommandButton" Text="Apply Changes" runat="server" onclick="Apply_Click" />
        </td>
    </tr>
</table>