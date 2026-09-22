<%@ Page
    Language="c#"
    CodeBehind="EmployeeDirectory.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EmployeeDirectory"
    MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

<%--
  <lang>
    <zh-CN>P6.3-S4 员工组织目录页的列表本身只读，新增和编辑交给独立维护页处理。</zh-CN>
    <en>The P6.3-S4 employee and organization directory list is read-only; creation and editing are handled by dedicated maintenance pages.</en>
  </lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
      <lang>
        <zh-CN>员工目录页只重构展示壳，数据读取、新增和编辑入口保持既有行为。</zh-CN>
        <en>The employee directory page only rebuilds the presentation shell; data reads, creation, and edit entry behavior remain unchanged.</en>
      </lang>
    --%>
    <div class="portal-admin-page portal-admin-employee-directory">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title"><%= lang.Admin_EmployeeDirectory_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_EmployeeDirectory_Subtitle %></p>
            </div>
            <%--
              <lang>
                <zh-CN>页面自有导航入口：本页刻意汇集跨族入口——System Health 属 Admin.Ops 族、User Administration 属 Admin.Account 族，其余属 Admin.Capability 族；渲染器按族输出、只表达同族入口，无法覆盖这些跨族链接，故整块保留在页面内并只做文案本地化。其中新建组织单元、新建员工、绑定用户／员工同时是本页的维护动作入口。</zh-CN>
                <en>Page-owned navigation entries: this page deliberately aggregates cross-group entries - System Health belongs to Admin.Ops, User Administration to Admin.Account, and the rest to Admin.Capability. The renderer is group-scoped and expresses only same-group entries, so it cannot cover these cross-group links and the block stays in the page, localized only. New organization unit, new employee, and bind user/employee are also this page's maintenance actions.</en>
              </lang>
            --%>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="SystemHealth.aspx"><%= lang.Admin_EmployeeDirectory_LinkSystemHealth %></a>
                <a class="CommandButton" href="ManageUsers.aspx"><%= lang.Admin_EmployeeDirectory_LinkUserAdministration %></a>
                <a class="CommandButton" href="OrganizationUnitEdit.aspx"><%= lang.Admin_EmployeeDirectory_LinkNewOrganizationUnit %></a>
                <a class="CommandButton" href="EmployeeEdit.aspx"><%= lang.Admin_EmployeeDirectory_LinkNewEmployee %></a>
                <a class="CommandButton" href="UserEmployeeBindingEdit.aspx"><%= lang.Admin_EmployeeDirectory_LinkBindUserEmployee %></a>
                <a class="CommandButton" href="EmployeeProfileCorrectionRequests.aspx"><%= lang.Admin_EmployeeDirectory_LinkProfileCorrections %></a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

        <div class="portal-admin-section portal-filter-panel">
            <%--
                <lang>
                    <zh-CN>筛选字段只构造目录查询条件；关键字、员工状态、绑定状态和停用组织选项仍由服务器规范化并决定可见结果。</zh-CN>
                    <en>These filters only construct directory query criteria; the server still normalizes keyword, employee status, binding status, and inactive-organization choices before deciding visible results.</en>
                </lang>
            --%>
            <div class="portal-filter-grid">
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_EmployeeDirectory_LabelKeyword %></span>
                    <asp:TextBox ID="KeywordTextBox" CssClass="NormalTextBox portal-filter-input" Width="150" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_EmployeeDirectory_LabelEmployeeStatus %></span>
                    <asp:DropDownList ID="EmployeeStatusList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_EmployeeDirectory_LabelBindingStatus %></span>
                    <asp:DropDownList ID="BindingStatusList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                </div>
                <div class="portal-filter-field portal-checkbox-field">
                    <asp:CheckBox ID="IncludeInactiveOrganizations" Text="<%$ Resources:lang, Admin_EmployeeDirectory_CheckboxIncludeInactiveOrganizations %>" runat="server" />
                </div>
                <div class="portal-filter-actions">
                    <asp:LinkButton
                        ID="SearchButton"
                        Text="<%$ Resources:lang, Admin_EmployeeDirectory_ButtonSearch %>"
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
                <h2 class="Head portal-section-title"><%= lang.Admin_EmployeeDirectory_SectionOrganizations %></h2>
            </div>
            <div class="portal-table-wrap">
                <%--
                    <lang>
                        <zh-CN>组织行使用编码绑定的只读字段，并把编辑入口指向服务器生成的 URL；页面不在表格中自行推断组织权限或关系。</zh-CN>
                        <en>Organization rows use encoded read-only bindings and link to server-generated edit URLs; the table does not infer organization permissions or relationships on its own.</en>
                    </lang>
                --%>
                <asp:Repeater ID="OrganizationsRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="70" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnId %></th>
                                <th scope="col" width="120" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnCode %></th>
                                <th scope="col" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnOrgName %></th>
                                <th scope="col" width="190" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnParent %></th>
                                <th scope="col" width="70" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnSort %></th>
                                <th scope="col" width="80" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnActive %></th>
                                <th scope="col" width="70" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnAction %></th>
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
                                        Text="<%$ Resources:lang, Admin_EmployeeDirectory_ButtonEdit %>"
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
                <h2 class="Head portal-section-title"><%= lang.Admin_EmployeeDirectory_SectionEmployees %></h2>
            </div>
            <div class="portal-table-wrap">
                <%--
                    <lang>
                        <zh-CN>员工行同时提供编辑和绑定入口，绑定 URL 只针对员工记录生成；编码输出与操作授权仍由服务器端模型和 code-behind 负责。</zh-CN>
                        <en>Employee rows expose edit and binding entries, with binding URLs generated only for employee records; encoded output and operation authorization remain owned by the server model and code-behind.</en>
                    </lang>
                --%>
                <asp:Repeater ID="EmployeesRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="110" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnEmployeeCode %></th>
                                <th scope="col" width="140" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnEmployeeName %></th>
                                <th scope="col" width="120" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnPreferred %></th>
                                <th scope="col" width="180" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnWorkEmail %></th>
                                <th scope="col" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnOrganization %></th>
                                <th scope="col" width="95" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnStatus %></th>
                                <th scope="col" width="90" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnSource %></th>
                                <th scope="col" width="100" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnAction %></th>
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
                                        Text="<%$ Resources:lang, Admin_EmployeeDirectory_ButtonEdit %>"
                                        NavigateUrl='<%# Eval("EditUrl") %>'
                                        runat="server" />
                                    <%--
                                      <lang>
                                        <zh-CN>员工账号绑定以员工行为入口，避免组织行误引用不存在的绑定地址。</zh-CN>
                                        <en>User-employee binding is exposed from employee rows to avoid organization rows referencing a binding URL that does not exist.</en>
                                      </lang>
                                    --%>
                                    <asp:HyperLink
                                        CssClass="CommandButton"
                                        Text="<%$ Resources:lang, Admin_EmployeeDirectory_ButtonBind %>"
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
                <h2 class="Head portal-section-title"><%= lang.Admin_EmployeeDirectory_SectionBindings %></h2>
            </div>
            <div class="portal-table-wrap">
                <asp:Repeater ID="BindingsRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="80" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnBindingId %></th>
                                <th scope="col" width="80" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnUserId %></th>
                                <th scope="col" width="140" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnUserName %></th>
                                <th scope="col" width="120" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnEmployeeCode %></th>
                                <th scope="col" width="150" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnBindingEmployeeName %></th>
                                <th scope="col" width="95" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnStatus %></th>
                                <th scope="col" width="155" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnBoundUtc %></th>
                                <th scope="col" width="80" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnAction %></th>
                                <th scope="col" class="SubHead"><%= lang.Admin_EmployeeDirectory_ColumnReason %></th>
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
                                        Text="<%$ Resources:lang, Admin_EmployeeDirectory_ButtonManage %>"
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
