<%@ Page
    Language="c#"
    CodeBehind="WorkItems.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.WorkItems"
    MasterPageFile="~/Default.master" %>

<%-- P12.3 轻量待办后台入口：第一版只读集中查看，不提供流程设计器或转办。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="portal-admin-page portal-admin-work-items">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Work Items</h1>
                <p class="Normal portal-admin-subtitle">Review lightweight business work items and their current handling state.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="EmployeeProfileCorrectionRequests.aspx">Correction Requests</a>
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
                <h2 class="Head portal-section-title">Current Work Items</h2>
            </div>
            <div class="portal-table-wrap">
                <asp:Repeater ID="WorkItemsRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="70" class="SubHead">ID</th>
                                <th scope="col" width="95" class="SubHead">Status</th>
                                <th scope="col" width="180" class="SubHead">Business</th>
                                <th scope="col" class="SubHead">Title / Summary</th>
                                <th scope="col" width="190" class="SubHead">Assigned To</th>
                                <th scope="col" width="145" class="SubHead">Created UTC</th>
                                <th scope="col" width="145" class="SubHead">Completed UTC</th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("WorkItemId") %></td>
                                <td><%#: Eval("WorkItemStatus") %></td>
                                <td>
                                    <div class="portal-value-stack">
                                        <div><%#: Eval("BusinessKind") %></div>
                                        <div><a class="CommandButton" href='<%#: Eval("BusinessUrl") %>'>Open Source</a></div>
                                    </div>
                                </td>
                                <td>
                                    <div class="portal-value-stack">
                                        <div class="SubHead"><%#: Eval("Title") %></div>
                                        <div><%#: Eval("Summary") %></div>
                                    </div>
                                </td>
                                <td><%#: Eval("AssignedText") %></td>
                                <td><%#: Eval("CreatedUtcText") %></td>
                                <td><%#: Eval("CompletedUtcText") %></td>
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
