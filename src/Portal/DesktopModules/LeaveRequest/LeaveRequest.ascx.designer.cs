//------------------------------------------------------------------------------
// <lang>
//   <zh-CN>此文件为请假申请模块的控件字段声明镜像；字段由标记层控件 ID 决定，重新生成时会覆盖手工补充的字段文档。</zh-CN>
//   <en>This file mirrors the control field declarations of the leave-request module; fields follow markup control IDs and manual field documentation may be overwritten on regeneration.</en>
// </lang>
//------------------------------------------------------------------------------

namespace ASPNET.StarterKit.Portal
{
    public partial class LeaveRequest
    {
        /// <summary><lang><zh-CN>承载提交结果与校验提示的消息面板，仅在需要提示时可见。</zh-CN><en>Message panel that carries submit results and validation prompts, visible only when a message is present.</en></lang></summary>
        protected global::System.Web.UI.WebControls.Panel pnlMessage;

        /// <summary><lang><zh-CN>输出消息文本的文本控件；内容在服务端赋值以保证编码安全。</zh-CN><en>Literal that outputs message text; assigned server-side so the content stays encoding-safe.</en></lang></summary>
        protected global::System.Web.UI.WebControls.Literal litMessage;

        /// <summary><lang><zh-CN>请假类型下拉框（年假/事假/病假/调休）。</zh-CN><en>Drop-down for the leave type (annual/personal/sick/compensatory).</en></lang></summary>
        protected global::System.Web.UI.WebControls.DropDownList ddlLeaveType;

        /// <summary><lang><zh-CN>请假开始日期输入框。</zh-CN><en>Input box for the leave start date.</en></lang></summary>
        protected global::System.Web.UI.WebControls.TextBox txtStartDate;

        /// <summary><lang><zh-CN>请假结束日期输入框。</zh-CN><en>Input box for the leave end date.</en></lang></summary>
        protected global::System.Web.UI.WebControls.TextBox txtEndDate;

        /// <summary><lang><zh-CN>请假天数输入框，仅接受正数。</zh-CN><en>Input box for the number of leave days; only positive values are accepted.</en></lang></summary>
        protected global::System.Web.UI.WebControls.TextBox txtDays;

        /// <summary><lang><zh-CN>请假附件上传控件，仅登记文件名而不保存文件内容。</zh-CN><en>Attachment upload control; only the file name is recorded, the content is not persisted.</en></lang></summary>
        protected global::System.Web.UI.WebControls.FileUpload fileAttach;

        /// <summary><lang><zh-CN>请假事由输入框。</zh-CN><en>Input box for the leave reason.</en></lang></summary>
        protected global::System.Web.UI.WebControls.TextBox txtReason;

        /// <summary><lang><zh-CN>提交请假申请的按钮。</zh-CN><en>Button that submits the leave request.</en></lang></summary>
        protected global::System.Web.UI.WebControls.Button btnSubmit;

        /// <summary><lang><zh-CN>暂无请假记录时展示的空态面板。</zh-CN><en>Empty-state panel shown when no leave records exist.</en></lang></summary>
        protected global::System.Web.UI.WebControls.Panel pnlEmpty;

        /// <summary><lang><zh-CN>展示当前用户最近请假申请的列表控件。</zh-CN><en>Repeater that lists the current user's recent leave requests.</en></lang></summary>
        protected global::System.Web.UI.WebControls.Repeater rptRecent;
    }
}
