namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：结束门户账号与员工当前有效绑定的请求。
    ///
    /// English: Request for ending a current active binding between a Portal user and an employee.
    /// </summary>
    public sealed class UserEmployeeBindingEndRequest
    {
        /// <summary>中文：绑定记录标识。English: Binding identifier.</summary>
        public int BindingId { get; set; }

        /// <summary>中文：非敏感解绑说明。English: Non-sensitive unbinding reason.</summary>
        public string Reason { get; set; }

        /// <summary>中文：执行解绑的操作者标识。English: Operator identifier ending the binding.</summary>
        public string ActorName { get; set; }
    }
}
