<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EmployeeProfileConfirm.ascx.cs" Inherits="ASPNET.StarterKit.Portal.EmployeeProfileConfirm" %>

<%--
    <lang>
        <zh-CN>P6.4 首批业务模块样板：员工只确认自己当前绑定的低敏资料，不提供上传、脚本或外部资源。</zh-CN>
        <en>P6.4 first business module sample: employees only confirm their currently bound low-sensitivity profile data; no upload, script, or external resource capability is provided.</en>
    </lang>
--%>
<div class="employee-profile-confirm">
    <div class="employee-profile-confirm-title">员工资料确认</div>
    <asp:Label ID="MessageLabel" CssClass="employee-profile-confirm-message" runat="server" />

    <%--
        <lang>
            <zh-CN>资料面板的可见性由服务器根据当前身份和在职绑定决定；标记层只提供承载区域，不授予确认权限。</zh-CN>
            <en>The server decides panel visibility from the current identity and active binding; the markup only provides a host and does not grant confirmation permission.</en>
        </lang>
    --%>
    <asp:Panel ID="ProfilePanel" CssClass="employee-profile-confirm-profile" Visible="false" runat="server">
        <%--
            <lang>
                <zh-CN>资料字段使用块级网格，避免业务模块继续保留旧表格布局。</zh-CN>
                <en>Profile fields use a block grid so business modules do not continue the old table-based layout.</en>
            </lang>
        --%>
        <%--
            <lang>
                <zh-CN>字段值由当前用户的服务器端资料视图绑定并按展示规则编码；页面不接收客户端员工标识来决定要确认的对象。</zh-CN>
                <en>Field values bind from the current user's server-side profile view and follow display encoding rules; the page does not accept a client employee identifier to choose the confirmation target.</en>
            </lang>
        --%>
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

        <%--
            <lang>
                <zh-CN>确认按钮只触发 ConfirmButton_Click；是否允许确认、目标绑定和幂等结果由服务器处理，按钮本身不代表资料已写入。</zh-CN>
                <en>The confirm button only triggers ConfirmButton_Click; the server handles permission, target binding, and idempotent outcome, so the button itself does not mean the profile was written.</en>
            </lang>
        --%>
        <div class="employee-profile-confirm-actions">
            <asp:Button ID="ConfirmButton" CssClass="CommandButton" Text="确认资料无误" OnClick="ConfirmButton_Click" runat="server" />
        </div>
    </asp:Panel>
</div>
