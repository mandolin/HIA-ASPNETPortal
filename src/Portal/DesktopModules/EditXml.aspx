<%@ Page Language="c#" CodeBehind="EditXml.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.EditXml"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
        <lang>
            <zh-CN>P7.4.2-E 将 XML/XSL 配置页改为主题化表单；资源仍必须由受信任部署提供。</zh-CN>
            <en>P7.4.2-E rebuilds the XML/XSL settings page with a themed form while deployed resources must still be provided by trusted deployment.</en>
        </lang>
    --%>
    <section class="portal-page-section portal-edit-page portal-edit-xml">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">XML Settings</h1>
        </div>

        <div class="portal-detail-card portal-edit-form">
            <%--
                <lang>
                    <zh-CN>XML/XSL 输入只维护受信部署目录内的资源引用；本页不提供上传、在线编辑、外部 URL 或任意物理路径能力。</zh-CN>
                    <en>XML/XSL inputs maintain references only to trusted deployment resources; this page provides no upload, online editing, external URL, or arbitrary physical-path capability.</en>
                </lang>
            --%>
            <div class="portal-option-stack">
                <strong>Deployment resources only</strong>
                <span class="portal-field-help">XML 与 XSL/T 文件必须位于当前应用部署目录内。本页只维护路径，不提供上传、在线编辑、外部 URL 或任意物理路径能力。</span>
            </div>

            <div class="portal-field-stack">
                <%--
                    <lang>
                        <zh-CN>XmlDataSrc 与 XslTransformSrc 由服务器规范化并限制在应用虚拟路径边界；文本框值不是外部资源信任证明。</zh-CN>
                        <en>The server normalizes XmlDataSrc and XslTransformSrc within the application virtual-path boundary; a textbox value is not proof that an external resource is trusted.</en>
                    </lang>
                --%>
                <asp:Label ID="XmlDataSrcLabel" CssClass="portal-field-stack-label" AssociatedControlID="XmlDataSrc"
                    runat="server" Text="XML Data File" />
                <%--
                    <lang>
                        <zh-CN>保存时会规范化为当前应用内虚拟路径，避免写入外部 URL 或任意物理路径。</zh-CN>
                        <en>Saving normalizes this value to a virtual path inside the current application, avoiding external URLs or arbitrary physical paths.</en>
                    </lang>
                --%>
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

            <%--
                <lang>
                    <zh-CN>Update/Cancel 进入既有 XML/XSL 设置流程；按钮不绕过部署路径验证、权限或错误提示。</zh-CN>
                    <en>Update and Cancel enter the existing XML/XSL settings flow; the buttons do not bypass deployment-path validation, authorization, or error reporting.</en>
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
