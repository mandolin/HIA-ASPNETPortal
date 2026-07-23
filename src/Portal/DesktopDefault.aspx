<%@ Page Language="c#" CodeBehind="DesktopDefault.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.DesktopDefault"
    MasterPageFile="Default.master" %>

<%--

    The DesktopDefault.aspx page is used to load and populate each Portal View.  It accomplishes
    this by reading the layout configuration of the portal from the Portal Configuration
    system, and then using this information to dynamically instantiate portal modules
    (each implemented as an ASP.NET User Control), and then inject them into the page.

    DesktopDefault.aspx 页面用于加载和填充每个门户视图。它通过从门户配置系统中读取门户的布局配置信息，
    然后使用这些信息动态实例化门户模块（每个都实现为 ASP.NET 用户控件），
    然后将它们注入到页面中。

--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文：P7.4 使用现代分栏容器；CSS 提供现代浏览器增强与旧浏览器基本块级回退。English: P7.4 uses modern pane containers; CSS provides modern enhancement and basic block fallback for old browsers. --%>
    <div class="portal-dashboard-layout">
        <%-- <lang><zh-CN>左侧模块容器；仅在当前 Tab 配置了左侧模块时由 code-behind 显示。</zh-CN><en>Left module pane; code-behind shows it only when the current tab has left-pane modules.</en></lang> --%>
        <div id="LeftPane" class="portal-pane portal-pane-left" runat="server" visible="false"></div>
        <%-- <lang><zh-CN>主内容模块容器，是多数旧模块和业务模块的默认落点。</zh-CN><en>Main content pane, the default target for most legacy and business modules.</en></lang> --%>
        <div id="ContentPane" class="portal-pane portal-pane-content" runat="server" visible="false"></div>
        <%-- <lang><zh-CN>右侧模块容器；保留旧门户三栏布局契约，并由主题决定实际展现。</zh-CN><en>Right module pane; it preserves the legacy three-pane portal contract while themes decide the actual presentation.</en></lang> --%>
        <div id="RightPane" class="portal-pane portal-pane-right" runat="server" visible="false"></div>
    </div>
</asp:Content>
