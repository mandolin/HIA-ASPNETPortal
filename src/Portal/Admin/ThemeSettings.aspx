<%@ Page
    Language="c#"
    CodeBehind="ThemeSettings.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.ThemeSettings"
    MasterPageFile="~/Default.master" %>

<%--
    <lang>
        <zh-CN>P3.1 主题选择页：管理员仅能选择已部署且通过 manifest 校验的主题，不提供包上传或在线样式编辑。</zh-CN>
        <en>P3.1 theme selection page: administrators can only select deployed themes that pass manifest validation; package upload and online style editing are not provided.</en>
    </lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
        <lang>
            <zh-CN>主题设置页只调整展示结构，主题写入和审计仍由 code-behind 处理。</zh-CN>
            <en>The theme settings page only adjusts the presentation structure; theme writes and audit recording remain in code-behind.</en>
        </lang>
    --%>
    <div class="portal-admin-page portal-admin-theme-settings">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Theme Settings</h1>
                <p class="Normal portal-admin-subtitle">Select trusted deployed themes for the portal and individual tabs.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="SystemHealth.aspx">System Health</a>
                <a class="CommandButton" href="ModuleCatalog.aspx">Module Catalog</a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Global Theme</h2>
            </div>
            <div class="portal-filter-panel">
                <div class="portal-filter-grid">
                    <div class="portal-filter-field">
                        <span class="SubHead portal-filter-label">Global Theme</span>
                        <asp:DropDownList ID="GlobalThemeList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                    </div>
                    <div class="portal-filter-actions">
                        <asp:LinkButton ID="SaveGlobalThemeButton" CssClass="CommandButton portal-primary-action" Text="Apply" OnClick="SaveGlobalThemeButton_Click" runat="server" />
                        <asp:LinkButton ID="ResetGlobalThemeButton" CssClass="CommandButton portal-secondary-action" Text="Reset Override" CausesValidation="False" OnClick="ResetGlobalThemeButton_Click" runat="server" />
                    </div>
                </div>
                <div class="Normal portal-status-line">
                    <span class="SubHead">Effective Source:</span>
                    <asp:Label ID="GlobalThemeStatusLabel" runat="server" />
                </div>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Tab Override</h2>
            </div>
            <div class="portal-filter-panel">
                <div class="portal-filter-grid">
                    <div class="portal-filter-field">
                        <span class="SubHead portal-filter-label">Portal Tab</span>
                        <asp:DropDownList ID="TabList" CssClass="NormalTextBox portal-filter-input" AutoPostBack="True" OnSelectedIndexChanged="TabList_SelectedIndexChanged" runat="server" />
                    </div>
                    <div class="portal-filter-field">
                        <span class="SubHead portal-filter-label">Override Theme</span>
                        <asp:DropDownList ID="TabThemeList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                    </div>
                    <div class="portal-filter-actions">
                        <asp:LinkButton ID="SaveTabThemeButton" CssClass="CommandButton portal-primary-action" Text="Apply" OnClick="SaveTabThemeButton_Click" runat="server" />
                        <asp:LinkButton ID="ClearTabThemeButton" CssClass="CommandButton portal-secondary-action" Text="Clear Override" CausesValidation="False" OnClick="ClearTabThemeButton_Click" runat="server" />
                    </div>
                </div>
                <div class="Normal portal-status-line">
                    <span class="SubHead">Current Override:</span>
                    <asp:Label ID="TabThemeStatusLabel" runat="server" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>
