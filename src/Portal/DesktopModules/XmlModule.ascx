<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.XmlModule" CodeBehind="XmlModule.ascx.cs" AutoEventWireup="True" %>
<%--
    <lang>
        <zh-CN>共享模块标题控件承载受服务器权限控制的 XML 编辑入口；标记层不自行授予编辑能力。</zh-CN>
        <en>The shared module-title control hosts the XML-edit entry under server permission control; the markup does not grant edit capability by itself.</en>
    </lang>
--%>
<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<%--
    <lang>
        <zh-CN>XML 容器只为已通过受信任部署路径校验的站内资源提供输出区域；无效配置由服务器给出中性提示。</zh-CN>
        <en>The XML container provides an output area only for in-application resources that pass trusted-deployment path checks; invalid configuration receives a neutral server-side notice.</en>
    </lang>
--%>
<portal:title EditText="Edit" EditUrl="~/DesktopModules/EditXml.aspx" runat="server" id=Title1 />

<div class="portal-content-xml">
    <%--
        <lang>
            <zh-CN>DocumentSource 和 TransformSource 由 code-behind 在路径和文件存在性验证后设置；标记层不接受客户端路径或远程资源。</zh-CN>
            <en>Code-behind sets DocumentSource and TransformSource only after path and file-existence checks; the markup accepts neither client paths nor remote resources.</en>
        </lang>
    --%>
    <asp:xml id="xml1" runat="server" />
</div>
