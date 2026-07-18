<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.HtmlModule" CodeBehind="HtmlModule.ascx.cs" AutoEventWireup="True" %>
<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<portal:title EditText="Edit" EditUrl="~/DesktopModules/EditHtml.aspx" runat="server" id=Title1 />

<%-- 中文：原始 HTML 仍由 code-behind 按受信任管理员边界注入，这里只提供主题化展示容器。English: Raw HTML is still injected by code-behind under the trusted-admin boundary; this markup only supplies the themed display container. --%>
<div id="HtmlHolder" class="portal-content-html" runat="server"></div>
