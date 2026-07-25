using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧门户事件模块的 EF 投影实体。</zh-CN>
    ///   <en>Entity Framework projection entity for the legacy portal events module.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该类型直接映射 `Portal_Events` 表，用于 Data1 时代的事件展示和编辑兼容路径。字段保持旧表语义，不在实体层附加权限、过期过滤或 HTML 安全策略。</zh-CN>
    ///   <en>This type maps directly to the `Portal_Events` table for the Data1-era event display and editing compatibility path. Field meanings follow the legacy table and the entity does not add authorization, expiration filtering or HTML safety policy.</en>
    /// </lang>
    /// </remarks>
    [Table("Portal_Events")]
    public class EventItem : IEventItem
    {
        #region IEventItem Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件条目的旧表主键。</zh-CN>
        ///   <en>Legacy table primary key for the event item.</en>
        /// </lang>
        /// </summary>
        [Key]
        public int ItemId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>承载该事件条目的模块实例标识。</zh-CN>
        ///   <en>Identifier of the module instance that owns this event item.</en>
        /// </lang>
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件标题，展示层负责按页面上下文进行输出编码。</zh-CN>
        ///   <en>Event title; the presentation layer performs output encoding according to page context.</en>
        /// </lang>
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建人显示名或历史用户名快照。</zh-CN>
        ///   <en>Creator display name or historical user-name snapshot.</en>
        /// </lang>
        /// </summary>
        public string CreatedByUser { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧模块合并保存的地点与时间说明文本。</zh-CN>
        ///   <en>Legacy free-form text combining event location and time description.</en>
        /// </lang>
        /// </summary>
        public string WhereWhen { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件创建时间；历史数据可能为空。</zh-CN>
        ///   <en>Event creation time; historical rows may leave it empty.</en>
        /// </lang>
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件过期时间；列表过滤规则由调用侧决定。</zh-CN>
        ///   <en>Event expiration time; list filtering rules are decided by callers.</en>
        /// </lang>
        /// </summary>
        public DateTime? ExpireDate { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件正文描述，可能包含旧模块允许的富文本内容。</zh-CN>
        ///   <en>Event body description, which may contain rich text accepted by the legacy module.</en>
        /// </lang>
        /// </summary>
        public string Description { get; set; }

        #endregion
    }
}
