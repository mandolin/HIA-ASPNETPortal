<%-- 页面声明 --%>
<%@ Page Language="c#" CodeBehind="EditDocs.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditDocs"
    MasterPageFile="~/Default.master" %>

<%-- 定义放置在主内容占位符中的内容 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文：P7.4.2-D 文档编辑页改为主题化表单外壳，并保留旧 WebForms 事件模型。English: P7.4.2-D rebuilds the document editor shell while keeping legacy WebForms events. --%>
    <section class="portal-page-section portal-edit-page portal-edit-docs">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">Document Details</h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <div class="portal-field-stack">
                <asp:Label ID="NameLabel" CssClass="portal-field-stack-label" AssociatedControlID="NameField"
                    runat="server" Text="Name" />
                <asp:TextBox ID="NameField" CssClass="NormalTextBox portal-input" Columns="28" MaxLength="150"
                    runat="server" />
                <asp:RequiredFieldValidator ID="RequiredFieldValidator1" CssClass="portal-validation-message"
                    Display="Static" runat="server" ErrorMessage="You Must Enter a Valid Name"
                    ControlToValidate="NameField" />
            </div>

            <div class="portal-field-stack portal-edit-short-field">
                <asp:Label ID="CategoryLabel" CssClass="portal-field-stack-label" AssociatedControlID="CategoryField"
                    runat="server" Text="Category" />
                <asp:TextBox ID="CategoryField" CssClass="NormalTextBox portal-input" Columns="28"
                    MaxLength="50" runat="server" />
            </div>

            <div class="portal-edit-subsection">
                <h2 class="portal-edit-subtitle">Browse URL</h2>
                <p class="portal-field-help">填写站内相对地址，或填写 http/https 浏览地址。</p>
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="PathLabel" CssClass="portal-field-stack-label" AssociatedControlID="PathField"
                    runat="server" Text="URL to Browse" />
                <asp:TextBox ID="PathField" CssClass="NormalTextBox portal-input" Columns="28" MaxLength="250"
                    runat="server" />
            </div>

            <div class="portal-edit-subsection portal-edit-upload-section">
                <h2 class="portal-edit-subtitle">Server Upload</h2>
                <asp:Label ID="UploadPolicyHint" CssClass="portal-field-help portal-upload-policy" runat="server" />
            </div>

            <div class="portal-option-stack">
                <asp:CheckBox ID="Upload" CssClass="Normal portal-checkbox" Text="Upload document to server"
                    runat="server" />
                <span class="portal-field-help">选择后将使用本次上传文件覆盖上方浏览地址。</span>
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="FileUploadLabel" CssClass="portal-field-stack-label" AssociatedControlID="FileUpload"
                    runat="server" Text="File" />
                <input type="file" id="FileUpload" class="portal-input portal-file-input" runat="server"
                    name="FileUpload" />
                <asp:Label ID="UploadMessage" CssClass="NormalRed portal-validation-message" runat="server" />
            </div>

            <div class="portal-option-stack portal-disabled-option">
                <asp:CheckBox ID="storeInDatabase" CssClass="Normal portal-checkbox"
                    Text="Store in database (web farm support)" runat="server" />
                <span class="portal-field-help">数据库文件存储暂未启用，本阶段不接收二进制内容入库。</span>
            </div>

            <div class="portal-form-actions">
                <asp:LinkButton ID="updateButton" Text="Update" runat="server"
                    CssClass="portal-button portal-button-primary" BorderStyle="none" OnClick="UpdateBtn_Click" />
                <asp:LinkButton ID="cancelButton" Text="Cancel" CausesValidation="False" runat="server"
                    CssClass="portal-button portal-button-secondary" BorderStyle="none" OnClick="CancelBtn_Click" />
                <asp:LinkButton ID="deleteButton" Text="Delete this item" CausesValidation="False"
                    runat="server" CssClass="portal-button portal-button-danger" BorderStyle="none"
                    OnClick="DeleteBtn_Click" />
            </div>
        </div>

        <p class="portal-edit-metadata">
            Created by <asp:Label ID="CreatedBy" runat="server" />
            on <asp:Label ID="CreatedDate" runat="server" />
        </p>
    </section>
</asp:Content>
