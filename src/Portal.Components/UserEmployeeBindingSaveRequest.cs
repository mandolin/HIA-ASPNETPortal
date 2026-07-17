namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户账号与员工建立当前有效绑定的请求。
    ///
    /// English: Request for creating a current active binding between a Portal user and an employee.
    /// </summary>
    /// <remarks>
    /// 中文：员工号只作为业务标识和可选登录标识，不是认证凭据；调用方不得在原因中写入密码、Cookie 或敏感证件号。
    ///
    /// English: The employee code is only a business identifier and optional sign-in identifier, not a credential;
    /// callers must not put passwords, cookies, or sensitive identity numbers in the reason.
    /// </remarks>
    public sealed class UserEmployeeBindingSaveRequest
    {
        /// <summary>中文：门户用户标识。English: Portal user identifier.</summary>
        public int UserId { get; set; }

        /// <summary>中文：员工号。English: Employee code.</summary>
        public string EmployeeCode { get; set; }

        /// <summary>中文：非敏感绑定说明。English: Non-sensitive binding reason.</summary>
        public string Reason { get; set; }

        /// <summary>中文：执行绑定的操作者标识。English: Operator identifier creating the binding.</summary>
        public string ActorName { get; set; }
    }
}
