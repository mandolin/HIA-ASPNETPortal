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
<%-- #todo 改造整体结构，使其适应各种自定义主题/皮肤及其动态切换 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table cellspacing="0" cellpadding="4" width="100%" border="0">
        <tbody>
            <tr valign="top" height="*">
                <td width="5">&nbsp;
                </td>
                <!-- 左侧面板 -->
                <td id="LeftPane" width="170" runat="server" visible="false"></td>
                <td width="1"></td>
                <!-- 内容面板 -->
                <td id="ContentPane" width="*" runat="server" visible="false"></td>
                <!-- 右侧面板 -->
                <td id="RightPane" width="230" runat="server" visible="false"></td>
                <td width="10">&nbsp;
                </td>
            </tr>
        </tbody>
    </table>
</asp:Content>
