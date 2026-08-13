using System;
using System.IO;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>按模块实例和调用方提供的受控运行身份缓存模块内部 HTML 输出的 Web Forms 容器。</zh-CN>
    ///   <en>Web Forms container that caches inner module HTML output by module instance and caller-supplied controlled runtime identity.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>调用方必须在生命周期使用前绑定模块配置、Portal、受控桌面入口和缓存身份。正缓存时间会复用内部输出，受信任包版本与状态修订可由 <see cref="CacheIdentity"/> 隔离；缓存键只区分模块实例和当前编辑角色判断，不按具体用户隔离。该容器不实施 Tab/模块授权，也不编码模块生成的 HTML，因此模块不得把仅对单一用户可见的敏感内容放入共享缓存输出。</zh-CN>
    ///   <en>The caller must bind module configuration, Portal, controlled desktop entry, and cache identity before lifecycle use. A positive cache duration reuses inner output, while <see cref="CacheIdentity"/> can isolate trusted-package version and state revision; the key distinguishes only the module instance and current edit-role check, not individual users. This container neither enforces Tab/module authorization nor encodes module-generated HTML, so modules must not place user-private sensitive content in shared cached output.</en>
    /// </lang>
    /// </remarks>

    public class CachedPortalModuleControl : Control
    {
        // <lang>
        //   <zh-CN>内部状态分别保存当前请求解析到的缓存输出和调用方绑定的模块配置；空字符串、null 与非空输出沿用既有生命周期语义，不在字段层验证或授权。</zh-CN>
        //   <en>Internal state retains the cache output resolved for the current request and the caller-bound module configuration; empty, null, and nonempty output preserve existing lifecycle semantics, with no validation or authorization at the field layer.</en>
        // </lang>

        private string _cachedOutput = ""; // 缓存的输出
        private ModuleSettings _moduleConfiguration; // 模块配置


        // <lang>
        //   <zh-CN>以下属性是页面装配与缓存容器之间的运行时绑定契约；赋值不复制、持久化、重新解析或授权其内容。</zh-CN>
        //   <en>The following properties form the runtime binding contract between page assembly and this cache container; assignment does not copy, persist, re-resolve, or authorize their contents.</en>
        // </lang>

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取或设置当前模块的运行时设置快照。</zh-CN>
        ///   <en>Gets or sets the runtime settings snapshot for the current module.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方必须在读取 <see cref="ModuleId"/>、计算 <see cref="CacheKey"/>、构造子控件或渲染前提供非 null 值。setter 保留对象引用，不验证缓存时间、角色、入口或模块授权。</zh-CN>
        ///   <en>The caller must provide a non-null value before reading <see cref="ModuleId"/>, computing <see cref="CacheKey"/>, constructing child controls, or rendering. The setter retains the object reference and validates neither cache duration, roles, entry, nor module authorization.</en>
        /// </lang>
        /// </remarks>
        public ModuleSettings ModuleConfiguration
        {
            get { return _moduleConfiguration; }
            set { _moduleConfiguration = value; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从已绑定模块配置获取模块实例标识。</zh-CN>
        ///   <en>Gets the module-instance identifier from the bound module configuration.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该值直接投影 <see cref="ModuleConfiguration"/>，既不生成新标识，也不证明模块属于当前 Portal 或当前用户已获授权。</zh-CN>
        ///   <en>The value directly projects <see cref="ModuleConfiguration"/>; it neither creates a new identifier nor proves that the module belongs to the current Portal or is authorized for the current user.</en>
        /// </lang>
        /// </remarks>
        /// <exception cref="NullReferenceException"><l zh-CN="尚未绑定 ModuleConfiguration 时访问该属性。" en="The property is accessed before ModuleConfiguration is bound." /></exception>
        public int ModuleId
        {
            get { return _moduleConfiguration.ModuleId; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取或设置随后传递给动态模块控件的当前 Portal 标识。</zh-CN>
        ///   <en>Gets or sets the current Portal identifier subsequently passed to the dynamic module control.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该属性只保存调用方上下文，不验证 Portal 存在性或成员关系，也不单独加入缓存键。</zh-CN>
        ///   <en>This property only retains caller context; it validates neither Portal existence nor membership and is not independently added to the cache key.</en>
        /// </lang>
        /// </remarks>
        public int PortalId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取或设置已由模块目录解析的桌面控件入口。</zh-CN>
        ///   <en>Gets or sets the desktop-control entry already resolved by the module catalog.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>非空值必须来自 <see cref="PortalModuleCatalog.TryResolveModule"/> 的运行描述符；本容器不会再次验证 manifest、Profile、包状态或路径。空值仅触发 <see cref="CreateChildControls"/> 中既有的旧模块定义路径规范化兼容。</zh-CN>
        ///   <en>A nonempty value must come from the runtime descriptor produced by <see cref="PortalModuleCatalog.TryResolveModule"/>; this container does not revalidate manifest, Profile, package state, or path. An empty value only triggers the existing legacy module-definition path normalization in <see cref="CreateChildControls"/>.</en>
        /// </lang>
        /// </remarks>
        public string DesktopSource { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取或设置由已验证包版本、状态修订或旧路径构成的缓存隔离身份。</zh-CN>
        ///   <en>Gets or sets the cache-isolation identity composed from validated package version, state revision, or a legacy path.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方应从受控运行描述符提供该值；本容器只把它作为不透明键片段，不解析或验证其内容。null 按空字符串处理以保持历史缓存键行为；该身份不是安全令牌或用户身份。</zh-CN>
        ///   <en>The caller should supply this value from a controlled runtime descriptor; the container uses it only as an opaque key segment and neither parses nor validates its content. Null is treated as an empty string to preserve historical cache-key behavior; the identity is neither a security token nor user identity.</en>
        /// </lang>
        /// </remarks>
        public string CacheIdentity { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取用于 ASP.NET 缓存读取和写入的模块输出键。</zh-CN>
        ///   <en>Gets the module-output key used for ASP.NET cache reads and writes.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>键按既有顺序组合容器类型、<see cref="ModuleId"/>、当前用户是否属于已配置编辑角色的布尔结果和 <see cref="CacheIdentity"/>。它不包含用户名或完整角色集合，不能用作授权结论，也不适合缓存用户私有输出。</zh-CN>
        ///   <en>The key combines, in the existing order, container type, <see cref="ModuleId"/>, the Boolean result of whether the current user is in configured edit roles, and <see cref="CacheIdentity"/>. It contains neither user name nor the complete role set, cannot serve as an authorization decision, and is unsuitable for user-private output.</en>
        /// </lang>
        /// </remarks>
        /// <exception cref="NullReferenceException"><l zh-CN="计算键前尚未绑定 ModuleConfiguration。" en="ModuleConfiguration has not been bound before key computation." /></exception>
        public string CacheKey
        {
            get
            {
                // <lang>
                //   <zh-CN>保留历史键文本和拼接顺序，使读取与写入继续命中同一项；角色 helper 只贡献编辑角色布尔分区，不扩大为用户身份或授权缓存。</zh-CN>
                //   <en>Preserve historical key text and concatenation order so reads and writes continue targeting the same item; the role helper contributes only an edit-role Boolean partition and is not expanded into user identity or cached authorization.</en>
                // </lang>
                return "Key:" + GetType().FullName + ModuleId +
                       PortalSecurity.IsInRoles(_moduleConfiguration.AuthorizedEditRoles) + "|" +
                       (CacheIdentity ?? string.Empty);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在缓存未命中时构造模块子控件，在命中时复用已缓存输出。</zh-CN>
        ///   <en>Constructs the module child control on a cache miss and reuses cached output on a hit.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方应先提供受控桌面入口、缓存身份、Portal 和模块配置。命中缓存时本方法不构造子控件；未命中时仅加载该入口并执行接口/配置注入。加载失败会记录诊断并留下空模块区域，避免单个模块阻断门户页；它不改写模块定义、包状态、缓存键或过期策略。</zh-CN>
        ///   <en>The caller must first provide a controlled desktop entry, cache identity, Portal, and module configuration. On a cache hit this method does not construct child controls; on a miss it loads only that entry and performs interface/configuration injection. A load failure records diagnostics and leaves an empty module region so one module cannot block the portal page; it does not rewrite module definitions, package state, cache keys, or expiry policy.</en>
        /// </lang>
        /// </remarks>
        protected override void CreateChildControls()
        {
            // <lang>
            //   <zh-CN>仅当模块配置启用缓存时读取 ASP.NET 缓存；零缓存时间保留 _cachedOutput 的未命中状态，随后按既有直接子控件构造路径继续。</zh-CN>
            //   <en>Read ASP.NET cache only when module configuration enables caching; a zero cache time retains the _cachedOutput miss state and subsequently follows the existing direct child-control construction path.</en>
            // </lang>
            if (_moduleConfiguration.CacheTime > 0)
            {
                // <lang>
                //   <zh-CN>缓存输出只按既有 CacheKey 读取；本方法不在读取时重算入口信任、角色授权或用户身份。</zh-CN>
                //   <en>Read cached output only by the existing CacheKey; this method does not recalculate entry trust, role authorization, or user identity during the read.</en>
                // </lang>
                _cachedOutput = (string)Context.Cache[CacheKey];
            }

            // <lang>
            //   <zh-CN>只有缓存未命中才构造子控件；命中时保留已缓存输出并避免重复加载或执行模块生命周期。</zh-CN>
            //   <en>Construct child controls only on a cache miss; on a hit retain cached output and avoid repeated loading or module lifecycle execution.</en>
            // </lang>
            if (_cachedOutput == null)
            {
                // <lang>
                //   <zh-CN>先让 Web Forms 建立本容器的基础子控件状态，再添加动态模块；不改变父类生命周期顺序。</zh-CN>
                //   <en>Let Web Forms establish this container's base child-control state before adding the dynamic module; do not change base lifecycle order.</en>
                // </lang>
                base.CreateChildControls();

                // <lang>
                //   <zh-CN>动态加载、接口检查与配置注入属于单模块失败边界；任何异常都会在下方诊断并保持空输出。</zh-CN>
                //   <en>Dynamic loading, interface checking, and configuration injection belong to the single-module failure boundary; any exception is diagnosed below while output remains empty.</en>
                // </lang>
                try
                {
                    // <lang>
                    //   <zh-CN>优先使用调用方从运行描述符传入的受控入口；空值时仅为历史兼容规范化模块定义中的旧路径，不把任意路径当作可加载来源。</zh-CN>
                    //   <en>Prefer the controlled entry supplied by the caller from a runtime descriptor; when it is empty, normalize only the legacy path in module definition for compatibility and do not treat arbitrary paths as loadable sources.</en>
                    // </lang>
                    string desktopSource = string.IsNullOrWhiteSpace(DesktopSource)
                        ? PortalModulePathValidator.NormalizeDesktopSourceOrThrow(_moduleConfiguration.DesktopSrc)
                        : DesktopSource;

                    // <lang>
                    //   <zh-CN>加载选定入口并要求实现门户模块接口，防止普通 Web Forms 控件获得 Portal/模块配置；本层不再次验证 manifest、Profile 或状态。</zh-CN>
                    //   <en>Load the selected entry and require the portal-module interface, preventing ordinary Web Forms controls from receiving Portal/module configuration; this layer does not revalidate manifest, Profile, or state.</en>
                    // </lang>
                    var module = Page.LoadControl(desktopSource) as IPortalModuleControl;
                    if (module == null)
                    {
                        throw new InvalidOperationException(
                            "The module control does not implement IPortalModuleControl.");
                    }

                    // <lang>
                    //   <zh-CN>在加入容器前传递调用方已绑定的模块设置和 Portal，使缓存未命中路径与未缓存路径看到同一运行时上下文；这不是持久化或授权动作。</zh-CN>
                    //   <en>Pass the caller-bound module settings and Portal before joining the container so the cache-miss path sees the same runtime context as the uncached path; this is neither persistence nor authorization.</en>
                    // </lang>
                    module.ModuleConfiguration = ModuleConfiguration;
                    module.PortalId = PortalId;

                    // <lang>
                    //   <zh-CN>仅将通过接口和配置注入的用户控件加入当前缓存容器；缓存 render 阶段是否保存其输出由独立方法决定。</zh-CN>
                    //   <en>Add only the user control that passed interface and configuration injection to the current cache container; whether the render stage stores its output is decided by a separate method.</en>
                    // </lang>
                    Controls.Add((UserControl)module);
                }
                catch (Exception exception)
                {
                    // <lang>
                    //   <zh-CN>缓存分支的加载失败不能中止整个门户页；诊断保留在服务器侧，并把当前输出设为空字符串以避免残留错误或部分控件树。</zh-CN>
                    //   <en>A load failure in the cache branch must not abort the entire portal page; retain diagnostics server-side and set current output to an empty string to avoid stale errors or a partial control tree.</en>
                    // </lang>
                    PortalDiagnostics.Error(
                        "ModulePackage.CachedLoad",
                        "Loading a cached portal module failed. ModuleId=" + ModuleId,
                        exception,
                        Context);
                    _cachedOutput = string.Empty;
                }
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把模块输出写入当前响应，并在启用缓存时创建或复用 ASP.NET 缓存项。</zh-CN>
        ///   <en>Writes module output to the current response and creates or reuses an ASP.NET cache entry when caching is enabled.</en>
        /// </lang>
        /// </summary>
        /// <param name="output"><l zh-CN="接收模块 HTML 的当前响应写入器；本方法不拥有或释放它。" en="Current response writer receiving module HTML; this method neither owns nor disposes it." /></param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>零缓存时间直接委托基类渲染。正缓存时间在未命中时先把子控件树渲染到内存字符串，再以本地 <see cref="DateTime.Now"/> 加配置秒数作为绝对过期时间写入，无缓存依赖且无滑动过期；命中时直接复用字符串。最终使用 <see cref="HtmlTextWriter.Write(string)"/> 输出已渲染 HTML，不在本层重新编码、授权或净化模块内容。</zh-CN>
        ///   <en>A zero cache duration delegates directly to base rendering. On a positive-duration miss, the child-control tree is first rendered to an in-memory string and inserted with local <see cref="DateTime.Now"/> plus configured seconds as absolute expiration, with no cache dependency and no sliding expiration; a hit reuses the string. The final <see cref="HtmlTextWriter.Write(string)"/> emits already-rendered HTML without re-encoding, authorizing, or sanitizing module content at this layer.</en>
        /// </lang>
        /// </remarks>
        protected override void Render(HtmlTextWriter output)
        {
            // <lang>
            //   <zh-CN>缓存时间为零时保持标准 Web Forms 渲染和生命周期，立即返回以避免缓冲、CacheKey 计算或缓存写入。</zh-CN>
            //   <en>When cache duration is zero, preserve standard Web Forms rendering and lifecycle, then return immediately to avoid buffering, CacheKey computation, or cache insertion.</en>
            // </lang>
            if (_moduleConfiguration.CacheTime == 0)
            {
                base.Render(output);
                return;
            }

            // <lang>
            //   <zh-CN>只有 CreateChildControls 留下 null（正缓存时间且未命中）时才缓冲本次子控件输出；已有字符串包括空字符串均按既有结果复用。</zh-CN>
            //   <en>Buffer the current child-control output only when CreateChildControls left null for a positive-duration miss; any existing string, including an empty string, is reused as the existing result.</en>
            // </lang>
            if (_cachedOutput == null)
            {
                // <lang>
                //   <zh-CN>临时 writer 只拥有本次内存缓冲；基类仍负责实际子控件渲染，using 在取出字符串后释放 writer，不接管响应 writer。</zh-CN>
                //   <en>The temporary writer owns only this request's in-memory buffer; the base class still renders the actual child controls, and using disposes the buffer writer after string extraction without taking ownership of the response writer.</en>
                // </lang>
                using (TextWriter tempWriter = new StringWriter())
                {
                    base.Render(new HtmlTextWriter(tempWriter));
                    _cachedOutput = tempWriter.ToString();

                    // <lang>
                    //   <zh-CN>按同一 CacheKey 写入无依赖、绝对过期且无滑动续期的 ASP.NET 缓存项；保留历史本地时钟与配置秒数，不在渲染期另设失效或分布式一致性机制。</zh-CN>
                    //   <en>Insert under the same CacheKey with no dependency, absolute expiration, and no sliding renewal; preserve the historical local clock and configured seconds without adding render-time invalidation or distributed-consistency behavior.</en>
                    // </lang>
                    Context.Cache.Insert(CacheKey, _cachedOutput, null,
                        DateTime.Now.AddSeconds(_moduleConfiguration.CacheTime), TimeSpan.Zero);
                }
            }

            // <lang>
            //   <zh-CN>把新渲染或缓存复用的 HTML 原样写入当前响应；本容器不再次编码，因此动态数据编码和用户级保密仍由模块实现及其授权边界负责。</zh-CN>
            //   <en>Write newly rendered or cached HTML verbatim to the current response; this container does not encode it again, so dynamic-data encoding and user-level confidentiality remain responsibilities of the module implementation and its authorization boundary.</en>
            // </lang>
            output.Write(_cachedOutput);
        }
    }
}
