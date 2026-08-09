<%@ Page Language="c#" CodeBehind="EditAccessDenied.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EditAccessDenied" MasterPageFile="../Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
      <lang>
        <zh-CN>编辑权限拒绝页复用统一消息块，并保留原有资源文案。</zh-CN>
        <en>The edit-access-denied page reuses the shared message block while preserving the existing resource text.</en>
      </lang>
    --%>
    <div class="portal-static-message portal-static-message-warning">
        <%--
          <lang>
            <zh-CN>编辑拒绝页复用资源化标题和说明，只呈现服务器确认的编辑权限结果，不泄露被保护对象的额外信息。</zh-CN>
            <en>The edit-denied page reuses resource-based title and explanation, presenting the server-confirmed edit result without disclosing extra details about the protected object.</en>
          </lang>
        --%>
        <div class="Head portal-static-message-title"><%=lang.Admin_EditAccessDenied_EditAccessDenied%></div>
        <div class="Normal portal-static-message-body"><%=lang.Admin_AccessDenied_DeniedAbout%></div>
        <%--
          <lang>
            <zh-CN>返回门户链接只提供安全的离开路径；是否允许再次进入编辑流程仍由目标页服务器校验。</zh-CN>
            <en>The portal return link provides only a safe exit path; any later edit attempt remains subject to server checks on the target page.</en>
          </lang>
        --%>
        <a class="CommandButton portal-static-message-action" href="<%=Global.GetApplicationPath(Request)%>/DesktopDefault.aspx">
            <%=lang.Admin_AccessDenied_ReturnToHome%></a>
    </div>
</asp:Content>
