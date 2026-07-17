<%@ Page
    Language="c#"
    CodeBehind="EmployeeProfileCorrectionRequests.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EmployeeProfileCorrectionRequests"
    MasterPageFile="~/Default.master" %>

<%-- P6.4.3 员工资料更正请求后台处理页：只处理请求状态，不直接修改员工主数据。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="20">&nbsp;</td>
            <td>
                <table width="100%" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">Employee Profile Correction Requests</td>
                    </tr>
                    <tr>
                        <td><hr noshade size="1"></td>
                    </tr>
                    <tr>
                        <td class="Normal">
                            <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
                            &nbsp;
                            <a class="CommandButton" href="OperationAudits.aspx">Operation Audits</a>
                        </td>
                    </tr>
                </table>

                <asp:Label ID="MessageLabel" CssClass="NormalRed" EnableViewState="false" runat="server" />

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td width="90" class="SubHead">Status:</td>
                        <td width="170">
                            <asp:DropDownList ID="StatusFilterList" CssClass="NormalTextBox" runat="server" />
                        </td>
                        <td>
                            <asp:LinkButton
                                ID="SearchButton"
                                Text="Search"
                                CssClass="CommandButton"
                                CausesValidation="False"
                                OnClick="SearchButton_Click"
                                runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td colspan="3" class="Normal">
                            <asp:Label ID="ResultLabel" runat="server" />
                        </td>
                    </tr>
                </table>

                <asp:Repeater ID="RequestsRepeater" OnItemCommand="RequestsRepeater_ItemCommand" runat="server">
                    <HeaderTemplate>
                        <table width="100%" cellspacing="0" cellpadding="3" border="1">
                            <tr class="SubHead">
                                <td width="70">ID</td>
                                <td width="145">Submitted UTC</td>
                                <td width="110">Employee</td>
                                <td width="120">User</td>
                                <td width="110">Field</td>
                                <td>Current / Proposed</td>
                                <td width="95">Status</td>
                                <td width="210">Review</td>
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
                                    <div>Current: <%#: Eval("CurrentValueSnapshot") %></div>
                                    <div>Proposed: <%#: Eval("ProposedValue") %></div>
                                    <div>Note: <%#: Eval("RequestNote") %></div>
                                    <div>Review: <%#: Eval("ReviewText") %></div>
                                </td>
                                <td><%#: Eval("RequestStatus") %></td>
                                <td>
                                    <asp:TextBox ID="ReviewNoteTextBox" CssClass="NormalTextBox" Width="190" MaxLength="1000" TextMode="MultiLine" Rows="3" runat="server" />
                                    <br>
                                    <asp:LinkButton ID="ReviewedButton" Text="Reviewed" CssClass="CommandButton" CommandName="Reviewed" CommandArgument='<%# Eval("RequestId") %>' CausesValidation="False" runat="server" />
                                    &nbsp;
                                    <asp:LinkButton ID="ClosedButton" Text="Close" CssClass="CommandButton" CommandName="Closed" CommandArgument='<%# Eval("RequestId") %>' CausesValidation="False" runat="server" />
                                    &nbsp;
                                    <asp:LinkButton ID="RejectedButton" Text="Reject" CssClass="CommandButton" CommandName="Rejected" CommandArgument='<%# Eval("RequestId") %>' CausesValidation="False" runat="server" />
                                </td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </td>
        </tr>
    </table>
</asp:Content>
