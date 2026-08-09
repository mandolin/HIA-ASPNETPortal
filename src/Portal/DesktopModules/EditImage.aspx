<%@ Page Language="c#" CodeBehind="EditImage.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditImage"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
        <lang>
            <zh-CN>P7.4.2-E 将图片配置页改为主题化表单，并保留站内或 HTTP(S) 图片地址边界。</zh-CN>
            <en>P7.4.2-E rebuilds the image settings page with a themed form while keeping the current application-or-HTTP(S) image URL boundary.</en>
        </lang>
    --%>
    <section class="portal-page-section portal-edit-page portal-edit-image">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">Image Settings</h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <%--
                <lang>
                    <zh-CN>Src 只接受当前兼容策略允许的站内或 HTTP(S) 图片地址；预览面板是展示反馈，不替代服务器来源校验。</zh-CN>
                    <en>Src accepts only application-local or HTTP(S) image locations allowed by the compatibility policy; the preview is display feedback and does not replace server-side source validation.</en>
                </lang>
            --%>
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
                <%--
                    <lang>
                        <zh-CN>宽高字段可选但必须按服务器约束解释为非负整数；空值、范围和保存结果不由客户端或 CSS 决定。</zh-CN>
                        <en>Width and height are optional but must be interpreted by the server as non-negative integers; empty values, ranges, and save results are not decided by the client or CSS.</en>
                    </lang>
                --%>
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

            <%--
                <lang>
                    <zh-CN>Update/Cancel 进入图片配置的既有服务器流程；按钮不改变来源信任、尺寸校验或权限。</zh-CN>
                    <en>Update and Cancel enter the existing server flow for image settings; the buttons do not change source trust, dimension validation, or authorization.</en>
                </lang>
            --%>
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
