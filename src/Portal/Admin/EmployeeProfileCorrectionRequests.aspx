<%@ Page
    Language="c#"
    CodeBehind="EmployeeProfileCorrectionRequests.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EmployeeProfileCorrectionRequests"
    MasterPageFile="~/Default.master" %>

<%-- P6.4.3 员工资料更正请求后台处理页：只处理请求状态，不直接修改员工主数据。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 审核页只调整展示结构，状态命令和审计记录仍由 code-behind 处理。 --%>
    <div class="portal-admin-page portal-admin-correction-requests">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Employee Profile Correction Requests</h1>
                <p class="Normal portal-admin-subtitle">Review employee-submitted profile correction requests without directly changing master data.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
                <a class="CommandButton" href="OperationAudits.aspx">Operation Audits</a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

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
                                    <asp:TextBox ID="ReviewNoteTextBox" CssClass="NormalTextBox portal-review-note" Width="210" MaxLength="1000" TextMode="MultiLine" Rows="3" runat="server" />
                                    <div class="portal-row-actions">
                                        <asp:LinkButton ID="ReviewedButton" Text="Reviewed" CssClass="CommandButton" CommandName="Reviewed" CommandArgument='<%# Eval("RequestId") %>' CausesValidation="False" runat="server" />
                                        <asp:LinkButton ID="ClosedButton" Text="Close" CssClass="CommandButton" CommandName="Closed" CommandArgument='<%# Eval("RequestId") %>' CausesValidation="False" runat="server" />
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
