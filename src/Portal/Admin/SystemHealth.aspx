<%@ Page
    Language="c#"
    CodeBehind="SystemHealth.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.SystemHealth"
    MasterPageFile="~/Default.master" %>

<%--
<lang>
  <zh-CN>P2.2 只读系统健康页仅展示检查结果、设置 registry 摘要和重新检测入口，不在页面上提供自动修复、配置写入或命令执行能力。</zh-CN>
  <en>The P2.2 read-only system health page only displays check results, the settings-registry summary, and a recheck entry point; it does not provide automated repair, configuration writes, or command execution.</en>
</lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
    <lang>
      <zh-CN>后台页级布局样板保留服务器控件绑定和 Repeater 数据源，只重构页面壳、摘要卡片和表格语义，避免影响 code-behind 健康检查流程。</zh-CN>
      <en>The admin page-level layout sample preserves server-control bindings and Repeater data sources, only rebuilding the page shell, summary cards, and table semantics so the code-behind health-check flow is not changed.</en>
    </lang>
    --%>
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
            <%--
            <lang>
              <zh-CN>摘要卡片只呈现 code-behind 生成的总体状态和检查时间；重新检测按钮重新触发只读检查，不提供修复或配置写入。</zh-CN>
              <en>Summary cards only present the overall status and check time produced by the code-behind; recheck triggers read-only checks and does not repair or write configuration.</en>
            </lang>
            --%>
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
                <%--
                <lang>
                  <zh-CN>健康检查 Repeater 以编码绑定展示类别、摘要、详情和事件 ID；详情是诊断输出，不应被页面层当作可执行命令。</zh-CN>
                  <en>The health-check Repeater displays category, summary, detail, and event ID through encoded bindings; detail is diagnostic output and must not be treated as an executable command by the page layer.</en>
                </lang>
                --%>
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
                <%--
                <lang>
                  <zh-CN>设置 registry 行同时标出敏感性、在线可编辑和重启要求；这些标签只反映服务器元数据，不能绕过配置写入策略。</zh-CN>
                  <en>Settings-registry rows expose sensitivity, online-editability, and restart requirements; these labels reflect server metadata and cannot bypass configuration-write policy.</en>
                </lang>
                --%>
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
