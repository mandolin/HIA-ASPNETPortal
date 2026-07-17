<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EmployeeProfileConfirm.ascx.cs" Inherits="ASPNET.StarterKit.Portal.EmployeeProfileConfirm" %>

<%-- P6.4 首批业务模块样板：员工只确认自己当前绑定的低敏资料，不提供上传、脚本或外部资源。 --%>
<div class="employee-profile-confirm">
    <div class="employee-profile-confirm-title">员工资料确认</div>
    <asp:Label ID="MessageLabel" CssClass="employee-profile-confirm-message" runat="server" />

    <asp:Panel ID="ProfilePanel" CssClass="employee-profile-confirm-profile" Visible="false" runat="server">
        <table class="employee-profile-confirm-table" cellspacing="0" cellpadding="4" border="0">
            <tr>
                <td class="employee-profile-confirm-label">员工号</td>
                <td><asp:Label ID="EmployeeCodeLabel" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-confirm-label">姓名</td>
                <td><asp:Label ID="DisplayNameLabel" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-confirm-label">称呼</td>
                <td><asp:Label ID="PreferredNameLabel" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-confirm-label">工作邮箱</td>
                <td><asp:Label ID="WorkEmailLabel" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-confirm-label">组织</td>
                <td><asp:Label ID="OrganizationLabel" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-confirm-label">状态</td>
                <td><asp:Label ID="EmploymentStatusLabel" runat="server" /></td>
            </tr>
            <tr>
                <td class="employee-profile-confirm-label">上次确认</td>
                <td><asp:Label ID="LastConfirmedLabel" runat="server" /></td>
            </tr>
        </table>

        <div class="employee-profile-confirm-actions">
            <asp:Button ID="ConfirmButton" CssClass="CommandButton" Text="确认资料无误" OnClick="ConfirmButton_Click" runat="server" />
        </div>
    </asp:Panel>
</div>
