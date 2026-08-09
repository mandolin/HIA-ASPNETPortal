<%@ Page Language="c#" CodeBehind="EditLinks.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditLinks"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
        <lang>
            <zh-CN>链接编辑页统一到 P7 表单视觉，并保留既有校验、回发和删除事件。</zh-CN>
            <en>The link edit page is aligned with the P7 themed form style while preserving existing validation, postback, and delete events.</en>
        </lang>
    --%>
    <section class="portal-page-section portal-edit-page portal-edit-links">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">Link Details</h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <%--
                <lang>
                    <zh-CN>标题、Url 和移动端 Url 是导航目标输入；标记层只收集值，协议、主机、站内路径和权限边界必须由服务器验证。</zh-CN>
                    <en>Title, Url, and mobile Url are navigation-target inputs; markup only collects values, while the server must validate scheme, host, local paths, and authorization boundaries.</en>
                </lang>
            --%>
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
                <%--
                    <lang>
                        <zh-CN>Description 和 ViewOrder 维持旧显示模型；整数验证只限制表单格式，排序范围和持久化冲突仍由服务器处理。</zh-CN>
                        <en>Description and ViewOrder preserve the legacy display model; integer validation limits form shape, while order range and persistence conflicts remain server-side.</en>
                    </lang>
                --%>
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

            <%--
                <lang>
                    <zh-CN>Update/Cancel/Delete 进入链接记录的既有服务器流程；按钮不自行解析 URL、改变排序或绕过删除权限。</zh-CN>
                    <en>Update, Cancel, and Delete enter the existing server flow for the link record; the buttons do not parse URLs, change ordering, or bypass delete authorization.</en>
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
