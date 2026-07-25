<%@ Page Language="c#" CodeBehind="NotImplemented.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.NotImplemented" MasterPageFile="../Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
    <lang>
      <zh-CN>未实现示例链接页改为统一提示卡，用于样例数据链接的低风险落地页；动态主题切换后不再使用旧 OutputCache，避免缓存错误主题。</zh-CN>
      <en>The not-implemented sample-link page now uses the unified message card as a low-risk landing page for sample-data links; after dynamic theme switching it no longer uses the old OutputCache, avoiding cached wrong themes.</en>
    </lang>
    --%>
    <div class="portal-static-message portal-static-message-info">
        <div class="Head portal-static-message-title" id="title" runat="server">Linked Content Not Provided</div>
        <div class="Normal portal-static-message-body">
            The link you clicked was provided as a part of the sample data for the <b>ASP.NET Portal
                Starter Kit</b>. The content for this link is not provided as part of the sample
            application.
        </div>
        <a class="CommandButton portal-static-message-action" href="<%=Global.GetApplicationPath(Request)%>/DesktopDefault.aspx">
            Return to ASP.NET Portal Starter Kit Home</a>
    </div>
</asp:Content>
