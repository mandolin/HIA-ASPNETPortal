<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.HtmlModule" CodeBehind="HtmlModule.ascx.cs" AutoEventWireup="True" %>
<%--
    <lang>
        <zh-CN>共享模块标题控件负责承载编辑入口和主题化标题；其是否输出动作仍由服务器权限上下文决定。</zh-CN>
        <en>The shared module-title control hosts the edit entry and themed title; whether the action is rendered remains a server-side permission decision.</en>
    </lang>
--%>
<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<%--
    <lang>
        <zh-CN>编辑地址只连接到既有 HTML 编辑页，标记层不把普通用户输入或外部资源接入受信任 HTML 渲染路径。</zh-CN>
        <en>The edit address points only to the existing HTML editor; the markup does not route general-user input or external resources into the trusted-HTML rendering path.</en>
    </lang>
--%>
<portal:title EditText="Edit" EditUrl="~/DesktopModules/EditHtml.aspx" runat="server" id=Title1 />

<%--
    <lang>
        <zh-CN>原始 HTML 仍由 code-behind 按受信任管理员边界注入，这里只提供主题化展示容器。</zh-CN>
        <en>Raw HTML is still injected by code-behind under the trusted-admin boundary; this markup only supplies the themed display container.</en>
    </lang>
--%>
<%--
    <lang>
        <zh-CN>HtmlHolder 只是 code-behind 注入受信任内容的服务器容器；没有有效记录时保持空容器，不向客户端泄露原始配置值。</zh-CN>
        <en>HtmlHolder is only the server container for trusted code-behind content; without a valid record it stays empty and does not disclose raw configuration values to the client.</en>
    </lang>
--%>
<div id="HtmlHolder" class="portal-content-html" runat="server"></div>
