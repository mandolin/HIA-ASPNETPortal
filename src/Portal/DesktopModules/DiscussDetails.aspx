<%@ Page Language="c#" CodeBehind="DiscussDetails.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.DiscussDetails" MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %> <%-- 导入命名空间 --%>

<%--主要内容区域--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="portal-page-section portal-discussion-detail">
        <div class="portal-page-heading-row">
            <h1 class="portal-page-title">Message Detail</h1>
            <asp:Panel ID="ButtonPanel" CssClass="portal-page-actions" runat="server"> <%-- 按钮面板 --%>
                <a class="portal-button portal-button-secondary portal-button-compact portal-icon-nav" id="prevItem" title="Previous Message" runat="server">Previous</a> <%-- 上一条消息按钮 --%>
                <a class="portal-button portal-button-secondary portal-button-compact portal-icon-nav" id="nextItem" title="Next Message" runat="server">Next</a> <%-- 下一条消息按钮 --%>
                <asp:LinkButton ID="ReplyBtn" runat="server" EnableViewState="false" CssClass="portal-button portal-button-primary portal-button-compact"
                    Text="Reply to this Message" OnClick="ReplyBtn_Click"></asp:LinkButton> <%-- 回复按钮 --%>
            </asp:Panel>
        </div>

        <%-- 编辑面板 --%>
        <asp:Panel ID="EditPanel" CssClass="portal-edit-panel portal-detail-card" runat="server" Visible="false">
            <div class="portal-field-stack">
                <span class="portal-field-stack-label">Title:</span> <%-- 标题标签 --%>
                <asp:TextBox ID="TitleField" runat="server" MaxLength="100" Columns="40" Width="500"
                    CssClass="NormalTextBox portal-input"></asp:TextBox> <%-- 输入框用于编辑标题 --%>
            </div>
            <div class="portal-field-stack">
                <span class="portal-field-stack-label">Body:</span> <%-- 内容标签 --%>
                <asp:TextBox ID="BodyField" runat="server" Columns="59" Width="500" Rows="15" TextMode="Multiline" CssClass="portal-input"></asp:TextBox> <%-- 多行输入框用于编辑内容 --%>
            </div>
            <div class="portal-form-actions">
                <asp:LinkButton CssClass="portal-button portal-button-primary" ID="updateButton" runat="server" Text="Submit"
                    OnClick="UpdateBtn_Click"></asp:LinkButton> <%-- 提交按钮 --%>
                <asp:LinkButton CssClass="portal-button portal-button-secondary" ID="cancelButton" runat="server" Text="Cancel"
                    CausesValidation="False" OnClick="CancelBtn_Click"></asp:LinkButton> <%-- 取消按钮 --%>
            </div>
            <div class="portal-content-item-meta">Original Message:</div> <%-- 原始消息标签 --%>
        </asp:Panel>

        <%-- 消息内容展示区 --%>
        <div class="portal-detail-card portal-message-detail-card">
            <div class="portal-detail-row">
                <span class="portal-detail-label">Subject:</span> <%-- 主题 --%>
                <asp:Label ID="Subject" runat="server"></asp:Label>
            </div>
            <div class="portal-detail-row">
                <span class="portal-detail-label">Author:</span> <%-- 发布者 --%>
                <asp:Label ID="CreatedByUser" runat="server"></asp:Label>
            </div>
            <div class="portal-detail-row">
                <span class="portal-detail-label">Date:</span> <%-- 发布日期 --%>
                <asp:Label ID="CreatedDate" runat="server"></asp:Label>
            </div>
            <div class="portal-message-body">
                <asp:Label ID="Body" runat="server"></asp:Label> <%-- 消息正文 --%>
            </div>
        </div>
    </div>
</asp:Content>
