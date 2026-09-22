<%@ Page Language="c#" CodeBehind="EditAnnouncements.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EditAnnouncements" MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
        <lang>
            <zh-CN>P7 编辑页改用主题化表单外壳，并保留既有公告校验、回发和删除事件。</zh-CN>
            <en>P7 edit pages use the shared themed form shell while preserving existing announcement validation, postback, and delete events.</en>
        </lang>
    --%>
    <section class="portal-page-section portal-edit-page portal-edit-announcements">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title"><%= lang.EditAnnouncements_Heading %></h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <%--
                <lang>
                    <zh-CN>公告标题、描述和验证器组成受控内容输入；长度、必填、规范化和最终持久化仍由服务器处理。</zh-CN>
                    <en>Announcement title, description, and validators form controlled content input; length, requiredness, normalization, and final persistence remain server-side.</en>
                </lang>
            --%>
            <div class="portal-field-stack">
                <asp:Label ID="TitleLabel" CssClass="portal-field-stack-label" AssociatedControlID="TitleField"
                    runat="server" Text="<%$ Resources:lang,LegacyEdit_LabelTitle %>" />
                <asp:TextBox ID="TitleField" CssClass="NormalTextBox portal-input" Columns="30" MaxLength="100"
                    runat="server" />
                <asp:RequiredFieldValidator ID="Req1" CssClass="portal-validation-message" Display="Static"
                    ErrorMessage="<%$ Resources:lang,LegacyEdit_MessageValidTitle %>" ControlToValidate="TitleField" runat="server" />
            </div>

            <div class="portal-field-stack">
                <%--
                    <lang>
                        <zh-CN>公告的 Read More 链接及移动端变体只是导航数据；目标地址和协议安全不能由文本输入或标记层自行放宽。</zh-CN>
                        <en>The announcement Read More link and mobile variant are navigation data; target and scheme safety cannot be relaxed by text input or markup.</en>
                    </lang>
                --%>
                <asp:Label ID="MoreLinkLabel" CssClass="portal-field-stack-label" AssociatedControlID="MoreLinkField"
                    runat="server" Text="<%$ Resources:lang,LegacyEdit_LabelReadMoreLink %>" />
                <asp:TextBox ID="MoreLinkField" CssClass="NormalTextBox portal-input" Columns="30"
                    MaxLength="100" runat="server" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="MobileMoreLabel" CssClass="portal-field-stack-label" AssociatedControlID="MobileMoreField"
                    runat="server" Text="<%$ Resources:lang,LegacyEdit_LabelReadMoreMobile %>" />
                <asp:TextBox ID="MobileMoreField" CssClass="NormalTextBox portal-input" Columns="30"
                    MaxLength="100" runat="server" />
            </div>

            <div class="portal-field-stack">
                <asp:Label ID="DescriptionLabel" CssClass="portal-field-stack-label" AssociatedControlID="DescriptionField"
                    runat="server" Text="<%$ Resources:lang,LegacyEdit_LabelDescription %>" />
                <asp:TextBox ID="DescriptionField" CssClass="NormalTextBox portal-input" TextMode="Multiline"
                    Columns="44" Rows="6" runat="server" />
                <asp:RequiredFieldValidator ID="Req2" CssClass="portal-validation-message" Display="Static"
                    ErrorMessage="<%$ Resources:lang,EditEvents_MessageValidDescription %>" ControlToValidate="DescriptionField" runat="server" />
            </div>

            <div class="portal-field-stack portal-edit-date-field">
                <asp:Label ID="ExpireLabel" CssClass="portal-field-stack-label" AssociatedControlID="ExpireField"
                    runat="server" Text="<%$ Resources:lang,EditEvents_LabelExpires %>" />
                <asp:TextBox ID="ExpireField" Text="12/31/2025" CssClass="NormalTextBox portal-input" Columns="8"
                    runat="server" />
                <asp:RequiredFieldValidator Display="Static" ID="RequiredExpireDate" CssClass="portal-validation-message"
                    runat="server" ErrorMessage="<%$ Resources:lang,EditEvents_MessageValidExpirationDate %>" ControlToValidate="ExpireField" />
                <asp:CompareValidator Display="Static" ID="VerifyExpireDate" CssClass="portal-validation-message"
                    runat="server" Operator="DataTypeCheck" ControlToValidate="ExpireField" Type="Date"
                    ErrorMessage="<%$ Resources:lang,EditEvents_MessageValidExpirationDate %>" />
            </div>

            <asp:Label ID="ValidationMessage" CssClass="NormalRed portal-validation-message" EnableViewState="false"
                Visible="false" runat="server" />

            <%--
                <lang>
                    <zh-CN>过期日期和 Update/Cancel/Delete 命令共同进入旧公告状态流程；页面验证器不能替代服务器权限和当前记录校验。</zh-CN>
                    <en>The expiration date and Update/Cancel/Delete commands enter the legacy announcement state flow together; page validators do not replace server authorization or current-record checks.</en>
                </lang>
            --%>
            <div class="portal-form-actions">
                <asp:LinkButton ID="updateButton" Text="<%$ Resources:lang,LegacyEdit_ButtonUpdate %>" runat="server"
                    CssClass="portal-button portal-button-primary" BorderStyle="none" OnClick="UpdateBtn_Click" />
                <asp:LinkButton ID="cancelButton" Text="<%$ Resources:lang,LegacyEdit_ButtonCancel %>" CausesValidation="False" runat="server"
                    CssClass="portal-button portal-button-secondary" BorderStyle="none" OnClick="CancelBtn_Click" />
                <asp:LinkButton ID="deleteButton" Text="<%$ Resources:lang,LegacyEdit_ButtonDelete %>" CausesValidation="False" runat="server"
                    CssClass="portal-button portal-button-danger" BorderStyle="none" OnClick="DeleteBtn_Click" />
            </div>
        </div>

        <p class="portal-edit-metadata">
            <%= lang.LegacyEdit_CreatedBy %> <asp:Label ID="CreatedBy" runat="server" />
            <%= lang.LegacyEdit_CreatedOn %> <asp:Label ID="CreatedDate" runat="server" />
        </p>
    </section>
</asp:Content>
