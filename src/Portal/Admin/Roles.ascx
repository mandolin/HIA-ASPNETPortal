<%@ Control Inherits="ASPNET.StarterKit.Portal.Roles" CodeBehind="Roles.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title runat="server" id="Title1" />

<%--
    <lang>
        <zh-CN>旧角色入口保留真实创建、改名、删除和成员管理行为，并继续同步旧分号授权字符串。</zh-CN>
        <en>The legacy role entry preserves real create, rename, delete, and membership management behavior while continuing to synchronize legacy semicolon-delimited authorization strings.</en>
    </lang>
--%>
<div class="portal-admin-page portal-legacy-admin-module portal-legacy-roles">
    <div class="portal-admin-header">
        <div class="portal-admin-heading">
            <h2 class="Head portal-admin-title">Legacy Role Administration</h2>
            <p class="Normal portal-admin-subtitle">Manage legacy portal roles and enter role membership management.</p>
        </div>
        <div class="portal-admin-actions">
            <%--
                <lang>
                    <zh-CN>新增角色是写库入口，仍通过 AddRole_Click 执行服务器端创建、命名约束和授权边界。</zh-CN>
                    <en>Adding a role is a persistence entry point and still uses AddRole_Click for server-side creation, naming constraints, and authorization boundaries.</en>
                </lang>
            --%>
            <asp:LinkButton
                ID="AddRoleBtn"
                CssClass="portal-button portal-button-primary"
                Text="Add New Role"
                CausesValidation="False"
                OnClick="AddRole_Click"
                runat="server" />
        </div>
    </div>

    <asp:Label ID="Message" CssClass="NormalRed portal-status-line" runat="server" />

    <div class="portal-admin-section">
        <div class="portal-section-header">
            <h3 class="Head portal-section-title">Portal Roles</h3>
        </div>
        <div class="portal-chip-list-wrap">
            <%--
                <lang>
                    <zh-CN>DataList 以 RoleID 作为命令键并保留角色行事件；显示名称采用编码绑定，编辑和删除必须由服务器重新确认目标与权限。</zh-CN>
                    <en>The DataList keeps RoleID as the command key and retains role-row events; the name uses encoded binding, while edit and delete must be rechecked by the server for target and authorization.</en>
                </lang>
            --%>
            <asp:DataList
                ID="rolesList"
                CssClass="portal-chip-list portal-legacy-list"
                RepeatColumns="1"
                DataKeyField="RoleID"
                OnItemCommand="RolesList_ItemCommand"
                runat="server">
                <ItemTemplate>
                    <div class="portal-chip-item portal-legacy-role-item">
                        <asp:Label
                            Text='<%#: DataBinder.Eval(Container.DataItem, "RoleName") %>'
                            CssClass="Normal portal-chip-text"
                            runat="server" />
                        <div class="portal-row-actions">
                            <asp:LinkButton
                                Text="Edit"
                                CommandName="edit"
                                CssClass="portal-button portal-button-secondary portal-button-compact"
                                CausesValidation="False"
                                runat="server" />
                            <asp:LinkButton
                                Text="Delete"
                                CommandName="delete"
                                CssClass="portal-button portal-button-danger portal-button-compact"
                                CausesValidation="False"
                                runat="server" />
                        </div>
                    </div>
                </ItemTemplate>
                <EditItemTemplate>
                    <%--
                        <lang>
                            <zh-CN>编辑模板允许改名或进入成员管理，但角色删除保护、旧分号授权同步和保存结果仍由 code-behind 决定。</zh-CN>
                            <en>The edit template allows renaming or entering membership management, while deletion protection, legacy semicolon-authorization synchronization, and save results remain decided by the code-behind.</en>
                        </lang>
                    --%>
                    <div class="portal-chip-item portal-legacy-role-item portal-legacy-role-edit">
                        <asp:TextBox
                            ID="roleName"
                            CssClass="NormalTextBox portal-form-input"
                            MaxLength="50"
                            Text='<%# DataBinder.Eval(Container.DataItem, "RoleName") %>'
                            runat="server" />
                        <div class="portal-row-actions">
                            <asp:LinkButton
                                Text="Apply"
                                CommandName="apply"
                                CssClass="portal-button portal-button-primary portal-button-compact"
                                runat="server" />
                            <asp:LinkButton
                                Text="Change Role Members"
                                CommandName="members"
                                CssClass="portal-button portal-button-secondary portal-button-compact"
                                runat="server" />
                        </div>
                    </div>
                </EditItemTemplate>
            </asp:DataList>
        </div>
        <p class="Normal portal-status-line">Role deletion is blocked when members or authorization references still exist.</p>
    </div>
</div>
