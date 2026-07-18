<%@ Page Language="c#" CodeBehind="EditXml.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditXml"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文：P7.4.2-E 将 XML/XSL 配置页改为主题化表单；资源仍必须由受信任部署提供。English: P7.4.2-E rebuilds the XML/XSL settings page with a themed form while deployed resources must still be provided by trusted deployment. --%>
    <section class="portal-page-section portal-edit-page portal-edit-xml">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">XML Settings</h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <div class="portal-option-stack">
                <strong>Deployment resources only</strong>
                <span class="portal-field-help">XML 与 XSL/T 文件必须位于当前应用部署目录内。本页只维护路径，不提供上传、在线编辑、外部 URL 或任意物理路径能力。</span>
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="XmlDataSrcLabel" CssClass="portal-field-stack-label" AssociatedControlID="XmlDataSrc"
                    runat="server" Text="XML Data File" />
                <%-- 中文：保存时会规范化为当前应用内虚拟路径。English: Saving normalizes this to a virtual path inside the current application. --%>
                <asp:TextBox ID="XmlDataSrc" CssClass="NormalTextBox portal-input" Columns="26"
                    MaxLength="250" runat="server" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="XslTransformSrcLabel" CssClass="portal-field-stack-label" AssociatedControlID="XslTransformSrc"
                    runat="server" Text="XSL/T Transform File" />
                <asp:TextBox ID="XslTransformSrc" CssClass="NormalTextBox portal-input" Columns="26"
                    MaxLength="250" runat="server" />
            </div>

            <asp:Label ID="ValidationMessage" CssClass="NormalRed portal-validation-message"
                EnableViewState="false" Visible="false" runat="server" />

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
