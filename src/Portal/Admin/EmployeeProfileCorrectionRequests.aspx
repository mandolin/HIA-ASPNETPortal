<%@ Page
    Language="c#"
    CodeBehind="EmployeeProfileCorrectionRequests.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EmployeeProfileCorrectionRequests"
    MasterPageFile="~/Default.master" %>

<%--
  <lang>
    <zh-CN>P6.4.3 员工资料更正请求后台处理页只处理请求状态，不直接修改员工主数据。</zh-CN>
    <en>The P6.4.3 employee profile correction Admin page handles request status only and does not directly modify employee master data.</en>
  </lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
      <lang>
        <zh-CN>审核页只调整展示结构，状态命令和审计记录仍由 code-behind 处理。</zh-CN>
        <en>The review page only adjusts presentation structure; status commands and audit records remain handled by code-behind.</en>
      </lang>
    --%>
    <div class="portal-admin-page portal-admin-correction-requests">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Employee Profile Correction Requests</h1>
                <p class="Normal portal-admin-subtitle">Review employee-submitted profile correction requests without directly changing master data.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
                <a class="CommandButton" href="WorkItems.aspx">Work Items</a>
                <a class="CommandButton" href="OperationAudits.aspx">Operation Audits</a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

        <%--
          <lang>
            <zh-CN>状态筛选只表达审核列表的查询意图；状态值解析、页大小、授权和空结果处理仍由 SearchButton_Click 的服务端职责负责。</zh-CN>
            <en>The status filter expresses only review-list query intent; SearchButton_Click remains responsible for parsing, page size, authorization, and empty-result handling.</en>
          </lang>
        --%>
        <div class="portal-admin-section portal-filter-panel">
            <div class="portal-filter-grid">
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">Status</span>
                    <asp:DropDownList ID="StatusFilterList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                </div>
                <div class="portal-filter-actions">
                    <asp:LinkButton
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
                <h2 class="Head portal-section-title">Correction Requests</h2>
            </div>
            <div class="portal-table-wrap">
                <%--
                  <lang>
                    <zh-CN>Repeater 只读呈现当前值、提议值和审核文本，使用编码绑定避免把申请内容当作标记执行；请求编号和状态命令仍需服务端重新校验。</zh-CN>
                    <en>The Repeater renders current, proposed, and review text read-only with encoded bindings so request content is not executed as markup; the server must revalidate request id and status commands.</en>
                  </lang>
                --%>
                <asp:Repeater ID="RequestsRepeater" OnItemCommand="RequestsRepeater_ItemCommand" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="70" class="SubHead">ID</th>
                                <th scope="col" width="145" class="SubHead">Submitted UTC</th>
                                <th scope="col" width="110" class="SubHead">Employee</th>
                                <th scope="col" width="120" class="SubHead">User</th>
                                <th scope="col" width="110" class="SubHead">Field</th>
                                <th scope="col" class="SubHead">Current / Proposed</th>
                                <th scope="col" width="95" class="SubHead">Status</th>
                                <th scope="col" width="230" class="SubHead">Review</th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("RequestId") %></td>
                                <td><%#: Eval("SubmittedUtcText") %></td>
                                <td><%#: Eval("EmployeeText") %></td>
                                <td><%#: Eval("UserText") %></td>
                                <td><%#: Eval("FieldName") %></td>
                                <td>
                                    <div class="portal-value-stack">
                                        <div><span class="SubHead">Current:</span> <%#: Eval("CurrentValueSnapshot") %></div>
                                        <div><span class="SubHead">Proposed:</span> <%#: Eval("ProposedValue") %></div>
                                        <div><span class="SubHead">Note:</span> <%#: Eval("RequestNote") %></div>
                                        <div><span class="SubHead">Review:</span> <%#: Eval("ReviewText") %></div>
                                    </div>
                                </td>
                                <td><%#: Eval("RequestStatus") %></td>
                                <td>
                                    <%--
                                      <lang>
                                        <zh-CN>审核备注有长度上限但仍是管理员输入；状态转换、请求归属、审计事件和主数据旁路规则由 code-behind 校验。</zh-CN>
                                        <en>The review note has a length limit but remains administrator input; code-behind validates state transition, request ownership, audit events, and the master-data side-channel rule.</en>
                                      </lang>
                                    --%>
                                    <asp:TextBox ID="ReviewNoteTextBox" CssClass="NormalTextBox portal-review-note" Width="210" MaxLength="1000" TextMode="MultiLine" Rows="3" runat="server" />
                                    <div class="portal-row-actions">
                                        <asp:LinkButton ID="ReviewedButton" Text="Approve" CssClass="CommandButton" CommandName="Reviewed" CommandArgument='<%# Eval("RequestId") %>' CausesValidation="False" runat="server" />
                                        <asp:LinkButton ID="ClosedButton" Text="Cancel" CssClass="CommandButton" CommandName="Closed" CommandArgument='<%# Eval("RequestId") %>' CausesValidation="False" runat="server" />
                                        <asp:LinkButton ID="RejectedButton" Text="Reject" CssClass="CommandButton" CommandName="Rejected" CommandArgument='<%# Eval("RequestId") %>' CausesValidation="False" runat="server" />
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
