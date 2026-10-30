<%@ Page
    Language="c#"
    CodeBehind="LeaveRequests.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.LeaveRequests"
    MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="portal-admin-page portal-admin-leave-requests">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title"><%= lang.Admin_LeaveRequests_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_LeaveRequests_Subtitle %></p>
            </div>
            <%= PortalNavigationEntryRenderer.RenderActions("Admin.Capability.LeaveRequests", Context) %>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

        <div class="portal-admin-section portal-filter-panel">
            <div class="portal-filter-grid">
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_BusinessApplications_LabelStatus %></span>
                    <asp:DropDownList ID="StatusFilterList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                </div>
                <div class="portal-filter-actions">
                    <asp:Button
                        ID="SearchButton"
                        Text="<%$ Resources:lang, Admin_BusinessApplications_ButtonSearch %>"
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
                <h2 class="Head portal-section-title"><%= lang.Admin_LeaveRequests_SectionRequests %></h2>
            </div>
            <div class="portal-table-wrap">
                <asp:Repeater ID="RequestsRepeater" OnItemCommand="RequestsRepeater_ItemCommand" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="70" class="SubHead"><%= lang.Admin_BusinessApplications_ColumnId %></th>
                                <th scope="col" width="15%" class="SubHead"><%= lang.Admin_BusinessApplications_ColumnSubmittedUtc %></th>
                                <th scope="col" width="15%" class="SubHead"><%= lang.Admin_BusinessApplications_ColumnCode %></th>
                                <th scope="col" width="14%" class="SubHead"><%= lang.Admin_BusinessApplications_ColumnApplicant %></th>
                                <th scope="col" class="SubHead"><%= lang.Admin_LeaveRequests_ColumnApplication %></th>
                                <th scope="col" width="95" class="SubHead"><%= lang.Admin_BusinessApplications_ColumnStatus %></th>
                                <th scope="col" width="24%" class="SubHead"><%= lang.Admin_BusinessApplications_ColumnReview %></th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("RequestId") %></td>
                                <td><%#: Eval("SubmittedUtcText") %></td>
                                <td><%#: Eval("RequestCode") %></td>
                                <td><%#: Eval("ApplicantText") %></td>
                                <td>
                                    <div class="portal-value-stack">
                                        <div><span class="SubHead"><%= lang.Admin_LeaveRequests_InlineLeaveType %></span> <%#: Eval("LeaveType") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_LeaveRequests_InlineStartDate %></span> <%#: Eval("StartDateText") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_LeaveRequests_InlineEndDate %></span> <%#: Eval("EndDateText") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_LeaveRequests_InlineDays %></span> <%#: Eval("DaysText") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_LeaveRequests_InlineReason %></span> <%#: Eval("Reason") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_LeaveRequests_InlineAttachment %></span> <%#: Eval("AttachmentName") %></div>
                                        <div><span class="SubHead"><%= lang.Admin_BusinessApplications_InlineLatestReview %></span> <%#: Eval("ReviewText") %></div>
                                    </div>
                                </td>
                                <td><%#: Eval("RequestStatus") %></td>
                                <td>
                                    <asp:TextBox ID="ReviewCommentTextBox" CssClass="NormalTextBox portal-review-note" Width="230" MaxLength="1000" TextMode="MultiLine" Rows="3" runat="server" />
                                    <div class="portal-row-actions">
                                        <asp:Button ID="ApproveButton" Text="<%$ Resources:lang, Admin_BusinessApplications_ButtonApprove %>" CssClass="CommandButton" CommandName="Approve" CommandArgument='<%# Eval("RequestId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="ReturnButton" Text="<%$ Resources:lang, Admin_BusinessApplications_ButtonReturn %>" CssClass="CommandButton" CommandName="Return" CommandArgument='<%# Eval("RequestId") %>' CausesValidation="False" runat="server" />
                                        <asp:Button ID="RejectButton" Text="<%$ Resources:lang, Admin_BusinessApplications_ButtonReject %>" CssClass="CommandButton CommandButtonDanger" CommandName="Reject" CommandArgument='<%# Eval("RequestId") %>' CausesValidation="False" runat="server" />
                                    </div>
                                </td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        <%= PortalEmptyStateRenderer.Render(EmptyStateRowCount, EmptyStateText, 7) %>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>
    </div>
</asp:Content>
