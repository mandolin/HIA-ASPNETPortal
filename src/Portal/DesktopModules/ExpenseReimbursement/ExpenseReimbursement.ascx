<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ExpenseReimbursement.ascx.cs" Inherits="ASPNET.StarterKit.Portal.ExpenseReimbursement" %>
<%--
    <lang>
        <zh-CN>C-anp-P20 业务模块：员工提交费用报销单并查看本人历史记录。低敏数据（费用类别、金额、事由、发票附件），
        不含脚本、外部资源或第三方引用；标记层不执行任何数据写入，提交与读取全部由 code-behind 负责。</zh-CN>
        <en>C-anp-P20 business module: employees submit expense reimbursement claims and review their own history. It handles low-sensitivity data (category, amount, reason, invoice attachment) with no script, external resource or third-party reference; the markup performs no data writes — submission and reads are entirely the code-behind's responsibility.</en>
    </lang>
--%>
<div class="expense-reimbursement">
    <div class="expense-reimbursement-title"><asp:Literal Text="<%$ Resources:lang,ExpenseReimbursement_Title%>" runat="server" /></div>
    <%--
        <lang>
            <zh-CN>消息面板默认隐藏，仅在 code-behind 判定成功或失败后置可见；文案由 code-behind 写入 Literal，不在此处拼接。</zh-CN>
            <en>The message panel is hidden by default and only made visible after the code-behind determines success or failure; its text is written into the Literal by the code-behind and is never assembled here.</en>
        </lang>
    --%>
    <asp:Panel ID="pnlMessage" runat="server" Visible="false">
        <span class="expense-reimbursement-message"><asp:Literal ID="litMessage" runat="server" /></span>
    </asp:Panel>
    <%--
        <lang>
            <zh-CN>表单区为纯低敏字段：费用类别下拉、金额输入、受 PortalDocumentPolicy 约束的发票附件上传，以及事由多行文本。
            控件 ID 与 code-behind 的事件绑定保持一致，改动 ID 会破坏回发兼容。</zh-CN>
            <en>The form area is entirely low-sensitivity fields: a category dropdown, an amount input, one invoice attachment upload constrained by PortalDocumentPolicy, and a multi-line reason. Control IDs stay aligned with code-behind event bindings, so changing an ID breaks postback compatibility.</en>
        </lang>
    --%>
    <div class="expense-reimbursement-form-grid">
        <div class="expense-reimbursement-form-field expense-reimbursement-form-field-wide">
            <label class="SubHead expense-reimbursement-label"><asp:Literal Text="<%$ Resources:lang,ExpenseReimbursement_LabelCategory%>" runat="server" /></label>
            <asp:DropDownList ID="ddlCategory" runat="server" CssClass="NormalTextBox expense-reimbursement-input">
                <asp:ListItem Text="<%$ Resources:lang,ExpenseReimbursement_CategoryTravel %>" Value="差旅" />
                <asp:ListItem Text="<%$ Resources:lang,ExpenseReimbursement_CategoryMeal %>" Value="餐饮" />
                <asp:ListItem Text="<%$ Resources:lang,ExpenseReimbursement_CategoryOffice %>" Value="办公" />
                <asp:ListItem Text="<%$ Resources:lang,ExpenseReimbursement_CategoryOther %>" Value="其他" />
            </asp:DropDownList>
        </div>
        <div class="expense-reimbursement-form-field">
            <label class="SubHead expense-reimbursement-label"><asp:Literal Text="<%$ Resources:lang,ExpenseReimbursement_LabelAmount%>" runat="server" /></label>
            <asp:TextBox ID="txtAmount" runat="server" CssClass="NormalTextBox expense-reimbursement-input" />
        </div>
        <div class="expense-reimbursement-form-field">
            <label class="SubHead expense-reimbursement-label"><asp:Literal Text="<%$ Resources:lang,ExpenseReimbursement_LabelInvoice%>" runat="server" /></label>
            <asp:FileUpload ID="fileAttach" runat="server" CssClass="expense-reimbursement-input" />
        </div>
        <div class="expense-reimbursement-form-field expense-reimbursement-form-field-wide">
            <label class="SubHead expense-reimbursement-label"><asp:Literal Text="<%$ Resources:lang,ExpenseReimbursement_LabelReason%>" runat="server" /></label>
            <asp:TextBox ID="txtReason" runat="server" TextMode="MultiLine" Rows="5" CssClass="NormalTextBox expense-reimbursement-input expense-reimbursement-body" />
        </div>
    </div>
    <%--
        <lang>
            <zh-CN>动作区只有一个服务端提交按钮，无客户端脚本、无确认框；提交后的校验与落库都在 code-behind，
            标记层不参与业务规则判断。</zh-CN>
            <en>The action area holds a single server-side submit button with no client script and no confirmation dialog; post-submit validation and persistence live entirely in the code-behind, and the markup takes no part in business rules.</en>
        </lang>
    --%>
    <div class="expense-reimbursement-actions">
        <asp:Button ID="btnSubmit" runat="server" CssClass="CommandButton" Text="<%$ Resources:lang,ExpenseReimbursement_ButtonSubmit %>" OnClick="btnSubmit_Click" />
    </div>
    <%--
        <lang>
            <zh-CN>列表区只读：Repeater 绑定 code-behind 提供的本人报销记录，Header/Item/Footer 模板只做展示，
            不含编辑或删除入口；无记录时显示空态面板。金额按固定两位小数格式化，不在此处做币种换算。</zh-CN>
            <en>The list area is read-only: the Repeater binds the caller's own expense records supplied by the code-behind, and the Header/Item/Footer templates only render — no edit or delete entry point exists, and an empty-state panel covers the no-records case. Amounts use a fixed two-decimal format; no currency conversion happens here.</en>
        </lang>
    --%>
    <div class="expense-reimbursement-subtitle"><asp:Literal Text="<%$ Resources:lang,ExpenseReimbursement_SubtitleMyClaims%>" runat="server" /></div>
    <div class="expense-reimbursement-list-wrap">
        <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
            <span class="expense-reimbursement-empty"><asp:Literal Text="<%$ Resources:lang,ExpenseReimbursement_EmptyState%>" runat="server" /></span>
        </asp:Panel>
        <asp:Repeater ID="rptRecent" runat="server" Visible="false">
            <HeaderTemplate>
                <table class="expense-reimbursement-list" cellspacing="0" cellpadding="4" border="0">
                    <tr><th scope="col"><asp:Literal Text="<%$ Resources:lang,ExpenseReimbursement_ColumnId%>" runat="server" /></th><th scope="col"><asp:Literal Text="<%$ Resources:lang,ExpenseReimbursement_ColumnCategory%>" runat="server" /></th><th scope="col"><asp:Literal Text="<%$ Resources:lang,ExpenseReimbursement_ColumnAmount%>" runat="server" /></th><th scope="col"><asp:Literal Text="<%$ Resources:lang,ExpenseReimbursement_ColumnStatus%>" runat="server" /></th></tr>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><%# Eval("RequestId") %></td>
                    <td><%# Eval("Category") %></td>
                    <td><%# Eval("Amount", "{0:N2}") %></td>
                    <td><%# Eval("Status") %></td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                </table>
            </FooterTemplate>
        </asp:Repeater>
    </div>
</div>
