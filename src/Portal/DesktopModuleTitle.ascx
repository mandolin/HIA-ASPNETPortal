<%@ Control CodeBehind="DesktopModuleTitle.ascx.cs" Language="c#" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.DesktopModuleTitle" %>

<!-- 注释描述了该用户控件的作用，它负责在门户中显示每个模块的标题，以及在必要时显示模块的“编辑页面”链接（如果配置了的话） -->
<%--
   The PortalModuleTitle User Control is responsible for displaying the title of each
   portal module within the portal -- as well as optionally the module's "Edit Page"
   (if such a page has been configured).
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
