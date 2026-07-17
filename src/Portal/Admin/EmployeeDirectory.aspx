<%@ Page
    Language="c#"
    CodeBehind="EmployeeDirectory.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EmployeeDirectory"
    MasterPageFile="~/Default.master" %>

<%-- P6.3-S4 员工组织目录页：列表本身只读，新增和编辑交给独立维护页处理。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="20">&nbsp;</td>
            <td>
                <table width="100%" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">Employee Directory</td>
                    </tr>
                    <tr>
                        <td><hr noshade size="1"></td>
                    </tr>
                    <tr>
                        <td class="Normal">
                            <a class="CommandButton" href="SystemHealth.aspx">System Health</a>
                            &nbsp;
                            <a class="CommandButton" href="ManageUsers.aspx">User Administration</a>
                            &nbsp;
                            <a class="CommandButton" href="OrganizationUnitEdit.aspx">New Organization Unit</a>
                            &nbsp;
                            <a class="CommandButton" href="EmployeeEdit.aspx">New Employee</a>
                            &nbsp;
                            <a class="CommandButton" href="UserEmployeeBindingEdit.aspx">Bind User/Employee</a>
                        </td>
                    </tr>
                </table>

                <asp:Label ID="MessageLabel" CssClass="NormalRed" EnableViewState="false" runat="server" />

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td width="95" class="SubHead">Keyword:</td>
                        <td width="170">
                            <asp:TextBox ID="KeywordTextBox" CssClass="NormalTextBox" Width="150" runat="server" />
                        </td>
                        <td width="110" class="SubHead">Employee Status:</td>
                        <td width="150">
                            <asp:DropDownList ID="EmployeeStatusList" CssClass="NormalTextBox" runat="server" />
                        </td>
                        <td width="100" class="SubHead">Binding Status:</td>
                        <td width="150">
                            <asp:DropDownList ID="BindingStatusList" CssClass="NormalTextBox" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="SubHead">Organizations:</td>
                        <td colspan="3" class="Normal">
                            <asp:CheckBox ID="IncludeInactiveOrganizations" Text="Include inactive organization units" runat="server" />
                        </td>
                        <td colspan="2">
                            <asp:LinkButton
                                ID="SearchButton"
                                Text="Search"
                                CssClass="CommandButton"
                                CausesValidation="False"
                                OnClick="SearchButton_Click"
                                runat="server" />
                        </td>
                    </tr>
                </table>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr>
                        <td class="Normal">
                            <asp:Label ID="SchemaStatusLabel" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="Normal">
                            <asp:Label ID="ResultLabel" runat="server" />
                        </td>
                    </tr>
                </table>

                <br>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr><td class="Head">Organization Units</td></tr>
                </table>
                <asp:Repeater ID="OrganizationsRepeater" runat="server">
                    <HeaderTemplate>
                        <table width="100%" cellspacing="0" cellpadding="3" border="1">
                            <tr class="SubHead">
                                <td width="70">ID</td>
                                <td width="120">Code</td>
                                <td>Name</td>
                                <td width="190">Parent</td>
                                <td width="70">Sort</td>
                                <td width="80">Active</td>
                                <td width="70">Action</td>
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
                                    &nbsp;
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

                <br>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr><td class="Head">Employees</td></tr>
                </table>
                <asp:Repeater ID="EmployeesRepeater" runat="server">
                    <HeaderTemplate>
                        <table width="100%" cellspacing="0" cellpadding="3" border="1">
                            <tr class="SubHead">
                                <td width="110">Employee Code</td>
                                <td width="140">Name</td>
                                <td width="120">Preferred</td>
                                <td width="180">Work Email</td>
                                <td>Organization</td>
                                <td width="95">Status</td>
                                <td width="90">Source</td>
                                <td width="70">Action</td>
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
                                </td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>

                <br>

                <table width="100%" cellspacing="0" cellpadding="3" border="0">
                    <tr><td class="Head">Portal User Bindings</td></tr>
                </table>
                <asp:Repeater ID="BindingsRepeater" runat="server">
                    <HeaderTemplate>
                        <table width="100%" cellspacing="0" cellpadding="3" border="1">
                            <tr class="SubHead">
                                <td width="80">Binding ID</td>
                                <td width="80">User ID</td>
                                <td width="140">User Name</td>
                                <td width="120">Employee Code</td>
                                <td width="150">Employee Name</td>
                                <td width="95">Status</td>
                                <td width="155">Bound UTC</td>
                                <td width="80">Action</td>
                                <td>Reason</td>
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
            </td>
        </tr>
    </table>
</asp:Content>
