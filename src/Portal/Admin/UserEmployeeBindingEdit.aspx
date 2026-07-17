<%@ Page
    Language="c#"
    CodeBehind="UserEmployeeBindingEdit.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.UserEmployeeBindingEdit"
    MasterPageFile="~/Default.master" %>

<%-- P6.3-S5 门户账号与员工单条绑定维护页。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table width="720" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td>
                <table width="100%" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">User Employee Binding</td>
                    </tr>
                    <tr>
                        <td><hr noshade size="1"></td>
                    </tr>
                    <tr>
                        <td class="Normal">
                            <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
                            &nbsp;
                            <asp:HyperLink ID="ManageUserLink" CssClass="CommandButton" Text="Manage User" Visible="false" runat="server" />
                        </td>
                    </tr>
                </table>

                <asp:Label ID="MessageLabel" CssClass="NormalRed" EnableViewState="false" runat="server" />
                <asp:HiddenField ID="ActiveBindingId" runat="server" />

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td width="150" class="SubHead">Current Binding:</td>
                        <td class="Normal">
                            <asp:Label ID="CurrentBindingText" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Portal User ID:</td>
                        <td>
                            <asp:TextBox ID="UserIdTextBox" CssClass="NormalTextBox" Width="120" MaxLength="12" runat="server" />
                            &nbsp;
                            <asp:Label ID="UserSummaryText" CssClass="Normal" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Employee Code:</td>
                        <td>
                            <asp:TextBox ID="EmployeeCodeTextBox" CssClass="NormalTextBox" Width="220" MaxLength="64" runat="server" />
                            &nbsp;
                            <asp:Label ID="EmployeeSummaryText" CssClass="Normal" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Reason:</td>
                        <td>
                            <asp:TextBox ID="ReasonTextBox" CssClass="NormalTextBox" Width="420" MaxLength="200" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td class="Normal">
                            <asp:LinkButton ID="BindButton" CssClass="CommandButton" Text="Bind User To Employee"
                                OnClick="BindButton_Click" runat="server" />
                            &nbsp;
                            <asp:LinkButton ID="EndBindingButton" CssClass="CommandButton" Text="End Active Binding"
                                CausesValidation="False" OnClick="EndBindingButton_Click"
                                OnClientClick="return confirm('确认结束当前员工绑定？');" runat="server" />
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</asp:Content>
