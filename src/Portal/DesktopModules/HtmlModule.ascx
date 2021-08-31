<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.HtmlModule" CodeBehind="HtmlModule.ascx.cs" AutoEventWireup="True" %>
<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<portal:title EditText="Edit" EditUrl="~/DesktopModules/EditHtml.aspx" runat="server" id=Title1 />

<table id="t1" cellspacing="0" cellpadding="0" runat="server">
    <tr valign="top">
        <td id="HtmlHolder" runat="server">
        </td>
    </tr>
</table>