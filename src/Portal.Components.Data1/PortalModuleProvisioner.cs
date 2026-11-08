using System;
using System.Data.Entity;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>独立模块安装/卸载机制的可复用实现，基于门户结构配置上下文执行幂等 SQL。
    ///   安装把模块定义、页签/模块实例与包状态写入 PortalCfg_* 表，使容纳机制（可信目录 + Profile 形态感知 + 状态表 IsEnabled）能实际装载运行；
    ///   卸载做反向清理且不残留。与 W-anp-P284 的 Form Builder 模块包配合，构成 R2「本体之外提供的能力」证据闭环的安装/卸载两端。</zh-CN>
    ///   <en>Reusable implementation of the independent-module install/uninstall mechanism, executing idempotent SQL against the portal structural configuration context.
    ///   Install writes the module definition, tab/module instance and package state into the PortalCfg_* tables so the hosting mechanism (trusted catalog + profile shape awareness + state-table IsEnabled) can actually load and run the module;
    ///   uninstall reverses the cleanup without residue. Together with the W-anp-P284 Form Builder module package it closes the R2 evidence loop for capabilities provided externally.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalModuleProvisioner : IPortalModuleProvisioner
    {
        // <lang>
        //   <zh-CN>门户结构配置上下文：承载 PortalCfg_Tabs / PortalCfg_ModuleDefinitions / PortalCfg_Modules / PortalCfg_ModulePackageStates 的读写。</zh-CN>
        //   <en>Portal structural configuration context: owns read/write for PortalCfg_Tabs / PortalCfg_ModuleDefinitions / PortalCfg_Modules / PortalCfg_ModulePackageStates.</en>
        // </lang>
        private readonly PortalCfgDbContext context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>注入门户结构配置上下文；连接串由启动期注册的命名实例提供。</zh-CN>
        ///   <en>Injects the portal structural configuration context; the connection string comes from the startup-registered named instance.</en>
        /// </lang>
        /// </summary>
        /// <param name="context"><l zh-CN="门户结构配置数据上下文。" en="Portal structural configuration data context." /></param>
        public PortalModuleProvisioner(PortalCfgDbContext context)
        {
            // <lang>
            //   <zh-CN>上下文为后续所有幂等写入的前提；空引用直接失败，不进入静默降级分支。</zh-CN>
            //   <en>The context is the prerequisite for all subsequent idempotent writes; a null reference fails fast instead of entering a silent degraded branch.</en>
            // </lang>
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public bool TryProvision(ModuleProvisionRequest request, out int moduleId)
        {
            // <lang>
            //   <zh-CN>调用方契约校验：包标识、控件路径、实例标题与页签名称为最小必需项；缺一则视为非法请求且不触碰数据库。</zh-CN>
            //   <en>Caller-contract validation: package id, control path, instance title and tab name are the minimal required fields; any missing field is an illegal request that never touches the database.</en>
            // </lang>
            moduleId = 0;
            if (request == null
                || string.IsNullOrWhiteSpace(request.PackageId)
                || string.IsNullOrWhiteSpace(request.DesktopSourceFile)
                || string.IsNullOrWhiteSpace(request.ModuleTitle)
                || string.IsNullOrWhiteSpace(request.TabName))
            {
                return false;
            }

            try
            {
                // <lang>
                //   <zh-CN>1) 页签：不存在才插入，PortalId 固定写 1（与种子一致；EF 未映射该列，故用原始 SQL）。</zh-CN>
                //   <en>1) Tab: insert only when missing; PortalId is fixed to 1 (matching the seed; raw SQL avoids the unmapped EF column).</en>
                // </lang>
                context.Database.ExecuteSqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_Tabs] WHERE [TabName]=@p0) " +
                    "INSERT INTO [dbo].[PortalCfg_Tabs]([TabName],[TabOrder],[AccessRoles],[ShowMobile],[MobileTabName],[PortalId]) " +
                    "VALUES(@p0,@p1,@p2,@p3,@p0,1);",
                    request.TabName,
                    request.TabOrder,
                    request.TabAccessRoles ?? "All Users;",
                    request.ShowMobile ? 1 : 0);

                // <lang>
                //   <zh-CN>2) 模块定义：按 DesktopSourceFile 去重，避免重复注册同一控件路径。</zh-CN>
                //   <en>2) Module definition: deduplicate by DesktopSourceFile to avoid double-registering the same control path.</en>
                // </lang>
                context.Database.ExecuteSqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [DesktopSourceFile]=@p0) " +
                    "INSERT INTO [dbo].[PortalCfg_ModuleDefinitions]([FriendlyName],[DesktopSourceFile],[MobileSourceFile]) " +
                    "VALUES(@p1,@p0,N'');",
                    request.DesktopSourceFile,
                    request.FriendlyName);

                // <lang>
                //   <zh-CN>3) 模块实例：按「定义 + 页签」去重；字段全部来自请求，缺省项回退到安全值（ContentPane / Admins;）。</zh-CN>
                //   <en>3) Module instance: deduplicate by definition + tab; fields come from the request, with safe fallbacks (ContentPane / Admins;) for blanks.</en>
                // </lang>
                context.Database.ExecuteSqlCommand(
                    "DECLARE @DefId int = (SELECT [ModuleDefId] FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [DesktopSourceFile]=@p0); " +
                    "DECLARE @TabId int = (SELECT [TabId] FROM [dbo].[PortalCfg_Tabs] WHERE [TabName]=@p1); " +
                    "IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_Modules] WHERE [ModuleDefId]=@DefId AND [TabId]=@TabId) " +
                    "INSERT INTO [dbo].[PortalCfg_Modules]([ModuleDefId],[ModuleTitle],[ModuleTitleEn],[PaneName],[EditRoles],[CacheTimeout],[ShowMobile],[ModuleOrder],[TabId]) " +
                    "VALUES(@DefId,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@TabId);",
                    request.DesktopSourceFile,
                    request.TabName,
                    request.ModuleTitle,
                    request.ModuleTitleEn ?? request.ModuleTitle,
                    request.PaneName ?? "ContentPane",
                    request.EditRoles ?? "Admins;",
                    request.CacheTimeout,
                    request.ShowMobile ? 1 : 0,
                    request.ModuleOrder);

                // <lang>
                //   <zh-CN>4) 包状态：确保 HIA.FormBuilder 在状态表中为启用；缺行则插入，已有则更新为启用。</zh-CN>
                //   <en>4) Package state: ensure the package is enabled in the state table; insert when missing, otherwise update to enabled.</en>
                // </lang>
                context.Database.ExecuteSqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_ModulePackageStates] WHERE [PackageId]=@p0) " +
                    "INSERT INTO [dbo].[PortalCfg_ModulePackageStates]([PackageId],[IsEnabled],[Note],[UpdatedBy],[UpdatedUtc]) " +
                    "VALUES(@p0,1,N'Enabled by module provisioner (W-anp-P285).',N'Provisioner',SYSUTCDATETIME()); " +
                    "ELSE UPDATE [dbo].[PortalCfg_ModulePackageStates] SET [IsEnabled]=1,[UpdatedBy]=N'Provisioner',[UpdatedUtc]=SYSUTCDATETIME() WHERE [PackageId]=@p0;",
                    request.PackageId);

                // <lang>
                //   <zh-CN>回读刚确保的模块实例主键，供调用方引用（如审计、导航）。</zh-CN>
                //   <en>Read back the just-ensured module-instance primary key for caller reference (audit, navigation).</en>
                // </lang>
                var id = context.Database.SqlQuery<int?>(
                    "DECLARE @DefId int = (SELECT [ModuleDefId] FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [DesktopSourceFile]=@p0); " +
                    "DECLARE @TabId int = (SELECT [TabId] FROM [dbo].[PortalCfg_Tabs] WHERE [TabName]=@p1); " +
                    "SELECT [ModuleId] FROM [dbo].[PortalCfg_Modules] WHERE [ModuleDefId]=@DefId AND [TabId]=@TabId;",
                    request.DesktopSourceFile,
                    request.TabName).FirstOrDefault();
                moduleId = id ?? 0;
                return moduleId > 0;
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>安装失败（约束冲突、连接异常等）一律返回 false，由调用方决定重试或上报，不在本层吞掉语义。</zh-CN>
                //   <en>Any install failure (constraint conflict, connection error, etc.) returns false; the caller decides retry or escalation rather than this layer swallowing the semantics.</en>
                // </lang>
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeprovision(int moduleId, string packageId)
        {
            // <lang>
            //   <zh-CN>非法主键直接视为无需处理，返回 true（已清理态）。</zh-CN>
            //   <en>An illegal primary key is treated as already cleaned; returns true.</en>
            // </lang>
            if (moduleId <= 0)
            {
                return true;
            }

            try
            {
                // <lang>
                //   <zh-CN>删除模块实例；若其定义不再被任何实例引用，回收定义，避免孤儿类型定义残留。</zh-CN>
                //   <en>Remove the module instance; when its definition is no longer referenced by any instance, reclaim the definition to avoid an orphaned type definition.</en>
                // </lang>
                context.Database.ExecuteSqlCommand(
                    "DECLARE @DefId int = (SELECT [ModuleDefId] FROM [dbo].[PortalCfg_Modules] WHERE [ModuleId]=@p0); " +
                    "DELETE FROM [dbo].[PortalCfg_Modules] WHERE [ModuleId]=@p0; " +
                    "IF @DefId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_Modules] WHERE [ModuleDefId]=@DefId) " +
                    "DELETE FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [ModuleDefId]=@DefId;",
                    moduleId);

                // <lang>
                //   <zh-CN>清理包状态；包标识为空时跳过（卸载物理文件与部署 Profile 允许集回滚属部署动作，不在此层）。</zh-CN>
                //   <en>Clear the package state; skip when the package id is blank (removing physical files and reverting the deployment profile allow-list are deployment actions outside this layer).</en>
                // </lang>
                if (!string.IsNullOrWhiteSpace(packageId))
                {
                    context.Database.ExecuteSqlCommand(
                        "DELETE FROM [dbo].[PortalCfg_ModulePackageStates] WHERE [PackageId]=@p0;",
                        packageId);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
