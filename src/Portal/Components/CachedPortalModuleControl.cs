using System;
using System.IO;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 以模块实例和受控运行身份为边界缓存模块输出的 Web Forms 容器。
    /// Web Forms container that caches module output by module instance and controlled runtime identity.
    /// </summary>
    /// <remarks>
    /// 当 <see cref="ModuleSettings.CacheTime"/> 大于零时，容器缓存模块内部输出；受信任包版本和状态修订由
    /// <see cref="CacheIdentity"/> 隔离。缓存键只区分当前编辑角色判断，不按具体用户隔离，因此模块不应在缓存输出中
    /// 写入仅对单一用户可见的敏感内容。
    /// When <see cref="ModuleSettings.CacheTime"/> is greater than zero, this container caches inner module output;
    /// trusted-package version and state revision are isolated by <see cref="CacheIdentity"/>. The cache key separates
    /// only the current edit-role check, not individual users, so modules must not render user-private sensitive content
    /// into cached output.
    /// </remarks>

    public class CachedPortalModuleControl : Control
    {
        // 私有字段变量

        private string _cachedOutput = ""; // 缓存的输出
        private ModuleSettings _moduleConfiguration; // 模块配置


        // 公共属性访问器

        /// <summary>
        /// 获取或设置当前模块的运行时设置快照。
        /// Gets or sets the runtime settings snapshot for the current module.
        /// </summary>
        public ModuleSettings ModuleConfiguration
        {
            get { return _moduleConfiguration; }
            set { _moduleConfiguration = value; }
        }

        /// <summary>
        /// 获取已绑定模块实例的标识。
        /// Gets the identifier of the bound module instance.
        /// </summary>
        public int ModuleId
        {
            get { return _moduleConfiguration.ModuleId; }
        }

        /// <summary>
        /// 获取或设置当前门户标识，并传递给动态加载的模块控件。
        /// Gets or sets the current portal identifier and passes it to the dynamically loaded module control.
        /// </summary>
        public int PortalId { get; set; }

        /// <summary>
        /// 已通过模块目录解析的桌面控件路径；为空时兼容读取旧模块定义路径。
        /// Desktop control path resolved by the module catalog; when empty, the legacy module-definition path remains compatible.
        /// </summary>
        /// <remarks>
        /// 非空值必须来自 <see cref="PortalModuleCatalog.TryResolveModule"/>；本控件不会再次验证它。
        /// A non-empty value must come from <see cref="PortalModuleCatalog.TryResolveModule"/>; this control does not validate it again.
        /// </remarks>
        public string DesktopSource { get; set; }

        /// <summary>
        /// 已验证包版本、状态修订或旧路径构成的缓存隔离身份。
        /// Cache-isolation identity composed from validated package version, state revision, or a legacy path.
        /// </summary>
        /// <remarks>
        /// 由调用方从受控运行描述提供；为空时保持历史缓存键行为。
        /// Supplied by the caller from a controlled runtime descriptor; when empty, historical cache-key behavior remains.
        /// </remarks>
        public string CacheIdentity { get; set; }

        /// <summary>
        /// 获取用于 ASP.NET 缓存的模块输出键。
        /// Gets the module-output key used by ASP.NET cache.
        /// </summary>
        /// <remarks>
        /// 键包含模块实例、当前编辑角色判断和 <see cref="CacheIdentity"/>。它不是安全令牌，也不代表完整用户身份。
        /// The key includes the module instance, current edit-role check, and <see cref="CacheIdentity"/>. It is not a
        /// security token and does not represent a complete user identity.
        /// </remarks>
        public string CacheKey
        {
            get
            {
                return "Key:" + GetType().FullName + ModuleId +
                       PortalSecurity.IsInRoles(_moduleConfiguration.AuthorizedEditRoles) + "|" +
                       (CacheIdentity ?? string.Empty);
            }
        }

        /// <summary>
        /// 在缓存未命中时加载模块控件，在命中时复用已缓存输出。
        /// Loads the module control on a cache miss and reuses cached output on a hit.
        /// </summary>
        /// <remarks>
        /// 缓存分支的加载失败会记录诊断事件并输出空模块区域，避免单个模块阻断整个门户页面；它不会自动改写
        /// 模块定义、包状态或缓存配置。
        /// A load failure in the cache branch records a diagnostic event and renders an empty module region so one
        /// module cannot block the entire portal page; it does not automatically rewrite module definitions, package
        /// state, or cache configuration.
        /// </remarks>
        protected override void CreateChildControls()
        {
            // 尝试从 ASP.NET 缓存中解析先前缓存的内容

            if (_moduleConfiguration.CacheTime > 0)
            {
                _cachedOutput = (string)Context.Cache[CacheKey];
            }

            // 如果没有找到缓存的内容，则实例化并将门户模块用户控件添加到门户的页面服务器控件树中

            if (_cachedOutput == null)
            {
                base.CreateChildControls();

                try
                {
                    string desktopSource = string.IsNullOrWhiteSpace(DesktopSource)
                        ? PortalModulePathValidator.NormalizeDesktopSourceOrThrow(_moduleConfiguration.DesktopSrc)
                        : DesktopSource;
                    var module = Page.LoadControl(desktopSource) as IPortalModuleControl;
                    if (module == null)
                    {
                        throw new InvalidOperationException(
                            "The module control does not implement IPortalModuleControl.");
                    }

                    module.ModuleConfiguration = ModuleConfiguration;
                    module.PortalId = PortalId;

                    Controls.Add((UserControl)module);
                }
                catch (Exception exception)
                {
                    // 缓存分支的加载失败不能中止整个门户页；记录事件并输出空模块区域。
                    // A load failure in the cache branch must not abort the entire portal page; record the event
                    // and render an empty module region.
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
        /// 渲染模块输出，并在启用缓存时写入或复用 ASP.NET 缓存项。
        /// Renders module output and writes or reuses an ASP.NET cache entry when caching is enabled.
        /// </summary>
        /// <param name="output">当前响应的 HTML 输出写入器。
        /// HTML output writer for the current response.</param>
        /// <remarks>
        /// 缓存有效期沿用模块配置中的秒数；零值直接渲染控件树。该实现保留历史本地时间过期计算，未在本文档批次
        /// 改变缓存时钟或失效策略。
        /// Cache lifetime follows the seconds in the module configuration; zero renders the control tree directly.
        /// This implementation retains historical local-time expiry calculation and does not change the cache clock or
        /// invalidation policy in this documentation batch.
        /// </remarks>
        protected override void Render(HtmlTextWriter output)
        {
            // 如果没有指定缓存，则渲染子控件树并返回

            if (_moduleConfiguration.CacheTime == 0)
            {
                base.Render(output);
                return;
            }

            // 如果从之前的请求中没有找到缓存的输出，则渲染子控件到 TextWriter，并将结果
            // 存储在 ASP.NET 缓存中以供将来请求使用。

            if (_cachedOutput == null)
            {
                using (TextWriter tempWriter = new StringWriter())
                {
                    base.Render(new HtmlTextWriter(tempWriter));
                    _cachedOutput = tempWriter.ToString();

                    Context.Cache.Insert(CacheKey, _cachedOutput, null,
                        DateTime.Now.AddSeconds(_moduleConfiguration.CacheTime), TimeSpan.Zero);
                }
            }

            // 输出用户控件的内容

            output.Write(_cachedOutput);
        }
    }
}
