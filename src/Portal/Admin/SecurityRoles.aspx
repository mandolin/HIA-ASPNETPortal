<%@ Page Language="c#" CodeBehind="SecurityRoles.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.SecurityRoles" MasterPageFile="~/Default.master" %>

<%--
<lang>
  <zh-CN>角色成员页用于维护选定门户角色下的用户成员关系；角色定义本身仍由旧 Roles/ModuleDefinitions 等后台入口维护。</zh-CN>
  <en>The role-membership page maintains user membership for the selected Portal role; role definitions themselves remain managed by legacy admin entries such as Roles and ModuleDefinitions.</en>
</lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
    <lang>
      <zh-CN>角色成员页只调整后台展示结构，成员增删仍由原 DataList 命令、下拉用户列表和 code-behind 控制，避免改动旧授权字符串同步逻辑。</zh-CN>
      <en>The role-membership page only adjusts the admin display structure; membership add/remove behavior still uses the original DataList commands, user dropdown, and code-behind, avoiding changes to legacy authorization-string synchronization.</en>
    </lang>
    --%>
    <div class="portal-admin-page portal-admin-role-membership">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <span id="title" class="Head portal-admin-title" runat="server">Role Membership</span>
                <p class="Normal portal-admin-subtitle">Manage users assigned to the selected portal role.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
                <a class="CommandButton" href="SystemHealth.aspx">System Health</a>
            </div>
        </div>

        <asp:Label ID="Message" CssClass="NormalRed portal-status-line" runat="server" />

        <div class="portal-admin-section portal-filter-panel">
            <div class="portal-filter-grid">
                <%--
                    <lang>
                      <zh-CN>历史 Windows 用户创建入口仍保留为不可见标记参考；当前阶段不启用该机制，避免绕过门户注册、审核和账号员工绑定流程。</zh-CN>
                      <en>The historical Windows-user creation entry remains as invisible markup reference; this stage keeps it disabled to avoid bypassing Portal registration, approval, and user-employee binding flows.</en>
                    </lang>
                    <div class="portal-filter-field">
                        <asp:TextBox ID="windowsUserName" Text="DOMAIN\username" Visible="False" runat="server" />
                    </div>
                    <div class="portal-filter-actions">
                        <asp:LinkButton ID="addNew" CssClass="CommandButton" Text="Create new user and add to role"
                            Visible="False" runat="server" OnClick="AddUser_Click" />
                    </div>
                --%>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label">User</span>
                    <asp:DropDownList ID="allUsers" CssClass="NormalTextBox portal-filter-input" DataTextField="Name" DataValueField="UserID" runat="server" />
                </div>
                <div class="portal-filter-actions">
                    <asp:LinkButton ID="addExisting" CssClass="CommandButton portal-primary-action" Text="Add existing user to role"
                        runat="server" OnClick="AddUser_Click" />
                </div>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Users In Role</h2>
            </div>
            <div class="portal-chip-list-wrap">
                <asp:DataList ID="usersInRole" CssClass="portal-chip-list" RepeatColumns="2" DataKeyField="UserId" OnItemCommand="usersInRole_ItemCommand" runat="server">
                    <ItemStyle Width="225" CssClass="portal-chip-item" />
                    <ItemTemplate>
                        <asp:LinkButton CommandName="delete" Text="Remove" CssClass="CommandButton portal-chip-delete portal-danger-action"
                            CausesValidation="False" runat="server" />
                        <asp:Label Text='<%#: DataBinder.Eval(Container.DataItem, "Name") %>' CssClass="Normal portal-chip-text"
                            runat="server" />
                    </ItemTemplate>
                </asp:DataList>
            </div>
        </div>

        <div class="portal-form-actions">
            <asp:LinkButton ID="saveBtn" CssClass="CommandButton portal-primary-action" Text="Save Role Changes" runat="server"
                OnClick="Save_Click" />
        </div>
    </div>
</asp:Content>
