using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项和事项事件的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for enterprise collaboration items and item events.</en>
    /// </lang>
    /// </summary>
    public interface ICollaborationItemDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查协同事项主表和事件表是否已部署。</zh-CN>
        ///   <en>Checks whether the collaboration-item fact and event tables are deployed.</en>
        /// </lang>
        /// </summary>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建并提交一条企业协同事项。</zh-CN>
        ///   <en>Creates and submits one enterprise collaboration item.</en>
        /// </lang>
        /// </summary>
        CollaborationItemResult CreateSubmittedItem(CollaborationItemCreateRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定用户最近发起或负责的协同事项。</zh-CN>
        ///   <en>Reads recent collaboration items initiated by or assigned to a specific user.</en>
        /// </lang>
        /// </summary>
        IList<CollaborationItemInfo> GetRecentItemsForUser(int userId, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取后台协同事项列表。</zh-CN>
        ///   <en>Reads the administration collaboration-item list.</en>
        /// </lang>
        /// </summary>
        IList<CollaborationItemInfo> GetAdminItems(string status, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前用户可见的事项时间线；服务端会重新验证参与者或管理员范围。</zh-CN>
        ///   <en>Reads the item timeline visible to the current user; the server revalidates participant or administrator scope.</en>
        /// </lang>
        /// </summary>
        IList<CollaborationItemEventInfo> GetVisibleEvents(long itemId, int actorUserId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建不改变事项状态的纯文本评论。</zh-CN>
        ///   <en>Creates a plain-text comment that does not change item state.</en>
        /// </lang>
        /// </summary>
        CollaborationItemCommentResult AddComment(CollaborationItemCommentCreateRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行协同事项状态动作。</zh-CN>
        ///   <en>Applies a state action to a collaboration item.</en>
        /// </lang>
        /// </summary>
        CollaborationItemResult ApplyAction(CollaborationItemActionRequest request);
    }
}
