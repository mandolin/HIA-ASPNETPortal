using System;
using System.Globalization;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;
using Resources;

namespace ASPNET.StarterKit.Portal
{
    /// <summary><lang>
    ///   <zh-CN>费用报销模块：提交后将业务记录持久化到 PortalBiz_ExpenseReimbursements，并写入「我的待办」工作项（与业务申请/员工资料更正同构）。</zh-CN>
    ///   <en>Expense-reimbursement module: on submit it persists the business record to PortalBiz_ExpenseReimbursements and writes a work item into "My Work Items" (isomorphic to business-application / employee-profile-correction).</en>
    /// </lang></summary>
    public partial class ExpenseReimbursement : PortalModuleControl<ExpenseReimbursement>
    {
        /// <summary><lang><zh-CN>用户数据访问服务，用于把当前登录名解析为门户用户标识。</zh-CN><en>User data service used to resolve the current sign-in name to a Portal user identifier.</en></lang></summary>
        [Dependency]
        public IUsersDb UsersDb { private get; set; }

        /// <summary><lang><zh-CN>费用报销数据访问（Unity 注入）。</zh-CN><en>Expense-reimbursement data access (Unity injected).</en></lang></summary>
        [Dependency]
        public IExpenseReimbursementDb ExpenseReimbursementDb { private get; set; }

        /// <summary><lang><zh-CN>待办数据访问（Unity 注入）。</zh-CN><en>Work-item data access (Unity injected).</en></lang></summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        /// <summary><lang><zh-CN>页面加载：首次进入时渲染“我的报销”列表；回发时保留表单状态，交由提交处理。</zh-CN><en>Page load: renders the "My Reimbursement" list on first entry; on postback the form state is preserved and handled by submit.</en></lang></summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                RenderRecent();
            }
        }

        private void RenderRecent()
        {
            int userId = GetCurrentUserId();
            if (userId <= 0 || ExpenseReimbursementDb == null || !ExpenseReimbursementDb.IsSchemaAvailable())
            {
                rptRecent.Visible = false;
                pnlEmpty.Visible = true;
                return;
            }

            var items = ExpenseReimbursementDb.GetRecentByUser(userId, 20);
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

        /// <summary><lang><zh-CN>提交费用报销：校验金额为正数后写入业务表，并生成一条指派给审批角色的待办；任一步失败都以低敏提示呈现。</zh-CN><en>Submits the expense reimbursement: validates that the amount is positive, writes the business row, and creates a work item assigned to the approval role; any failure surfaces as a low-sensitivity message.</en></lang></summary>
        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
            {
                ShowMessage("请先登录后再提交报销单。");
                return;
            }

            if (ExpenseReimbursementDb == null || !ExpenseReimbursementDb.IsSchemaAvailable())
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

            decimal amount;
            if (!decimal.TryParse(txtAmount.Text, out amount) || amount <= 0)
            {
                ShowMessage("报销金额必须为正数。");
                return;
            }

            var attachmentName = fileAttach.HasFile ? fileAttach.FileName : null;
            var result = ExpenseReimbursementDb.CreateRequest(new ExpenseReimbursementCreateRequest
            {
                UserId = userId,
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
                BusinessId = result.RequestId.ToString(CultureInfo.InvariantCulture),
                Title = "费用报销 #" + result.RequestId,
                Summary = category + " ¥" + amount.ToString("N2", CultureInfo.InvariantCulture),
                AssignedRoleKey = "Admins"
            });

            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                "ExpenseReimbursement.Submit",
                "Request",
                result.RequestId.ToString(CultureInfo.InvariantCulture),
                "Expense reimbursement submitted. UserId=" + userId,
                Context,
                null,
                "Success");

            ShowMessage("报销单已提交，可在「我的待办」跟踪审批进度。", false);
            RenderRecent();
        }

        private int GetCurrentUserId()
        {
            string userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || UsersDb == null)
            {
                return 0;
            }

            IUserItem user = UsersDb.GetSingleUser(userName);
            return user == null ? 0 : user.UserId;
        }

        private static string GetCurrentUserName()
        {
            var context = System.Web.HttpContext.Current;
            if (context == null || context.User == null || context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                return string.Empty;
            }

            return context.User.Identity.Name;
        }

        private void ShowMessage(string message, bool isError = true)
        {
            litMessage.Text = Server.HtmlEncode(message ?? string.Empty);
            pnlMessage.Visible = true;
            pnlMessage.CssClass = isError ? "expense-reimbursement-message" : "expense-reimbursement-message";
        }
    }
}
