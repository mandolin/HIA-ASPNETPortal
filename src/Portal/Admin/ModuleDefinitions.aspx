<%@ Page 
    Language="c#" 
    CodeBehind="ModuleDefinitions.aspx.cs" 
    AutoEventWireup="True" 
    Inherits="ASPNET.StarterKit.Portal.ModuleDefinitions" 
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
        <lang>
            <zh-CN>历史模块定义页只维护名称和受保护删除，路径字段保持只读安全边界。</zh-CN>
            <en>The legacy module-definition page only maintains names and protected deletion; source-path fields keep their read-only safety boundary.</en>
        </lang>
    --%>
    <div class="portal-admin-page portal-admin-module-definition-edit">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Module Type Definition</h1>
                <p class="Normal portal-admin-subtitle">Legacy definition maintenance for trusted deployed modules.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="ModuleCatalog.aspx">Module Catalog</a>
                <a class="CommandButton" href="SystemHealth.aspx">System Health</a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

        <div class="portal-status-strip">
            <div class="Normal portal-status-line">
                New module definitions should be registered from verified packages in Module Catalog.
            </div>
            <div class="Normal portal-status-line">
                Desktop and mobile source paths are read-only here to preserve the trusted deployment boundary.
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Definition Metadata</h2>
            </div>
            <%--
              <lang>
                <zh-CN>定义表单只允许友好名称编辑；桌面/移动源路径保持只读安全边界，并由服务器验证受信相对 ASCX 路径。</zh-CN>
                <en>The definition form edits only the friendly name; desktop/mobile source paths remain read-only safety boundaries and the server validates trusted relative ASCX paths.</en>
              </lang>
            --%>
            <div class="portal-form-grid">
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Friendly Name</span>
                    <asp:TextBox
                        ID="FriendlyName"
                        CssClass="NormalTextBox portal-form-input"
                        Columns="30"
                        MaxLength="150"
                        runat="server" />
                    <span class="NormalRed portal-field-value">
                        <asp:RequiredFieldValidator
                            ID="Req1"
                            Display="Dynamic"
                            ErrorMessage="Enter a Module Name"
                            ControlToValidate="FriendlyName"
                            runat="server" />
                    </span>
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Desktop Source</span>
                    <asp:TextBox
                        ID="DesktopSrc"
                        CssClass="NormalTextBox portal-form-input"
                        Columns="30"
                        MaxLength="150"
                        runat="server" />
                    <span class="NormalRed portal-field-value">
                        <asp:RequiredFieldValidator
                            ID="Req2"
                            Display="Dynamic"
                            ErrorMessage="You Must Enter Source Path for the Desktop Module"
                            ControlToValidate="DesktopSrc"
                            runat="server" />
                        <%--
                          <lang>
                            <zh-CN>CustomValidator 只是触发服务器路径规则的标记入口；它不把客户端校验视为路径信任或授权证明。</zh-CN>
                            <en>CustomValidator only invokes the server path rule from markup; client-side validation is not treated as path trust or authorization proof.</en>
                          </lang>
                        --%>
                        <asp:CustomValidator
                            ID="DesktopSrcPathValidator"
                            Display="Dynamic"
                            ErrorMessage="Desktop Source must be a relative .ascx path under DesktopModules/ or Admin/."
                            ControlToValidate="DesktopSrc"
                            OnServerValidate="DesktopSrcPathValidator_ServerValidate"
                            runat="server" />
                    </span>
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Mobile Source</span>
                    <asp:TextBox
                        ID="MobileSrc"
                        CssClass="NormalTextBox portal-form-input"
                        Columns="30"
                        MaxLength="150"
                        runat="server" />
                </div>
            </div>
            <%--
              <lang>
                <zh-CN>更新、取消和删除按钮只发送命令意图；定义身份、实例引用、受信路径、删除保护和审计仍由 code-behind 决定。</zh-CN>
                <en>Update, cancel, and delete buttons submit only command intent; code-behind decides definition identity, instance references, trusted paths, delete protection, and auditing.</en>
              </lang>
            --%>
            <div class="portal-form-actions">
                <asp:LinkButton
                    ID="updateButton"
                    Text="Update"
                    runat="server"
                    CssClass="CommandButton portal-primary-action"
                    OnClick="UpdateBtn_Click" />
                <asp:LinkButton
                    ID="cancelButton"
                    Text="Cancel"
                    CausesValidation="False"
                    runat="server"
                    CssClass="CommandButton portal-secondary-action"
                    OnClick="CancelBtn_Click" />
                <asp:LinkButton
                    ID="deleteButton"
                    Text="Delete this module type"
                    CausesValidation="False"
                    runat="server"
                    CssClass="CommandButton portal-danger-action"
                    OnClick="DeleteBtn_Click" />
            </div>
        </div>
    </div>
</asp:Content>
