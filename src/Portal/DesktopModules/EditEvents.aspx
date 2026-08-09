<%@ Page Language="c#" CodeBehind="EditEvents.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditEvents"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
        <lang>
            <zh-CN>事件编辑页使用统一主题化表单，保留既有校验、回发和删除事件。</zh-CN>
            <en>The event edit page uses the shared themed form while preserving existing validation, postback, and delete events.</en>
        </lang>
    --%>
    <section class="portal-page-section portal-edit-page portal-edit-events">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">Event Details</h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <%--
                <lang>
                    <zh-CN>事件标题、描述和 Where/When 字段构成事件内容输入；必填验证只提供表单反馈，服务器仍需规范化和授权。</zh-CN>
                    <en>Event title, description, and Where/When fields form event content input; required validators provide form feedback while the server still normalizes and authorizes.</en>
                </lang>
            --%>
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
                <%--
                    <lang>
                        <zh-CN>ExpireField 的日期校验表达当前页面格式契约；日期解释、时区、过期策略和保存结果仍由服务器决定。</zh-CN>
                        <en>ExpireField validators express the page format contract; date interpretation, time zone, expiry policy, and save result remain server-decided.</en>
                    </lang>
                --%>
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

            <%--
                <lang>
                    <zh-CN>Update/Cancel/Delete 通过既有事件处理器进入事件持久化流程；按钮本身不改变权限或绕过删除保护。</zh-CN>
                    <en>Update, Cancel, and Delete use the existing event handlers for event persistence; the buttons do not change authorization or bypass deletion protection.</en>
                </lang>
            --%>
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
