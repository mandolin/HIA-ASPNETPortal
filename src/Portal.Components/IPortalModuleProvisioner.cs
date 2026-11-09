using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>独立模块安装/卸载请求的可选参数载体；所有字段为空字符串时由提供方使用安全默认值。</zh-CN>
    ///   <en>Optional parameter carrier for independent-module provisioning/deprovisioning; blank fields let the provider apply safe defaults.</en>
    /// </lang>
    /// </summary>
    public sealed class ModuleProvisionRequest
    {
        /// <summary><lang><zh-CN>稳定包标识，须与 manifest 的 packageId 一致（如 HIA.FormBuilder）。</zh-CN><en>Stable package identifier, matching the manifest packageId (e.g. HIA.FormBuilder).</en></lang></summary>
        public string PackageId { get; set; }

        /// <summary><lang><zh-CN>关联的能力标识（如 BasicBusiness.FormBuilder）；安装期据此在关闭的能力词表中校验，未登记或属管理级则拒绝（W-anp-P286 Admins 边界）。</zh-CN><en>Associated capability id (e.g. BasicBusiness.FormBuilder); used at install time to validate against the closed capability registry, rejecting unknown or administrative capabilities (W-anp-P286 Admins boundary).</en></lang></summary>
        public string CapabilityId { get; set; }

        /// <summary><lang><zh-CN>模块类型后台显示名称。</zh-CN><en>Administration display name for the module type.</en></lang></summary>
        public string FriendlyName { get; set; }

        /// <summary><lang><zh-CN>桌面端控件相对路径，必须落在受信任部署边界内（如 DesktopModules/FormBuilder/FormBuilder.ascx）。</zh-CN><en>Desktop control relative path, inside the trusted deployment boundary (e.g. DesktopModules/FormBuilder/FormBuilder.ascx).</en></lang></summary>
        public string DesktopSourceFile { get; set; }

        /// <summary><lang><zh-CN>模块实例标题（中文列兼默认回退）。</zh-CN><en>Module instance title (Chinese column and default fallback).</en></lang></summary>
        public string ModuleTitle { get; set; }

        /// <summary><lang><zh-CN>模块实例英文标题；为空回退到 ModuleTitle。</zh-CN><en>English module instance title; falls back to ModuleTitle when blank.</en></lang></summary>
        public string ModuleTitleEn { get; set; }

        /// <summary><lang><zh-CN>布局 Pane 名称（如 ContentPane/LeftPane/RightPane）。</zh-CN><en>Layout pane name (e.g. ContentPane/LeftPane/RightPane).</en></lang></summary>
        public string PaneName { get; set; }

        /// <summary><lang><zh-CN>分号分隔的编辑角色字符串。</zh-CN><en>Semicolon-delimited edit-role string.</en></lang></summary>
        public string EditRoles { get; set; }

        /// <summary><lang><zh-CN>承载模块实例的页签名称；不存在时按请求创建。</zh-CN><en>Tab that hosts the module instance; created from the request when missing.</en></lang></summary>
        public string TabName { get; set; }

        /// <summary><lang><zh-CN>页签访问角色字符串；为空默认 All Users;。</zh-CN><en>Tab access-role string; defaults to All Users; when blank.</en></lang></summary>
        public string TabAccessRoles { get; set; }

        /// <summary><lang><zh-CN>页签排序值。</zh-CN><en>Tab sort order.</en></lang></summary>
        public int TabOrder { get; set; }

        /// <summary><lang><zh-CN>输出缓存秒数。</zh-CN><en>Output cache duration in seconds.</en></lang></summary>
        public int CacheTimeout { get; set; }

        /// <summary><lang><zh-CN>是否在移动端显示。</zh-CN><en>Whether the module is visible on mobile.</en></lang></summary>
        public bool ShowMobile { get; set; }

        /// <summary><lang><zh-CN>同 Pane 内显示顺序。</zh-CN><en>Display order within the pane.</en></lang></summary>
        public int ModuleOrder { get; set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>独立模块安装/卸载的可复用机制契约。安装把模块定义、页签/模块实例与包状态写入门户结构配置库，使容纳机制能实际装载运行；卸载做反向清理且不残留。</zh-CN>
    ///   <en>Reusable contract for installing/uninstalling independent modules. Install writes the module definition, tab/module instance and package state into the portal structural configuration so the hosting mechanism can actually load and run the module; uninstall reverses the cleanup without leaving residue.</en>
    /// </lang>
    /// </summary>
    public interface IPortalModuleProvisioner
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>幂等安装：已存在的页签/定义/实例/包状态不会重复插入。成功返回 true 并通过 out 参数给出模块实例主键。</zh-CN>
        ///   <en>Idempotent install: an existing tab/definition/instance/package-state is not inserted again. Returns true on success and yields the module-instance primary key via the out parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="request"><l zh-CN="安装请求参数。" en="Provisioning request parameters." /></param>
        /// <param name="moduleId"><l zh-CN="成功时返回模块实例主键，否则为 0。" en="Module-instance primary key on success, otherwise 0." /></param>
        bool TryProvision(ModuleProvisionRequest request, out int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>卸载：删除模块实例，并在其定义不再被任何实例引用时回收定义；同时清理包状态。返回是否已完成（实例不存在也视为已清理）。</zh-CN>
        ///   <en>Uninstall: removes the module instance, and reclaims the definition when no instance references it; also clears the package state. Returns whether it completed (a missing instance is treated as already cleaned).</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId"><l zh-CN="要卸载的模块实例主键。" en="Primary key of the module instance to uninstall." /></param>
        /// <param name="packageId"><l zh-CN="关联的包标识，用于清理包状态；为空则跳过状态清理。" en="Associated package identifier used to clear package state; skipped when blank." /></param>
        bool TryDeprovision(int moduleId, string packageId);
    }
}
