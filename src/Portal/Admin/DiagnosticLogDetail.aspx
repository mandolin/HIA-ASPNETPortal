<%@ Page
    Language="c#"
    CodeBehind="DiagnosticLogDetail.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.DiagnosticLogDetail"
    MasterPageFile="~/Default.master" %>

<%-- P2.4 诊断日志详情：仅按事件编号查询已净化记录，不接受日志文件路径。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 详情页只改只读展示结构，不改变事件编号查询和部署级开关。 --%>
    <div class="portal-admin-page portal-admin-diagnostic-detail">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Diagnostic Log Detail</h1>
                <p class="Normal portal-admin-subtitle">One structured diagnostic event.</p>
            </div>
            <div class="portal-admin-actions">
                <asp:HyperLink ID="BackLink" NavigateUrl="~/Admin/DiagnosticsLogs.aspx" Text="Back to Logs" CssClass="CommandButton" runat="server" />
                <a class="CommandButton" href="SystemHealth.aspx">System Health</a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" runat="server" />

        <asp:Panel ID="DetailPanel" CssClass="portal-admin-section" runat="server" Visible="False">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Event Detail</h2>
            </div>
            <div class="portal-table-wrap">
                <table class="portal-data-table portal-detail-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                    <tr class="Normal"><th scope="row" width="170" class="SubHead">Event ID</th><td><asp:Label ID="EventIdLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">UTC</th><td><asp:Label ID="UtcTimeLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">Level</th><td><asp:Label ID="LevelLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">Category</th><td><asp:Label ID="CategoryLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">Message</th><td><asp:Label ID="MessageTextLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">Exception Type</th><td><asp:Label ID="ExceptionTypeLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">Exception Detail</th><td><asp:TextBox ID="ExceptionDetailTextBox" TextMode="MultiLine" Rows="12" Width="95%" ReadOnly="True" CssClass="NormalTextBox portal-detail-text" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">Request Path</th><td><asp:Label ID="RequestPathLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">HTTP Method</th><td><asp:Label ID="HttpMethodLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">User Name</th><td><asp:Label ID="UserNameLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">Client IP</th><td><asp:Label ID="ClientIpLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">Physical Path</th><td><asp:Label ID="PhysicalPathLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead">User-Agent</th><td><asp:Label ID="UserAgentLabel" runat="server" /></td></tr>
                </table>
            </div>
        </asp:Panel>
    </div>
</asp:Content>
