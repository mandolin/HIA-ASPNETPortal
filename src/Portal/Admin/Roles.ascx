<%@ Control Inherits="ASPNET.StarterKit.Portal.Roles" CodeBehind="Roles.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title runat="server" id="Title1" />

<%-- 中文 / English: 旧角色入口保留真实创建、改名、删除和成员管理行为，并继续同步旧分号授权字符串。 --%>
<div class="portal-admin-page portal-legacy-admin-module portal-legacy-roles">
    <div class="portal-admin-header">
        <div class="portal-admin-heading">
            <h2 class="Head portal-admin-title">Legacy Role Administration</h2>
            <p class="Normal portal-admin-subtitle">Manage legacy portal roles and enter role membership management.</p>
        </div>
        <div class="portal-admin-actions">
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
