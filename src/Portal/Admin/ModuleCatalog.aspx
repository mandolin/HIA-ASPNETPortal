<%@ Page
    Language="c#"
    CodeBehind="ModuleCatalog.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.ModuleCatalog"
    MasterPageFile="~/Default.master" %>

<%-- P3.2 只管理受信任部署包的注册和启停；不提供 ZIP、DLL、脚本、外链或在线编译入口。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 模块目录页只重构展示壳，包注册、启停和预检命令保持既有安全边界。 --%>
    <div class="portal-admin-page portal-admin-module-catalog">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Module Catalog</h1>
                <p class="Normal portal-admin-subtitle">Manage trusted deployed module packages without upload, extraction, or script loading.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="SystemHealth.aspx">System Health</a>
                <a class="CommandButton" href="ModuleDefinitions.aspx">Legacy Module Definitions</a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

        <div class="portal-status-strip">
            <div class="Normal portal-status-line">
                <asp:Label ID="ResultLabel" EnableViewState="false" runat="server" />
            </div>
            <div class="Normal portal-status-line">
                Packages are discovered from deployed <code>module.json</code> files. This page never uploads,
                deletes, extracts, compiles, or loads package scripts.
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Trusted Packages</h2>
            </div>
            <div class="portal-table-wrap">
                <asp:GridView
                    ID="PackagesGrid"
                    AutoGenerateColumns="False"
                    GridLines="None"
                    CssClass="portal-data-table portal-module-catalog-table"
                    Width="100%"
                    HeaderStyle-CssClass="SubHead"
                    HeaderStyle-Wrap="False"
                    RowStyle-CssClass="Normal"
                    OnRowCommand="PackagesGrid_RowCommand"
                    runat="server">
                    <Columns>
                        <asp:BoundField DataField="PackageId" HeaderText="Package ID" />
                        <asp:BoundField DataField="DisplayName" HeaderText="Name" />
                        <asp:BoundField DataField="Version" HeaderText="Version" />
                        <asp:BoundField DataField="DesktopEntry" HeaderText="Desktop Entry" />
                        <asp:BoundField DataField="StateText" HeaderText="State" />
                        <asp:BoundField DataField="DefinitionText" HeaderText="Definition" />
                        <asp:BoundField DataField="InstanceCount" HeaderText="Instances" />
                        <asp:TemplateField HeaderText="Actions">
                            <ItemTemplate>
                                <div class="portal-row-actions">
                                    <asp:LinkButton
                                        ID="RegisterButton"
                                        Text="Register"
                                        CommandName="Register"
                                        CommandArgument='<%# Eval("PackageId") %>'
                                        CssClass="CommandButton"
                                        CausesValidation="False"
                                        Visible='<%# !(bool)Eval("IsRegistered") %>'
                                        runat="server" />
                                    <asp:LinkButton
                                        ID="EnableButton"
                                        Text="Enable"
                                        CommandName="Enable"
                                        CommandArgument='<%# Eval("PackageId") %>'
                                        CssClass="CommandButton"
                                        CausesValidation="False"
                                        Visible='<%# !(bool)Eval("IsEnabled") %>'
                                        runat="server" />
                                    <asp:LinkButton
                                        ID="DisableButton"
                                        Text="Disable"
                                        CommandName="Disable"
                                        CommandArgument='<%# Eval("PackageId") %>'
                                        CssClass="CommandButton"
                                        CausesValidation="False"
                                        Visible='<%# (bool)Eval("IsEnabled") %>'
                                        runat="server" />
                                    <asp:LinkButton
                                        ID="PreflightButton"
                                        Text="Preflight"
                                        CommandName="Preflight"
                                        CommandArgument='<%# Eval("PackageId") %>'
                                        CssClass="CommandButton"
                                        CausesValidation="False"
                                        runat="server" />
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>
</asp:Content>
