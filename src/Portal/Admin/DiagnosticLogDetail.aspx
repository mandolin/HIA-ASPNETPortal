<%@ Page
    Language="c#"
    CodeBehind="DiagnosticLogDetail.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.DiagnosticLogDetail"
    MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

<%--
  <lang>
    <zh-CN>P2.4 诊断日志详情页仅按事件编号查询已净化记录，不接受日志文件路径。</zh-CN>
    <en>The P2.4 diagnostic log detail page queries sanitized entries only by event id and does not accept log file paths.</en>
  </lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
      <lang>
        <zh-CN>详情页只调整只读展示结构，不改变事件编号查询和部署级详情开关。</zh-CN>
        <en>The detail page changes only the read-only presentation structure and does not change event-id lookup or the deployment-level detail switch.</en>
      </lang>
    --%>
    <div class="portal-admin-page portal-admin-diagnostic-detail">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title"><%= lang.Admin_DiagnosticLogDetail_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_DiagnosticLogDetail_Subtitle %></p>
            </div>
            <div class="portal-admin-actions">
                <asp:HyperLink ID="BackLink" NavigateUrl="~/Admin/DiagnosticsLogs.aspx" Text="<%$ Resources:lang, Admin_DiagnosticLogDetail_LinkBackToLogs %>" CssClass="CommandButton" runat="server" />
                <%= PortalNavigationEntryRenderer.RenderActions("Admin.Ops.DiagnosticLogDetail", Context) %>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" runat="server" />

        <%--
          <lang>
            <zh-CN>详情面板默认隐藏并由 code-behind 控制可见性；标记层不自行打开包含异常、路径、IP 和 User-Agent 的敏感事件数据。</zh-CN>
            <en>The detail panel is hidden by default and its visibility is controlled by code-behind; the markup must not open sensitive event data containing exception, path, IP, or User-Agent fields by itself.</en>
          </lang>
        --%>
        <asp:Panel ID="DetailPanel" CssClass="portal-admin-section" runat="server" Visible="False">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_DiagnosticLogDetail_SectionEventDetail %></h2>
            </div>
            <div class="portal-table-wrap">
                <%--
                  <lang>
                    <zh-CN>详情表只呈现 code-behind 已授权并净化后的字段；标记层不接收事件编号以外的路径或文件输入，敏感字段的裁剪仍由服务端职责负责。</zh-CN>
                    <en>The detail table presents only fields authorized and sanitized by code-behind; the markup accepts no path or file input beyond the event-id route, and server code remains responsible for sensitive-field redaction.</en>
                  </lang>
                --%>
                <table class="portal-data-table portal-detail-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                    <tr class="Normal"><th scope="row" width="170" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldEventId %></th><td><asp:Label ID="EventIdLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldUtc %></th><td><asp:Label ID="UtcTimeLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldLevel %></th><td><asp:Label ID="LevelLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldCategory %></th><td><asp:Label ID="CategoryLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldMessage %></th><td><asp:Label ID="MessageTextLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldExceptionType %></th><td><asp:Label ID="ExceptionTypeLabel" runat="server" /></td></tr>
                    <%--
                      <lang>
                        <zh-CN>异常详情使用只读多行控件承载展示，不提供回发编辑能力，也不把异常文本重新作为用户输入提交。</zh-CN>
                        <en>Render exception detail in a read-only multiline control; it provides no edit-back capability and does not resubmit exception text as user input.</en>
                      </lang>
                    --%>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldExceptionDetail %></th><td><asp:TextBox ID="ExceptionDetailTextBox" TextMode="MultiLine" Rows="12" Width="95%" ReadOnly="True" CssClass="NormalTextBox portal-detail-text" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldRequestPath %></th><td><asp:Label ID="RequestPathLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldHttpMethod %></th><td><asp:Label ID="HttpMethodLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldUserName %></th><td><asp:Label ID="UserNameLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldClientIp %></th><td><asp:Label ID="ClientIpLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldPhysicalPath %></th><td><asp:Label ID="PhysicalPathLabel" runat="server" /></td></tr>
                    <tr class="Normal"><th scope="row" class="SubHead"><%= lang.Admin_DiagnosticLogDetail_FieldUserAgent %></th><td><asp:Label ID="UserAgentLabel" runat="server" /></td></tr>
                </table>
            </div>
        </asp:Panel>
    </div>
</asp:Content>
