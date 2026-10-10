using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Web.UI.WebControls;
using Portal;
using Portal.Components;

namespace Portal.DesktopModules.ExpenseReimbursement
{
    /// <summary><lang>
    ///   <zh-CN>费用报销模块：提交后将业务记录持久化到 PortalBiz_ExpenseReimbursements，并写入「我的待办」工作项（与业务申请/员工资料更正同构）。</zh-CN>
    ///   <en>Expense-reimbursement module: on submit it persists the business record to PortalBiz_ExpenseReimbursements and writes a work item into "My Work Items" (isomorphic to business-application / employee-profile-correction).</en>
    /// </lang></summary>
    public partial class ExpenseReimbursement : PortalModuleControl<ModuleType.BasicBusiness>
    {
        /// <summary><lang><zh-CN>费用报销数据访问（MEF 注入）。</zh-CN><en>Expense-reimbursement data access (MEF injected).</en></lang></summary>
        [Dependency]
        public IExpenseReimbursementDb ExpenseReimbursementDb { get; set; }

        /// <summary><lang><zh-CN>待办数据访问（MEF 注入）。</zh-CN><en>Work-item data access (MEF injected).</en></lang></summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                RenderRecent();
            }
        }

        private void RenderRecent()
        {
            var user = PortalUserContext.Current;
            if (user == null || !ExpenseReimbursementDb.IsSchemaAvailable())
            {
                rptRecent.Visible = false;
                pnlEmpty.Visible = true;
                return;
            }

            var items = ExpenseReimbursementDb.GetRecentByUser(user.UserId, 20);
            if (items == null || items.Count == 0)
            {
                rptRecent.Visible = false;
                pnlEmpty.Visible = true;
                return;
            }

            rptRecent.DataSource = items;
            rptRecent.DataBind();
            rptRecent.Visible = true;
            pnlEmpty.Visible = false;
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            var user = PortalUserContext.Current;
            if (user == null)
            {
                ShowMessage("请先登录后再提交报销单。");
                return;
            }

            if (!ExpenseReimbursementDb.IsSchemaAvailable())
            {
                ShowMessage("报销服务暂不可用，请稍后重试。");
                return;
            }

            var category = ddlCategory.SelectedValue;
            if (string.IsNullOrWhiteSpace(txtAmount.Text))
            {
                ShowMessage("请填写报销金额。");
                return;
            }

            if (!decimal.TryParse(txtAmount.Text, out var amount) || amount <= 0)
            {
                ShowMessage("报销金额必须为正数。");
                return;
            }

            var attachmentName = fileAttach.HasFile ? fileAttach.FileName : null;
            var result = ExpenseReimbursementDb.CreateRequest(new ExpenseReimbursementCreateRequest
            {
                UserId = user.UserId,
                Category = category,
                Amount = amount,
                Reason = txtReason.Text,
                AttachmentName = attachmentName
            });

            if (!result.Ok)
            {
                ShowMessage("提交失败，请稍后重试。");
                return;
            }

            WorkItemDb.EnsureWorkItem(new PortalWorkItemCreateRequest
            {
                BusinessKind = PortalWorkItemBusinessKinds.ExpenseReimbursement,
                BusinessId = result.RequestId,
                Title = "费用报销 #" + result.RequestId,
                Summary = category + " ¥" + amount.ToString("N2")
            });

            PortalOperationAudit.Record("ExpenseReimbursement.Submit", "UserId=" + user.UserId + " RequestId=" + result.RequestId);
            ShowMessage("报销单已提交，可在「我的待办」跟踪审批进度。", false);
            RenderRecent();
        }

        private void ShowMessage(string text, bool isError = true)
        {
            litMessage.Text = text;
            pnlMessage.Visible = true;
            pnlMessage.CssClass = isError ? "expense-reimbursement-message" : "expense-reimbursement-message";
        }
    }
}
