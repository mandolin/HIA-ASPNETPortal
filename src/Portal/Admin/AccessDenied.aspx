<%@ Page Language="c#" CodeBehind="AccessDenied.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.AccessDenied" MasterPageFile="../Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
      <lang>
        <zh-CN>静态拒绝访问页采用主题化消息块，避免继续使用旧固定宽度表格。</zh-CN>
        <en>The static access-denied page uses a themed message block instead of the old fixed-width table.</en>
      </lang>
    --%>
    <div class="portal-static-message portal-static-message-warning">
        <%--
          <lang>
            <zh-CN>标题和说明使用资源文本输出拒绝原因；页面只展示服务器决定的结果，不尝试解释或放宽权限。</zh-CN>
            <en>The title and explanation render resource text for the denial; the page only presents the server decision and does not interpret or relax authorization.</en>
          </lang>
        --%>
        <div class="Head portal-static-message-title"><%=lang.Admin_AccessDenied_AccessDenied%></div>
        <div class="Normal portal-static-message-body"><%=lang.Admin_AccessDenied_DeniedAbout%></div>
        <%--
          <lang>
            <zh-CN>返回链接只回到应用生成的门户根路径，不把拒绝页当作可执行管理入口。</zh-CN>
            <en>The return link targets only the application-generated portal root and does not turn the denial page into an executable administration entry.</en>
          </lang>
        --%>
        <a class="CommandButton portal-static-message-action" href="<%=Global.GetApplicationPath(Request)%>/DesktopDefault.aspx">
            <%=lang.Admin_AccessDenied_ReturnToHome%></a>
    </div>
</asp:Content>
