<%@ Page
    Language="c#"
    CodeBehind="OrganizationUnitEdit.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.OrganizationUnitEdit"
    MasterPageFile="~/Default.master" %>

<%-- P6.3-S4 组织单元最小维护页：不提供硬删除、导入、导出或批量同步。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="20">&nbsp;</td>
            <td>
                <table width="100%" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">
                            <asp:Label ID="TitleLabel" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td><hr noshade size="1"></td>
                    </tr>
                    <tr>
                        <td class="Normal">
                            <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
                            &nbsp;
                            <a class="CommandButton" href="SystemHealth.aspx">System Health</a>
                        </td>
                    </tr>
                </table>

                <asp:Label ID="MessageLabel" CssClass="NormalRed" EnableViewState="false" runat="server" />
                <asp:HiddenField ID="OrganizationUnitIdField" runat="server" />
                <asp:HiddenField ID="OriginalUpdatedUtcField" runat="server" />

                <table width="620" cellspacing="0" cellpadding="4" border="0">
                    <tr>
                        <td width="160" class="SubHead">Organization Code:</td>
                        <td>
                            <asp:TextBox ID="OrganizationCodeTextBox" CssClass="NormalTextBox" Width="260" MaxLength="100" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Display Name:</td>
                        <td>
                            <asp:TextBox ID="DisplayNameTextBox" CssClass="NormalTextBox" Width="320" MaxLength="150" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Parent:</td>
                        <td>
                            <asp:DropDownList ID="ParentOrganizationList" CssClass="NormalTextBox" Width="320" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Sort Order:</td>
                        <td>
                            <asp:TextBox ID="SortOrderTextBox" CssClass="NormalTextBox" Width="80" MaxLength="10" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Active:</td>
                        <td class="Normal">
                            <asp:CheckBox ID="IsActiveCheckBox" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td>
                            <asp:LinkButton
                                ID="SaveButton"
                                CssClass="CommandButton"
                                Text="Save"
                                CausesValidation="False"
                                OnClick="SaveButton_Click"
                                runat="server" />
                            &nbsp;
                            <a class="CommandButton" href="EmployeeDirectory.aspx">Cancel</a>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</asp:Content>
