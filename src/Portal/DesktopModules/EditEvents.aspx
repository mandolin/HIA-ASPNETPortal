<%@ Page Language="c#" CodeBehind="EditEvents.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditEvents"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文：事件编辑页使用统一主题化表单，保留既有校验和回发事件；English: Event edit page keeps existing validation and postback handlers. --%>
    <section class="portal-page-section portal-edit-page portal-edit-events">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">Event Details</h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <div class="portal-field-stack">
                <asp:Label ID="TitleLabel" CssClass="portal-field-stack-label" AssociatedControlID="TitleField"
                    runat="server" Text="Title" />
                <asp:TextBox ID="TitleField" CssClass="NormalTextBox portal-input" Columns="30" MaxLength="150"
                    runat="server" />
                <asp:RequiredFieldValidator Display="Static" CssClass="portal-validation-message" runat="server"
                    ErrorMessage="You Must Enter a Valid Title" ControlToValidate="TitleField" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="DescriptionLabel" CssClass="portal-field-stack-label" AssociatedControlID="DescriptionField"
                    runat="server" Text="Description" />
                <asp:TextBox ID="DescriptionField" CssClass="NormalTextBox portal-input" TextMode="Multiline"
                    Columns="44" Rows="6" runat="server" />
                <asp:RequiredFieldValidator Display="Static" CssClass="portal-validation-message" runat="server"
                    ErrorMessage="You Must Enter a Valid Description" ControlToValidate="DescriptionField" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="WhereWhenLabel" CssClass="portal-field-stack-label" AssociatedControlID="WhereWhenField"
                    runat="server" Text="Where/When" />
                <asp:TextBox ID="WhereWhenField" CssClass="NormalTextBox portal-input" Columns="30"
                    MaxLength="150" runat="server" />
                <asp:RequiredFieldValidator Display="Static" CssClass="portal-validation-message" runat="server"
                    ErrorMessage="You Must Enter a Valid Time/Location" ControlToValidate="WhereWhenField" />
            </div>

            <div class="portal-field-stack portal-edit-date-field">
                <asp:Label ID="ExpireLabel" CssClass="portal-field-stack-label" AssociatedControlID="ExpireField"
                    runat="server" Text="Expires" />
                <asp:TextBox ID="ExpireField" Text="12/31/2001" CssClass="NormalTextBox portal-input" Columns="8"
                    runat="server" />
                <asp:RequiredFieldValidator Display="Static" ID="RequiredExpireDate" CssClass="portal-validation-message"
                    runat="server" ErrorMessage="You Must Enter a Valid Expiration Date" ControlToValidate="ExpireField" />
                <asp:CompareValidator Display="Static" ID="VerifyExpireDate" CssClass="portal-validation-message"
                    runat="server" Operator="DataTypeCheck" ControlToValidate="ExpireField" Type="Date"
                    ErrorMessage="You Must Enter a Valid Expiration Date" />
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
