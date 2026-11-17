using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>同形模块首证（独立模块 MVP）：以最小用户控件证明「同形态模块包可发现、可挂载、可运行」，
    ///   不读写业务数据、不提供上传/脚本/远程资源/动态编译能力、不声明权限键或角色。</zh-CN>
    ///   <en>Same-form first-proof independent module (MVP): a minimal user control proving that a same-form package
    ///   can be discovered, mounted and run. It reads/writes no business data and offers no upload/script/remote/dynamic-compile
    ///   capability, nor does it declare permission keys or roles.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>刻意保持 MVP：只渲染静态标记，无表单设计器、无数据访问。能力归属复用已登记词表中的
    ///   `BasicBusiness.FormBuilder`（role=Contributor，见 module.json），不新创能力条目（A 级硬边界③）。</zh-CN>
    ///   <en>Kept deliberately minimal: static markup only, no form designer and no data access. Capability reuses the
    ///   registered `BasicBusiness.FormBuilder` (role=Contributor in module.json); no capability is self-created.</en>
    /// </lang>
    /// </remarks>
    public partial class ModuleHostProof : PortalModuleControl<ModuleHostProof>
    {
        /// <summary>
        /// <lang><zh-CN>首次加载无需初始化（静态 MVP）；保留入口以供后续包扩展，不引入任何副作用。</zh-CN><en>No initialization needed on first load (static MVP); the hook is reserved for later packages with no side effects.</en></lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
