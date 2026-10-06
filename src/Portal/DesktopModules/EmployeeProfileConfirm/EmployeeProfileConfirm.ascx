<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EmployeeProfileConfirm.ascx.cs" Inherits="ASPNET.StarterKit.Portal.EmployeeProfileConfirm" %>

<%--
    <lang>
        <zh-CN>P6.4 首批业务模块样板：员工只确认自己当前绑定的低敏资料，不提供上传、脚本或外部资源。</zh-CN>
        <en>P6.4 first business module sample: employees only confirm their currently bound low-sensitivity profile data; no upload, script, or external resource capability is provided.</en>
    </lang>
--%>
<div class="employee-profile-confirm">
    <%--
    <lang>
        <zh-CN>标题语义（W87 补）：同 EnterpriseCapabilityWorkbench —— 模块自带标题 div，显式补
        role="heading" 与 aria-level="1"，只加属性不改 class，视觉零变化。</zh-CN>
        <en>Title semantics (added in W87): as in EnterpriseCapabilityWorkbench — the module renders its own title div, so
        role="heading" and aria-level="1" are set explicitly; attributes only, no class change, no visual change.</en>
    </lang>
--%>
<div class="employee-profile-confirm-title" role="heading" aria-level="1"><%= lang.EmployeeProfileConfirm_Heading %></div>
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
        <dl class="employee-profile-field-grid">
            <div class="employee-profile-field">
                <dt class="employee-profile-confirm-label employee-profile-field-label"><%= lang.EmployeeProfileConfirm_LabelEmployeeCode %></dt>
                <dd class="employee-profile-field-value"><asp:Label ID="EmployeeCodeLabel" runat="server" /></dd>
            </div>
            <div class="employee-profile-field">
                <dt class="employee-profile-confirm-label employee-profile-field-label"><%= lang.EmployeeProfileConfirm_LabelName %></dt>
                <dd class="employee-profile-field-value"><asp:Label ID="DisplayNameLabel" runat="server" /></dd>
            </div>
            <div class="employee-profile-field">
                <dt class="employee-profile-confirm-label employee-profile-field-label"><%= lang.EmployeeProfileConfirm_LabelSalutation %></dt>
                <dd class="employee-profile-field-value"><asp:Label ID="PreferredNameLabel" runat="server" /></dd>
            </div>
            <div class="employee-profile-field">
                <dt class="employee-profile-confirm-label employee-profile-field-label"><%= lang.EmployeeProfileConfirm_LabelWorkEmail %></dt>
                <dd class="employee-profile-field-value"><asp:Label ID="WorkEmailLabel" runat="server" /></dd>
            </div>
            <div class="employee-profile-field">
                <dt class="employee-profile-confirm-label employee-profile-field-label"><%= lang.EmployeeProfileConfirm_LabelOrganization %></dt>
                <dd class="employee-profile-field-value"><asp:Label ID="OrganizationLabel" runat="server" /></dd>
            </div>
            <%--
                <lang>
                    <zh-CN>P74.2 起"在职状态"改用本模块自有键 EmployeeProfileConfirm_LabelEmploymentStatus：此前借用资料更正模块的"状态列"标题，属**语义借用**（用列表列标题表达在职状态），任一方改文案都会误伤对方。文案值保持"状态"不变，属纯键归属整理；把文案改为更明确的"在职状态"属可见文案变化，另作候选登记。</zh-CN>
                    <en>Since P74.2 the employment-status field uses this module's own key EmployeeProfileConfirm_LabelEmploymentStatus; it previously borrowed the correction module's list-column title, which is a **semantic borrow** (a list column title standing in for employment status) that made either side's wording change break the other. The value stays "状态", so this is pure key-ownership cleanup; renaming it to a clearer "在职状态" is a visible wording change and is registered as a separate candidate.</en>
                </lang>
            --%>
            <div class="employee-profile-field">
                <dt class="employee-profile-confirm-label employee-profile-field-label"><%= lang.EmployeeProfileConfirm_LabelEmploymentStatus %></dt>
                <dd class="employee-profile-field-value"><asp:Label ID="EmploymentStatusLabel" runat="server" /></dd>
            </div>
            <div class="employee-profile-field employee-profile-field-wide">
                <dt class="employee-profile-confirm-label employee-profile-field-label"><%= lang.EmployeeProfileConfirm_LabelLastConfirmed %></dt>
                <dd class="employee-profile-field-value"><asp:Label ID="LastConfirmedLabel" runat="server" /></dd>
            </div>
        </dl>

        <%--
            <lang>
                <zh-CN>确认按钮只触发 ConfirmButton_Click；是否允许确认、目标绑定和幂等结果由服务器处理，按钮本身不代表资料已写入。</zh-CN>
                <en>The confirm button only triggers ConfirmButton_Click; the server handles permission, target binding, and idempotent outcome, so the button itself does not mean the profile was written.</en>
            </lang>
        --%>
        <div class="employee-profile-confirm-actions">
            <asp:Button ID="ConfirmButton" CssClass="CommandButton" Text="<%$ Resources:lang,EmployeeProfileConfirm_ButtonConfirm %>" OnClick="ConfirmButton_Click" runat="server" />
        </div>
    </asp:Panel>
</div>
