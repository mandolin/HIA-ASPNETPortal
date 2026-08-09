<%@ Control Inherits="ASPNET.StarterKit.Portal.Users" CodeBehind="Users.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Import Namespace="Resources" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>
<ASPNETPortal:title runat="server" id="Title1" />

<%--
    <lang>
        <zh-CN>旧用户入口保留显式管理员 POST 行为；不在页面打开时创建用户。</zh-CN>
        <en>The legacy user entry preserves explicit administrator POST behavior and does not create users when the page is merely opened.</en>
    </lang>
--%>
<div class="portal-admin-page portal-legacy-admin-module portal-legacy-users">
    <div class="portal-admin-header">
        <div class="portal-admin-heading">
            <h2 class="Head portal-admin-title">Legacy User Entry</h2>
            <p class="Normal portal-admin-subtitle">Select an existing user, create a placeholder account, or enter the modern user management page.</p>
        </div>
        <div class="portal-admin-actions">
            <%--
                <lang>
                    <zh-CN>新增用户按钮是显式管理员写库入口，继续通过 AddUser_Click 执行服务器端占位账号创建与授权边界。</zh-CN>
                    <en>The add-user button is an explicit administrator persistence entry point and still uses AddUser_Click for server-side placeholder creation and authorization boundaries.</en>
                </lang>
            --%>
            <asp:LinkButton
                ID="btn_AddUser"
                CssClass="portal-button portal-button-primary"
                CommandName="Add"
                Text="<%$ Resources:lang,Admin_User_AddUser %>"
                CausesValidation="False"
                OnClick="AddUser_Click"
                runat="server" />
        </div>
    </div>

    <div class="Normal portal-status-line">
        <asp:Literal ID="Message" runat="server" />
    </div>

    <div class="portal-admin-section">
        <div class="portal-section-header">
            <h3 class="Head portal-section-title"><%=lang.Admin_Users_RegisteredUsers%></h3>
        </div>
        <div class="portal-form-grid portal-legacy-user-grid">
            <div class="portal-form-field portal-form-field-wide">
                <span class="SubHead portal-form-label">User</span>
                <%--
                    <lang>
                        <zh-CN>下拉列表以 UserID 作为服务器目标键、Email 作为展示文本；选中项不能单独证明当前管理员可操作该账号。</zh-CN>
                        <en>The dropdown uses UserID as the server target key and Email as display text; the selected item alone does not prove that the administrator may operate on that account.</en>
                    </lang>
                --%>
                <asp:DropDownList
                    ID="ddl_AllUsers"
                    CssClass="NormalTextBox portal-form-input"
                    DataTextField="Email"
                    DataValueField="UserID"
                    runat="server" />
            </div>
            <div class="portal-form-field portal-form-actions-field">
                <span class="SubHead portal-form-label">Actions</span>
                <div class="portal-action-row portal-legacy-action-stack">
                    <%--
                        <lang>
                            <zh-CN>编辑和删除命令沿用旧回调；删除是不可逆风险动作，服务器必须重新确认目标、权限和引用状态。</zh-CN>
                            <en>Edit and delete commands use the legacy callbacks; deletion is an irreversible-risk action, so the server must recheck target, authorization, and reference state.</en>
                        </lang>
                    --%>
                    <asp:LinkButton
                        ID="btn_EditUser"
                        CssClass="portal-button portal-button-primary portal-button-compact"
                        CommandName="edit"
                        Text="<%$ Resources:lang,Admin_User_EditUser %>"
                        CausesValidation="False"
                        OnClick="EditUser_Click"
                        runat="server" />
                    <asp:LinkButton
                        ID="btn_DeleteUser"
                        CssClass="portal-button portal-button-danger portal-button-compact"
                        Text="<%$ Resources:lang,Admin_User_DelUser %>"
                        CausesValidation="False"
                        OnClick="btn_DeleteUser_Click"
                        runat="server" />
                </div>
                <p class="Normal portal-status-line">Deleting a user is a real write operation and is not executed by the P7 screenshot matrix.</p>
            </div>
        </div>
    </div>
</div>
