using System;
using System.Collections;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>为动态加载的桌面模块提供 Portal/模块上下文、编辑入口提示、设置懒加载和当前请求依赖注入的基类。</zh-CN>
    ///   <en>Base class providing Portal/module context, edit-entry hints, lazy settings loading, and current-request dependency injection to dynamically loaded desktop modules.</en>
    /// </lang>
    /// </summary>
    /// <typeparam name="T"><l zh-CN="由当前请求 Unity 容器用于成员注入的具体模块类型；派生类应传入自身类型。" en="Concrete module type used by the current-request Unity container for member injection; a derived class should supply its own type." /></typeparam>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>动态加载器必须在模块使用上下文属性前设置 <see cref="ModuleConfiguration"/> 和 <see cref="PortalId"/>。派生控件在自己的初始化代码读取注入依赖前必须先调用 base.OnInit；本基类在该调用中先保留 Web Forms 基类顺序，再使用当前请求容器注入成员。<see cref="IsEditable"/> 仅是实例内 UI 提示，所有实际读写仍需独立服务器端授权。</zh-CN>
    ///   <en>The dynamic loader must set <see cref="ModuleConfiguration"/> and <see cref="PortalId"/> before the module uses context properties. A derived control must call base.OnInit before reading injected dependencies in its own initialization code; this base class first preserves Web Forms base ordering and then injects members from the current-request container. <see cref="IsEditable"/> is only a per-instance UI hint, and every actual read or write still requires independent server-side authorization.</en>
    /// </lang>
    /// </remarks>
    public class PortalModuleControl<T> : UserControl, IPortalModuleControl where T : class
    {
        // <lang>
        //   <zh-CN>实例字段只在当前 Web Forms 控件生命周期内保存派生状态；它们不是跨请求授权、配置刷新或数据缓存。</zh-CN>
        //   <en>Instance fields retain derived state only for the current Web Forms control lifecycle; they are not cross-request authorization, configuration refresh, or data caches.</en>
        // </lang>

        // <lang>
        //   <zh-CN>三态编辑提示：0 表示尚未计算，1 表示显示，2 表示隐藏；一旦计算便在本控件实例内复用。</zh-CN>
        //   <en>Tri-state edit hint: 0 means not computed, 1 means display, and 2 means hide; once computed it is reused within this control instance.</en>
        // </lang>
        private int _isEditable;

        // <lang>
        //   <zh-CN>保存动态加载器绑定的模块设置对象引用；setter 不复制、校验或持久化该对象。</zh-CN>
        //   <en>Retains the module-settings object reference bound by the dynamic loader; the setter neither copies, validates, nor persists it.</en>
        // </lang>
        private ModuleSettings _moduleConfiguration;

        // <lang>
        //   <zh-CN>保存首次成功读取的旧式键值设置；非 null 时同一控件实例后续访问复用该可变 Hashtable。</zh-CN>
        //   <en>Retains the first successfully loaded legacy key-value settings; when non-null, subsequent access in the same control instance reuses the mutable Hashtable.</en>
        // </lang>
        private Hashtable _settings;

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取由当前请求 Unity 容器注入的模块设置读取服务。</zh-CN>
        ///   <en>Gets the module-settings read service injected by the current-request Unity container.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>动态控件不由常规页面容器构造，因此 <see cref="OnInit"/> 显式请求成员注入。派生类只有在调用 base.OnInit 后才能依赖该属性；服务只负责读取设置，不证明模块或用户授权。</zh-CN>
        ///   <en>Dynamic controls are not constructed by the normal page container, so <see cref="OnInit"/> explicitly requests member injection. A derived class may depend on this property only after calling base.OnInit; the service only reads settings and proves neither module nor user authorization.</en>
        /// </lang>
        /// </remarks>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; } // 

        #region IPortalModuleControl 成员

        /// <summary>
        /// <lang>
        ///   <zh-CN>从已绑定模块配置获取模块实例标识。</zh-CN>
        ///   <en>Gets the module-instance identifier from the bound module configuration.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该值只投影 <see cref="ModuleConfiguration"/>，不生成标识、不验证 Portal 归属，也不代表用户授权。</zh-CN>
        ///   <en>The value only projects <see cref="ModuleConfiguration"/>; it neither generates an identifier, validates Portal ownership, nor represents user authorization.</en>
        /// </lang>
        /// </remarks>
        /// <exception cref="NullReferenceException"><l zh-CN="动态加载器尚未绑定 ModuleConfiguration。" en="The dynamic loader has not yet bound ModuleConfiguration." /></exception>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // 设置属性不可浏览且不在设计器序列化中可见
        public int ModuleId
        {
            // <lang>
            //   <zh-CN>直接返回调用方设置快照中的稳定实例标识，保持接口与数据读取使用同一来源。</zh-CN>
            //   <en>Return the stable instance identifier directly from the caller-bound settings snapshot so the interface and data reads use the same source.</en>
            // </lang>
            get { return _moduleConfiguration.ModuleId; } 
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取或设置动态加载器绑定的当前 Portal 标识。</zh-CN>
        ///   <en>Gets or sets the current Portal identifier bound by the dynamic loader.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该属性仅传递运行上下文，不验证 Portal 存在性、模块归属、成员关系或权限。</zh-CN>
        ///   <en>This property only conveys runtime context and validates neither Portal existence, module ownership, membership, nor permission.</en>
        /// </lang>
        /// </remarks>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int PortalId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取当前控件实例是否显示编辑入口的 UI 提示。</zh-CN>
        ///   <en>Gets the UI hint indicating whether this control instance displays an edit entry point.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>首次读取将全局 AlwaysShowEditButton 展示选项与当前请求对 <see cref="ModuleSettings.AuthorizedEditRoles"/> 的角色布尔判断合并，并把结果以 1/2 缓存在本控件实例。后续读取不重新获取 Portal 设置、角色或模块配置；该提示不能授权保存、删除、管理或数据读取。</zh-CN>
        ///   <en>The first read combines the global AlwaysShowEditButton display option with the current request's Boolean role check against <see cref="ModuleSettings.AuthorizedEditRoles"/>, then caches the result as 1 or 2 in this control instance. Subsequent reads do not re-fetch Portal settings, roles, or module configuration; the hint cannot authorize save, delete, administration, or data reads.</en>
        /// </lang>
        /// </remarks>
        /// <exception cref="NullReferenceException"><l zh-CN="角色分支需要模块配置但动态加载器尚未绑定 ModuleConfiguration。" en="The role branch requires module configuration before the dynamic loader has bound ModuleConfiguration." /></exception>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsEditable
        {
            get
            {
                // <lang>
                //   <zh-CN>只在三态值未知时计算展示提示，避免同一控件实例反复读取 Portal 设置和执行角色匹配。</zh-CN>
                //   <en>Compute the display hint only while the tri-state value is unknown, avoiding repeated Portal-settings reads and role matching within the same control instance.</en>
                // </lang>
                if (_isEditable == 0) // 如果状态未知
                {
                    // <lang>
                    //   <zh-CN>读取当前请求已建立的 Portal 设置，仅消费 AlwaysShowEditButton 展示偏好；该对象不是模块授权结果。</zh-CN>
                    //   <en>Read Portal settings already established for the current request and consume only the AlwaysShowEditButton display preference; the object is not a module-authorization result.</en>
                    // </lang>
                    var portalSettings = PortalContext.GetPortalSettings();

                    // <lang>
                    //   <zh-CN>全局强制显示时短路角色查询；否则用已绑定配置的编辑角色列表生成 UI 布尔提示，两条分支都不授权具体动作。</zh-CN>
                    //   <en>Short-circuit role lookup when global display is forced; otherwise derive a UI Boolean hint from the bound configuration's edit-role list, with neither branch authorizing a concrete action.</en>
                    // </lang>
                    if (portalSettings.AlwaysShowEditButton || // 如果总是显示编辑按钮
                        PortalSecurity.IsInRoles(_moduleConfiguration.AuthorizedEditRoles)) // 或者当前用户具有编辑权限
                    {
                        // <lang>
                        //   <zh-CN>以 1 固化本实例的“显示”提示，不写入角色、配置或用户状态。</zh-CN>
                        //   <en>Persist the per-instance “display” hint as 1 without writing role, configuration, or user state.</en>
                        // </lang>
                        _isEditable = 1; // 设置为可编辑
                    }
                    else
                    {
                        // <lang>
                        //   <zh-CN>以 2 固化本实例的“隐藏”提示，避免 0 与 false 结果混淆并重复计算。</zh-CN>
                        //   <en>Persist the per-instance “hide” hint as 2 so a false result is not confused with unknown 0 and recomputed.</en>
                        // </lang>
                        _isEditable = 2; // 设置为不可编辑
                    }
                }

                // <lang>
                //   <zh-CN>只把三态 1 投影为 true；返回值供界面使用，不携带授权证明。</zh-CN>
                //   <en>Project only tri-state 1 to true; the return value is for UI use and carries no authorization proof.</en>
                // </lang>
                return (_isEditable == 1); // 返回是否可编辑
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取或设置动态加载器绑定的当前模块运行时设置快照。</zh-CN>
        ///   <en>Gets or sets the current module runtime-settings snapshot bound by the dynamic loader.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方必须在读取 <see cref="ModuleId"/>、<see cref="IsEditable"/> 或 <see cref="Settings"/> 前提供非 null 值。setter 只替换引用，保留既有编辑提示和已加载设置状态，因此生命周期内不应在首次消费后重新绑定。</zh-CN>
        ///   <en>The caller must provide a non-null value before reading <see cref="ModuleId"/>, <see cref="IsEditable"/>, or <see cref="Settings"/>. The setter only replaces the reference and retains existing edit-hint and loaded-settings state, so rebinding should not occur after first consumption within the lifecycle.</en>
        /// </lang>
        /// </remarks>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ModuleSettings ModuleConfiguration
        {
            // <lang>
            //   <zh-CN>返回当前绑定的对象引用，不创建防御副本。</zh-CN>
            //   <en>Return the currently bound object reference without creating a defensive copy.</en>
            // </lang>
            get { return _moduleConfiguration; } // 获取模块配置
            // <lang>
            //   <zh-CN>保存调用方引用但不重置已计算的编辑提示或已加载设置，也不验证或持久化内容。</zh-CN>
            //   <en>Retain the caller reference without resetting the computed edit hint or loaded settings, and without validating or persisting content.</en>
            // </lang>
            set { _moduleConfiguration = value; } // 设置模块配置
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按需读取并在当前控件实例内缓存模块键值设置。</zh-CN>
        ///   <en>Loads module key-value settings on demand and caches them within the current control instance.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>当内部值为 null 时，使用已注入的 <see cref="ModulesConfig"/> 和已绑定 <see cref="ModuleId"/> 读取；只有非 null 结果会避免下次重读。返回的是旧式可变 <see cref="Hashtable"/> 引用，不做复制、值净化或线程安全包装，也不自动感知数据库并发更新。读取设置不等于授权使用其中的路径、HTML 或其它值。</zh-CN>
        ///   <en>When the internal value is null, the property reads through injected <see cref="ModulesConfig"/> and bound <see cref="ModuleId"/>; only a non-null result prevents another read. It returns the legacy mutable <see cref="Hashtable"/> reference without copying, value sanitization, or thread-safety wrapping and does not automatically observe concurrent database changes. Reading settings does not authorize use of paths, HTML, or other contained values.</en>
        /// </lang>
        /// </remarks>
        /// <exception cref="NullReferenceException"><l zh-CN="访问发生在 ModuleConfiguration 绑定或 OnInit 依赖注入之前。" en="Access occurs before ModuleConfiguration binding or OnInit dependency injection." /></exception>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Hashtable Settings
        {
            get
            {
                // <lang>
                //   <zh-CN>仅在尚无非 null 设置对象时访问数据服务；这是一项同步惰性读取，不提供锁或跨请求缓存。</zh-CN>
                //   <en>Access the data service only while no non-null settings object is retained; this is a synchronous lazy read with neither locking nor cross-request caching.</en>
                // </lang>
                if (_settings == null) // 如果尚未加载设置
                {
                    // <lang>
                    //   <zh-CN>以绑定配置中的模块实例标识读取旧式键值集合；服务返回 null 时字段仍为 null，后续访问会再次读取。</zh-CN>
                    //   <en>Read the legacy key-value collection by the module-instance identifier from bound configuration; if the service returns null, the field remains null and a later access reads again.</en>
                    // </lang>
                    _settings = ModulesConfig.GetModuleSettings(ModuleId); // 从数据库获取模块设置
                }

                // <lang>
                //   <zh-CN>原样返回保存的可变引用以维持旧模块兼容；调用方修改只影响该对象，是否持久化由独立数据写入 API 决定。</zh-CN>
                //   <en>Return the retained mutable reference verbatim for legacy module compatibility; caller mutation affects only this object, while persistence is decided by separate data-write APIs.</en>
                // </lang>
                return _settings; // 返回模块设置
            }
        }

        #endregion

        /// <summary>
        /// <lang>
        ///   <zh-CN>在 Web Forms 初始化阶段为动态模块执行基类初始化并补充当前请求范围的依赖。</zh-CN>
        ///   <en>Performs base initialization and supplies current-request dependencies to a dynamic module during Web Forms initialization.</en>
        /// </lang>
        /// </summary>
        /// <param name="e"><l zh-CN="Web Forms 初始化事件参数，原样传递给 UserControl 基类。" en="Web Forms initialization event arguments passed unchanged to the UserControl base class." /></param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>先调用 <see cref="UserControl.OnInit"/>，再让 <c>Global.BuildItemWithCurrentContext&lt;T&gt;</c> 使用当前请求 Unity 上下文向此实例执行成员注入。派生类应先调用 base.OnInit，再读取注入属性。本方法不创建模块包、不复验入口/Profile/状态，也不实施模块授权；依赖解析失败按既有异常行为向上传播。</zh-CN>
        ///   <en>Calls <see cref="UserControl.OnInit"/> first, then lets <c>Global.BuildItemWithCurrentContext&lt;T&gt;</c> perform member injection into this instance from the current-request Unity context. A derived class should call base.OnInit before reading injected properties. This method neither creates a module package, revalidates entry/Profile/state, nor enforces module authorization; dependency-resolution failures propagate under existing exception behavior.</en>
        /// </lang>
        /// </remarks>
        protected override void OnInit(EventArgs e) // 重写初始化事件
        {
            // <lang>
            //   <zh-CN>保留标准 UserControl 初始化顺序和事件语义，再执行门户专用成员注入。</zh-CN>
            //   <en>Preserve standard UserControl initialization order and event semantics before portal-specific member injection.</en>
            // </lang>
            base.OnInit(e); // 调用基类的初始化方法

            // <lang>
            //   <zh-CN>动态加载绕过常规容器构造，因此使用当前请求容器按具体 T 向现有控件实例注入 [Dependency] 成员；不替换控件实例。</zh-CN>
            //   <en>Dynamic loading bypasses normal container construction, so use the current-request container and concrete T to inject [Dependency] members into the existing control instance without replacing it.</en>
            // </lang>
            Global.BuildItemWithCurrentContext<T>(this); // 动态注入依赖
        }
    }
}
