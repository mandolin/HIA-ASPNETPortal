<%@ Page
    Language="c#"
    CodeBehind="OperationAudits.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.OperationAudits"
    MasterPageFile="~/Default.master" %>

<%-- P2.4 只读运营审计页：查询高价值状态变更，不记录普通查看行为。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 运营审计页只调整展示结构，查询、分页和权限逻辑仍由 code-behind 控制。 --%>
    <div class="portal-admin-page portal-admin-audits">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Operation Audits</h1>
                <p class="Normal portal-admin-subtitle">Review high-value administration and workflow changes.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="SystemHealth.aspx">System Health</a>
                <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
            </div>
        </div>

        <div class="portal-admin-section portal-filter-panel">
            <div class="portal-filter-grid">
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">Start UTC</span>
                    <asp:TextBox ID="StartDateTextBox" CssClass="NormalTextBox portal-filter-input" Width="110" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">End UTC</span>
                    <asp:TextBox ID="EndDateTextBox" CssClass="NormalTextBox portal-filter-input" Width="110" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">Category</span>
                    <asp:TextBox ID="CategoryFilter" CssClass="NormalTextBox portal-filter-input" Width="120" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">Action</span>
                    <asp:TextBox ID="ActionFilter" CssClass="NormalTextBox portal-filter-input" Width="110" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">Target ID</span>
                    <asp:TextBox ID="TargetIdFilter" CssClass="NormalTextBox portal-filter-input" Width="150" runat="server" />
                </div>
                <div class="portal-filter-actions">
                    <asp:LinkButton ID="SearchButton" Text="Search" CssClass="CommandButton" CausesValidation="False" OnClick="SearchButton_Click" runat="server" />
                </div>
            </div>
            <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" runat="server" />
        </div>

        <div class="portal-pager">
            <div class="Normal portal-pager-info">
                <asp:Label ID="ResultLabel" runat="server" />
            </div>
            <div class="portal-pager-actions">
                <asp:LinkButton ID="PreviousButton" Text="Previous" CssClass="CommandButton" CausesValidation="False" OnClick="PreviousButton_Click" runat="server" />
                <asp:LinkButton ID="NextButton" Text="Next" CssClass="CommandButton" CausesValidation="False" OnClick="NextButton_Click" runat="server" />
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Audit Entries</h2>
            </div>
            <div class="portal-table-wrap">
                <asp:Repeater ID="EntriesRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="155" class="SubHead">UTC</th>
                                <th scope="col" width="130" class="SubHead">Category</th>
                                <th scope="col" width="110" class="SubHead">Action</th>
                                <th scope="col" width="110" class="SubHead">Actor</th>
                                <th scope="col" width="95" class="SubHead">Target</th>
                                <th scope="col" width="100" class="SubHead">Target ID</th>
                                <th scope="col" class="SubHead">Summary</th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("OccurredUtc", "{0:yyyy-MM-dd HH:mm:ss} UTC") %></td>
                                <td><%#: Eval("Category") %></td>
                                <td><%#: Eval("Action") %></td>
                                <td><%#: Eval("ActorUserName") %></td>
                                <td><%#: Eval("TargetType") %></td>
                                <td><%#: Eval("TargetId") %></td>
                                <td><%#: Eval("Summary") %></td>
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
