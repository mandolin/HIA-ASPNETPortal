<%@ Page Language="c#" CodeBehind="SecurityRoles.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.SecurityRoles" MasterPageFile="~/Default.master" %>

<%--
    The SecurityRoles.aspx page is used to create and edit security roles within
    the Portal application.
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 角色成员页只调整后台展示结构，成员增删仍由原 DataList 命令和 code-behind 控制。 --%>
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
                <%-- note 暂不考虑支持windowsUser机制
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
                    <asp:LinkButton ID="addExisting" CssClass="CommandButton" Text="Add existing user to role"
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
                        <asp:ImageButton ImageUrl="~/images/delete.gif" CommandName="delete" AlternateText="Remove this user from role"
                            CssClass="portal-chip-delete" runat="server" />
                        <asp:Label Text='<%#: DataBinder.Eval(Container.DataItem, "Name") %>' CssClass="Normal portal-chip-text"
                            runat="server" />
                    </ItemTemplate>
                </asp:DataList>
            </div>
        </div>

        <div class="portal-form-actions">
            <asp:LinkButton ID="saveBtn" class="CommandButton" Text="Save Role Changes" runat="server"
                OnClick="Save_Click" />
        </div>
    </div>
</asp:Content>
