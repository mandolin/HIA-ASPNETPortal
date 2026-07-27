using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>抽象业务申请和轻量 Workflow 事实的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for abstract business applications and lightweight workflow facts.</en>
    /// </lang>
    /// </summary>
    public interface IBusinessApplicationDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查申请表和流程事件表是否已部署。</zh-CN>
        ///   <en>Checks whether the application and workflow-event tables are deployed.</en>
        /// </lang>
        /// </summary>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交一条抽象业务申请。</zh-CN>
        ///   <en>Submits one abstract business application.</en>
        /// </lang>
        /// </summary>
        BusinessApplicationResult SubmitApplication(BusinessApplicationSubmitRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前用户最近提交的申请。</zh-CN>
        ///   <en>Reads recent applications submitted by the current user.</en>
        /// </lang>
        /// </summary>
        IList<BusinessApplicationInfo> GetRecentApplicationsForUser(int applicantUserId, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取后台申请列表。</zh-CN>
        ///   <en>Reads the administration application list.</en>
        /// </lang>
        /// </summary>
        IList<BusinessApplicationInfo> GetAdminApplications(string status, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行管理员审核动作。</zh-CN>
        ///   <en>Applies an administrator review action.</en>
        /// </lang>
        /// </summary>
        BusinessApplicationResult ReviewApplication(BusinessApplicationReviewRequest request);
    }
}
