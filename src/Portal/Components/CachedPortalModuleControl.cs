using System;
using System.IO;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    /* CachedPortalModuleControl 类
     
     CachedPortalModuleControl 类是一个自定义服务器控件，门户框架使用它来选择性地启用单个门户模块内容的输出缓存。
     
     如果在 ASPNET.StarterKit.Portal.Config 配置文件中指定了大于 0 秒的 CacheTime 值，那么 CachedPortalModuleControl
     将自动捕获它所包装的门户模块用户控件的输出，并将其存储在 ASP.NET 缓存 API 中。对于后续请求（无论是来自同一浏览器还是
     其他浏览器访问相同的门户页面），CachedPortalModuleControl 将尝试从缓存中解析已缓存的输出。
     
     注意：如果在 ASP.NET 缓存中找不到先前缓存的输出，CachedPortalModuleControl 将自动实例化适当的门户模块用户控件并将其放置在
     门户页面中。
    
     CachedPortalModuleControl Class
    
     The CachedPortalModuleControl class is a custom server control that
     the Portal framework uses to optionally enable output caching of 
     individual portal module's content.
    
     If a CacheTime value greater than 0 seconds is specified within the 
     ASPNET.StarterKit.Portal.Config configuration file, then the CachePortalModuleControl
     will automatically capture the output of the Portal Module User Control
     it wraps.  It will then store this captured output within the ASP.NET
     Cache API.  On subsequent requests (either by the same browser -- or
     by other browsers visiting the same portal page), the CachedPortalModuleControl
     will attempt to resolve the cached output out of the cache.
    
     Note: In the event that previously cached output can't be found in the
     ASP.NET Cache, the CachedPortalModuleControl will automatically instatiate
     the appropriate portal module user control and place it within the
     portal page.
    
    */

    public class CachedPortalModuleControl : Control
    {
        // 私有字段变量

        private string _cachedOutput = ""; // 缓存的输出
        private ModuleSettings _moduleConfiguration; // 模块配置


        // 公共属性访问器

        /// <summary>
        /// 获取或设置模块配置
        /// </summary>
        public ModuleSettings ModuleConfiguration
        {
            get { return _moduleConfiguration; }
            set { _moduleConfiguration = value; }
        }

        /// <summary>
        /// 获取模块 ID
        /// </summary>
        public int ModuleId
        {
            get { return _moduleConfiguration.ModuleId; }
        }

        /// <summary>
        /// 获取或设置门户 ID
        /// </summary>
        public int PortalId { get; set; }

        /// <summary>
        /// 已通过模块目录解析的桌面控件路径；为空时兼容读取旧模块定义路径。
        /// Desktop control path resolved by the module catalog; when empty, the legacy module-definition path remains compatible.
        /// </summary>
        public string DesktopSource { get; set; }

        /// <summary>
        /// 已验证包版本、状态修订或旧路径构成的缓存隔离身份。
        /// Cache-isolation identity composed from validated package version, state revision, or a legacy path.
        /// </summary>
        public string CacheIdentity { get; set; }

        /* CacheKey 属性
        
         CacheKey 属性用于计算一个“唯一”的缓存键条目，用于在 ASP.NET 缓存中存储/检索门户模块的内容。
        
        
         CacheKey Property
        
         The CacheKey property is used to calculate a "unique" cache key
         entry to be used to store/retrieve the portal module's content
         from the ASP.NET Cache.
        
        */

        /// <summary>
        /// 获取缓存键
        /// </summary>
        public string CacheKey
        {
            get
            {
                return "Key:" + GetType().FullName + ModuleId +
                       PortalSecurity.IsInRoles(_moduleConfiguration.AuthorizedEditRoles) + "|" +
                       (CacheIdentity ?? string.Empty);
            }
        }

        /* CreateChildControls 方法
        
         当 ASP.NET 页面框架确定是时候实例化服务器控件时，会调用 CreateChildControls 方法。
         
         CachedPortalModuleControl 控件覆盖此方法，并尝试从 ASP.NET 缓存中解析门户模块的先前缓存输出。
         
         如果它没有从之前的请求中找到缓存输出，那么 CachedPortalModuleControl 将实例化并将门户模块的
         用户控件实例添加到页面控件树中。
        
         CreateChildControls Method
        
         The CreateChildControls method is called when the ASP.NET Page Framework
         determines that it is time to instantiate a server control.
         
         The CachedPortalModuleControl control overrides this method and attempts
         to resolve any previously cached output of the portal module from the 
         ASP.NET cache.  
        
         If it doesn't find cached output from a previous request, then the
         CachedPortalModuleControl will instantiate and add the portal module's
         User Control instance into the page tree.
        
        */
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

        /* Render 方法


         当 ASP.NET 页面框架确定是时候将内容渲染到页面输出流时，会调用 Render 方法。


         CachedPortalModuleControl 控件覆盖此方法并捕获门户模块用户控件生成的输出。然后，
         将这些内容添加到 ASP.NET 缓存中以供将来请求使用。


         Render Method


         The Render method is called when the ASP.NET Page Framework
         determines that it is time to render content into the page output stream.

         The CachedPortalModuleControl control overrides this method and captures
         the output generated by the portal module user control.  It then
         adds this content into the ASP.NET Cache for future requests.

        */
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
