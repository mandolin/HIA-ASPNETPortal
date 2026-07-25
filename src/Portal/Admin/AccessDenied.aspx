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
        <div class="Head portal-static-message-title"><%=lang.Admin_AccessDenied_AccessDenied%></div>
        <div class="Normal portal-static-message-body"><%=lang.Admin_AccessDenied_DeniedAbout%></div>
        <a class="CommandButton portal-static-message-action" href="<%=Global.GetApplicationPath(Request)%>/DesktopDefault.aspx">
            <%=lang.Admin_AccessDenied_ReturnToHome%></a>
    </div>
</asp:Content>
