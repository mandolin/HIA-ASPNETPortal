<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EmployeeProfileCorrectionRequest.ascx.cs" Inherits="ASPNET.StarterKit.Portal.EmployeeProfileCorrectionRequest" %>
<%--
    <lang>
        <zh-CN>P74.3 空态渲染器位于 ASPNET.StarterKit.Portal 命名空间，而 Web.config 只全局导入 Resources：标记层若引用该类型必须显式导入本命名空间，否则**运行期编译**报 CS0103（msbuild 构建不会校验 ASCX）。</zh-CN>
        <en>The P74.3 empty-state renderer lives in the ASPNET.StarterKit.Portal namespace while Web.config imports only Resources globally: markup that references that type must import this namespace explicitly, otherwise the **runtime compilation** of the ASCX reports CS0103 (an msbuild build does not validate ASCX files).</en>
    </lang>
--%>
<%@ Import Namespace="ASPNET.StarterKit.Portal" %>

<%--
    <lang>
        <zh-CN>P6.4.3 业务模块样板：员工提交低敏字段级更正请求，不提供附件、脚本或外部资源。</zh-CN>
        <en>P6.4.3 business module sample: employees submit low-sensitivity field-level correction requests; no attachment, script, or external resource capability is provided.</en>
    </lang>
--%>
<div class="employee-profile-correction">
    <%--
    <lang>
        <zh-CN>标题语义（W87 补）：同 EnterpriseCapabilityWorkbench —— 模块自带标题 div，显式补
        role="heading" 与 aria-level="1"，只加属性不改 class，视觉零变化。</zh-CN>
        <en>Title semantics (added in W87): as in EnterpriseCapabilityWorkbench — the module renders its own title div, so
        role="heading" and aria-level="1" are set explicitly; attributes only, no class change, no visual change.</en>
    </lang>
--%>
<div class="employee-profile-correction-title" role="heading" aria-level="1"><%= lang.EmployeeProfileCorrectionRequest_Heading %></div>
    <asp:Label ID="MessageLabel" CssClass="employee-profile-correction-message" runat="server" />

    <asp:Panel ID="RequestPanel" CssClass="employee-profile-correction-panel" Visible="false" runat="server">
        <%--
            <lang>
                <zh-CN>当前资料快照用字段网格展示；提交区仍保留原控件 ID 和事件，以维持 code-behind 绑定和回发兼容。</zh-CN>
                <en>The current profile snapshot renders in a field grid; the submission area keeps the original control IDs and events to preserve code-behind binding and postback compatibility.</en>
            </lang>
        --%>
        <div class="employee-profile-field-grid">
            <div class="employee-profile-field">
                <span class="employee-profile-correction-label employee-profile-field-label"><%= lang.EmployeeProfileCorrectionRequest_LabelEmployeeCode %></span>
                <span class="employee-profile-field-value"><asp:Label ID="EmployeeCodeLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field">
                <span class="employee-profile-correction-label employee-profile-field-label"><%= lang.EmployeeProfileCorrectionRequest_LabelName %></span>
                <span class="employee-profile-field-value"><asp:Label ID="DisplayNameLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field">
                <span class="employee-profile-correction-label employee-profile-field-label"><%= lang.EmployeeProfileCorrectionRequest_LabelSalutation %></span>
                <span class="employee-profile-field-value"><asp:Label ID="PreferredNameLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field">
                <span class="employee-profile-correction-label employee-profile-field-label"><%= lang.EmployeeProfileCorrectionRequest_LabelWorkEmail %></span>
                <span class="employee-profile-field-value"><asp:Label ID="WorkEmailLabel" runat="server" /></span>
            </div>
            <div class="employee-profile-field employee-profile-field-wide">
                <span class="employee-profile-correction-label employee-profile-field-label"><%= lang.EmployeeProfileCorrectionRequest_LabelOrganization %></span>
                <span class="employee-profile-field-value"><asp:Label ID="OrganizationLabel" runat="server" /></span>
            </div>
        </div>

        <%--
            <lang>
                <zh-CN>更正表单只承载服务器生成的字段白名单和有界低敏文本；MaxLength 约束改善交互，但规范化、授权与敏感字段边界仍由 code-behind 和数据服务负责。</zh-CN>
                <en>The correction form carries only a server-generated field allowlist and bounded low-sensitivity text; MaxLength improves interaction, while normalization, authorization, and sensitive-field boundaries remain owned by code-behind and the data service.</en>
            </lang>
        --%>
        <div class="employee-profile-correction-subtitle"><%= lang.EmployeeProfileCorrectionRequest_ButtonSubmitCorrection %></div>
        <div class="employee-profile-form-grid">
            <div class="employee-profile-form-field">
                <label class="employee-profile-correction-label employee-profile-field-label" for="<%= FieldNameList.ClientID %>"><%= lang.EmployeeProfileCorrectionRequest_LabelCorrectionFields %></label>
                <asp:DropDownList ID="FieldNameList" CssClass="NormalTextBox employee-profile-correction-input" runat="server" />
            </div>
            <div class="employee-profile-form-field">
                <label class="employee-profile-correction-label employee-profile-field-label" for="<%= ProposedValueTextBox.ClientID %>"><%= lang.EmployeeProfileCorrectionRequest_LabelSuggestedValue %></label>
                <asp:TextBox ID="ProposedValueTextBox" CssClass="NormalTextBox employee-profile-correction-input" MaxLength="512" runat="server" />
            </div>
            <div class="employee-profile-form-field employee-profile-form-field-wide">
                <label class="employee-profile-correction-label employee-profile-field-label" for="<%= RequestNoteTextBox.ClientID %>"><%= lang.EmployeeProfileCorrectionRequest_LabelNote %></label>
                <asp:TextBox ID="RequestNoteTextBox" CssClass="NormalTextBox employee-profile-correction-input employee-profile-correction-note"
                    MaxLength="1000" TextMode="MultiLine" Rows="4" runat="server" />
            </div>
            <%--
                <lang>
                    <zh-CN>提交按钮只进入 SubmitButton_Click 回发流程；它不直接修改员工主数据，当前身份、员工绑定、字段白名单和写入结果均由服务器重新确认。</zh-CN>
                    <en>The submit button only enters the SubmitButton_Click postback flow; it does not modify employee master data directly, because the server rechecks identity, employee binding, field allowlist, and write result.</en>
                </lang>
            --%>
            <div class="employee-profile-correction-actions">
                <asp:Button ID="SubmitButton" CssClass="CommandButton" Text="<%$ Resources:lang,EmployeeProfileCorrectionRequest_ButtonSubmit %>" OnClick="SubmitButton_Click" runat="server" />
            </div>
        </div>

        <div class="employee-profile-correction-subtitle"><%= lang.EmployeeProfileCorrectionRequest_SectionRecentRequests %></div>
        <div class="employee-profile-list-wrap">
            <%--
                <lang>
                    <zh-CN>最近请求列表只展示服务器返回的有限快照，字段使用编码绑定；列表是状态提示，不等同于员工主数据已修改或审核已完成。</zh-CN>
                    <en>The recent-request list shows a bounded server-returned snapshot with encoded fields; it is a status hint, not proof that employee master data changed or review completed.</en>
                </lang>
            --%>
            <asp:Repeater ID="RecentRequestsRepeater" runat="server">
                <HeaderTemplate>
                    <table class="employee-profile-correction-list" cellspacing="0" cellpadding="4" border="0">
                        <tr>
                            <th scope="col"><%= lang.EmployeeProfileCorrectionRequest_ColumnUtc %></th>
                            <th scope="col"><%= lang.EmployeeProfileCorrectionRequest_ColumnField %></th>
                            <th scope="col"><%= lang.EmployeeProfileCorrectionRequest_ColumnCurrentSnapshot %></th>
                            <th scope="col"><%= lang.EmployeeProfileCorrectionRequest_LabelSuggestedValue %></th>
                            <th scope="col"><%= lang.EmployeeProfileCorrectionRequest_ColumnStatus %></th>
                        </tr>
                </HeaderTemplate>
                <ItemTemplate>
                        <tr>
                            <td><%#: Eval("SubmittedUtcText") %></td>
                            <td><%#: Eval("FieldName") %></td>
                            <td><%#: Eval("CurrentValueSnapshot") %></td>
                            <td><%#: Eval("ProposedValue") %></td>
                            <td><%#: Eval("RequestStatus") %></td>
                        </tr>
                </ItemTemplate>
                <FooterTemplate>
                    <%--
                        <lang>
                            <zh-CN>P74.3 空态：零条时在表尾渲染"表头 + 提示行"，整行居中且弱化。放在 FooterTemplate 内，因为 Repeater 在零条时仍渲染表头与表尾；有数据时渲染器返回空串，无需分支。列数（5）与本表表头一致。</zh-CN>
                            <en>P74.3 empty state: with zero rows the footer renders the header-plus-hint row, centered and muted. It lives in the FooterTemplate because a Repeater still renders header and footer with zero rows, and the renderer returns an empty string when rows exist so no branch is needed. The column count (5) matches this table's header.</en>
                        </lang>
                    --%>
                    <%= PortalEmptyStateRenderer.Render(RecentRequestCount, lang.EmployeeProfileCorrectionRequest_EmptyNoItems, 5) %>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
        </div>
    </asp:Panel>
</div>
