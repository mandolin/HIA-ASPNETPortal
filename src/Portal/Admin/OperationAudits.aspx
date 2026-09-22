<%@ Page
    Language="c#"
    CodeBehind="OperationAudits.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.OperationAudits"
    MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

<%--
<lang>
  <zh-CN>P2.4 只读运营审计页用于查询高价值状态变更；普通查看行为当前不写审计，相关策略扩展由审计策略配置统一控制。</zh-CN>
  <en>The P2.4 read-only operation-audit page queries high-value state changes; ordinary view actions are not audited at this stage and can later be governed by audit-policy settings if needed.</en>
</lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
    <lang>
      <zh-CN>运营审计页只调整筛选区、分页区和结果表格的展示结构，查询条件解析、分页边界和管理员权限逻辑仍由 code-behind 控制。</zh-CN>
      <en>The operation-audit page only adjusts the display structure for filters, paging, and the result table; query parsing, paging boundaries, and administrator authorization remain controlled by the code-behind.</en>
    </lang>
    --%>
    <div class="portal-admin-page portal-admin-audits">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title"><%= lang.Admin_OperationAudits_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_OperationAudits_Subtitle %></p>
            </div>
            <%= PortalNavigationEntryRenderer.RenderActions("Admin.Ops.OperationAudits", Context) %>
        </div>

        <%--
          <lang>
            <zh-CN>审计筛选控件只收集展示查询条件；日期、分类、动作和目标编号的解析、范围限制及管理员授权仍由 code-behind 完成。</zh-CN>
            <en>The audit filter controls only collect display-query criteria; code-behind remains responsible for parsing, range limits, and administrator authorization for dates, category, action, and target id.</en>
          </lang>
        --%>
        <div class="portal-admin-section portal-filter-panel">
            <div class="portal-filter-grid">
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_OperationAudits_LabelStartUtc %></span>
                    <asp:TextBox ID="StartDateTextBox" CssClass="NormalTextBox portal-filter-input" Width="110" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_OperationAudits_LabelEndUtc %></span>
                    <asp:TextBox ID="EndDateTextBox" CssClass="NormalTextBox portal-filter-input" Width="110" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_OperationAudits_LabelCategory %></span>
                    <asp:TextBox ID="CategoryFilter" CssClass="NormalTextBox portal-filter-input" Width="120" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_OperationAudits_LabelAction %></span>
                    <asp:TextBox ID="ActionFilter" CssClass="NormalTextBox portal-filter-input" Width="110" runat="server" />
                </div>
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_OperationAudits_LabelTargetId %></span>
                    <asp:TextBox ID="TargetIdFilter" CssClass="NormalTextBox portal-filter-input" Width="150" runat="server" />
                </div>
                <div class="portal-filter-actions">
                    <asp:LinkButton ID="SearchButton" Text="<%$ Resources:lang, Admin_OperationAudits_ButtonSearch %>" CssClass="CommandButton" CausesValidation="False" OnClick="SearchButton_Click" runat="server" />
                </div>
            </div>
            <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" runat="server" />
        </div>

        <%--
          <lang>
            <zh-CN>分页按钮只表达前后页请求且不触发页面验证；实际审计结果页边界和空结果回退由服务端处理。</zh-CN>
            <en>Pager buttons express only previous/next requests without page validation; the server handles audit-result page boundaries and empty-result fallback.</en>
          </lang>
        --%>
        <div class="portal-pager">
            <div class="Normal portal-pager-info">
                <asp:Label ID="ResultLabel" runat="server" />
            </div>
            <div class="portal-pager-actions">
                <asp:LinkButton ID="PreviousButton" Text="<%$ Resources:lang, Admin_OperationAudits_ButtonPrevious %>" CssClass="CommandButton" CausesValidation="False" OnClick="PreviousButton_Click" runat="server" />
                <asp:LinkButton ID="NextButton" Text="<%$ Resources:lang, Admin_OperationAudits_ButtonNext %>" CssClass="CommandButton" CausesValidation="False" OnClick="NextButton_Click" runat="server" />
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_OperationAudits_SectionAuditEntries %></h2>
            </div>
            <div class="portal-table-wrap">
                <%--
                  <lang>
                    <zh-CN>Repeater 仅呈现审计记录，不提供写入或删除动作；文本字段使用编码绑定，摘要、操作者和目标信息的脱敏边界由服务端查询层负责。</zh-CN>
                    <en>The Repeater only renders audit records and exposes no write or delete action; encoded bindings render text, while the server query layer owns redaction boundaries for summary, actor, and target data.</en>
                  </lang>
                --%>
                <asp:Repeater ID="EntriesRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="155" class="SubHead"><%= lang.Admin_OperationAudits_ColumnUtc %></th>
                                <th scope="col" width="130" class="SubHead"><%= lang.Admin_OperationAudits_ColumnCategory %></th>
                                <th scope="col" width="110" class="SubHead"><%= lang.Admin_OperationAudits_ColumnAction %></th>
                                <th scope="col" width="110" class="SubHead"><%= lang.Admin_OperationAudits_ColumnActor %></th>
                                <th scope="col" width="95" class="SubHead"><%= lang.Admin_OperationAudits_ColumnTarget %></th>
                                <th scope="col" width="100" class="SubHead"><%= lang.Admin_OperationAudits_ColumnTargetId %></th>
                                <th scope="col" class="SubHead"><%= lang.Admin_OperationAudits_ColumnSummary %></th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr class="Normal">
                                <td><%#: Eval("OccurredUtc", "{0:yyyy-MM-dd HH:mm:ss} UTC") %></td>
                                <td><%#: Eval("Category") %></td>
                                <td><%#: Eval("Action") %></td>
                                <td><%#: Eval("ActorUserName") %></td>
                                <td><%#: Eval("TargetType") %></td>
                                <td><%#: Eval("TargetId") %></td>
                                <td><%#: Eval("Summary") %></td>
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
