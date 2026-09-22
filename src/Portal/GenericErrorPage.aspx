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
    <title><%= lang.GenericError_Title %></title>
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
             <zh-CN>面板文本改为资源驱动：原先使用实体编码的硬编码中文，导致英文界面下错误页仍显示中文（与本地化目标冲突）。实体编码不再必要，因为文案已移入 UTF-8 且带编码声明的资源文件。事件编号仍由服务端表达式输出；视觉样式保持内联最小化，避免依赖外部主题资产。</zh-CN>
             <en>The panel text is now resource-driven: it previously used entity-encoded hard-coded Chinese, which made the error page display Chinese even in the English UI and conflicted with the localization goal. Entity encoding is no longer needed because the copy now lives in a UTF-8 resource file with an encoding declaration. The event id still comes from a server-side expression, and visual styling stays minimal and inline to avoid depending on external theme assets.</en>
           </lang>
        --%>
        <div class="error-panel">
            <h1 class="error-title"><%= lang.GenericError_Heading %></h1>
            <p><%= lang.GenericError_Body %></p>
            <p class="error-id"><%= lang.GenericError_EventIdLabel %><%= EventIdText %></p>
            <p><a href="Default.aspx"><%= lang.GenericError_ReturnHome %></a></p>
        </div>
    </form>
</body>
</html>
