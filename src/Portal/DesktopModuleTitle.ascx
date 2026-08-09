<%@ Control CodeBehind="DesktopModuleTitle.ascx.cs" Language="c#" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.DesktopModuleTitle" %>

<%--
    <lang>
        <zh-CN>模块标题控件负责显示门户模块标题，并在配置了编辑页且当前用户具备编辑权限时显示模块动作入口。</zh-CN>
        <en>The module-title control displays each portal module title and, when an edit page is configured and the current user has edit permission, shows the module action entry.</en>
    </lang>
--%>

<%--
    <lang>
        <zh-CN>P8.3 将模块标题正式拆为标题区和动作区，其他模块可逐步复用同一套语义契约。</zh-CN>
        <en>P8.3 splits module headers into title and action areas so modules can gradually share one semantic contract.</en>
    </lang>
--%>
<div class="portal-module-header">
    <div class="portal-module-title-wrap">
        <%--
            <lang>
                <zh-CN>模块标题由服务器控件提供并关闭自身 ViewState，页面只呈现当前模块上下文。</zh-CN>
                <en>The server control supplies the module title with its own ViewState disabled; the page only presents the current module context.</en>
            </lang>
        --%>
        <asp:label id="ModuleTitle" cssclass="Head portal-module-title portal-module-heading" EnableViewState="false" runat="server" />
    </div>
    <%--
        <lang>
            <zh-CN>动作区是否可见由服务器根据编辑页配置和当前权限决定，不能由客户端样式或标记覆盖。</zh-CN>
            <en>The server decides action-area visibility from edit-page configuration and current permission; client styling or markup cannot override it.</en>
        </lang>
    --%>
    <asp:Panel id="ModuleActions" cssclass="portal-module-actions" EnableViewState="false" runat="server">
        <%--
            <lang>
                <zh-CN>编辑链接沿用服务器生成的目标和权限判断；没有有效编辑页时不应把它当作可用导航。</zh-CN>
                <en>The edit link keeps the server-generated target and permission decision; without a valid edit page it must not be treated as usable navigation.</en>
            </lang>
        --%>
        <asp:hyperlink id="EditButton" cssclass="CommandButton portal-module-action portal-secondary-action" EnableViewState="false" runat="server" />
    </asp:Panel>
</div>
