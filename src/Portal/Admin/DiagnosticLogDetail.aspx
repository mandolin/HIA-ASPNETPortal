<%@ Page
    Language="c#"
    CodeBehind="DiagnosticLogDetail.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.DiagnosticLogDetail"
    MasterPageFile="~/Default.master" %>

<%-- P2.4 诊断日志详情：仅按事件编号查询已净化记录，不接受日志文件路径。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="20">&nbsp;</td>
            <td>
                <table width="100%" cellspacing="0" cellpadding="0">
                    <tr><td align="left" class="Head">Diagnostic Log Detail</td></tr>
                    <tr><td><hr noshade size="1"></td></tr>
                </table>

                <asp:Label ID="MessageLabel" CssClass="NormalRed" runat="server" />
                <asp:Panel ID="DetailPanel" runat="server" Visible="False">
                    <table width="100%" cellspacing="0" cellpadding="3" border="1">
                        <tr class="Normal"><td width="170" class="SubHead">Event ID</td><td><asp:Label ID="EventIdLabel" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">UTC</td><td><asp:Label ID="UtcTimeLabel" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">Level</td><td><asp:Label ID="LevelLabel" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">Category</td><td><asp:Label ID="CategoryLabel" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">Message</td><td><asp:Label ID="MessageTextLabel" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">Exception Type</td><td><asp:Label ID="ExceptionTypeLabel" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">Exception Detail</td><td><asp:TextBox ID="ExceptionDetailTextBox" TextMode="MultiLine" Rows="12" Width="95%" ReadOnly="True" CssClass="NormalTextBox" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">Request Path</td><td><asp:Label ID="RequestPathLabel" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">HTTP Method</td><td><asp:Label ID="HttpMethodLabel" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">User Name</td><td><asp:Label ID="UserNameLabel" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">Client IP</td><td><asp:Label ID="ClientIpLabel" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">Physical Path</td><td><asp:Label ID="PhysicalPathLabel" runat="server" /></td></tr>
                        <tr class="Normal"><td class="SubHead">User-Agent</td><td><asp:Label ID="UserAgentLabel" runat="server" /></td></tr>
                    </table>
                </asp:Panel>
                <br>
                <asp:HyperLink ID="BackLink" NavigateUrl="~/Admin/DiagnosticsLogs.aspx" Text="Back to Diagnostics Logs" CssClass="CommandButton" runat="server" />
            </td>
        </tr>
    </table>
</asp:Content>
