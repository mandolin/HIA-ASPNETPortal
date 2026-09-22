<%@ Page Language="c#" CodeBehind="TabLayout.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.TabLayout"
    MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
        <lang>
            <zh-CN>只重构页面外观，保留所有 WebForms 控件 ID、事件和自定义属性以维持旧排序逻辑。</zh-CN>
            <en>Only the page presentation is rebuilt; all WebForms control IDs, events, and custom attributes are preserved to keep the legacy ordering logic intact.</en>
        </lang>
    --%>
    <div class="portal-admin-page portal-admin-tab-layout">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title"><%= lang.Admin_TabLayout_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_TabLayout_Subtitle %></p>
            </div>
            <%--
              <lang>
                <zh-CN>页面自有导航入口：Security Roles 仍是旧控件宿主页，registry 中的 Core.Admin.Roles 指向 Roles.ascx、目标并不相同，故该链接无法由渲染器表达；且本页入口属 Admin.Modules 族、渲染器按族输出，故整块保留在页面内并只做文案本地化。</zh-CN>
                <en>Page-owned navigation entries: Security Roles is still the legacy control host page and the registry entry Core.Admin.Roles targets Roles.ascx rather than this page, so the renderer cannot express that link; this page also belongs to Admin.Modules and the renderer is group-scoped, so the block stays in the page and is localized only.</en>
              </lang>
            --%>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="ModuleCatalog.aspx"><%= lang.Admin_TabLayout_LinkModuleCatalog %></a>
                <a class="CommandButton" href="SecurityRoles.aspx"><%= lang.Admin_TabLayout_LinkSecurityRoles %></a>
            </div>
        </div>

        <asp:Label ID="Message" CssClass="NormalRed portal-status-line" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_TabLayout_SectionTabSettings %></h2>
            </div>
            <div class="portal-form-grid portal-tab-settings-grid">
                <%--
                    <lang>
                        <zh-CN>这些字段承载 Tab 元数据、角色授权和移动端可见性；变更事件仍交由 code-behind 处理并由服务器决定最终状态。</zh-CN>
                        <en>These fields carry tab metadata, role authorization, and mobile visibility; change events remain handled by the code-behind, with the server deciding the final state.</en>
                    </lang>
                --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_TabLayout_LabelTabName %></span>
                    <asp:TextBox ID="tabName" CssClass="NormalTextBox portal-form-input" runat="server" OnTextChanged="TabSettings_Change" />
                </div>
                <div class="portal-form-field portal-form-field-wide">
                    <span class="SubHead portal-form-label"><%= lang.Admin_TabLayout_LabelAuthorizedRoles %></span>
                    <asp:CheckBoxList ID="authRoles" CssClass="portal-role-checklist" RepeatColumns="2" Font-Size="8pt"
                        runat="server" OnSelectedIndexChanged="TabSettings_Change" />
                </div>
                <div class="portal-form-field portal-checkbox-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_TabLayout_LabelMobileVisibility %></span>
                    <asp:CheckBox ID="showMobile" Text="<%$ Resources:lang, Admin_TabLayout_CheckboxShowToMobileUsers %>" Font-Size="8pt" runat="server"
                        OnCheckedChanged="TabSettings_Change" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_TabLayout_LabelMobileTabName %></span>
                    <asp:TextBox ID="mobileTabName" CssClass="NormalTextBox portal-form-input" runat="server"
                        OnTextChanged="TabSettings_Change" />
                </div>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_TabLayout_SectionAddModule %></h2>
            </div>
            <div class="portal-form-grid portal-module-add-grid">
                <%--
                    <lang>
                        <zh-CN>新增模块只从服务器提供的定义列表取类型，标题作为当前 Tab 的模块实例名称提交，不能把客户端值视为受信任路径或权限。</zh-CN>
                        <en>Module creation selects a type from the server-provided definition list and submits a title for the current tab instance; client values are not trusted paths or permissions.</en>
                    </lang>
                --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_TabLayout_LabelModuleType %></span>
                    <asp:DropDownList ID="moduleType" CssClass="NormalTextBox portal-form-input" DataValueField="ModuleDefID"
                        DataTextField="FriendlyName" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_TabLayout_LabelModuleName %></span>
                    <asp:TextBox ID="moduleTitle" EnableViewState="false" Text="<%$ Resources:lang, Admin_TabLayout_DefaultNewModuleName %>" CssClass="NormalTextBox portal-form-input"
                        runat="server" />
                </div>
                <div class="portal-form-field portal-form-actions-field">
                    <span class="SubHead portal-form-label">&nbsp;</span>
                    <asp:LinkButton ID="AddModuleBtn" CssClass="CommandButton" Text="<%$ Resources:lang, Admin_TabLayout_ButtonAddToOrganizeModules %>"
                        runat="server" OnClick="AddModuleToPane_Click" />
                </div>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_TabLayout_SectionOrganizeModules %></h2>
            </div>
            <div class="portal-layout-board">
                <%--
                    <lang>
                        <zh-CN>三个 ListBox 及其命令栏共同表达模块所在 pane 和顺序；移动、编辑、删除操作均通过既有事件回调进入服务器状态机。</zh-CN>
                        <en>The three ListBoxes and their command bars together express module pane membership and order; move, edit, and delete actions enter the server state machine through the existing callbacks.</en>
                    </lang>
                --%>
                <div class="portal-layout-panes">
                    <div class="portal-layout-pane portal-layout-pane-mini">
                        <h3 class="SubHead portal-layout-pane-title"><%= lang.Admin_TabLayout_PaneTitleLeft %></h3>
                        <div class="portal-layout-pane-body">
                            <asp:ListBox ID="leftPane" CssClass="NormalTextBox portal-layout-list" DataSource="<%# leftList %>" DataTextField="ModuleTitle"
                                DataValueField="ModuleId" Width="100%" Rows="9" runat="server" />
                            <div class="portal-layout-toolbar">
                                <asp:LinkButton ID="LeftUpBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonUp %>" CommandName="up" CommandArgument="leftPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipMoveUp %>" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="LeftRightBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonRight %>" CommandName="right" sourcepane="leftPane"
                                    targetpane="contentPane" ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipMoveToContentPane %>"
                                    runat="server" OnClick="RightLeft_Click" />
                                <asp:LinkButton ID="LeftDownBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonDown %>" CommandName="down" CommandArgument="leftPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipMoveDown %>" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="LeftEditBtn" CssClass="CommandButton portal-layout-command portal-primary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonEdit %>" CommandName="edit" CommandArgument="leftPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipEditItem %>" runat="server" OnClick="EditBtn_Click" />
                                <asp:LinkButton ID="LeftDeleteBtn" CssClass="CommandButton portal-layout-command portal-danger-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonDelete %>" CommandName="delete" CommandArgument="leftPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipDeleteItem %>" runat="server" OnClick="DeleteBtn_Click" />
                            </div>
                        </div>
                    </div>

                    <div class="portal-layout-pane portal-layout-pane-content">
                        <h3 class="SubHead portal-layout-pane-title"><%= lang.Admin_TabLayout_PaneTitleContent %></h3>
                        <div class="portal-layout-pane-body">
                            <asp:ListBox ID="contentPane" CssClass="NormalTextBox portal-layout-list" DataSource="<%# contentList %>" DataTextField="ModuleTitle"
                                DataValueField="ModuleId" Width="100%" Rows="9" runat="server" />
                            <div class="portal-layout-toolbar portal-layout-toolbar-wide">
                                <asp:LinkButton ID="ContentUpBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonUp %>" CommandName="up" CommandArgument="contentPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipMoveUp %>" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="ContentLeftBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonLeft %>" sourcepane="contentPane" targetpane="leftPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipMoveToLeftPane %>" runat="server" OnClick="RightLeft_Click" />
                                <asp:LinkButton ID="ContentRightBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonRight %>" sourcepane="contentPane" targetpane="rightPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipMoveToRightPane %>" runat="server" OnClick="RightLeft_Click" />
                                <asp:LinkButton ID="ContentDownBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonDown %>" CommandName="down" CommandArgument="contentPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipMoveDown %>" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="ContentEditBtn" CssClass="CommandButton portal-layout-command portal-primary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonEdit %>" CommandName="edit" CommandArgument="contentPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipEditItem %>" runat="server" OnClick="EditBtn_Click" />
                                <asp:LinkButton ID="ContentDeleteBtn" CssClass="CommandButton portal-layout-command portal-danger-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonDelete %>" CommandName="delete" CommandArgument="contentPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipDeleteItem %>" runat="server" OnClick="DeleteBtn_Click" />
                            </div>
                        </div>
                    </div>

                    <div class="portal-layout-pane portal-layout-pane-mini">
                        <h3 class="SubHead portal-layout-pane-title"><%= lang.Admin_TabLayout_PaneTitleRight %></h3>
                        <div class="portal-layout-pane-body">
                            <asp:ListBox ID="rightPane" CssClass="NormalTextBox portal-layout-list" DataSource="<%# rightList %>" DataTextField="ModuleTitle"
                                DataValueField="ModuleId" Width="100%" Rows="9" runat="server" />
                            <div class="portal-layout-toolbar">
                                <asp:LinkButton ID="RightUpBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonUp %>" CommandName="up" CommandArgument="rightPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipMoveUp %>" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="RightLeftBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonLeft %>" sourcepane="rightPane" targetpane="contentPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipMoveToContentPane %>" runat="server" OnClick="RightLeft_Click" />
                                <asp:LinkButton ID="RightDownBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonDown %>" CommandName="down" CommandArgument="rightPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipMoveDown %>" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="RightEditBtn" CssClass="CommandButton portal-layout-command portal-primary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonEdit %>" CommandName="edit" CommandArgument="rightPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipEditItem %>" runat="server" OnClick="EditBtn_Click" />
                                <asp:LinkButton ID="RightDeleteBtn" CssClass="CommandButton portal-layout-command portal-danger-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonDelete %>" CommandName="delete" CommandArgument="rightPane"
                                    ToolTip="<%$ Resources:lang, Admin_TabLayout_TooltipDeleteItem %>" runat="server" OnClick="DeleteBtn_Click" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="portal-form-actions">
                <asp:LinkButton ID="applyBtn" CssClass="CommandButton portal-primary-action" Text="<%$ Resources:lang, Admin_TabLayout_ButtonApplyChanges %>" runat="server"
                    OnClick="Apply_Click" />
            </div>
        </div>
    </div>
</asp:Content>
