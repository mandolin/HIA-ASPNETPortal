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
                                <asp:ImageButton ID="LeftUpBtn" CssClass="portal-icon-button" ImageUrl="~/images/up.gif" CommandName="up" CommandArgument="leftPane"
                                    AlternateText="Move selected module up in list" runat="server" OnClick="UpDown_Click" />
                                <asp:ImageButton ID="LeftRightBtn" CssClass="portal-icon-button" ImageUrl="~/images/rt.gif" CommandName="right" sourcepane="leftPane"
                                    targetpane="contentPane" AlternateText="Move selected module to the content pane"
                                    runat="server" OnClick="RightLeft_Click" />
                                <asp:ImageButton ID="LeftDownBtn" CssClass="portal-icon-button" ImageUrl="~/images/dn.gif" CommandName="down" CommandArgument="leftPane"
                                    AlternateText="Move selected module down in list" runat="server" OnClick="UpDown_Click" />
                                <asp:ImageButton ID="LeftEditBtn" CssClass="portal-icon-button" ImageUrl="~/images/edit.gif" CommandName="edit" CommandArgument="leftPane"
                                    AlternateText="Edit this item" runat="server" OnClick="EditBtn_Click" />
                                <asp:ImageButton ID="LeftDeleteBtn" CssClass="portal-icon-button" ImageUrl="~/images/delete.gif" CommandName="delete" CommandArgument="leftPane"
                                    AlternateText="Delete this item" runat="server" OnClick="DeleteBtn_Click" />
                            </div>
                        </div>
                    </div>

                    <div class="portal-layout-pane portal-layout-pane-content">
                        <h3 class="SubHead portal-layout-pane-title">Content Pane</h3>
                        <div class="portal-layout-pane-body">
                            <asp:ListBox ID="contentPane" CssClass="NormalTextBox portal-layout-list" DataSource="<%# contentList %>" DataTextField="ModuleTitle"
                                DataValueField="ModuleId" Width="100%" Rows="9" runat="server" />
                            <div class="portal-layout-toolbar portal-layout-toolbar-wide">
                                <asp:ImageButton ID="ContentUpBtn" CssClass="portal-icon-button" ImageUrl="~/images/up.gif" CommandName="up" CommandArgument="contentPane"
                                    AlternateText="Move selected module up in list" runat="server" OnClick="UpDown_Click" />
                                <asp:ImageButton ID="ContentLeftBtn" CssClass="portal-icon-button" ImageUrl="~/images/lt.gif" sourcepane="contentPane" targetpane="leftPane"
                                    AlternateText="Move selected module to the left pane" runat="server" OnClick="RightLeft_Click" />
                                <asp:ImageButton ID="ContentRightBtn" CssClass="portal-icon-button" ImageUrl="~/images/rt.gif" sourcepane="contentPane" targetpane="rightPane"
                                    AlternateText="Move selected module to the right pane" runat="server" OnClick="RightLeft_Click" />
                                <asp:ImageButton ID="ContentDownBtn" CssClass="portal-icon-button" ImageUrl="~/images/dn.gif" CommandName="down" CommandArgument="contentPane"
                                    AlternateText="Move selected module down in list" runat="server" OnClick="UpDown_Click" />
                                <asp:ImageButton ID="ContentEditBtn" CssClass="portal-icon-button" ImageUrl="~/images/edit.gif" CommandName="edit" CommandArgument="contentPane"
                                    AlternateText="Edit this item" runat="server" OnClick="EditBtn_Click" />
                                <asp:ImageButton ID="ContentDeleteBtn" CssClass="portal-icon-button" ImageUrl="~/images/delete.gif" CommandName="delete" CommandArgument="contentPane"
                                    AlternateText="Delete this item" runat="server" OnClick="DeleteBtn_Click" />
                            </div>
                        </div>
                    </div>

                    <div class="portal-layout-pane portal-layout-pane-mini">
                        <h3 class="SubHead portal-layout-pane-title">Right Mini Pane</h3>
                        <div class="portal-layout-pane-body">
                            <asp:ListBox ID="rightPane" CssClass="NormalTextBox portal-layout-list" DataSource="<%# rightList %>" DataTextField="ModuleTitle"
                                DataValueField="ModuleId" Width="100%" Rows="9" runat="server" />
                            <div class="portal-layout-toolbar">
                                <asp:ImageButton ID="RightUpBtn" CssClass="portal-icon-button" ImageUrl="~/images/up.gif" CommandName="up" CommandArgument="rightPane"
                                    AlternateText="Move selected module up in list" runat="server" OnClick="UpDown_Click" />
                                <asp:ImageButton ID="RightLeftBtn" CssClass="portal-icon-button" ImageUrl="~/images/lt.gif" sourcepane="rightPane" targetpane="contentPane"
                                    AlternateText="Move selected module to the content pane" runat="server" OnClick="RightLeft_Click" />
                                <asp:ImageButton ID="RightDownBtn" CssClass="portal-icon-button" ImageUrl="~/images/dn.gif" CommandName="down" CommandArgument="rightPane"
                                    AlternateText="Move selected module down in list" runat="server" OnClick="UpDown_Click" />
                                <asp:ImageButton ID="RightEditBtn" CssClass="portal-icon-button" ImageUrl="~/images/edit.gif" CommandName="edit" CommandArgument="rightPane"
                                    AlternateText="Edit this item" runat="server" OnClick="EditBtn_Click" />
                                <asp:ImageButton ID="RightDeleteBtn" CssClass="portal-icon-button" ImageUrl="~/images/delete.gif" CommandName="delete" CommandArgument="rightPane"
                                    AlternateText="Delete this item" runat="server" OnClick="DeleteBtn_Click" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="portal-form-actions">
                <asp:LinkButton ID="applyBtn" CssClass="CommandButton" Text="Apply Changes" runat="server"
                    OnClick="Apply_Click" />
            </div>
        </div>
    </div>
</asp:Content>
