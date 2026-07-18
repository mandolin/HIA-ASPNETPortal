<%@ Page Language="c#" CodeBehind="EditLinks.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditLinks"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文：链接编辑页统一到 P7 表单视觉；English: Link edit page is aligned with the P7 themed form style. --%>
    <section class="portal-page-section portal-edit-page portal-edit-links">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">Link Details</h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <div class="portal-field-stack">
                <asp:Label ID="TitleLabel" CssClass="portal-field-stack-label" AssociatedControlID="TitleField"
                    runat="server" Text="Title" />
                <asp:TextBox ID="TitleField" CssClass="NormalTextBox portal-input" Columns="30" MaxLength="150"
                    runat="server" />
                <asp:RequiredFieldValidator ID="Req1" CssClass="portal-validation-message" Display="Static"
                    ErrorMessage="You Must Enter a Valid Title" ControlToValidate="TitleField" runat="server" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="UrlLabel" CssClass="portal-field-stack-label" AssociatedControlID="UrlField"
                    runat="server" Text="Url" />
                <asp:TextBox ID="UrlField" CssClass="NormalTextBox portal-input" Columns="30" MaxLength="150"
                    runat="server" />
                <asp:RequiredFieldValidator ID="Req2" CssClass="portal-validation-message" Display="Static"
                    runat="server" ErrorMessage="You Must Enter a Valid URL" ControlToValidate="UrlField" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="MobileUrlLabel" CssClass="portal-field-stack-label" AssociatedControlID="MobileUrlField"
                    runat="server" Text="Mobile Url" />
                <asp:TextBox ID="MobileUrlField" CssClass="NormalTextBox portal-input" Columns="30"
                    MaxLength="150" runat="server" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="DescriptionLabel" CssClass="portal-field-stack-label" AssociatedControlID="DescriptionField"
                    runat="server" Text="Description" />
                <asp:TextBox ID="DescriptionField" CssClass="NormalTextBox portal-input" Columns="30"
                    MaxLength="150" runat="server" />
            </div>

            <div class="portal-field-stack portal-edit-order-field">
                <asp:Label ID="ViewOrderLabel" CssClass="portal-field-stack-label" AssociatedControlID="ViewOrderField"
                    runat="server" Text="View Order" />
                <asp:TextBox ID="ViewOrderField" CssClass="NormalTextBox portal-input" Columns="30"
                    MaxLength="3" runat="server" />
                <asp:RequiredFieldValidator Display="Static" ID="RequiredViewOrder" CssClass="portal-validation-message"
                    runat="server" ControlToValidate="ViewOrderField" ErrorMessage="You Must Enter a Valid View Order" />
                <asp:CompareValidator Display="Static" ID="VerifyViewOrder" CssClass="portal-validation-message"
                    runat="server" Operator="DataTypeCheck" ControlToValidate="ViewOrderField" Type="Integer"
                    ErrorMessage="You Must Enter a Valid View Order" />
            </div>

            <asp:Label ID="ValidationMessage" CssClass="NormalRed portal-validation-message" EnableViewState="false"
                Visible="false" runat="server" />

            <div class="portal-form-actions">
                <asp:LinkButton ID="updateButton" Text="Update" runat="server"
                    CssClass="portal-button portal-button-primary" BorderStyle="none" OnClick="UpdateBtn_Click" />
                <asp:LinkButton ID="cancelButton" Text="Cancel" CausesValidation="False" runat="server"
                    CssClass="portal-button portal-button-secondary" BorderStyle="none" OnClick="CancelBtn_Click" />
                <asp:LinkButton ID="deleteButton" Text="Delete this item" CausesValidation="False" runat="server"
                    CssClass="portal-button portal-button-danger" BorderStyle="none" OnClick="DeleteBtn_Click" />
            </div>
        </div>

        <p class="portal-edit-metadata">
            Created by <asp:Label ID="CreatedBy" runat="server" />
            on <asp:Label ID="CreatedDate" runat="server" />
        </p>
    </section>
</asp:Content>
