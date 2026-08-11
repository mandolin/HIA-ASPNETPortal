<%@ Page Language="c#" CodeBehind="GenericErrorPage.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.GenericErrorPage" %>
<%--
   <lang>
     <zh-CN>通用错误页是异常收敛出口；页面只显示低敏事件编号，避免把堆栈、路径或请求细节输出给客户端。</zh-CN>
     <en>The generic error page is the exception-convergence endpoint; it only displays a low-sensitivity event identifier so stack traces, paths, or request details are not sent to the client.</en>
   </lang>
--%>
<%--
   <lang>
     <zh-CN>此页不使用母版页，确保错误路径在主题、导航或模块初始化失败时仍能渲染一份最小 HTML 响应。</zh-CN>
     <en>This page does not use the master page, ensuring the error path can still render a minimal HTML response when theme, navigation, or module initialization fails.</en>
   </lang>
--%>
<!DOCTYPE html>
<html>
<head runat="server">
    <meta charset="utf-8" />
    <title>&#x5E94;&#x7528;&#x7A0B;&#x5E8F;&#x9519;&#x8BEF;</title>
    <style>
        body {
            margin: 0;
            padding: 40px;
            color: #333;
            background: #f6f7f9;
            font-family: sans-serif;
        }

        .error-panel {
            max-width: 720px;
            padding: 24px;
            border: 1px solid #d8dde6;
            background: #fff;
        }

        .error-title {
            margin: 0 0 16px;
            color: #9b1c1c;
            font-size: 22px;
        }

        .error-id {
            padding: 8px 10px;
            border: 1px solid #e5e7eb;
            background: #f9fafb;
            font-family: monospace;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <%--
           <lang>
             <zh-CN>面板文本使用实体编码和服务端事件编号表达式；视觉样式保持内联最小化，避免依赖外部主题资产。</zh-CN>
             <en>The panel text uses entity encoding and a server-side event-id expression; visual styling stays minimal and inline to avoid depending on external theme assets.</en>
           </lang>
        --%>
        <div class="error-panel">
            <h1 class="error-title">&#x5E94;&#x7528;&#x7A0B;&#x5E8F;&#x6682;&#x65F6;&#x65E0;&#x6CD5;&#x5B8C;&#x6210;&#x8BF7;&#x6C42;</h1>
            <p>&#x7CFB;&#x7EDF;&#x5DF2;&#x8BB0;&#x5F55;&#x672C;&#x6B21;&#x9519;&#x8BEF;&#x3002;&#x8BF7;&#x7A0D;&#x540E;&#x91CD;&#x8BD5;&#xFF0C;&#x6216;&#x5C06;&#x4E0B;&#x9762;&#x7684;&#x4E8B;&#x4EF6;&#x7F16;&#x53F7;&#x63D0;&#x4F9B;&#x7ED9;&#x7BA1;&#x7406;&#x5458;&#x3002;</p>
            <p class="error-id">&#x4E8B;&#x4EF6;&#x7F16;&#x53F7;&#xFF1A;<%= EventIdText %></p>
            <p><a href="Default.aspx">&#x8FD4;&#x56DE;&#x9996;&#x9875;</a></p>
        </div>
    </form>
</body>
</html>
