<%@ Page
    Language="c#"
    CodeBehind="EmployeeEdit.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EmployeeEdit"
    MasterPageFile="~/Default.master" %>

<%-- P6.3-S4 员工主数据最小维护页：不提供账号绑定、工号登录启用、导入、导出或敏感个人资料字段。 --%>
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
                            <a class="CommandButton" href="OrganizationUnitEdit.aspx">New Organization Unit</a>
                        </td>
                    </tr>
                </table>

                <asp:Label ID="MessageLabel" CssClass="NormalRed" EnableViewState="false" runat="server" />
                <asp:HiddenField ID="EmployeeIdField" runat="server" />
                <asp:HiddenField ID="OriginalUpdatedUtcField" runat="server" />

                <table width="720" cellspacing="0" cellpadding="4" border="0">
                    <tr>
                        <td width="170" class="SubHead">Employee Code:</td>
                        <td>
                            <asp:TextBox ID="EmployeeCodeTextBox" CssClass="NormalTextBox" Width="220" MaxLength="64" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Display Name:</td>
                        <td>
                            <asp:TextBox ID="DisplayNameTextBox" CssClass="NormalTextBox" Width="320" MaxLength="150" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Preferred Name:</td>
                        <td>
                            <asp:TextBox ID="PreferredNameTextBox" CssClass="NormalTextBox" Width="260" MaxLength="100" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Work Email:</td>
                        <td>
                            <asp:TextBox ID="WorkEmailTextBox" CssClass="NormalTextBox" Width="320" MaxLength="256" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Organization:</td>
                        <td>
                            <asp:DropDownList ID="OrganizationUnitList" CssClass="NormalTextBox" Width="320" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Status:</td>
                        <td>
                            <asp:DropDownList ID="EmploymentStatusList" CssClass="NormalTextBox" Width="160" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Joined UTC:</td>
                        <td>
                            <asp:TextBox ID="JoinedUtcTextBox" CssClass="NormalTextBox" Width="180" MaxLength="25" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Left UTC:</td>
                        <td>
                            <asp:TextBox ID="LeftUtcTextBox" CssClass="NormalTextBox" Width="180" MaxLength="25" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Source System:</td>
                        <td>
                            <asp:TextBox ID="SourceSystemTextBox" CssClass="NormalTextBox" Width="180" MaxLength="80" runat="server" />
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
