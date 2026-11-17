/*
<lang>
  <zh-CN>同形模块首证（HIA.ModuleHostProof，Independent 形态）安装脚本。把模块定义、页签/模块实例与包状态写入 PortalCfg_* 表，使容纳机制能实际装载运行。
  本脚本幂等：已存在的页签/定义/实例/包状态不会重复插入。配合部署 Profile 允许集（各活动 Profile 的 Packages 显式列出 HIA.ModuleHostProof）共同生效。
  能力归属复用已登记词表 BasicBusiness.FormBuilder（role=Contributor），本脚本不写权限键、不授予角色、不新创能力条目（A 级硬边界③）。</zh-CN>
  <en>Same-form first-proof (HIA.ModuleHostProof, Independent shape) provisioning script. Writes module definition, tab/module instance and package state into PortalCfg_* tables so the hosting mechanism can load and run it.
  This script is idempotent: existing tab/definition/instance/package-state rows are not inserted twice. It takes effect with the deployment profile allow-list (every active profile's Packages explicitly lists HIA.ModuleHostProof).
  Capability reuses the registered BasicBusiness.FormBuilder (role=Contributor); this script writes no permission key, grants no role and creates no capability.</en>
</lang>
*/

-- 1) 页签：不存在才创建（PortalId 固定 1，与种子一致；TabOrder 12 接续 Form Builder 的 11）
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_Tabs] WHERE [TabName] = N'Module Host Proof')
BEGIN
    INSERT INTO [dbo].[PortalCfg_Tabs] ([TabName], [TabOrder], [AccessRoles], [ShowMobile], [MobileTabName], [PortalId])
    VALUES (N'Module Host Proof', 12, N'All Users;', 1, N'Module Host Proof', 1);
END
DECLARE @TabId int = (SELECT [TabId] FROM [dbo].[PortalCfg_Tabs] WHERE [TabName] = N'Module Host Proof');

-- 2) 模块定义：按 DesktopSourceFile 去重
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [DesktopSourceFile] = N'DesktopModules/ModuleHostProof/ModuleHostProof.ascx')
BEGIN
    INSERT INTO [dbo].[PortalCfg_ModuleDefinitions] ([FriendlyName], [DesktopSourceFile], [MobileSourceFile])
    VALUES (N'Module Host Proof', N'DesktopModules/ModuleHostProof/ModuleHostProof.ascx', N'');
END
DECLARE @DefId int = (SELECT [ModuleDefId] FROM [dbo].[PortalCfg_ModuleDefinitions] WHERE [DesktopSourceFile] = N'DesktopModules/ModuleHostProof/ModuleHostProof.ascx');

-- 3) 模块实例：按「定义 + 页签」去重；EditRoles 沿用种子模块实例编辑角色口径（非能力权限键、不授予 Admins 角色给第三方）
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_Modules] WHERE [ModuleDefId] = @DefId AND [TabId] = @TabId)
BEGIN
    INSERT INTO [dbo].[PortalCfg_Modules] ([ModuleDefId], [ModuleTitle], [ModuleTitleEn], [PaneName], [EditRoles], [CacheTimeout], [ShowMobile], [ModuleOrder], [TabId])
    VALUES (@DefId, N'同形模块首证', N'Module Host Proof', N'ContentPane', N'Admins;', 0, 1, 1, @TabId);
END

-- 4) 包状态：确保 HIA.ModuleHostProof 启用
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_ModulePackageStates] WHERE [PackageId] = N'HIA.ModuleHostProof')
BEGIN
    INSERT INTO [dbo].[PortalCfg_ModulePackageStates] ([PackageId], [IsEnabled], [Note], [UpdatedBy], [UpdatedUtc])
    VALUES (N'HIA.ModuleHostProof', 1, N'Enabled by same-form first-proof provisioning.', N'Provisioner', SYSUTCDATETIME());
END
ELSE
BEGIN
    UPDATE [dbo].[PortalCfg_ModulePackageStates] SET [IsEnabled] = 1, [UpdatedBy] = N'Provisioner', [UpdatedUtc] = SYSUTCDATETIME() WHERE [PackageId] = N'HIA.ModuleHostProof';
END
