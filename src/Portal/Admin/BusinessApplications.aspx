<%@ Page
    Language="c#"
    CodeBehind="BusinessApplications.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.BusinessApplications"
    MasterPageFile="~/Default.master" %>

<%--
  <lang>
    <zh-CN>P19.4 抽象业务申请后台处理页只处理通用申请状态，不暴露具体领域工作流。</zh-CN>
    <en>The P19.4 abstract business-application Admin page handles generic application states only and does not expose a domain-specific workflow.</en>
  </lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="portal-admin-page portal-admin-business-applications">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Business Applications</h1>
                <p class="Normal portal-admin-subtitle">Review low-sensitivity abstract business applications and validate the lightweight workflow sample.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="WorkItems.aspx">Work Items</a>
                <a class="CommandButton" href="CollaborationItems.aspx">Collaboration Items</a>
                <a class="CommandButton" href="OperationAudits.aspx">Operation Audits</a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

        <div class="portal-admin-section portal-filter-panel">
            <%--
                <lang>
                    <zh-CN>状态筛选和 SearchButton_Click 只构造审核查询条件；最终可见申请、状态解释和权限仍由服务器决定。</zh-CN>
                    <en>The status filter and SearchButton_Click only construct review-query criteria; the server still decides visible applications, status interpretation, and authorization.</en>
                </lang>
            --%>
            <div class="portal-filter-grid">
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">Status</span>
                    <asp:DropDownList ID="StatusFilterList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                </div>
                <div class="portal-filter-actions">
                    <asp:Button
                        ID="SearchButton"
                        Text="Search"
                        CssClass="CommandButton"
                        CausesValidation="False"
                        OnClick="SearchButton_Click"
                        runat="server" />
                </div>
            </div>
        </div>

        <div class="portal-status-strip">
            <div class="Normal portal-status-line">
                <asp:Label ID="ResultLabel" runat="server" />
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Application Requests</h2>
            </div>
            <div class="portal-table-wrap">
                <%--
                    <lang>
                        <zh-CN>申请 Repeater 使用编码绑定展示低敏字段，并以 ApplicationId 作为命令目标；展示键不等于客户端可授予的操作权限。</zh-CN>
                        <en>The application Repeater uses encoded bindings for low-sensitivity fields and ApplicationId as the command target; a displayed key does not grant client-side operation permission.</en>
                    </lang>
                --%>
                <asp:Repeater ID="ApplicationsRepeater" OnItemCommand="ApplicationsRepeater_ItemCommand" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="70" class="SubHead">ID</th>
                                <th scope="col" width="145" class="SubHead">Submitted UTC</th>
                                <th scope="col" width="155" class="SubHead">Code</th>
                                <th scope="col" width="140" class="SubHead">Applicant</th>
                                <th scope="col" class="SubHead">Application</th>
                                <th scope="col" width="95" class="SubHead">Status</th>
                                <th scope="col" width="250" class="SubHead">Review</th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("ApplicationId") %></td>
                                <td><%#: Eval("SubmittedUtcText") %></td>
                                <td><%#: Eval("ApplicationCode") %></td>
                                <td><%#: Eval("ApplicantText") %></td>
                                <td>
                                    <div class="portal-value-stack">
                                        <div><span class="SubHead">Title:</span> <%#: Eval("Title") %></div>
                                        <div><span class="SubHead">Category:</span> <%#: Eval("CategoryKey") %></div>
                                        <div><span class="SubHead">Summary:</span> <%#: Eval("Summary") %></div>
                                        <div><span class="SubHead">Body:</span> <%#: Eval("Body") %></div>
                                        <div><span class="SubHead">Latest Review:</span> <%#: Eval("ReviewText") %></div>
                                    </div>
                                </td>
                                <td><%#: Eval("ApplicationStatus") %></td>
                                <td>
                                    <asp:TextBox ID="ReviewCommentTextBox" CssClass="NormalTextBox portal-review-note" Width="230" MaxLength="1000" TextMode="MultiLine" Rows="3" runat="server" />
                                    <%--
                                        <lang>
                                            <zh-CN>审核备注与 Approve/Return/Reject 命令共同进入服务器状态机；备注内容、当前状态和目标 ID 必须由 code-behind 重新校验。</zh-CN>
                                            <en>The review note and Approve/Return/Reject commands enter the server state machine together; code-behind must revalidate note content, current status, and target ID.</en>
                                        </lang>
                                    --%>
                                    <div class="portal-row-actions">
                                        <asp:Button ID="ApproveButton" Text="Approve" CssClass="CommandButton" CommandName="Approve" CommandArgument='<%# Eval("ApplicationId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="ReturnButton" Text="Return" CssClass="CommandButton" CommandName="Return" CommandArgument='<%# Eval("ApplicationId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="RejectButton" Text="Reject" CssClass="CommandButton CommandButtonDanger" CommandName="Reject" CommandArgument='<%# Eval("ApplicationId") %>' CausesValidation="False" runat="server" />
                                    </div>
                                </td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>
    </div>
</asp:Content>
