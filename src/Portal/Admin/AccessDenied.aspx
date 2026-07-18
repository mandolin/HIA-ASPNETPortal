<%@ Page Language="c#" CodeBehind="AccessDenied.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.AccessDenied" MasterPageFile="../Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 静态拒绝访问页改为主题化消息块，避免继续使用旧固定宽度表格。 --%>
    <div class="portal-static-message portal-static-message-warning">
        <div class="Head portal-static-message-title"><%=lang.Admin_AccessDenied_AccessDenied%></div>
        <div class="Normal portal-static-message-body"><%=lang.Admin_AccessDenied_DeniedAbout%></div>
        <a class="CommandButton portal-static-message-action" href="<%=Global.GetApplicationPath(Request)%>/DesktopDefault.aspx">
            <%=lang.Admin_AccessDenied_ReturnToHome%></a>
    </div>
</asp:Content>
