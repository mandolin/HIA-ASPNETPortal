namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户模块编辑权限检查契约。
    ///
    /// English: Contract for checking Portal module-edit permissions.
    /// </summary>
    public interface IPortalSecurity
    {
        /// <summary>
        /// 中文：判断当前请求身份是否可编辑指定模块的设置。
        ///
        /// English: Determines whether the current request identity may edit settings of the specified module.
        /// </summary>
        /// <param name="moduleId">中文：要检查的模块实例标识。English: Module-instance identifier to check.</param>
        /// <returns>中文：同时满足父 Tab 访问角色和模块编辑角色时为 <c>true</c>。English: <c>true</c> only when both the parent-tab access roles and module edit roles are satisfied.</returns>
        bool HasEditPermissions(int moduleId);
    }
}
