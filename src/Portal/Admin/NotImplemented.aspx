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
        <%--
        <lang>
          <zh-CN>标题由服务器控件保留为样例落地页标识；该页不加载未实现模块，也不执行链接携带的业务命令。</zh-CN>
          <en>The server control keeps the title as the sample landing-page identifier; this page does not load an unimplemented module or execute a business command carried by the link.</en>
        </lang>
        --%>
        <%--
        <lang>
          <zh-CN>标题元素由服务器控制，默认标题由 code-behind 从资源写入；此处不能放代码块，否则设置 InnerText 会因"内容非字面"而抛异常。</zh-CN>
          <en>The title element is server-controlled and its default text is written by code-behind from resources; a code block must not be placed here, otherwise setting InnerText throws because the contents are not literal.</en>
        </lang>
        --%>
        <div class="Head portal-static-message-title" id="title" runat="server"></div>
        <%--
        <lang>
          <zh-CN>正文只解释样例链接没有提供内容，保持静态信息边界，不把样例数据当作可用业务资源。</zh-CN>
          <en>The body only explains that sample-linked content is not provided, keeping a static-information boundary rather than treating sample data as an available business resource.</en>
        </lang>
        --%>
        <div class="Normal portal-static-message-body">
            <%= lang.Admin_NotImplemented_BodyPre %> <b><%= lang.Admin_NotImplemented_Brand %></b><%= lang.Admin_NotImplemented_BodyPost %></div>
        <%--
        <lang>
          <zh-CN>返回首页链接使用应用路径生成，离开落地页但不改变任何配置或业务状态。</zh-CN>
          <en>The home link uses the application path and only leaves the landing page; it does not change configuration or business state.</en>
        </lang>
        --%>
        <a class="CommandButton portal-static-message-action" href="<%=Global.GetApplicationPath(Request)%>/DesktopDefault.aspx"><%= lang.Admin_NotImplemented_ReturnHome %></a>
    </div>
</asp:Content>
