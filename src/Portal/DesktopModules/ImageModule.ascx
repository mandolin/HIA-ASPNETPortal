<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.ImageModule" CodeBehind="ImageModule.ascx.cs" AutoEventWireup="True" %>
<%--
    <lang>
        <zh-CN>共享模块标题控件承载受服务器权限控制的图片编辑入口；标记层不自行授予编辑能力。</zh-CN>
        <en>The shared module-title control hosts the image-edit entry under server permission control; the markup does not grant edit capability by itself.</en>
    </lang>
--%>
<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<%--
    <lang>
        <zh-CN>媒体容器只承载经过导航策略归一化的图片输出；非法地址由 code-behind 隐藏图片，不回显原始设置。</zh-CN>
        <en>The media container hosts only image output normalized by the navigation policy; code-behind hides invalid addresses instead of echoing raw settings.</en>
    </lang>
--%>
<portal:title EditText="Edit" EditUrl="~/DesktopModules/EditImage.aspx" runat="server" id=Title1 />

<div class="portal-content-media">
    <%--
        <lang>
            <zh-CN>图片地址和可选尺寸均由服务器从模块设置写入控件；标记层不处理上传、外链白名单或二进制资源。</zh-CN>
            <en>The server writes the image URL and optional dimensions from module settings; the markup does not process uploads, external-link allow lists, or binary resources.</en>
        </lang>
    --%>
    <asp:image id="Image1" CssClass="portal-content-image" runat="server" />
</div>
