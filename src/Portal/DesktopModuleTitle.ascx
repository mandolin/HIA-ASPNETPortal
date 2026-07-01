<%@ Control CodeBehind="DesktopModuleTitle.ascx.cs" Language="c#" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.DesktopModuleTitle" %>

<!-- 注释描述了该用户控件的作用，它负责在门户中显示每个模块的标题，以及在必要时显示模块的“编辑页面”链接（如果配置了的话） -->
<%--
   The PortalModuleTitle User Control is responsible for displaying the title of each
   portal module within the portal -- as well as optionally the module's "Edit Page"
   (if such a page has been configured).
--%>

<!-- HTML表格结构，用于布局模块标题和编辑按钮 -->
<table width="98%" cellspacing="0" cellpadding="0">
    <!-- 第一行 -->
    <tr>
        <!-- 左侧单元格用于显示模块标题 -->
        <td align="left">
            <!-- ASP.NET Label 控件，用于显示模块标题文本 -->
            <asp:label id="ModuleTitle" cssclass="Head" EnableViewState="false" runat="server" />
        </td>
        
        <!-- 右侧单元格用于显示编辑按钮 -->
        <td align="right">
            <!-- ASP.NET HyperLink 控件，模拟按钮效果，用于跳转到编辑页面 -->
            <asp:hyperlink id="EditButton" cssclass="CommandButton" EnableViewState="false" runat="server" />
        </td>
    </tr>
    
    <!-- 第二行，包含一条水平分割线，用于视觉上划分模块标题和主体内容 -->
    <tr>
        <td colspan="2">
            <hr noshade size="1">
        </td>
    </tr>
</table>