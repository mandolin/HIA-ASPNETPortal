<%@ Page
    Language="c#"
    CodeBehind="EmployeeDirectory.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EmployeeDirectory"
    MasterPageFile="~/Default.master" %>

<%-- P6.3-S4 员工组织目录页：列表本身只读，新增和编辑交给独立维护页处理。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 员工目录页只重构展示壳，数据读取、新增和编辑入口保持既有行为。 --%>
    <div class="portal-admin-page portal-admin-employee-directory">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Employee Directory</h1>
                <p class="Normal portal-admin-subtitle">Read-only overview for organizations, employees, and user bindings.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="SystemHealth.aspx">System Health</a>
                <a class="CommandButton" href="ManageUsers.aspx">User Administration</a>
                <a class="CommandButton" href="OrganizationUnitEdit.aspx">New Organization Unit</a>
                <a class="CommandButton" href="EmployeeEdit.aspx">New Employee</a>
                <a class="CommandButton" href="UserEmployeeBindingEdit.aspx">Bind User/Employee</a>
                <a class="CommandButton" href="EmployeeProfileCorrectionRequests.aspx">Profile Corrections</a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

        <div class="portal-admin-section portal-filter-panel">
            <div class="portal-filter-grid">
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">Keyword</span>
                    <asp:TextBox ID="KeywordTextBox" CssClass="NormalTextBox portal-filter-input" Width="150" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">Employee Status</span>
                    <asp:DropDownList ID="EmployeeStatusList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">Binding Status</span>
                    <asp:DropDownList ID="BindingStatusList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                </div>
                <div class="portal-filter-field portal-checkbox-field">
                    <asp:CheckBox ID="IncludeInactiveOrganizations" Text="Include inactive organization units" runat="server" />
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
                <asp:Label ID="SchemaStatusLabel" runat="server" />
            </div>
            <div class="Normal portal-status-line">
                <asp:Label ID="ResultLabel" runat="server" />
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Organization Units</h2>
            </div>
            <div class="portal-table-wrap">
                <asp:Repeater ID="OrganizationsRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="70" class="SubHead">ID</th>
                                <th scope="col" width="120" class="SubHead">Code</th>
                                <th scope="col" class="SubHead">Name</th>
                                <th scope="col" width="190" class="SubHead">Parent</th>
                                <th scope="col" width="70" class="SubHead">Sort</th>
                                <th scope="col" width="80" class="SubHead">Active</th>
                                <th scope="col" width="70" class="SubHead">Action</th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("OrganizationUnitId") %></td>
                                <td><%#: Eval("OrganizationCode") %></td>
                                <td><%#: Eval("DisplayName") %></td>
                                <td><%#: Eval("ParentText") %></td>
                                <td><%#: Eval("SortOrder") %></td>
                                <td><%#: Eval("IsActiveText") %></td>
                                <td>
                                    <asp:HyperLink
                                        CssClass="CommandButton"
                                        Text="Edit"
                                        NavigateUrl='<%# Eval("EditUrl") %>'
                                        runat="server" />
                                </td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Employees</h2>
            </div>
            <div class="portal-table-wrap">
                <asp:Repeater ID="EmployeesRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="110" class="SubHead">Employee Code</th>
                                <th scope="col" width="140" class="SubHead">Name</th>
                                <th scope="col" width="120" class="SubHead">Preferred</th>
                                <th scope="col" width="180" class="SubHead">Work Email</th>
                                <th scope="col" class="SubHead">Organization</th>
                                <th scope="col" width="95" class="SubHead">Status</th>
                                <th scope="col" width="90" class="SubHead">Source</th>
                                <th scope="col" width="100" class="SubHead">Action</th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("EmployeeCode") %></td>
                                <td><%#: Eval("DisplayName") %></td>
                                <td><%#: Eval("PreferredName") %></td>
                                <td><%#: Eval("WorkEmail") %></td>
                                <td><%#: Eval("OrganizationText") %></td>
                                <td><%#: Eval("EmploymentStatus") %></td>
                                <td><%#: Eval("SourceSystem") %></td>
                                <td>
                                    <asp:HyperLink
                                        CssClass="CommandButton"
                                        Text="Edit"
                                        NavigateUrl='<%# Eval("EditUrl") %>'
                                        runat="server" />
                                    <%-- 员工账号绑定以员工行为入口，避免组织行误引用不存在的绑定地址。 --%>
                                    <asp:HyperLink
                                        CssClass="CommandButton"
                                        Text="Bind"
                                        NavigateUrl='<%# Eval("BindUrl") %>'
                                        runat="server" />
                                </td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Portal User Bindings</h2>
            </div>
            <div class="portal-table-wrap">
                <asp:Repeater ID="BindingsRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="80" class="SubHead">Binding ID</th>
                                <th scope="col" width="80" class="SubHead">User ID</th>
                                <th scope="col" width="140" class="SubHead">User Name</th>
                                <th scope="col" width="120" class="SubHead">Employee Code</th>
                                <th scope="col" width="150" class="SubHead">Employee Name</th>
                                <th scope="col" width="95" class="SubHead">Status</th>
                                <th scope="col" width="155" class="SubHead">Bound UTC</th>
                                <th scope="col" width="80" class="SubHead">Action</th>
                                <th scope="col" class="SubHead">Reason</th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("BindingId") %></td>
                                <td><%#: Eval("UserId") %></td>
                                <td><%#: Eval("UserName") %></td>
                                <td><%#: Eval("EmployeeCode") %></td>
                                <td><%#: Eval("EmployeeDisplayName") %></td>
                                <td><%#: Eval("BindingStatus") %></td>
                                <td><%#: Eval("BoundUtcText") %></td>
                                <td>
                                    <asp:HyperLink
                                        CssClass="CommandButton"
                                        Text="Manage"
                                        NavigateUrl='<%# Eval("EditUrl") %>'
                                        runat="server" />
                                </td>
                                <td><%#: Eval("Reason") %></td>
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
