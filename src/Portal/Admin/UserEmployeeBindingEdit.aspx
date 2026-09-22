<%@ Page
    Language="c#"
    CodeBehind="UserEmployeeBindingEdit.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.UserEmployeeBindingEdit"
    MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

<%--
    <lang>
        <zh-CN>P6.3-S5 门户账号与员工单条绑定维护页。</zh-CN>
        <en>P6.3-S5 single binding maintenance page between a portal account and an employee record.</en>
    </lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
        <lang>
            <zh-CN>绑定页只重构展示壳，绑定、解绑、安全版本刷新和审计逻辑仍由 code-behind 处理。</zh-CN>
            <en>The binding page only rebuilds the presentation shell; binding, unbinding, security-version refresh, and audit logic remain in code-behind.</en>
        </lang>
    --%>
    <div class="portal-admin-page portal-admin-user-employee-binding">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title"><%= lang.Admin_UserEmployeeBindingEdit_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_UserEmployeeBindingEdit_Subtitle %></p>
            </div>
            <%--
              <lang>
                <zh-CN>页面自有导航入口：User Administration 属 Admin.Account 族，而本页入口属 Admin.Capability 族；渲染器按族输出、无法表达该跨族链接，故整块保留在页面内并只做文案本地化。ManageUserLink 由 code-behind 决定可见性与目标，此处只本地化其文案。</zh-CN>
                <en>Page-owned navigation entries: User Administration belongs to Admin.Account while this page belongs to Admin.Capability; the group-scoped renderer cannot express that cross-group link, so the block stays in the page and is localized only. ManageUserLink has its visibility and target decided by code-behind, so only its text is localized here.</en>
              </lang>
            --%>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="EmployeeDirectory.aspx"><%= lang.Admin_UserEmployeeBindingEdit_LinkEmployeeDirectory %></a>
                <a class="CommandButton" href="ManageUsers.aspx"><%= lang.Admin_UserEmployeeBindingEdit_LinkUserAdministration %></a>
                <asp:HyperLink ID="ManageUserLink" CssClass="CommandButton" Text="<%$ Resources:lang, Admin_UserEmployeeBindingEdit_LinkManageUser %>" Visible="false" runat="server" />
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />
        <%--
          <lang>
            <zh-CN>ActiveBindingId 只是服务端状态的回发载体，客户端可修改其值；绑定结束和版本校验不能信任该隐藏字段。</zh-CN>
            <en>ActiveBindingId is only a postback carrier for server state and can be modified by the client; unbinding and version checks must not trust this hidden field.</en>
          </lang>
        --%>
        <asp:HiddenField ID="ActiveBindingId" runat="server" />

        <div class="portal-status-strip">
            <div class="SubHead portal-status-line"><%= lang.Admin_UserEmployeeBindingEdit_LabelCurrentBinding %></div>
            <div class="Normal portal-status-line">
                <asp:Label ID="CurrentBindingText" runat="server" />
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_UserEmployeeBindingEdit_SectionBindingOperation %></h2>
            </div>
            <%--
              <lang>
                <zh-CN>用户、员工和原因输入受标记层长度上限约束，但格式、存在性、唯一性、当前绑定和审计要求仍由 code-behind 校验。</zh-CN>
                <en>User, employee, and reason inputs have markup-level length limits, while code-behind still validates format, existence, uniqueness, current binding, and audit requirements.</en>
              </lang>
            --%>
            <div class="portal-form-grid">
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_UserEmployeeBindingEdit_LabelPortalUserId %></span>
                    <asp:TextBox ID="UserIdTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="12" runat="server" />
                    <span class="Normal portal-field-value"><asp:Label ID="UserSummaryText" runat="server" /></span>
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_UserEmployeeBindingEdit_LabelEmployeeCode %></span>
                    <asp:TextBox ID="EmployeeCodeTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="64" runat="server" />
                    <span class="Normal portal-field-value"><asp:Label ID="EmployeeSummaryText" runat="server" /></span>
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_UserEmployeeBindingEdit_LabelReason %></span>
                    <asp:TextBox ID="ReasonTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="200" runat="server" />
                </div>
            </div>
            <%--
              <lang>
                <zh-CN>绑定和结束绑定按钮只触发服务端命令；浏览器确认仅降低误操作，不替代管理员授权、并发版本和事务审计检查。</zh-CN>
                <en>The bind and end-binding buttons only trigger server commands; browser confirmation reduces accidental actions but does not replace administrator authorization, concurrency-version, or transactional-audit checks.</en>
              </lang>
            --%>
            <div class="portal-form-actions">
                <asp:LinkButton ID="BindButton" CssClass="CommandButton portal-primary-action" Text="<%$ Resources:lang, Admin_UserEmployeeBindingEdit_ButtonBindUserToEmployee %>"
                    OnClick="BindButton_Click" runat="server" />
                <%--
                  <lang>
                    <zh-CN>结束绑定按钮的 OnClientClick 原先硬编码中文确认文案，英文界面下会显示中文；现整体取自资源（该资源值必须保持为合法的 JS 语句，且消息文本使用单引号，避免破坏标记属性）。</zh-CN>
                    <en>The end-binding button previously hardcoded Chinese confirmation text, which showed Chinese even in an English UI; the whole statement now comes from a resource (the value must stay a valid JS statement with single-quoted text so the markup attribute is not broken).</en>
                  </lang>
                --%>
                <asp:LinkButton ID="EndBindingButton" CssClass="CommandButton" Text="<%$ Resources:lang, Admin_UserEmployeeBindingEdit_ButtonEndActiveBinding %>"
                    CausesValidation="False" OnClick="EndBindingButton_Click"
                    OnClientClick="<%$ Resources:lang, Admin_UserEmployeeBindingEdit_ConfirmEndBinding %>" runat="server" />
            </div>
        </div>
    </div>
</asp:Content>
