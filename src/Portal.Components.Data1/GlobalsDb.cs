using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧门户全局配置的数据访问实现。</zh-CN>
    ///     <en>Data access implementation for legacy portal global configuration.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该实现围绕 <see cref="PortalCfgDbContext.Globals"/> 做最小读取和保存，并在实例内缓存已加载的全局配置行。
    ///       调用方仍负责决定是否允许当前用户修改站点名称或编辑按钮策略。
    ///     </zh-CN>
    ///     <en>
    ///       This implementation performs minimal read and save operations around <see cref="PortalCfgDbContext.Globals"/>
    ///       and caches loaded global configuration rows inside the instance. Callers still decide whether the current
    ///       user may change the site name or edit-button policy.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public class GlobalsDb : IGlobalsDb
    {
        private readonly PortalCfgDbContext _context;
        private List<GlobalItem> _items;

        /// <summary>
        ///   <lang>
        ///     <zh-CN>初始化全局配置数据访问实现，并加载当前配置快照。</zh-CN>
        ///     <en>Initializes the global configuration data access implementation and loads the current configuration snapshot.</en>
        ///   </lang>
        /// </summary>
        /// <param name="context">
        ///   <l>
        ///     <zh-CN>门户结构配置数据库上下文。</zh-CN>
        ///     <en>Portal structural configuration database context.</en>
        ///   </l>
        /// </param>
        public GlobalsDb(PortalCfgDbContext context)
        {
            _context = context;

            // <lang>
            //   <zh-CN>构造时加载一次配置快照，后面的读取直接走内存列表，符合旧站点设置控件的轻量访问模式。</zh-CN>
            //   <en>Load one configuration snapshot during construction so later reads use the in-memory list, matching the lightweight access pattern of the legacy site-settings control.</en>
            // </lang>
            _items = _context.Globals.ToList();
        }

        #region IGlobalsDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取单个门户的全局配置。</zh-CN>
        ///   <en>Gets global configuration for a single portal.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalId">
        ///   <l>
        ///     <zh-CN>门户标识符。</zh-CN>
        ///     <en>Portal identifier.</en>
        ///   </l>
        /// </param>
        /// <returns>
        ///   <l>
        ///     <zh-CN>匹配的全局配置项。</zh-CN>
        ///     <en>Matching global configuration item.</en>
        ///   </l>
        /// </returns>
        public IGlobalItem GetSinglePortal(int portalId)
        {
            // <lang>
            //   <zh-CN>这里保持旧实现的严格查找语义：缺少或重复配置会抛出异常，便于暴露基础配置损坏。</zh-CN>
            //   <en>Keep the legacy strict lookup semantics here: missing or duplicated configuration throws, making damaged base configuration visible.</en>
            // </lang>
            return _items.Single(i => i.PortalId == portalId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新指定门户的信息。</zh-CN>
        ///   <en>Updates information for the specified portal.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalId">
        ///   <l>
        ///     <zh-CN>门户标识符。</zh-CN>
        ///     <en>Portal identifier.</en>
        ///   </l>
        /// </param>
        /// <param name="portalName">
        ///   <l>
        ///     <zh-CN>门户名称。</zh-CN>
        ///     <en>Portal name.</en>
        ///   </l>
        /// </param>
        /// <param name="alwaysShow">
        ///   <l>
        ///     <zh-CN>是否始终显示模块编辑按钮。</zh-CN>
        ///     <en>Whether module edit buttons should always be shown.</en>
        ///   </l>
        /// </param>
        public void UpdatePortalInfo(int portalId, string portalName, bool alwaysShow)
        {
            // <lang>
            //   <zh-CN>先定位内存快照中的目标行，保持和读取路径一致的严格单行语义。</zh-CN>
            //   <en>Locate the target row in the in-memory snapshot first, preserving the same strict single-row semantics as the read path.</en>
            // </lang>
            var globalRow = _items.Single(i => i.PortalId == portalId);

            // <lang>
            //   <zh-CN>只更新旧全局配置表拥有的两个字段；在线设置、主题设置和安全策略不在这里写入。</zh-CN>
            //   <en>Update only the two fields owned by the legacy global configuration table; online settings, theme settings, and security policy are not written here.</en>
            // </lang>
            globalRow.PortalName = portalName;
            globalRow.AlwaysShowEditButton = alwaysShow;

            // <lang>
            //   <zh-CN>EF 跟踪实体负责生成更新语句；调用页面负责权限检查、输入归一化和审计。</zh-CN>
            //   <en>The EF tracked entity produces the update statement; the calling page owns authorization checks, input normalization, and auditing.</en>
            // </lang>
            _context.SaveChanges();

            // <lang>
            //   <zh-CN>保存后刷新快照，避免同一个数据访问实例继续返回保存前的配置值。</zh-CN>
            //   <en>Refresh the snapshot after saving so the same data access instance does not keep returning pre-save values.</en>
            // </lang>
            _items = _context.Globals.ToList();
        }

        #endregion
    }
}
