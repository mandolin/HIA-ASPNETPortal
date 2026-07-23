<%@ Control CodeBehind="DesktopModuleTitle.ascx.cs" Language="c#" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.DesktopModuleTitle" %>

<%--
    <lang>
        <zh-CN>模块标题控件负责显示门户模块标题，并在配置了编辑页且当前用户具备编辑权限时显示模块动作入口。</zh-CN>
        <en>The module-title control displays each portal module title and, when an edit page is configured and the current user has edit permission, shows the module action entry.</en>
    </lang>
--%>

<%-- 中文：P8.3 模块标题正式拆为标题区和动作区，后续模块可逐步复用同一套语义契约。
     English: P8.3 splits module headers into title and action areas so modules can gradually share one semantic contract. --%>
<div class="portal-module-header">
    <div class="portal-module-title-wrap">
        <asp:label id="ModuleTitle" cssclass="Head portal-module-title portal-module-heading" EnableViewState="false" runat="server" />
    </div>
    <asp:Panel id="ModuleActions" cssclass="portal-module-actions" EnableViewState="false" runat="server">
        <asp:hyperlink id="EditButton" cssclass="CommandButton portal-module-action portal-secondary-action" EnableViewState="false" runat="server" />
    </asp:Panel>
</div>
