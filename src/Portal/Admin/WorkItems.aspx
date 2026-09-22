<%@ Page
    Language="c#"
    CodeBehind="WorkItems.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.WorkItems"
    MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

<%--
  <lang>
    <zh-CN>P12.3 轻量待办后台入口：第一版只读集中查看，不提供流程设计器或转办；P21.3 起同时承接协同事项待办投影。</zh-CN>
    <en>P12.3 lightweight work-item Admin entry: the first version is a read-only central view without workflow designer or reassignment; P21.3 also projects collaboration-item work entries here.</en>
  </lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="portal-admin-page portal-admin-work-items">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title"><%= lang.Admin_WorkItems_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_WorkItems_Subtitle %></p>
            </div>
            <%--
              <lang>
                <zh-CN>页面自有导航入口：其中"资料更正请求"的目标页在导航 registry 中尚无入口键，若整体交给渲染器，该入口会因分组解析不到而静默消失；待 registry 补齐后再统一改为渲染器。</zh-CN>
                <en>Page-owned navigation entries: the target of "Correction Requests" has no navigation-registry entry yet, so handing the block to the renderer would silently drop that entry when no group can be resolved; switch to the renderer once the registry is complete.</en>
              </lang>
            --%>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="EmployeeProfileCorrectionRequests.aspx"><%= lang.Admin_WorkItems_LinkCorrectionRequests %></a>
                <a class="CommandButton" href="CollaborationItems.aspx"><%= lang.Admin_WorkItems_LinkCollaborationItems %></a>
                <a class="CommandButton" href="OperationAudits.aspx"><%= lang.Admin_WorkItems_LinkOperationAudits %></a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />

        <%--
        <lang>
          <zh-CN>状态筛选项由 code-behind 在首次加载时从稳定状态契约建立；标记层只承载已授权页面的控件状态，不直接读取数据库或解释状态迁移。</zh-CN>
          <en>Status-filter options are created by code-behind from the stable status contract during the initial load; this markup layer carries only the authorized page control state and neither reads the database nor interprets state transitions.</en>
        </lang>
        --%>
        <div class="portal-admin-section portal-filter-panel">
            <%--
            <lang>
              <zh-CN>SearchButton_Click 依据当前状态筛选刷新待办投影；按钮只触发查询，不代表客户端可以改变工作项状态。</zh-CN>
              <en>SearchButton_Click refreshes the work-item projection for the current status filter; the button triggers a query and does not let the client change work-item state.</en>
            </lang>
            --%>
            <div class="portal-filter-grid">
                <div class="portal-filter-field">
                    <span class="SubHead portal-filter-label"><%= lang.Admin_WorkItems_LabelStatus %></span>
                    <asp:DropDownList ID="StatusFilterList" CssClass="NormalTextBox portal-filter-input" runat="server" />
                </div>
                <div class="portal-filter-actions">
                    <asp:LinkButton
                        ID="SearchButton"
                        Text="<%$ Resources:lang, Admin_WorkItems_ButtonSearch %>"
                        CssClass="CommandButton"
                        CausesValidation="False"
                        OnClick="SearchButton_Click"
                        runat="server" />
                </div>
            </div>
        </div>

        <div class="portal-status-strip">
            <%--
            <lang>
              <zh-CN>ResultLabel 由服务器写入结果和 schema 状态提示；提示文本不构成数据存在、权限或迁移成功的客户端证明。</zh-CN>
              <en>The server writes result and schema-status messaging to ResultLabel; the text is not client proof of data existence, authorization, or transition success.</en>
            </lang>
            --%>
            <div class="Normal portal-status-line">
                <asp:Label ID="ResultLabel" runat="server" />
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title"><%= lang.Admin_WorkItems_SectionCurrentWorkItems %></h2>
            </div>
            <div class="portal-table-wrap">
                <%--
                <lang>
                  <zh-CN>待办数据由已通过权限门禁的 code-behind 绑定；此标记层不直接查询或拼接业务数据。空集合和 schema 不可用状态由后端清空 Repeater 并显示受控提示。</zh-CN>
                  <en>Work-item data is bound by code-behind after the permission gate; this markup layer neither queries nor concatenates business data directly. The backend clears the Repeater and shows controlled messaging for empty collections and unavailable schema.</en>
                </lang>
                --%>
                <asp:Repeater ID="WorkItemsRepeater" runat="server">
                    <HeaderTemplate>
                        <table class="portal-data-table" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                                <th scope="col" width="70" class="SubHead"><%= lang.Admin_WorkItems_ColumnId %></th>
                                <th scope="col" width="95" class="SubHead"><%= lang.Admin_WorkItems_ColumnStatus %></th>
                                <th scope="col" width="180" class="SubHead"><%= lang.Admin_WorkItems_ColumnBusiness %></th>
                                <th scope="col" class="SubHead"><%= lang.Admin_WorkItems_ColumnTitleSummary %></th>
                                <th scope="col" width="190" class="SubHead"><%= lang.Admin_WorkItems_ColumnAssignedTo %></th>
                                <th scope="col" width="145" class="SubHead"><%= lang.Admin_WorkItems_ColumnCreatedUtc %></th>
                                <th scope="col" width="145" class="SubHead"><%= lang.Admin_WorkItems_ColumnCompletedUtc %></th>
                            </tr>
                    </HeaderTemplate>
                    <%--
                    <lang>
                      <zh-CN>展示值使用 `<%#:` 编码绑定，避免待办标题、摘要和分派文字作为原始 HTML 输出。业务入口只绑定 code-behind 生成的固定本地白名单 URL；不展示或拼接 BusinessId，未知类型已在服务器映射到占位页。</zh-CN>
                      <en>Display values use `<%#:` encoded binding so work-item title, summary, and assignment text are not emitted as raw HTML. The business entry binds only the fixed local allowlisted URL generated by code-behind; it neither displays nor concatenates BusinessId, and unknown kinds are server-mapped to the placeholder page.</en>
                    </lang>
                    --%>
                    <ItemTemplate>
                        <%--
                        <lang>
                          <zh-CN>单项模板只呈现 code-behind 生成的编码字段与固定本地业务入口；页面不从 BusinessId 推导 URL，也不执行写操作。</zh-CN>
                          <en>The item template only presents encoded fields and the fixed local business entry generated by code-behind; the page neither derives URLs from BusinessId nor performs writes.</en>
                        </lang>
                        --%>
                            <tr class="Normal">
                                <td><%#: Eval("WorkItemId") %></td>
                                <td><%#: Eval("WorkItemStatus") %></td>
                                <td>
                                    <div class="portal-value-stack">
                                        <div><%#: Eval("BusinessKind") %></div>
                                        <div><a class="CommandButton" href='<%#: Eval("BusinessUrl") %>'><%= lang.Admin_WorkItems_ButtonOpenSource %></a></div>
                                    </div>
                                </td>
                                <td>
                                    <div class="portal-value-stack">
                                        <div class="SubHead"><%#: Eval("Title") %></div>
                                        <div><%#: Eval("Summary") %></div>
                                    </div>
                                </td>
                                <td><%#: Eval("AssignedText") %></td>
                                <td><%#: Eval("CreatedUtcText") %></td>
                                <td><%#: Eval("CompletedUtcText") %></td>
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
