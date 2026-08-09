<%@ Page Language="c#" CodeBehind="EditContacts.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditContacts"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
        <lang>
            <zh-CN>联系人编辑页仅替换表现层外壳，保留既有 WebForms 事件和数据流。</zh-CN>
            <en>The contact edit page only replaces the presentation shell while preserving existing WebForms events and data flow.</en>
        </lang>
    --%>
    <section class="portal-page-section portal-edit-page portal-edit-contacts">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">Contact Details</h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <%--
                <lang>
                    <zh-CN>姓名、角色和联系方式组成联系人资料输入；长度、必填、规范化和最终持久化仍由服务器处理。</zh-CN>
                    <en>Name, role, and contact fields form contact-profile input; length, requiredness, normalization, and final persistence remain server-side.</en>
                </lang>
            --%>
            <div class="portal-field-stack">
                <asp:Label ID="NameLabel" CssClass="portal-field-stack-label" AssociatedControlID="NameField"
                    runat="server" Text="Name" />
                <asp:TextBox ID="NameField" CssClass="NormalTextBox portal-input" Columns="30" MaxLength="50"
                    runat="server" />
                <asp:RequiredFieldValidator Display="Static" CssClass="portal-validation-message" runat="server"
                    ErrorMessage="You Must Enter a Valid Name" ControlToValidate="NameField" ID="RequiredFieldValidator1" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="RoleLabel" CssClass="portal-field-stack-label" AssociatedControlID="RoleField"
                    runat="server" Text="Role" />
                <asp:TextBox ID="RoleField" CssClass="NormalTextBox portal-input" Columns="30" MaxLength="100"
                    runat="server" />
            </div>

            <div class="portal-field-stack">
                <%--
                    <lang>
                        <zh-CN>Email、Contact1 和 Contact2 仅承载联系信息文本；格式、隐私处理和输出编码不由标记层假定或放宽。</zh-CN>
                        <en>Email, Contact1, and Contact2 carry contact text only; format, privacy handling, and output encoding are not assumed or relaxed by markup.</en>
                    </lang>
                --%>
                <asp:Label ID="EmailLabel" CssClass="portal-field-stack-label" AssociatedControlID="EmailField"
                    runat="server" Text="Email" />
                <asp:TextBox ID="EmailField" CssClass="NormalTextBox portal-input" Columns="30" MaxLength="100"
                    runat="server" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="Contact1Label" CssClass="portal-field-stack-label" AssociatedControlID="Contact1Field"
                    runat="server" Text="Contact 1" />
                <asp:TextBox ID="Contact1Field" CssClass="NormalTextBox portal-input" Columns="30"
                    MaxLength="250" runat="server" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="Contact2Label" CssClass="portal-field-stack-label" AssociatedControlID="Contact2Field"
                    runat="server" Text="Contact 2" />
                <asp:TextBox ID="Contact2Field" CssClass="NormalTextBox portal-input" Columns="30"
                    MaxLength="250" runat="server" />
            </div>

            <%--
                <lang>
                    <zh-CN>Update/Cancel/Delete 进入既有联系人持久化流程；按钮不绕过权限、当前记录校验或删除保护。</zh-CN>
                    <en>Update, Cancel, and Delete enter the existing contact persistence flow; the buttons do not bypass authorization, current-record checks, or deletion protection.</en>
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
