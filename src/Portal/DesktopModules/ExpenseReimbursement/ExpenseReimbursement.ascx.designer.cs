//------------------------------------------------------------------------------
// <lang>
//   <zh-CN>此文件为费用报销模块的控件字段声明镜像；字段由标记层控件 ID 决定，重新生成时会覆盖手工补充的字段文档。</zh-CN>
//   <en>This file mirrors the control field declarations of the expense-reimbursement module; fields follow markup control IDs and manual field documentation may be overwritten on regeneration.</en>
// </lang>
//------------------------------------------------------------------------------

namespace ASPNET.StarterKit.Portal
{
    public partial class ExpenseReimbursement
    {
        /// <summary><lang><zh-CN>承载提交结果与校验提示的消息面板，仅在需要提示时可见。</zh-CN><en>Message panel that carries submit results and validation prompts, visible only when a message is present.</en></lang></summary>
        protected global::System.Web.UI.WebControls.Panel pnlMessage;

        /// <summary><lang><zh-CN>输出消息文本的文本控件；内容在服务端赋值以保证编码安全。</zh-CN><en>Literal that outputs message text; assigned server-side so the content stays encoding-safe.</en></lang></summary>
        protected global::System.Web.UI.WebControls.Literal litMessage;

        /// <summary><lang><zh-CN>费用类别下拉框（差旅/餐饮/办公/其他）。</zh-CN><en>Drop-down for the expense category (travel/meal/office/other).</en></lang></summary>
        protected global::System.Web.UI.WebControls.DropDownList ddlCategory;

        /// <summary><lang><zh-CN>报销金额输入框，仅接受正数并按两位小数展示。</zh-CN><en>Input box for the reimbursement amount; only positive values are accepted and displayed with two decimals.</en></lang></summary>
        protected global::System.Web.UI.WebControls.TextBox txtAmount;

        /// <summary><lang><zh-CN>发票/附件上传控件，仅登记文件名而不保存文件内容。</zh-CN><en>Invoice/attachment upload control; only the file name is recorded, the content is not persisted.</en></lang></summary>
        protected global::System.Web.UI.WebControls.FileUpload fileAttach;

        /// <summary><lang><zh-CN>费用事由输入框。</zh-CN><en>Input box for the expense reason.</en></lang></summary>
        protected global::System.Web.UI.WebControls.TextBox txtReason;

        /// <summary><lang><zh-CN>提交费用报销的按钮。</zh-CN><en>Button that submits the expense reimbursement.</en></lang></summary>
        protected global::System.Web.UI.WebControls.Button btnSubmit;

        /// <summary><lang><zh-CN>暂无报销记录时展示的空态面板。</zh-CN><en>Empty-state panel shown when no reimbursement records exist.</en></lang></summary>
        protected global::System.Web.UI.WebControls.Panel pnlEmpty;

        /// <summary><lang><zh-CN>展示当前用户最近报销单的列表控件。</zh-CN><en>Repeater that lists the current user's recent reimbursements.</en></lang></summary>
        protected global::System.Web.UI.WebControls.Repeater rptRecent;
    }
}
