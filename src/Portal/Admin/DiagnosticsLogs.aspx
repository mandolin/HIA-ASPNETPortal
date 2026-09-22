<%@ Page
    Language="c#"
    CodeBehind="DiagnosticsLogs.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.DiagnosticsLogs"
    MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

<%--
  <lang>
    <zh-CN>P2.4 只读诊断日志页仅查询受限 NDJSON 记录，不提供下载、删除或路径输入。</zh-CN>
    <en>The P2.4 read-only diagnostic log page queries only constrained NDJSON entries and provides no download, delete, or path-input action.</en>
  </lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
      <lang>
        <zh-CN>诊断日志页只重构后台展示结构，查询范围、分页和权限仍由 code-behind 控制。</zh-CN>
        <en>The diagnostic log page only rebuilds the Admin presentation structure; query scope, paging, and authorization remain controlled by code-behind.</en>
      </lang>
    --%>
    <div class="portal-admin-page portal-admin-diagnostics-logs">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title"><%= lang.Admin_DiagnosticsLogs_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_DiagnosticsLogs_Subtitle %></p>
            </div>
            <%= PortalNavigationEntryRenderer.RenderActions("Admin.Ops.DiagnosticsLogs", Context) %>
        </div>

        <%--
          <lang>
            <zh-CN>筛选控件只收集展示查询条件；日期、级别、分类和事件编号的解析、范围限制及授权仍由 SearchButton_Click 的服务端职责完成。</zh-CN>
            <en>The filter controls only collect display-query criteria; server code in SearchButton_Click remains responsible for parsing, range limits, and authorization for dates, level, category, and event id.</en>
          </lang>
        --%>
        <div class="portal-admin-section portal-filter-panel">
            <div class="portal-filter-grid">
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_DiagnosticsLogs_LabelStartUtc %></span>
                    <asp:TextBox ID="StartDateTextBox" CssClass="NormalTextBox portal-filter-input" Width="110" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_DiagnosticsLogs_LabelEndUtc %></span>
                    <asp:TextBox ID="EndDateTextBox" CssClass="NormalTextBox portal-filter-input" Width="110" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_DiagnosticsLogs_LabelLevel %></span>
                    <asp:DropDownList ID="LevelFilter" CssClass="NormalTextBox portal-filter-input" runat="server">
                        <asp:ListItem Text="<%$ Resources:lang, Admin_DiagnosticsLogs_LevelAll %>" Value="" />
                        <asp:ListItem Text="<%$ Resources:lang, Admin_DiagnosticsLogs_LevelInfo %>" Value="Info" />
                        <asp:ListItem Text="<%$ Resources:lang, Admin_DiagnosticsLogs_LevelWarning %>" Value="Warning" />
                        <asp:ListItem Text="<%$ Resources:lang, Admin_DiagnosticsLogs_LevelError %>" Value="Error" />
                    </asp:DropDownList>
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_DiagnosticsLogs_LabelCategory %></span>
                    <asp:TextBox ID="CategoryFilter" CssClass="NormalTextBox portal-filter-input" Width="110" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_DiagnosticsLogs_LabelEventId %></span>
                    <asp:TextBox ID="EventIdFilter" CssClass="NormalTextBox portal-filter-input" Width="150" runat="server" />
                </div>
                <div class="portal-filter-actions">
                    <asp:LinkButton ID="SearchButton" Text="<%$ Resources:lang, Admin_DiagnosticsLogs_ButtonSearch %>" CssClass="CommandButton" CausesValidation="False" OnClick="SearchButton_Click" runat="server" />
                </div>
            </div>
            <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" runat="server" />
        </div>

        <%--
          <lang>
            <zh-CN>分页按钮仅发送前后页意图且关闭页面验证；实际页码边界、数据读取和越界回退由 code-behind 控制。</zh-CN>
            <en>Pager buttons submit only previous/next intent with page validation disabled; code-behind controls page boundaries, data reads, and out-of-range fallback.</en>
          </lang>
        --%>
        <div class="portal-pager">
            <div class="Normal portal-pager-info">
                <asp:Label ID="ResultLabel" runat="server" />
            </div>
            <div class="portal-pager-actions">
                <asp:LinkButton ID="PreviousButton" Text="<%$ Resources:lang, Admin_DiagnosticsLogs_ButtonPrevious %>" CssClass="CommandButton" CausesValidation="False" OnClick="PreviousButton_Click" runat="server" />
                <asp:LinkButton ID="NextButton" Text="<%$ Resources:lang, Admin_DiagnosticsLogs_ButtonNext %>" CssClass="CommandButton" CausesValidation="False" OnClick="NextButton_Click" runat="server" />
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_DiagnosticsLogs_SectionLogEntries %></h2>
            </div>
            <div class="portal-table-wrap">
                <%--
                  <lang>
                    <zh-CN>Repeater 只读展示受限诊断条目；`<%#: ... %>` 负责编码文本绑定，详情链接统一经过 GetDetailUrl 生成，不在标记层拼接路径。</zh-CN>
                    <en>The Repeater renders constrained diagnostic entries read-only; `<%#: ... %>` encodes text bindings, and detail links are produced by GetDetailUrl rather than concatenated in markup.</en>
                  </lang>
                --%>
                <asp:Repeater ID="EntriesRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table portal-diagnostics-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="155" class="SubHead"><%= lang.Admin_DiagnosticsLogs_ColumnUtc %></th>
                                <th scope="col" width="75" class="SubHead"><%= lang.Admin_DiagnosticsLogs_ColumnLevel %></th>
                                <th scope="col" width="150" class="SubHead"><%= lang.Admin_DiagnosticsLogs_ColumnCategory %></th>
                                <th scope="col" class="SubHead"><%= lang.Admin_DiagnosticsLogs_ColumnMessage %></th>
                                <th scope="col" width="195" class="SubHead"><%= lang.Admin_DiagnosticsLogs_ColumnEventId %></th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("UtcTime", "{0:yyyy-MM-dd HH:mm:ss} UTC") %></td>
                                <td><%#: Eval("Level") %></td>
                                <td><%#: Eval("Category") %></td>
                                <td class="portal-log-message"><%#: Eval("Message") %></td>
                                <td class="portal-log-event"><asp:HyperLink ID="DetailLink" NavigateUrl='<%# GetDetailUrl(Eval("EventId")) %>' Text='<%#: Eval("EventId") %>' runat="server" /></td>
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
