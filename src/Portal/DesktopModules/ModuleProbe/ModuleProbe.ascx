<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ModuleProbe.ascx.cs" Inherits="ASPNET.StarterKit.Portal.ModuleProbe" %>

<%-- P3.2 只读模块包验证样例：不写入业务数据，不提供脚本、上传或外链入口。 --%>
<div class="module-probe">
    <div class="module-probe-title">Module Probe</div>
    <div class="module-probe-summary">Trusted deployment-package verification module.</div>
    <table class="module-probe-table" cellspacing="0" cellpadding="3" border="0">
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
