using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Web.UI.WebControls;
using Portal;
using Portal.Components;

namespace Portal.DesktopModules.LeaveRequest
{
    /// <summary><lang>
    ///   <zh-CN>请假申请模块：提交后将业务记录持久化到 PortalBiz_LeaveRequests，并写入「我的待办」工作项（与业务申请/员工资料更正同构）。</zh-CN>
    ///   <en>Leave-request module: on submit it persists the business record to PortalBiz_LeaveRequests and writes a work item into "My Work Items" (isomorphic to business-application / employee-profile-correction).</en>
    /// </lang></summary>
    public partial class LeaveRequest : PortalModuleControl<ModuleType.BasicBusiness>
    {
        /// <summary><lang><zh-CN>请假申请数据访问（MEF 注入）。</zh-CN><en>Leave-request data access (MEF injected).</en></lang></summary>
        [Dependency]
        public ILeaveRequestDb LeaveRequestDb { get; set; }

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
            if (user == null || !LeaveRequestDb.IsSchemaAvailable())
            {
                rptRecent.Visible = false;
                pnlEmpty.Visible = true;
                return;
            }

            var items = LeaveRequestDb.GetRecentByUser(user.UserId, 20);
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
                ShowMessage("请先登录后再提交请假申请。");
                return;
            }

            if (!LeaveRequestDb.IsSchemaAvailable())
            {
                ShowMessage("请假服务暂不可用，请稍后重试。");
                return;
            }

            var leaveType = ddlLeaveType.SelectedValue;
            if (!DateTime.TryParse(txtStartDate.Text, out var startDate) ||
                !DateTime.TryParse(txtEndDate.Text, out var endDate) ||
                string.IsNullOrWhiteSpace(txtDays.Text))
            {
                ShowMessage("请完整填写请假类型、起止日期与天数。");
                return;
            }

            if (!decimal.TryParse(txtDays.Text, out var days) || days <= 0)
            {
                ShowMessage("请假天数必须为正数。");
                return;
            }

            var attachmentName = fileAttach.HasFile ? fileAttach.FileName : null;
            var result = LeaveRequestDb.CreateRequest(new LeaveRequestCreateRequest
            {
                UserId = user.UserId,
                LeaveType = leaveType,
                StartDate = startDate,
                EndDate = endDate,
                Days = days,
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
                BusinessKind = PortalWorkItemBusinessKinds.LeaveRequest,
                BusinessId = result.RequestId,
                Title = "请假申请 #" + result.RequestId,
                Summary = leaveType + " " + startDate.ToString("yyyy-MM-dd") + " ~ " + endDate.ToString("yyyy-MM-dd")
            });

            PortalOperationAudit.Record("LeaveRequest.Submit", "UserId=" + user.UserId + " RequestId=" + result.RequestId);
            ShowMessage("请假申请已提交，可在「我的待办」跟踪审批进度。", false);
            RenderRecent();
        }

        private void ShowMessage(string text, bool isError = true)
        {
            litMessage.Text = text;
            pnlMessage.Visible = true;
            pnlMessage.CssClass = isError ? "leave-request-message" : "leave-request-message";
        }
    }
}
