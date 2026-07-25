namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户账号与员工绑定的后台写入契约。</zh-CN>
    ///   <en>Administration write contract for Portal-user to employee bindings.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本契约只负责单条绑定和解绑，不负责批量导入、外部 HR 同步或密码验证。成功写入后，页面或服务层必须同步写运营审计，并递增目标用户安全版本。</zh-CN>
    ///   <en>This contract handles only single-row bind and unbind operations, not bulk import, external HR synchronization, or password validation. After a successful write, the page or service layer must record operations audit and increment the target user's security version.</en>
    /// </lang>
    /// </remarks>
    public interface IUserEmployeeBindingAdminDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查 P6.3 绑定所需表是否可用。</zh-CN>
        ///   <en>Checks whether the P6.3 binding tables are available.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>全部所需表存在时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when all required tables exist.</en>
        /// </l>
        /// </returns>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识读取绑定记录。</zh-CN>
        ///   <en>Reads a binding row by id.</en>
        /// </lang>
        /// </summary>
        /// <param name="bindingId">
        /// <l>
        ///   <zh-CN>绑定记录标识。</zh-CN>
        ///   <en>Binding row identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>绑定记录；不存在或表不可用时返回 <c>null</c>。</zh-CN>
        ///   <en>Binding row, or <c>null</c> when it does not exist or schema is unavailable.</en>
        /// </l>
        /// </returns>
        IUserEmployeeBindingInfo GetBindingById(int bindingId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>建立一个当前有效绑定。</zh-CN>
        ///   <en>Creates one current active binding.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>账号、员工、状态、时间和操作者信息组成的保存请求。</zh-CN>
        ///   <en>Save request containing user, employee, status, timestamp and actor information.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果；失败消息应保持低敏，可直接展示给管理员。</zh-CN>
        ///   <en>Write result; failure messages should remain low-sensitivity and may be shown to administrators.</en>
        /// </l>
        /// </returns>
        EmployeeDirectoryWriteResult BindUserToEmployee(UserEmployeeBindingSaveRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>结束一个当前有效绑定。</zh-CN>
        ///   <en>Ends one current active binding.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>解绑请求，包含绑定标识、结束时间和操作者信息。</zh-CN>
        ///   <en>Unbind request containing binding id, end timestamp and actor information.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果；未找到当前有效绑定时返回失败。</zh-CN>
        ///   <en>Write result; returns failure when no current active binding is found.</en>
        /// </l>
        /// </returns>
        EmployeeDirectoryWriteResult EndBinding(UserEmployeeBindingEndRequest request);
    }
}
