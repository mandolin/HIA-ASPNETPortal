<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LeaveRequest.ascx.cs" Inherits="ASPNET.StarterKit.Portal.LeaveRequest" %>
<%--
    <lang>
        <zh-CN>C-anp-P20 业务模块：员工提交请假申请并查看本人历史记录。低敏数据（假期类型、起止日期、天数、事由、附件），
        不含脚本、外部资源或第三方引用；标记层不执行任何数据写入，提交与读取全部由 code-behind 负责。</zh-CN>
        <en>C-anp-P20 business module: employees submit leave requests and review their own history. It handles low-sensitivity
            data (leave type, start/end dates, day count, reason, attachment) with no script, external resource or third-party
            reference; the markup performs no data writes — submission and reads are entirely the code-behind's responsibility.</en>
    </lang>
--%>
<div class="leave-request">
    <div class="leave-request-title"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_Title%>" runat="server" /></div>
    <%--
        <lang>
            <zh-CN>消息面板默认隐藏，仅在 code-behind 判定成功或失败后置可见；文案由 code-behind 写入 Literal，不在此处拼接。</zh-CN>
            <en>The message panel is hidden by default and only made visible after the code-behind determines success or failure; its text is written into the Literal by the code-behind and is never assembled here.</en>
        </lang>
    --%>
    <asp:Panel ID="pnlMessage" runat="server" Visible="false">
        <span class="leave-request-message"><asp:Literal ID="litMessage" runat="server" /></span>
    </asp:Panel>
    <%--
        <lang>
            <zh-CN>表单区为纯低敏字段：下拉假期类型、两个日期输入、天数与事由，以及一个受 PortalDocumentPolicy 约束的附件上传。
            控件 ID 与 code-behind 的事件绑定保持一致，改动 ID 会破坏回发兼容。</zh-CN>
            <en>The form area is entirely low-sensitivity fields: a leave-type dropdown, two date inputs, day count and reason, plus one attachment upload constrained by PortalDocumentPolicy. Control IDs stay aligned with code-behind event bindings, so changing an ID breaks postback compatibility.</en>
        </lang>
    --%>
    <div class="leave-request-form-grid">
        <div class="leave-request-form-field leave-request-form-field-wide">
            <label class="SubHead leave-request-label"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_LabelLeaveType%>" runat="server" /></label>
            <asp:DropDownList ID="ddlLeaveType" runat="server" CssClass="NormalTextBox leave-request-input">
                <asp:ListItem Text="年假" Value="年假" />
                <asp:ListItem Text="事假" Value="事假" />
                <asp:ListItem Text="病假" Value="病假" />
                <asp:ListItem Text="调休" Value="调休" />
            </asp:DropDownList>
        </div>
        <div class="leave-request-form-field">
            <label class="SubHead leave-request-label"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_LabelStartDate%>" runat="server" /></label>
            <asp:TextBox ID="txtStartDate" runat="server" TextMode="Date" CssClass="NormalTextBox leave-request-input" />
        </div>
        <div class="leave-request-form-field">
            <label class="SubHead leave-request-label"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_LabelEndDate%>" runat="server" /></label>
            <asp:TextBox ID="txtEndDate" runat="server" TextMode="Date" CssClass="NormalTextBox leave-request-input" />
        </div>
        <div class="leave-request-form-field">
            <label class="SubHead leave-request-label"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_LabelDays%>" runat="server" /></label>
            <asp:TextBox ID="txtDays" runat="server" CssClass="NormalTextBox leave-request-input" />
        </div>
        <div class="leave-request-form-field">
            <label class="SubHead leave-request-label"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_LabelAttachment%>" runat="server" /></label>
            <asp:FileUpload ID="fileAttach" runat="server" CssClass="leave-request-input" />
        </div>
        <div class="leave-request-form-field leave-request-form-field-wide">
            <label class="SubHead leave-request-label"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_LabelReason%>" runat="server" /></label>
            <asp:TextBox ID="txtReason" runat="server" TextMode="MultiLine" Rows="5" CssClass="NormalTextBox leave-request-input leave-request-body" />
        </div>
    </div>
    <%--
        <lang>
            <zh-CN>动作区只有一个服务端提交按钮，无客户端脚本、无确认框；提交后的校验与落库都在 code-behind，
            标记层不参与业务规则判断。</zh-CN>
            <en>The action area holds a single server-side submit button with no client script and no confirmation dialog; post-submit validation and persistence live entirely in the code-behind, and the markup takes no part in business rules.</en>
        </lang>
    --%>
    <div class="leave-request-actions">
        <asp:Button ID="btnSubmit" runat="server" CssClass="CommandButton" Text="<%$ Resources:lang,LeaveRequest_ButtonSubmit %>" OnClick="btnSubmit_Click" />
    </div>
    <%--
        <lang>
            <zh-CN>列表区只读：Repeater 绑定 code-behind 提供的本人请假记录，Header/Item/Footer 模板只做展示，
            不含编辑或删除入口；无记录时显示空态面板。列表不引入分页或排序控件。</zh-CN>
            <en>The list area is read-only: the Repeater binds the caller's own leave records supplied by the code-behind, and the Header/Item/Footer templates only render — no edit or delete entry point exists, and an empty-state panel covers the no-records case. No paging or sorting control is introduced.</en>
        </lang>
    --%>
    <div class="leave-request-subtitle"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_SubtitleMyRequests%>" runat="server" /></div>
    <div class="leave-request-list-wrap">
        <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
            <span class="leave-request-empty"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_EmptyState%>" runat="server" /></span>
        </asp:Panel>
        <asp:Repeater ID="rptRecent" runat="server" Visible="false">
            <HeaderTemplate>
                <table class="leave-request-list" cellspacing="0" cellpadding="4" border="0">
                    <tr><th scope="col"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_ColumnId%>" runat="server" /></th><th scope="col"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_ColumnType%>" runat="server" /></th><th scope="col"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_ColumnPeriod%>" runat="server" /></th><th scope="col"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_ColumnDays%>" runat="server" /></th><th scope="col"><asp:Literal Text="<%$ Resources:lang,LeaveRequest_ColumnStatus%>" runat="server" /></th></tr>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><%# Eval("RequestId") %></td>
                    <td><%# Eval("LeaveType") %></td>
                    <td><%# Eval("StartDate", "{0:yyyy-MM-dd}") %> ~ <%# Eval("EndDate", "{0:yyyy-MM-dd}") %></td>
                    <td><%# Eval("Days") %></td>
                    <td><%# Eval("Status") %></td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                </table>
            </FooterTemplate>
        </asp:Repeater>
    </div>
</div>
