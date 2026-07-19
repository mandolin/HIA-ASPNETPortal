<%@ Page Language="c#" CodeBehind="TabLayout.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.TabLayout"
    MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 只重构页面外观，保留所有 WebForms 控件 ID、事件和自定义属性以维持旧排序逻辑。 --%>
    <div class="portal-admin-page portal-admin-tab-layout">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Tab Name and Layout</h1>
                <p class="Normal portal-admin-subtitle">Configure tab metadata, access roles, and module placement.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="ModuleCatalog.aspx">Module Catalog</a>
                <a class="CommandButton" href="SecurityRoles.aspx">Security Roles</a>
            </div>
        </div>

        <asp:Label ID="Message" CssClass="NormalRed portal-status-line" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Tab Settings</h2>
            </div>
            <div class="portal-form-grid portal-tab-settings-grid">
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Tab Name</span>
                    <asp:TextBox ID="tabName" CssClass="NormalTextBox portal-form-input" runat="server" OnTextChanged="TabSettings_Change" />
                </div>
                <div class="portal-form-field portal-form-field-wide">
                    <span class="SubHead portal-form-label">Authorized Roles</span>
                    <asp:CheckBoxList ID="authRoles" CssClass="portal-role-checklist" RepeatColumns="2" Font-Size="8pt"
                        runat="server" OnSelectedIndexChanged="TabSettings_Change" />
                </div>
                <div class="portal-form-field portal-checkbox-field">
                    <span class="SubHead portal-form-label">Mobile Visibility</span>
                    <asp:CheckBox ID="showMobile" Text="Show to mobile users" Font-Size="8pt" runat="server"
                        OnCheckedChanged="TabSettings_Change" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Mobile Tab Name</span>
                    <asp:TextBox ID="mobileTabName" CssClass="NormalTextBox portal-form-input" runat="server"
                        OnTextChanged="TabSettings_Change" />
                </div>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Add Module</h2>
            </div>
            <div class="portal-form-grid portal-module-add-grid">
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Module Type</span>
                    <asp:DropDownList ID="moduleType" CssClass="NormalTextBox portal-form-input" DataValueField="ModuleDefID"
                        DataTextField="FriendlyName" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Module Name</span>
                    <asp:TextBox ID="moduleTitle" EnableViewState="false" Text="New Module Name" CssClass="NormalTextBox portal-form-input"
                        runat="server" />
                </div>
                <div class="portal-form-field portal-form-actions-field">
                    <span class="SubHead portal-form-label">&nbsp;</span>
                    <asp:LinkButton ID="AddModuleBtn" CssClass="CommandButton" Text="Add to Organize Modules"
                        runat="server" OnClick="AddModuleToPane_Click" />
                </div>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Organize Modules</h2>
            </div>
            <div class="portal-layout-board">
                <div class="portal-layout-panes">
                    <div class="portal-layout-pane portal-layout-pane-mini">
                        <h3 class="SubHead portal-layout-pane-title">Left Mini Pane</h3>
                        <div class="portal-layout-pane-body">
                            <asp:ListBox ID="leftPane" CssClass="NormalTextBox portal-layout-list" DataSource="<%# leftList %>" DataTextField="ModuleTitle"
                                DataValueField="ModuleId" Width="100%" Rows="9" runat="server" />
                            <div class="portal-layout-toolbar">
                                <asp:LinkButton ID="LeftUpBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="Up" CommandName="up" CommandArgument="leftPane"
                                    ToolTip="Move selected module up in list" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="LeftRightBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="Right" CommandName="right" sourcepane="leftPane"
                                    targetpane="contentPane" ToolTip="Move selected module to the content pane"
                                    runat="server" OnClick="RightLeft_Click" />
                                <asp:LinkButton ID="LeftDownBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="Down" CommandName="down" CommandArgument="leftPane"
                                    ToolTip="Move selected module down in list" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="LeftEditBtn" CssClass="CommandButton portal-layout-command portal-primary-action" Text="Edit" CommandName="edit" CommandArgument="leftPane"
                                    ToolTip="Edit this item" runat="server" OnClick="EditBtn_Click" />
                                <asp:LinkButton ID="LeftDeleteBtn" CssClass="CommandButton portal-layout-command portal-danger-action" Text="Delete" CommandName="delete" CommandArgument="leftPane"
                                    ToolTip="Delete this item" runat="server" OnClick="DeleteBtn_Click" />
                            </div>
                        </div>
                    </div>

                    <div class="portal-layout-pane portal-layout-pane-content">
                        <h3 class="SubHead portal-layout-pane-title">Content Pane</h3>
                        <div class="portal-layout-pane-body">
                            <asp:ListBox ID="contentPane" CssClass="NormalTextBox portal-layout-list" DataSource="<%# contentList %>" DataTextField="ModuleTitle"
                                DataValueField="ModuleId" Width="100%" Rows="9" runat="server" />
                            <div class="portal-layout-toolbar portal-layout-toolbar-wide">
                                <asp:LinkButton ID="ContentUpBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="Up" CommandName="up" CommandArgument="contentPane"
                                    ToolTip="Move selected module up in list" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="ContentLeftBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="Left" sourcepane="contentPane" targetpane="leftPane"
                                    ToolTip="Move selected module to the left pane" runat="server" OnClick="RightLeft_Click" />
                                <asp:LinkButton ID="ContentRightBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="Right" sourcepane="contentPane" targetpane="rightPane"
                                    ToolTip="Move selected module to the right pane" runat="server" OnClick="RightLeft_Click" />
                                <asp:LinkButton ID="ContentDownBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="Down" CommandName="down" CommandArgument="contentPane"
                                    ToolTip="Move selected module down in list" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="ContentEditBtn" CssClass="CommandButton portal-layout-command portal-primary-action" Text="Edit" CommandName="edit" CommandArgument="contentPane"
                                    ToolTip="Edit this item" runat="server" OnClick="EditBtn_Click" />
                                <asp:LinkButton ID="ContentDeleteBtn" CssClass="CommandButton portal-layout-command portal-danger-action" Text="Delete" CommandName="delete" CommandArgument="contentPane"
                                    ToolTip="Delete this item" runat="server" OnClick="DeleteBtn_Click" />
                            </div>
                        </div>
                    </div>

                    <div class="portal-layout-pane portal-layout-pane-mini">
                        <h3 class="SubHead portal-layout-pane-title">Right Mini Pane</h3>
                        <div class="portal-layout-pane-body">
                            <asp:ListBox ID="rightPane" CssClass="NormalTextBox portal-layout-list" DataSource="<%# rightList %>" DataTextField="ModuleTitle"
                                DataValueField="ModuleId" Width="100%" Rows="9" runat="server" />
                            <div class="portal-layout-toolbar">
                                <asp:LinkButton ID="RightUpBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="Up" CommandName="up" CommandArgument="rightPane"
                                    ToolTip="Move selected module up in list" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="RightLeftBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="Left" sourcepane="rightPane" targetpane="contentPane"
                                    ToolTip="Move selected module to the content pane" runat="server" OnClick="RightLeft_Click" />
                                <asp:LinkButton ID="RightDownBtn" CssClass="CommandButton portal-layout-command portal-secondary-action" Text="Down" CommandName="down" CommandArgument="rightPane"
                                    ToolTip="Move selected module down in list" runat="server" OnClick="UpDown_Click" />
                                <asp:LinkButton ID="RightEditBtn" CssClass="CommandButton portal-layout-command portal-primary-action" Text="Edit" CommandName="edit" CommandArgument="rightPane"
                                    ToolTip="Edit this item" runat="server" OnClick="EditBtn_Click" />
                                <asp:LinkButton ID="RightDeleteBtn" CssClass="CommandButton portal-layout-command portal-danger-action" Text="Delete" CommandName="delete" CommandArgument="rightPane"
                                    ToolTip="Delete this item" runat="server" OnClick="DeleteBtn_Click" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="portal-form-actions">
                <asp:LinkButton ID="applyBtn" CssClass="CommandButton portal-primary-action" Text="Apply Changes" runat="server"
                    OnClick="Apply_Click" />
            </div>
        </div>
    </div>
</asp:Content>
