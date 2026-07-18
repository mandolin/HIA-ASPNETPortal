<%@ Page Language="c#" CodeBehind="EditAccessDenied.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EditAccessDenied" MasterPageFile="../Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 编辑权限拒绝页复用统一消息块，保留原资源文案。 --%>
    <div class="portal-static-message portal-static-message-warning">
        <div class="Head portal-static-message-title"><%=lang.Admin_EditAccessDenied_EditAccessDenied%></div>
        <div class="Normal portal-static-message-body"><%=lang.Admin_AccessDenied_DeniedAbout%></div>
        <a class="CommandButton portal-static-message-action" href="<%=Global.GetApplicationPath(Request)%>/DesktopDefault.aspx">
            <%=lang.Admin_AccessDenied_ReturnToHome%></a>
    </div>
</asp:Content>
