<%@ Page Language="c#" CodeBehind="EditHtml.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditHtml"
    ValidateRequest="false"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文：P7.4.2-E 将受信任 HTML 配置页改为主题化表单；原始 HTML 的安全边界仍由权限和后续治理承担。English: P7.4.2-E rebuilds the trusted HTML settings page with a themed form while keeping the raw-HTML security boundary in permissions and future governance. --%>
    <section class="portal-page-section portal-edit-page portal-edit-html">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">Html Settings</h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <div class="portal-option-stack">
                <strong>Trusted raw HTML entry</strong>
                <span class="portal-field-help">此入口仅面向受信任管理员。内容会按旧机制保存为 HTML 编码文本，后续将由“原始 HTML”细粒度权限继续收拢。</span>
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="DesktopTextLabel" CssClass="portal-field-stack-label" AssociatedControlID="DesktopText"
                    runat="server" Text="Desktop Html Content" />
                <%-- 中文：允许受信任管理员输入原始 HTML；不要在这里启用普通请求验证。English: Allows trusted administrators to enter raw HTML; ordinary request validation must remain disabled for this page. --%>
                <asp:TextBox ID="DesktopText" CssClass="NormalTextBox portal-input portal-code-textarea"
                    Columns="75" Rows="12" TextMode="MultiLine" runat="server" />
            </div>

            <div class="portal-edit-subsection">
                <h2 class="portal-edit-subtitle">Mobile Fallback</h2>
                <p class="portal-field-help">移动端字段保留旧数据兼容；后续移动端展示方案确定后再统一收拢。</p>
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="MobileSummaryLabel" CssClass="portal-field-stack-label" AssociatedControlID="MobileSummary"
                    runat="server" Text="Mobile Summary (optional)" />
                <asp:TextBox ID="MobileSummary" CssClass="NormalTextBox portal-input portal-small-textarea"
                    Columns="75" Rows="3" TextMode="MultiLine" runat="server" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="MobileDetailsLabel" CssClass="portal-field-stack-label" AssociatedControlID="MobileDetails"
                    runat="server" Text="Mobile Details (optional)" />
                <asp:TextBox ID="MobileDetails" CssClass="NormalTextBox portal-input portal-small-textarea"
                    Columns="75" Rows="5" TextMode="MultiLine" runat="server" />
            </div>

            <div class="portal-form-actions">
                <asp:LinkButton ID="updateButton" Text="Update" runat="server"
                    CssClass="portal-button portal-button-primary" BorderStyle="none"
                    OnClick="UpdateBtn_Click" />
                <asp:LinkButton ID="cancelButton" Text="Cancel" CausesValidation="False" runat="server"
                    CssClass="portal-button portal-button-secondary" BorderStyle="none"
                    OnClick="CancelBtn_Click" />
            </div>
        </div>
    </section>
</asp:Content>
