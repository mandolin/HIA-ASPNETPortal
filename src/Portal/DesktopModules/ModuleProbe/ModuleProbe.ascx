<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ModuleProbe.ascx.cs" Inherits="ASPNET.StarterKit.Portal.ModuleProbe" %>

<%--
    <lang>
        <zh-CN>P3.2 只读模块包验证样例：不写入业务数据，不提供脚本、上传或外链入口。</zh-CN>
        <en>P3.2 is a read-only deployment-package verification sample; it writes no business data and exposes no script, upload, or external-link entry.</en>
    </lang>
--%>
<div class="module-probe">
    <div class="module-probe-title">Module Probe</div>
    <div class="module-probe-summary">Trusted deployment-package verification module.</div>
    <%--
        <lang>
            <zh-CN>表格字段只呈现服务器提供的包、模块、放置、主题范围和渲染时间诊断值，不把它们当作客户端可修改配置。</zh-CN>
            <en>The table only presents server-provided package, module, placement, theme-scope, and render-time diagnostics; these values are not client-editable configuration.</en>
        </lang>
    --%>
    <table class="module-probe-table" cellspacing="0" cellpadding="3" border="0">
        <%--
            <lang>
                <zh-CN>所有 Label 均为编码后的只读结果，探针页面不提供回调、保存或导航副作用。</zh-CN>
                <en>All labels are encoded read-only results; the probe page provides no callback, persistence, or navigation side effect.</en>
            </lang>
        --%>
        <tr>
            <td class="module-probe-label">Package:</td>
            <td><asp:Label ID="PackageLabel" runat="server" /></td>
        </tr>
        <tr>
            <td class="module-probe-label">Module:</td>
            <td><asp:Label ID="ModuleLabel" runat="server" /></td>
        </tr>
        <tr>
            <td class="module-probe-label">Placement:</td>
            <td><asp:Label ID="PlacementLabel" runat="server" /></td>
        </tr>
        <tr>
            <td class="module-probe-label">Theme Scope:</td>
            <td><asp:Label ID="ThemeScopeLabel" runat="server" /></td>
        </tr>
        <tr>
            <td class="module-probe-label">Rendered UTC:</td>
            <td><asp:Label ID="RenderedUtcLabel" runat="server" /></td>
        </tr>
    </table>
</div>
