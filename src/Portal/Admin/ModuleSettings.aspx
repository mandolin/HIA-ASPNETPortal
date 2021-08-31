<%@ Page CodeBehind="ModuleSettings.aspx.cs" Language="c#" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.ModuleSettingsPage" MasterPageFile="~/Default.master" %>

<%--
    The ModuleSettings.aspx page is used to enable administrators to view/edit/update
    a portal module's settings (title, output cache properties, edit access)
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="150">
                &nbsp;
            </td>
            <td width="*">
                <table cellpadding="2" cellspacing="1" border="0">
                    <tr>
                        <td colspan="4">
                            <table width="100%" cellspacing="0" cellpadding="0">
                                <tr>
                                    <td align="left" class="Head">
                                        Module Settings
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <hr noshade size="1">
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td width="100" class="SubHead">
                            Module Name:
                        </td>
                        <td colspan="3">
                            &nbsp;<asp:TextBox ID="moduleTitle" Width="300" CssClass="NormalTextBox" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">
                            Cache Timeout (seconds):
                        </td>
                        <td colspan="3">
                            &nbsp;<asp:TextBox ID="cacheTime" Width="100" CssClass="NormalTextBox" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            &nbsp;
                        </td>
                        <td colspan="3">
                            <hr noshade size="1">
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">
                            Roles that can edit content:
                        </td>
                        <td colspan="3">
                            <asp:CheckBoxList ID="authEditRoles" RepeatColumns="2" Font-Names="Verdana,Arial"
                                Font-Size="8pt" Width="300" CellPadding="0" CellSpacing="0" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            &nbsp;
                        </td>
                        <td colspan="3">
                            <hr noshade size="1">
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead" nowrap>
                            Show to mobile users?:
                        </td>
                        <td colspan="3">
                            <asp:CheckBox ID="showMobile" Font-Names="Verdana,Arial" Font-Size="8pt" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td colspan="4">
                            <hr noshade size="1">
                        </td>
                    </tr>
                    <tr>
                        <td colspan="4">
                            <asp:LinkButton class="CommandButton" Text="Apply Module Changes" runat="server"
                                ID="ApplyButton" OnClick="ApplyChanges_Click" />
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</asp:Content>
