<%@ Page Language="c#" CodeBehind="EditImage.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditImage"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文：P7.4.2-E 将图片配置页改为主题化表单，并保留站内或 HTTP(S) 图片地址边界。English: P7.4.2-E rebuilds the image settings page with a themed form while keeping the current application-or-HTTP(S) image URL boundary. --%>
    <section class="portal-page-section portal-edit-page portal-edit-image">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">Image Settings</h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <div class="portal-edit-subsection">
                <h2 class="portal-edit-subtitle">Image Source</h2>
                <p class="portal-field-help">优先使用站内图片路径；HTTP(S) 外链图片会按当前兼容策略保留，但应只指向受信任来源。</p>
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="SrcLabel" CssClass="portal-field-stack-label" AssociatedControlID="Src"
                    runat="server" Text="Src Location" />
                <asp:TextBox ID="Src" CssClass="NormalTextBox portal-input" Columns="30" MaxLength="250"
                    runat="server" />
            </div>

            <asp:Panel ID="ImagePreviewPanel" CssClass="portal-option-stack portal-image-preview"
                Visible="false" runat="server">
                <asp:Image ID="ImagePreview" CssClass="portal-image-preview-image" AlternateText="Image preview"
                    runat="server" />
                <span class="portal-field-help">预览仅显示当前可识别地址；图片加载失败不会改变保存校验结果。</span>
            </asp:Panel>

            <div class="portal-edit-subsection">
                <h2 class="portal-edit-subtitle">Display Size</h2>
                <p class="portal-field-help">宽度和高度可留空；填写时必须是非负整数。</p>
            </div>

            <div class="portal-inline-field-grid">
                <div class="portal-inline-field">
                    <div class="portal-field-stack portal-edit-short-field">
                        <asp:Label ID="WidthLabel" CssClass="portal-field-stack-label" AssociatedControlID="Width"
                            runat="server" Text="Image Width" />
                        <asp:TextBox ID="Width" CssClass="NormalTextBox portal-input" Columns="12" MaxLength="8"
                            runat="server" />
                    </div>
                </div>
                <div class="portal-inline-field">
                    <div class="portal-field-stack portal-edit-short-field">
                        <asp:Label ID="HeightLabel" CssClass="portal-field-stack-label" AssociatedControlID="Height"
                            runat="server" Text="Image Height" />
                        <asp:TextBox ID="Height" CssClass="NormalTextBox portal-input" Columns="12" MaxLength="8"
                            runat="server" />
                    </div>
                </div>
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
