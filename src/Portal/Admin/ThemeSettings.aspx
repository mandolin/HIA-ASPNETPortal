<%@ Page
    Language="c#"
    CodeBehind="ThemeSettings.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.ThemeSettings"
    MasterPageFile="~/Default.master" %>

<%-- P3.1 主题选择页：管理员仅能选择已部署且通过 manifest 校验的主题，不提供包上传或在线样式编辑。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="20">&nbsp;</td>
            <td>
                <table width="100%" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">Theme Settings</td>
                    </tr>
                    <tr>
                        <td><hr noshade size="1"></td>
                    </tr>
                </table>

                <asp:Label ID="MessageLabel" CssClass="NormalRed" EnableViewState="false" runat="server" />

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td width="180" class="SubHead">Global Theme:</td>
                        <td class="Normal">
                            <asp:DropDownList ID="GlobalThemeList" CssClass="NormalTextBox" runat="server" />
                            <asp:LinkButton ID="SaveGlobalThemeButton" CssClass="CommandButton" Text="Apply" OnClick="SaveGlobalThemeButton_Click" runat="server" />
                            <asp:LinkButton ID="ResetGlobalThemeButton" CssClass="CommandButton" Text="Reset Override" CausesValidation="False" OnClick="ResetGlobalThemeButton_Click" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Effective Source:</td>
                        <td class="Normal"><asp:Label ID="GlobalThemeStatusLabel" runat="server" /></td>
                    </tr>
                </table>

                <br>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td class="Head">Tab Override</td>
                    </tr>
                </table>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td width="180" class="SubHead">Portal Tab:</td>
                        <td class="Normal">
                            <asp:DropDownList ID="TabList" CssClass="NormalTextBox" AutoPostBack="True" OnSelectedIndexChanged="TabList_SelectedIndexChanged" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Override Theme:</td>
                        <td class="Normal">
                            <asp:DropDownList ID="TabThemeList" CssClass="NormalTextBox" runat="server" />
                            <asp:LinkButton ID="SaveTabThemeButton" CssClass="CommandButton" Text="Apply" OnClick="SaveTabThemeButton_Click" runat="server" />
                            <asp:LinkButton ID="ClearTabThemeButton" CssClass="CommandButton" Text="Clear Override" CausesValidation="False" OnClick="ClearTabThemeButton_Click" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Current Override:</td>
                        <td class="Normal"><asp:Label ID="TabThemeStatusLabel" runat="server" /></td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</asp:Content>
