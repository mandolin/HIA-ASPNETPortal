<%@ Page
    Language="c#"
    CodeBehind="SystemHealth.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.SystemHealth"
    MasterPageFile="~/Default.master" %>

<%-- P2.2 只读系统健康页：仅展示检查结果，不提供修复、编辑或命令入口。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 后台页级布局样板，保留服务器控件绑定，仅重构页面壳和表格语义。 --%>
    <div class="portal-admin-page portal-admin-health">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">System Health</h1>
                <p class="Normal portal-admin-subtitle">Runtime diagnostics and configuration registry overview.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="ThemeSettings.aspx">Theme Settings</a>
                <a class="CommandButton" href="ModuleCatalog.aspx">Module Catalog</a>
                <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
            </div>
        </div>

        <div class="portal-admin-summary-grid">
            <div class="portal-admin-summary-item">
                <div class="SubHead portal-summary-label">Overall Status</div>
                <div class="Normal portal-summary-value">
                    <asp:Label ID="OverallStatusLabel" runat="server" />
                </div>
            </div>
            <div class="portal-admin-summary-item">
                <div class="SubHead portal-summary-label">Last Checked</div>
                <div class="Normal portal-summary-value">
                    <asp:Label ID="GeneratedUtcLabel" runat="server" />
                </div>
            </div>
            <div class="portal-admin-summary-item portal-summary-command">
                <asp:LinkButton
                    ID="RefreshButton"
                    Text="Recheck"
                    CssClass="CommandButton"
                    CausesValidation="False"
                    OnClick="RefreshButton_Click"
                    runat="server" />
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Health Checks</h2>
            </div>
            <div class="portal-table-wrap">
                <asp:Repeater ID="HealthChecksRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="110" class="SubHead">Category</th>
                                <th scope="col" width="150" class="SubHead">Check</th>
                                <th scope="col" width="90" class="SubHead">Status</th>
                                <th scope="col" width="220" class="SubHead">Summary</th>
                                <th scope="col" class="SubHead">Detail</th>
                                <th scope="col" width="150" class="SubHead">Event ID</th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("Category") %></td>
                                <td><%#: Eval("Name") %></td>
                                <td><%#: Eval("Status") %></td>
                                <td><%#: Eval("Summary") %></td>
                                <td><%#: Eval("Detail") %></td>
                                <td><%#: Eval("EventId") %></td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Settings Registry</h2>
            </div>
            <div class="portal-table-wrap">
                <asp:Repeater ID="SettingsRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="230" class="SubHead">Key</th>
                                <th scope="col" width="150" class="SubHead">Name</th>
                                <th scope="col" width="80" class="SubHead">Type</th>
                                <th scope="col" width="150" class="SubHead">Current Value</th>
                                <th scope="col" width="120" class="SubHead">Source</th>
                                <th scope="col" width="80" class="SubHead">Sensitive</th>
                                <th scope="col" width="90" class="SubHead">Editable</th>
                                <th scope="col" width="90" class="SubHead">Restart</th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("Key") %></td>
                                <td><%#: Eval("DisplayName") %></td>
                                <td><%#: Eval("ValueType") %></td>
                                <td><%#: Eval("CurrentValue") %></td>
                                <td><%#: Eval("Source") %></td>
                                <td><%#: Eval("IsSensitive") %></td>
                                <td><%#: Eval("CanEditOnline") %></td>
                                <td><%#: Eval("RequiresRestart") %></td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>
    </div>
</asp:Content>
