<%@ Control CodeBehind="DesktopModuleTitle.ascx.cs" Language="c#" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.DesktopModuleTitle" %>

<!-- 注释描述了该用户控件的作用，它负责在门户中显示每个模块的标题，以及在必要时显示模块的“编辑页面”链接（如果配置了的话） -->
<%--
   The PortalModuleTitle User Control is responsible for displaying the title of each
   portal module within the portal -- as well as optionally the module's "Edit Page"
   (if such a page has been configured).
--%>

<%-- 中文：P7.4 模块标题改为语义化容器，避免标题栏继续受旧 table 结构限制。English: P7.4 uses semantic containers for module titles so the header is no longer constrained by legacy table layout. --%>
<div class="portal-module-header">
    <asp:label id="ModuleTitle" cssclass="Head portal-module-heading" EnableViewState="false" runat="server" />
    <asp:hyperlink id="EditButton" cssclass="CommandButton portal-module-action" EnableViewState="false" runat="server" />
</div>
