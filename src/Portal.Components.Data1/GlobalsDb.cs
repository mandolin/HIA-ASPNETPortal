using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    public class GlobalsDb : IGlobalsDb
    {
        private readonly PortalCfgDbContext _context;
        private List<GlobalItem> _items;

        public GlobalsDb(PortalCfgDbContext context)
        {
            _context = context;
            // 初始化全局配置项列表，加载所有配置项
            _items = _context.Globals.ToList();
        }

        #region IGlobalsDb Members

        /// <summary>
        /// 获取单个门户的全局配置。
        /// </summary>
        /// <param name="portalId">门户标识符。</param>
        /// <returns>全局配置项。</returns>
        public IGlobalItem GetSinglePortal(int portalId)
        {
            // 从已加载的全局配置列表中查找指定门户ID的配置项
            return _items.Single(i => i.PortalId == portalId);
        }

        /// <summary>
        /// 更新指定门户的信息。
        /// </summary>
        /// <param name="portalId">门户标识符。</param>
        /// <param name="portalName">门户名称。</param>
        /// <param name="alwaysShow">是否总是显示编辑按钮。</param>
        public void UpdatePortalInfo(int portalId, string portalName, bool alwaysShow)
        {
            // 查找指定门户ID的全局配置项
            var globalRow = _items.Single(i => i.PortalId == portalId);

            // 更新配置项的属性
            globalRow.PortalName = portalName;
            globalRow.AlwaysShowEditButton = alwaysShow;

            // 保存更改到数据库
            _context.SaveChanges();

            // 重新加载所有配置项到内存，以反映最新的更改
            _items = _context.Globals.ToList();
        }

        #endregion
    }
}