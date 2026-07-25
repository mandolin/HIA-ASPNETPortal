namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户模块编辑权限检查契约。</zh-CN>
    ///   <en>Contract for checking Portal module-edit permissions.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该契约用于旧内容模块编辑入口的最后一道门禁。它不替代页面层的请求参数解析、条目归属检查或业务级权限判断。</zh-CN>
    ///   <en>This contract is the final gate for legacy content-module edit entries. It does not replace page-layer request parsing, item ownership checks, or business-level authorization.</en>
    /// </lang>
    /// </remarks>
    public interface IPortalSecurity
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求身份是否可编辑指定模块的设置。</zh-CN>
        ///   <en>Determines whether the current request identity may edit settings of the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>要检查的模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier to check.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>同时满足父 Tab 访问角色和模块编辑角色时为 <c>true</c>；模块、父 Tab 或必要关联缺失时安全返回 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> only when both parent-Tab access roles and module edit roles are satisfied; returns <c>false</c> safely when the module, parent Tab, or a required relationship is missing.</en>
        /// </l>
        /// </returns>
        bool HasEditPermissions(int moduleId);
    }
}
