<%@ Page
    Language="c#"
    CodeBehind="ModuleCatalog.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.ModuleCatalog"
    MasterPageFile="~/Default.master" %>

<%-- P3.2 只管理受信任部署包的注册和启停；不提供 ZIP、DLL、脚本、外链或在线编译入口。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr valign="top">
            <td width="20">&nbsp;</td>
            <td>
                <table width="100%" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left" class="Head">Module Catalog</td>
                    </tr>
                    <tr>
                        <td><hr noshade size="1"></td>
                    </tr>
                    <tr>
                        <td class="Normal">
                            <a class="CommandButton" href="ModuleDefinitions.aspx">Legacy Module Definitions</a>
                        </td>
                    </tr>
                </table>

                <asp:Label ID="MessageLabel" CssClass="NormalRed" EnableViewState="false" runat="server" />
                <asp:Label ID="ResultLabel" CssClass="Normal" EnableViewState="false" runat="server" />

                <br>

                <asp:GridView
                    ID="PackagesGrid"
                    AutoGenerateColumns="False"
                    GridLines="Both"
                    CssClass="Normal"
                    Width="100%"
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
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>

                <br>
                <div class="Normal">
                    Packages are discovered from deployed <code>module.json</code> files. This page never uploads,
                    deletes, extracts, compiles, or loads package scripts.
                </div>
            </td>
        </tr>
    </table>
</asp:Content>
