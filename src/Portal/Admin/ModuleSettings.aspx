<%@ Page CodeBehind="ModuleSettings.aspx.cs" Language="c#" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.ModuleSettingsPage" MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
    <lang>
      <zh-CN>模块实例设置页只重构展示壳和表单分组，实例归属校验、编辑角色保存、缓存策略写入和审计仍由 code-behind 控制。</zh-CN>
      <en>The module-instance settings page only rebuilds the presentation shell and form grouping; instance ownership checks, editor-role persistence, cache-policy writes, and auditing remain controlled by the code-behind.</en>
    </lang>
    --%>
    <div class="portal-admin-page portal-admin-module-settings">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Module Settings</h1>
                <p class="Normal portal-admin-subtitle">Edit a module instance title, cache policy, editor roles, and legacy mobile visibility.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="ModuleCatalog.aspx">Module Catalog</a>
                <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
            </div>
        </div>

        <asp:Label ID="Message" CssClass="NormalRed portal-status-line" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Instance Metadata</h2>
            </div>
            <%--
              <lang>
                <zh-CN>实例元数据输入只表达标题、秒数和移动兼容意图；实例归属、范围、缓存单位和合法值仍由 code-behind 校验。</zh-CN>
                <en>Instance metadata inputs express only title, seconds, and mobile-compatibility intent; code-behind validates instance ownership, ranges, cache units, and legal values.</en>
              </lang>
            --%>
            <div class="portal-form-grid">
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Module Name</span>
                    <asp:TextBox ID="moduleTitle" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Cache Timeout (seconds)</span>
                    <asp:TextBox ID="cacheTime" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field portal-checkbox-field">
                    <span class="SubHead portal-form-label">Mobile Compatibility</span>
                    <asp:CheckBox ID="showMobile" Text="Show to mobile users" runat="server" />
                </div>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Editor Roles</h2>
            </div>
            <%--
              <lang>
                <zh-CN>编辑角色列表只呈现可选角色键；角色是否属于当前实例、是否允许写入以及变更审计仍由服务端重新判断。</zh-CN>
                <en>The editor-role list only renders selectable role keys; the server rechecks instance membership, write permission, and change auditing.</en>
              </lang>
            --%>
            <div class="portal-chip-list-wrap">
                <asp:CheckBoxList ID="authEditRoles" RepeatColumns="2"
                    CssClass="portal-chip-list" CellPadding="0" CellSpacing="0" runat="server" />
            </div>
            <%--
              <lang>
                <zh-CN>应用按钮提交整组模块设置，事务、编辑角色授权、缓存策略写入和审计事件均由 ApplyChanges_Click 负责。</zh-CN>
                <en>The apply button submits the complete module-settings set; ApplyChanges_Click owns the transaction, editor-role authorization, cache-policy write, and audit event.</en>
              </lang>
            --%>
            <div class="portal-form-actions">
                <asp:LinkButton CssClass="CommandButton portal-primary-action" Text="Apply Module Changes" runat="server"
                    ID="ApplyButton" OnClick="ApplyChanges_Click" />
            </div>
        </div>
    </div>
</asp:Content>
