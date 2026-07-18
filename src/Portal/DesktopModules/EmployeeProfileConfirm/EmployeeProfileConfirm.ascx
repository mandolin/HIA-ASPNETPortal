<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EmployeeProfileConfirm.ascx.cs" Inherits="ASPNET.StarterKit.Portal.EmployeeProfileConfirm" %>

<%-- P6.4 首批业务模块样板：员工只确认自己当前绑定的低敏资料，不提供上传、脚本或外部资源。 --%>
<div class="employee-profile-confirm">
    <div class="employee-profile-confirm-title">员工资料确认</div>
    <asp:Label ID="MessageLabel" CssClass="employee-profile-confirm-message" runat="server" />

    <asp:Panel ID="ProfilePanel" CssClass="employee-profile-confirm-profile" Visible="false" runat="server">
        <%-- 中文 / English: 资料字段使用块级网格，避免业务模块继续保留旧表格布局。 --%>
        <div class="employee-profile-field-grid">
            <div class="employee-profile-field">
                <span class="employee-profile-confirm-label employee-profile-field-label">员工号</span>
                <span class="employee-profile-field-value"><asp:Label ID="EmployeeCodeLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field">
                <span class="employee-profile-confirm-label employee-profile-field-label">姓名</span>
                <span class="employee-profile-field-value"><asp:Label ID="DisplayNameLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field">
                <span class="employee-profile-confirm-label employee-profile-field-label">称呼</span>
                <span class="employee-profile-field-value"><asp:Label ID="PreferredNameLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field">
                <span class="employee-profile-confirm-label employee-profile-field-label">工作邮箱</span>
                <span class="employee-profile-field-value"><asp:Label ID="WorkEmailLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field">
                <span class="employee-profile-confirm-label employee-profile-field-label">组织</span>
                <span class="employee-profile-field-value"><asp:Label ID="OrganizationLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field">
                <span class="employee-profile-confirm-label employee-profile-field-label">状态</span>
                <span class="employee-profile-field-value"><asp:Label ID="EmploymentStatusLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field employee-profile-field-wide">
                <span class="employee-profile-confirm-label employee-profile-field-label">上次确认</span>
                <span class="employee-profile-field-value"><asp:Label ID="LastConfirmedLabel" runat="server" /></span>
            </div>
        </div>

        <div class="employee-profile-confirm-actions">
            <asp:Button ID="ConfirmButton" CssClass="CommandButton" Text="确认资料无误" OnClick="ConfirmButton_Click" runat="server" />
        </div>
    </asp:Panel>
</div>
