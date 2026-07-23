<%@ Page Language="c#" CodeBehind="DiscussDetails.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.DiscussDetails" MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>

<%--
<lang>
  <zh-CN>讨论详情页主体区域。页面允许访客读取已存在消息，但回复、创建主题等写操作仍由 code-behind 的模块编辑权限检查控制。</zh-CN>
  <en>Main discussion-detail area. Visitors may read existing messages, while write operations such as replying or creating a topic remain controlled by module edit-permission checks in the code-behind.</en>
</lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="portal-page-section portal-discussion-detail">
        <%--
        <lang>
          <zh-CN>页头动作区聚合上一条、下一条和回复入口；按钮只负责触发 Web Forms 事件或站内导航，不在标记层拼接动态脚本。</zh-CN>
          <en>The page-heading action area groups previous, next, and reply entry points. Buttons only trigger Web Forms events or site-local navigation; dynamic scripts are not concatenated in markup.</en>
        </lang>
        --%>
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">Message Detail</h1>
            <asp:Panel ID="ButtonPanel" CssClass="portal-page-actions" runat="server">
                <a class="portal-button portal-button-secondary portal-button-compact portal-icon-nav" id="prevItem" title="Previous Message" runat="server">Previous</a>
                <a class="portal-button portal-button-secondary portal-button-compact portal-icon-nav" id="nextItem" title="Next Message" runat="server">Next</a>
                <asp:LinkButton ID="ReplyBtn" runat="server" EnableViewState="false" CssClass="portal-button portal-button-primary portal-button-compact"
                    Text="Reply to this Message" OnClick="ReplyBtn_Click"></asp:LinkButton>
            </asp:Panel>
        </div>

        <%--
        <lang>
          <zh-CN>回复或新建主题编辑面板。默认隐藏，仅在 code-behind 确认当前用户具备模块编辑权限后显示；输入值提交前仍由服务器端编码处理。</zh-CN>
          <en>Reply or new-topic editor panel. It is hidden by default and shown only after the code-behind confirms module edit permission; submitted values are still encoded on the server.</en>
        </lang>
        --%>
        <asp:Panel ID="EditPanel" CssClass="portal-edit-panel portal-detail-card" runat="server" Visible="false">
            <div class="portal-field-stack">
                <span class="portal-field-stack-label">Title:</span>
                <asp:TextBox ID="TitleField" runat="server" MaxLength="100" Columns="40" Width="500"
                    CssClass="NormalTextBox portal-input"></asp:TextBox>
            </div>
            <div class="portal-field-stack">
                <span class="portal-field-stack-label">Body:</span>
                <asp:TextBox ID="BodyField" runat="server" Columns="59" Width="500" Rows="15" TextMode="Multiline" CssClass="portal-input"></asp:TextBox>
            </div>
            <%--
            <lang>
              <zh-CN>表单动作保持主题按钮 class；提交和取消都回到服务器端事件处理，避免客户端直接改变权限状态。</zh-CN>
              <en>Form actions keep themed button classes. Submit and cancel both return to server-side event handlers, avoiding client-side permission-state changes.</en>
            </lang>
            --%>
            <div class="portal-form-actions">
                <asp:LinkButton CssClass="portal-button portal-button-primary" ID="updateButton" runat="server" Text="Submit"
                    OnClick="UpdateBtn_Click"></asp:LinkButton>
                <asp:LinkButton CssClass="portal-button portal-button-secondary" ID="cancelButton" runat="server" Text="Cancel"
                    CausesValidation="False" OnClick="CancelBtn_Click"></asp:LinkButton>
            </div>
            <div class="portal-content-item-meta">Original Message:</div>
        </asp:Panel>

        <%--
        <lang>
          <zh-CN>消息内容展示区只渲染已经由 code-behind 规范化的显示文本；主题样式负责版式，安全边界仍在服务器端编码。</zh-CN>
          <en>The message display area renders only code-behind-normalized display text. Theme styles own layout, while the safety boundary remains server-side encoding.</en>
        </lang>
        --%>
        <div class="portal-detail-card portal-message-detail-card">
            <div class="portal-detail-row">
                <span class="portal-detail-label">Subject:</span>
                <asp:Label ID="Subject" runat="server"></asp:Label>
            </div>
            <div class="portal-detail-row">
                <span class="portal-detail-label">Author:</span>
                <asp:Label ID="CreatedByUser" runat="server"></asp:Label>
            </div>
            <div class="portal-detail-row">
                <span class="portal-detail-label">Date:</span>
                <asp:Label ID="CreatedDate" runat="server"></asp:Label>
            </div>
            <div class="portal-message-body">
                <asp:Label ID="Body" runat="server"></asp:Label>
            </div>
        </div>
    </div>
</asp:Content>
