namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户账号与员工绑定的后台写入契约。
    ///
    /// English: Administration write contract for Portal-user to employee bindings.
    /// </summary>
    /// <remarks>
    /// 中文：本契约只负责单条绑定和解绑，不负责批量导入、外部 HR 同步或密码验证。成功写入后，
    /// 页面或服务层必须同步写运营审计，并递增目标用户安全版本。
    ///
    /// English: This contract handles only single-row bind and unbind operations, not bulk import, external HR
    /// synchronization, or password validation. After a successful write, the page or service layer must record
    /// operations audit and increment the target user's security version.
    /// </remarks>
    public interface IUserEmployeeBindingAdminDb
    {
        /// <summary>
        /// 中文：检查 P6.3 绑定所需表是否可用。
        ///
        /// English: Checks whether the P6.3 binding tables are available.
        /// </summary>
        bool IsSchemaAvailable();

        /// <summary>
        /// 中文：按标识读取绑定记录。
        ///
        /// English: Reads a binding row by id.
        /// </summary>
        IUserEmployeeBindingInfo GetBindingById(int bindingId);

        /// <summary>
        /// 中文：建立一个当前有效绑定。
        ///
        /// English: Creates one current active binding.
        /// </summary>
        EmployeeDirectoryWriteResult BindUserToEmployee(UserEmployeeBindingSaveRequest request);

        /// <summary>
        /// 中文：结束一个当前有效绑定。
        ///
        /// English: Ends one current active binding.
        /// </summary>
        EmployeeDirectoryWriteResult EndBinding(UserEmployeeBindingEndRequest request);
    }
}
