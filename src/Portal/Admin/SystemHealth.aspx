<%@ Page
    Language="c#"
    CodeBehind="SystemHealth.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.SystemHealth"
    MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

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
                <h1 class="Head portal-admin-title"><%= lang.Admin_SystemHealth_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_SystemHealth_Subtitle %></p>
            </div>
            <%= PortalNavigationEntryRenderer.RenderActions("Admin.SystemHealth", Context) %>
        </div>

        <div class="portal-admin-summary-grid">
            <%--
            <lang>
              <zh-CN>摘要卡片只呈现 code-behind 生成的总体状态和检查时间；重新检测按钮重新触发只读检查，不提供修复或配置写入。</zh-CN>
              <en>Summary cards only present the overall status and check time produced by the code-behind; recheck triggers read-only checks and does not repair or write configuration.</en>
            </lang>
            --%>
            <div class="portal-admin-summary-item">
                <div class="SubHead portal-summary-label"><%= lang.Admin_SystemHealth_OverallStatus %></div>
                <div class="Normal portal-summary-value">
                    <asp:Label ID="OverallStatusLabel" runat="server" />
                </div>
            </div>
            <div class="portal-admin-summary-item">
                <div class="SubHead portal-summary-label"><%= lang.Admin_SystemHealth_LastChecked %></div>
                <div class="Normal portal-summary-value">
                    <asp:Label ID="GeneratedUtcLabel" runat="server" />
                </div>
            </div>
            <div class="portal-admin-summary-item portal-summary-command">
                <asp:LinkButton
                    ID="RefreshButton"
                    Text="<%$ Resources:lang, Admin_SystemHealth_Recheck %>"
                    CssClass="CommandButton"
                    CausesValidation="False"
                    OnClick="RefreshButton_Click"
                    runat="server" />
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_SystemHealth_ChecksSectionTitle %></h2>
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
                                <th scope="col" width="110" class="SubHead"><%= lang.Admin_SystemHealth_ColumnCategory %></th>
                                <th scope="col" width="150" class="SubHead"><%= lang.Admin_SystemHealth_ColumnCheck %></th>
                                <th scope="col" width="90" class="SubHead"><%= lang.Admin_SystemHealth_ColumnStatus %></th>
                                <th scope="col" width="220" class="SubHead"><%= lang.Admin_SystemHealth_ColumnSummary %></th>
                                <th scope="col" class="SubHead"><%= lang.Admin_SystemHealth_ColumnDetail %></th>
                                <th scope="col" width="150" class="SubHead"><%= lang.Admin_SystemHealth_ColumnEventId %></th>
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
                <h2 class="Head portal-section-title"><%= lang.Admin_SystemHealth_SettingsSectionTitle %></h2>
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
                                <th scope="col" width="230" class="SubHead"><%= lang.Admin_SystemHealth_ColumnKey %></th>
                                <th scope="col" width="150" class="SubHead"><%= lang.Admin_SystemHealth_ColumnName %></th>
                                <th scope="col" width="80" class="SubHead"><%= lang.Admin_SystemHealth_ColumnType %></th>
                                <th scope="col" width="150" class="SubHead"><%= lang.Admin_SystemHealth_ColumnCurrentValue %></th>
                                <th scope="col" width="120" class="SubHead"><%= lang.Admin_SystemHealth_ColumnSource %></th>
                                <th scope="col" width="80" class="SubHead"><%= lang.Admin_SystemHealth_ColumnSensitive %></th>
                                <th scope="col" width="90" class="SubHead"><%= lang.Admin_SystemHealth_ColumnEditable %></th>
                                <th scope="col" width="90" class="SubHead"><%= lang.Admin_SystemHealth_ColumnRestart %></th>
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
