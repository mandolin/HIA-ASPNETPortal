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
        <!-- 左侧面板 / Left pane -->
        <div id="LeftPane" class="portal-pane portal-pane-left" runat="server" visible="false"></div>
        <!-- 内容面板 / Content pane -->
        <div id="ContentPane" class="portal-pane portal-pane-content" runat="server" visible="false"></div>
        <!-- 右侧面板 / Right pane -->
        <div id="RightPane" class="portal-pane portal-pane-right" runat="server" visible="false"></div>
    </div>
</asp:Content>
