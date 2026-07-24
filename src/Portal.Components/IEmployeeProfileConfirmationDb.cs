namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料确认业务模块的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for the employee-profile confirmation business module.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此契约服务 P6.4 首批业务模块样板，只允许读取当前绑定员工的低敏资料并写入确认记录；不提供员工资料编辑、批量导入、附件上传或外部 HR 同步。</zh-CN>
    ///   <en>This contract serves the first P6.4 business-module sample. It allows only reading low-sensitivity fields for the current bound employee and writing confirmation records; it does not provide employee editing, batch import, attachment upload, or external HR synchronization.</en>
    /// </lang>
    /// </remarks>
    public interface IEmployeeProfileConfirmationDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查员工资料确认表和依赖的员工绑定基础表是否可用。</zh-CN>
        ///   <en>Checks whether the employee-profile confirmation table and required employee-binding foundation tables are available.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>相关表均可用时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when all related tables are available.</en>
        /// </l>
        /// </returns>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定门户用户当前可确认的员工资料。</zh-CN>
        ///   <en>Reads the employee profile currently confirmable by the specified Portal user.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>门户用户标识。</zh-CN>
        ///   <en>Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前资料视图；缺表、未绑定或非 Active 状态时为空。</zh-CN>
        ///   <en>Current profile view, or null when schema is missing, no active binding exists, or the employee is not active.</en>
        /// </l>
        /// </returns>
        EmployeeProfileConfirmationView GetCurrentProfileForUser(int userId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>为当前用户和员工写入一条资料确认记录。</zh-CN>
        ///   <en>Writes one profile-confirmation record for the current user and employee.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>确认请求。</zh-CN>
        ///   <en>Confirmation request.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果。</zh-CN>
        ///   <en>Write result.</en>
        /// </l>
        /// </returns>
        EmployeeProfileConfirmationResult ConfirmProfile(EmployeeProfileConfirmationRequest request);
    }
}
