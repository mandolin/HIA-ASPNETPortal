namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工资料确认业务模块的数据访问契约。
    ///
    /// English: Data-access contract for the employee-profile confirmation business module.
    /// </summary>
    /// <remarks>
    /// 中文：此契约服务 P6.4 首批业务模块样板，只允许读取当前绑定员工的低敏资料并写入确认记录；
    /// 不提供员工资料编辑、批量导入、附件上传或外部 HR 同步。
    ///
    /// English: This contract serves the first P6.4 business-module sample. It allows only reading low-sensitivity
    /// fields for the current bound employee and writing confirmation records; it does not provide employee editing,
    /// batch import, attachment upload, or external HR synchronization.
    /// </remarks>
    public interface IEmployeeProfileConfirmationDb
    {
        /// <summary>
        /// 中文：检查员工资料确认表和依赖的员工绑定基础表是否可用。
        ///
        /// English: Checks whether the employee-profile confirmation table and required employee-binding foundation
        /// tables are available.
        /// </summary>
        /// <returns>中文：相关表均可用时为 <c>true</c>。English: <c>true</c> when all related tables are available.</returns>
        bool IsSchemaAvailable();

        /// <summary>
        /// 中文：读取指定门户用户当前可确认的员工资料。
        ///
        /// English: Reads the employee profile currently confirmable by the specified Portal user.
        /// </summary>
        /// <param name="userId">中文：门户用户标识。English: Portal user identifier.</param>
        /// <returns>中文：当前资料视图；缺表、未绑定或非 Active 状态时为空。English: Current profile view, or null when schema is missing, no active binding exists, or the employee is not active.</returns>
        EmployeeProfileConfirmationView GetCurrentProfileForUser(int userId);

        /// <summary>
        /// 中文：为当前用户和员工写入一条资料确认记录。
        ///
        /// English: Writes one profile-confirmation record for the current user and employee.
        /// </summary>
        /// <param name="request">中文：确认请求。English: Confirmation request.</param>
        /// <returns>中文：写入结果。English: Write result.</returns>
        EmployeeProfileConfirmationResult ConfirmProfile(EmployeeProfileConfirmationRequest request);
    }
}
