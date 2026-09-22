<%@ Page CodeBehind="ModuleSettings.aspx.cs" Language="c#" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.ModuleSettingsPage" MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

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
                <h1 class="Head portal-admin-title"><%= lang.Admin_ModuleSettings_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_ModuleSettings_Subtitle %></p>
            </div>
            <%--
              <lang>
                <zh-CN>页面自有导航入口：员工目录的目标页在导航 registry 中尚无入口键，若改交渲染器会因分组无法解析而静默消失，故保留在页面内并只做文案本地化。</zh-CN>
                <en>Page-owned navigation entries: the employee-directory target has no navigation-registry entry yet, so handing it to the renderer would silently drop it when no group can be resolved; it stays in the page and is localized only.</en>
              </lang>
            --%>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="ModuleCatalog.aspx"><%= lang.Admin_ModuleSettings_LinkModuleCatalog %></a>
                <a class="CommandButton" href="EmployeeDirectory.aspx"><%= lang.Admin_ModuleSettings_LinkEmployeeDirectory %></a>
            </div>
        </div>

        <asp:Label ID="Message" CssClass="NormalRed portal-status-line" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_ModuleSettings_SectionInstanceMetadata %></h2>
            </div>
            <%--
              <lang>
                <zh-CN>实例元数据输入只表达标题、秒数和移动兼容意图；实例归属、范围、缓存单位和合法值仍由 code-behind 校验。</zh-CN>
                <en>Instance metadata inputs express only title, seconds, and mobile-compatibility intent; code-behind validates instance ownership, ranges, cache units, and legal values.</en>
              </lang>
            --%>
            <div class="portal-form-grid">
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_ModuleSettings_LabelModuleName %></span>
                    <asp:TextBox ID="moduleTitle" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_ModuleSettings_LabelCacheTimeout %></span>
                    <asp:TextBox ID="cacheTime" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field portal-checkbox-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_ModuleSettings_LabelMobileCompatibility %></span>
                    <asp:CheckBox ID="showMobile" Text="<%$ Resources:lang, Admin_ModuleSettings_CheckboxShowToMobileUsers %>" runat="server" />
                </div>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_ModuleSettings_SectionEditorRoles %></h2>
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
                <asp:LinkButton CssClass="CommandButton portal-primary-action" Text="<%$ Resources:lang, Admin_ModuleSettings_ButtonApplyModuleChanges %>" runat="server"
                    ID="ApplyButton" OnClick="ApplyChanges_Click" />
            </div>
        </div>
    </div>
</asp:Content>
