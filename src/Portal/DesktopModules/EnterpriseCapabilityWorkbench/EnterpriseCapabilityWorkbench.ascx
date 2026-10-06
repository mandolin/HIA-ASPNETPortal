<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EnterpriseCapabilityWorkbench.ascx.cs" Inherits="ASPNET.StarterKit.Portal.EnterpriseCapabilityWorkbench" %>
<%--
    <lang>
        <zh-CN>P74.3 空态渲染器位于 ASPNET.StarterKit.Portal 命名空间，而 Web.config 只全局导入 Resources：标记层若引用该类型必须显式导入本命名空间，否则**运行期编译**报 CS0103（msbuild 构建不会校验 ASCX，只有在真实站点加载模块时才暴露）。</zh-CN>
        <en>The P74.3 empty-state renderer lives in the ASPNET.StarterKit.Portal namespace while Web.config imports only Resources globally: markup that references that type must import this namespace explicitly, otherwise the **runtime compilation** of the ASCX reports CS0103 (an msbuild build does not validate ASCX files; it only surfaces when a real site loads the module).</en>
    </lang>
--%>
<%@ Import Namespace="ASPNET.StarterKit.Portal" %>

<%--
    <lang>
        <zh-CN>P22.4 企业能力工作台首版：为普通用户提供企业协同事项提交和本人事项查看入口，后台处理仍复用现有 Admin 页面。</zh-CN>
        <en>P22.4 first enterprise-capability workbench: gives ordinary users an entry to submit collaboration items and view their own items, while administration handling continues to reuse existing Admin pages.</en>
    </lang>
--%>
<div class="enterprise-workbench">
    <%--
    <lang>
        <zh-CN>标题语义（W87 补）：本模块自带标题 div 而不复用共享控件 DesktopModuleTitle，故此处显式补
        role="heading" 与 aria-level，使其对辅助技术等价于一级标题。只加属性、不改 class，
        因此视觉零变化（与 W82 对共享控件的处理同法）。前台模块固定 1 级：门户 chrome 没有 h1，
        模块标题即页面主内容入口；后台模块由共享控件按页面路径判为 2 级，此处不涉及。
        遗留：项目内因此并存两套标题实现（共享控件 + 模块自带 div），统一属独立项。</zh-CN>
        <en>Title semantics (added in W87): this module renders its own title div instead of reusing the shared
        DesktopModuleTitle control, so role="heading" and aria-level are set explicitly here to make it equivalent to a
        first-level heading for assistive technology. Only attributes are added and no class changes, so the visuals are
        unchanged (the same approach W82 took for the shared control). Level 1 is fixed because this is a front-office
        module: the portal chrome has no h1 and the module title is the page's primary content entry; admin modules are
        judged as level 2 by the shared control from the page path, which does not apply here. Left over: the project
        therefore carries two title implementations (shared control and module-local div); unifying them is a separate item.</en>
    </lang>
--%>
<div class="enterprise-workbench-title" role="heading" aria-level="1"><%= lang.EnterpriseCapabilityWorkbench_Heading %></div>
    <asp:Label ID="MessageLabel" CssClass="enterprise-workbench-message" EnableViewState="false" runat="server" />

    <%--
        <lang>
            <zh-CN>WorkbenchPanel 是否可见由服务器根据普通用户能力和上下文决定；标记层不自行授予协同事项权限。</zh-CN>
            <en>The server decides WorkbenchPanel visibility from ordinary-user capability and context; markup does not grant collaboration-item permission.</en>
        </lang>
    --%>
    <asp:Panel ID="WorkbenchPanel" CssClass="enterprise-workbench-panel" Visible="false" runat="server">
        <%--
            <lang>
                <zh-CN>首版只暴露低敏协同事项字段，不引入附件、动态表单、脚本扩展或具体行业字段，避免在链接治理阶段过早绑定复杂业务。</zh-CN>
                <en>The first version exposes only low-sensitivity collaboration-item fields and avoids attachments, dynamic forms, script extensions, or domain-specific fields so the link-governance phase does not bind too early to complex business.</en>
            </lang>
        --%>
        <div class="enterprise-workbench-form-grid">
            <%--
                <lang>
                    <zh-CN>标题、类型、优先级、期限、摘要和说明构成低敏协同事项输入；角色、时间、长度和正文规范化仍由服务器处理。</zh-CN>
                    <en>Title, type, priority, due time, summary, and description form low-sensitivity collaboration input; role, time, length, and body normalization remain server-side.</en>
                </lang>
            --%>
            <div class="enterprise-workbench-form-field enterprise-workbench-form-field-wide">
                <label class="SubHead enterprise-workbench-label" for="<%= TitleTextBox.ClientID %>"><%= lang.EnterpriseCapabilityWorkbench_LabelItemTitle %></label>
                <asp:TextBox ID="TitleTextBox" CssClass="NormalTextBox enterprise-workbench-input" MaxLength="200" runat="server" />
            </div>
            <div class="enterprise-workbench-form-field">
                <label class="SubHead enterprise-workbench-label" for="<%= ItemTypeList.ClientID %>"><%= lang.EnterpriseCapabilityWorkbench_LabelItemType %></label>
                <asp:DropDownList ID="ItemTypeList" CssClass="NormalTextBox enterprise-workbench-input" runat="server" />
            </div>
            <div class="enterprise-workbench-form-field">
                <label class="SubHead enterprise-workbench-label" for="<%= PriorityList.ClientID %>"><%= lang.EnterpriseCapabilityWorkbench_LabelPriority %></label>
                <asp:DropDownList ID="PriorityList" CssClass="NormalTextBox enterprise-workbench-input" runat="server" />
            </div>
            <div class="enterprise-workbench-form-field enterprise-workbench-form-field-wide">
                <label class="SubHead enterprise-workbench-label" for="<%= SummaryTextBox.ClientID %>"><%= lang.EnterpriseCapabilityWorkbench_LabelSummary %></label>
                <asp:TextBox ID="SummaryTextBox" CssClass="NormalTextBox enterprise-workbench-input" MaxLength="500" runat="server" />
            </div>
            <div class="enterprise-workbench-form-field">
                <label class="SubHead enterprise-workbench-label" for="<%= DueUtcTextBox.ClientID %>"><%= lang.EnterpriseCapabilityWorkbench_LabelDueUtc %></label>
                <asp:TextBox ID="DueUtcTextBox" CssClass="NormalTextBox enterprise-workbench-input" MaxLength="19" runat="server" />
            </div>
            <div class="enterprise-workbench-form-field enterprise-workbench-form-field-full">
                <label class="SubHead enterprise-workbench-label" for="<%= DescriptionTextBox.ClientID %>"><%= lang.EnterpriseCapabilityWorkbench_LabelItemDetail %></label>
                <asp:TextBox ID="DescriptionTextBox" CssClass="NormalTextBox enterprise-workbench-input enterprise-workbench-body"
                    MaxLength="4000" TextMode="MultiLine" Rows="6" runat="server" />
            </div>
        </div>

        <%--
            <lang>
                <zh-CN>提交、参与者评论和重新提交命令通过既有 ItemId 绑定事件进入服务器状态机；编码列表输出不替代状态和权限校验。</zh-CN>
                <en>Submit, participant-comment, and resubmit commands enter the server state machine through existing ItemId-bound events; encoded list output does not replace state or authorization checks.</en>
            </lang>
        --%>
        <div class="enterprise-workbench-actions">
            <asp:Button ID="SubmitButton" CssClass="CommandButton" Text="<%$ Resources:lang,EnterpriseCapabilityWorkbench_ButtonSubmit %>" OnClick="SubmitButton_Click" runat="server" />
        </div>

        <div class="enterprise-workbench-subtitle"><%= lang.EnterpriseCapabilityWorkbench_SectionMyRecentItems %></div>
        <div class="enterprise-workbench-list-wrap">
            <asp:Repeater ID="RecentItemsRepeater" OnItemCommand="RecentItemsRepeater_ItemCommand" runat="server">
                <HeaderTemplate>
                    <table class="enterprise-workbench-list" cellspacing="0" cellpadding="4" border="0">
                        <tr>
                            <%--
                                <lang>
                                    <zh-CN>P74.2 起改用本模块自有资源键，不再借用 BusinessApplicationRequest 的键：借用会使两模块的改名与文案互相牵动。</zh-CN>
                                    <en>Since P74.2 these headers use the module's own resource keys instead of borrowing keys from BusinessApplicationRequest, because borrowing couples the two modules' renames and wording.</en>
                                </lang>
                            --%>
                            <th scope="col"><%= lang.EnterpriseCapabilityWorkbench_ColumnUtc %></th>
                            <th scope="col"><%= lang.EnterpriseCapabilityWorkbench_ColumnId %></th>
                            <th scope="col"><%= lang.EnterpriseCapabilityWorkbench_LabelTitle %></th>
                            <th scope="col"><%= lang.EnterpriseCapabilityWorkbench_ColumnStatus %></th>
                            <th scope="col"><%= lang.EnterpriseCapabilityWorkbench_LabelPriority %></th>
                            <th scope="col"><%= lang.EnterpriseCapabilityWorkbench_ColumnRecentComment %></th>
                            <th scope="col"><%= lang.EnterpriseCapabilityWorkbench_SectionFollowUp %></th>
                        </tr>
                </HeaderTemplate>
                <ItemTemplate>
                        <tr>
                            <td><%#: Eval("LastActionUtcText") %></td>
                            <td><%#: Eval("ItemCode") %></td>
                            <td><%# Convert.ToBoolean(Eval("HasParentItem")) ? "&#9656; " : "" %><%#: Eval("Title") %></td>
                            <td><%#: Eval("StatusText") %></td>
                            <td><%#: Eval("PriorityKey") %></td>
                            <td><%#: Eval("LastActionComment") %></td>
                            <td>
                                <%--
                                    <lang>
                                      <zh-CN>P47.4 前台只展示参与人集合，不提供添加或移除入口。</zh-CN>
                                      <en>P47.4 the front end only displays the participant set and offers no add or remove entry.</en>
                                    </lang>
                                --%>
                                <%--
                                    <lang>
                                      <zh-CN>W90 起参与人改为逐人一行：逗号串在多人与长姓名时会挤成一行、换行位置不可控；逐行后
                                      每个"用户名（角色）"独立成行，可稳定换行且便于逐个阅读。数据层新增的
                                      BuildParticipantLines 与逗号串共用同一条格式串，故两种呈现不会本地化漂移。
                                      用 ul/li 而非多个 div：列表语义让辅助技术能播报项数，换行由浏览器保证。
                                      空集合由数据层返回单元素占位序列，故此处无需判空。</zh-CN>
                                      <en>Since W90 participants render one per line: a comma-joined string crowds several people
                                      onto one line with unpredictable wrapping, whereas one line each lets every "user name (role)"
                                      wrap stably and be read individually. BuildParticipantLines shares the same format string as the
                                      joined text, so the two cannot drift. A ul/li list is used so assistive tech can announce the
                                      count and wrapping is guaranteed by the browser. An empty set yields one placeholder line from
                                      the data layer, so no null check is needed.</en>
                                    </lang>
                                --%>
                                <div><span class="SubHead"><%= lang.EnterpriseCapabilityWorkbench_LabelParticipants %></span></div>
                                <ul class="enterprise-workbench-participants">
                                    <asp:Repeater ID="ParticipantLinesRepeater" DataSource='<%# Eval("ParticipantLines") %>' runat="server">
                                        <ItemTemplate><li><%#: Container.DataItem %></li></ItemTemplate>
                                    </asp:Repeater>
                                </ul>
                                <div><span class="SubHead"><%= lang.EnterpriseCapabilityWorkbench_LabelLatestCommentPrefix %></span><%#: Eval("LatestParticipantComment") %></div>
                                <asp:TextBox ID="ParticipantCommentTextBox" CssClass="NormalTextBox enterprise-workbench-input" MaxLength="1000" TextMode="MultiLine" Rows="2" runat="server" />
                                <div class="enterprise-workbench-actions">
                                    <asp:Button ID="AddParticipantCommentButton" CssClass="CommandButton" Text="<%$ Resources:lang,EnterpriseCapabilityWorkbench_ButtonAddComment %>" CommandName="AddParticipantComment" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                    <asp:Button ID="ResubmitButton" CssClass="CommandButton" Text="<%$ Resources:lang,EnterpriseCapabilityWorkbench_ButtonResubmit %>" CommandName="Resubmit" CommandArgument='<%# Eval("ItemId") %>' CausesValidation="False" runat="server" />
                                </div>
                            </td>
                        </tr>
                </ItemTemplate>
                <FooterTemplate>
                    <%--
                        <lang>
                            <zh-CN>P74.3 空态：零条时在表尾渲染"表头 + 提示行"，提示行整行居中且弱化。放在 FooterTemplate 内是因为 Repeater 在零条时仍会渲染表头与表尾；有数据时渲染器返回空串，故无需分支。列数（7）与本表表头列数一致。</zh-CN>
                            <en>P74.3 empty state: with zero rows the footer renders the header-plus-hint row, centered and muted. It lives in the FooterTemplate because a Repeater still renders header and footer with zero rows, and the renderer returns an empty string when rows exist so no branch is needed. The column count (7) matches this table's header.</en>
                        </lang>
                    --%>
                    <%= PortalEmptyStateRenderer.Render(RecentItemCount, lang.EnterpriseCapabilityWorkbench_EmptyNoItems, 7) %>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
        </div>
    </asp:Panel>
</div>
