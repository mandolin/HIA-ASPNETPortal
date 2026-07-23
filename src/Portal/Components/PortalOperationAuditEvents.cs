namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>集中保存门户运营审计的稳定分类、动作和目标类型，避免页面层散写字符串。</zh-CN>
    ///   <en>Centralizes stable Portal operations-audit categories, actions, and target types so page code does not scatter string literals.</en>
    /// </lang>
    /// </summary>
    public static class PortalOperationAuditEvents
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>用户生命周期分类，覆盖注册、审核、资料状态和账号启停等动作。</zh-CN>
        ///   <en>User-lifecycle category covering registration, review, profile status, and account enable/disable actions.</en>
        /// </lang>
        /// </summary>
        public const string UserLifecycleCategory = "UserLifecycle";

        /// <summary>
        /// <lang>
        ///   <zh-CN>安全凭据分类，覆盖密码重置和旧凭据升级等动作。</zh-CN>
        ///   <en>Security-credential category covering password resets and legacy-credential upgrades.</en>
        /// </lang>
        /// </summary>
        public const string SecurityCredentialsCategory = "SecurityCredentials";

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户管理分类，覆盖角色成员关系等仍属于后台管理而非生命周期状态的动作。</zh-CN>
        ///   <en>User-administration category for role memberships and other administrative actions that are not lifecycle-status changes.</en>
        /// </lang>
        /// </summary>
        public const string UserAdministrationCategory = "UserAdministration";

        /// <summary>
        /// <lang>
        ///   <zh-CN>企业目录分类，覆盖员工和组织主数据维护动作。</zh-CN>
        ///   <en>Enterprise-directory category covering employee and organization master-data maintenance actions.</en>
        /// </lang>
        /// </summary>
        public const string EnterpriseDirectoryCategory = "EnterpriseDirectory";

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务模块分类，覆盖新业务模块的领域动作。</zh-CN>
        ///   <en>Business-module category covering domain actions from new business modules.</en>
        /// </lang>
        /// </summary>
        public const string BusinessModuleCategory = "BusinessModule";

        /// <summary>
        /// <lang>
        ///   <zh-CN>审计目标类型：门户用户。</zh-CN>
        ///   <en>Audit target type for a Portal user.</en>
        /// </lang>
        /// </summary>
        public const string UserTargetType = "User";

        /// <summary>
        /// <lang>
        ///   <zh-CN>审计目标类型：组织单元。</zh-CN>
        ///   <en>Audit target type for an organization unit.</en>
        /// </lang>
        /// </summary>
        public const string OrganizationUnitTargetType = "OrganizationUnit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>审计目标类型：员工。</zh-CN>
        ///   <en>Audit target type for an employee.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeTargetType = "Employee";

        /// <summary>
        /// <lang>
        ///   <zh-CN>审计目标类型：门户账号员工绑定。</zh-CN>
        ///   <en>Audit target type for a Portal-user employee binding.</en>
        /// </lang>
        /// </summary>
        public const string UserEmployeeBindingTargetType = "UserEmployeeBinding";

        /// <summary>
        /// <lang>
        ///   <zh-CN>审计目标类型：员工资料确认记录。</zh-CN>
        ///   <en>Audit target type for an employee-profile confirmation record.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileConfirmationTargetType = "EmployeeProfileConfirmation";

        /// <summary>
        /// <lang>
        ///   <zh-CN>审计目标类型：员工资料更正请求。</zh-CN>
        ///   <en>Audit target type for an employee-profile correction request.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileCorrectionRequestTargetType = "EmployeeProfileCorrectionRequest";

        /// <summary>
        /// <lang>
        ///   <zh-CN>自主注册已提交。</zh-CN>
        ///   <en>Self-registration was submitted.</en>
        /// </lang>
        /// </summary>
        public const string RegistrationSubmitted = "RegistrationSubmitted";

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册申请已批准。</zh-CN>
        ///   <en>Registration request was approved.</en>
        /// </lang>
        /// </summary>
        public const string RegistrationApproved = "RegistrationApproved";

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册申请已拒绝。</zh-CN>
        ///   <en>Registration request was rejected.</en>
        /// </lang>
        /// </summary>
        public const string RegistrationRejected = "RegistrationRejected";

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户资料已更新。</zh-CN>
        ///   <en>User profile was updated.</en>
        /// </lang>
        /// </summary>
        public const string ProfileUpdated = "ProfileUpdated";

        /// <summary>
        /// <lang>
        ///   <zh-CN>账号已被管理员禁用。</zh-CN>
        ///   <en>Account was disabled by an administrator.</en>
        /// </lang>
        /// </summary>
        public const string UserDisabled = "UserDisabled";

        /// <summary>
        /// <lang>
        ///   <zh-CN>账号已由管理员恢复启用。</zh-CN>
        ///   <en>Account was restored by an administrator.</en>
        /// </lang>
        /// </summary>
        public const string UserRestored = "UserRestored";

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理员重置了密码。</zh-CN>
        ///   <en>Password was reset by an administrator.</en>
        /// </lang>
        /// </summary>
        public const string PasswordReset = "PasswordReset";

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧凭据在登录成功后升级。</zh-CN>
        ///   <en>Legacy credential was upgraded after a successful sign-in.</en>
        /// </lang>
        /// </summary>
        public const string LegacyCredentialUpgraded = "LegacyCredentialUpgraded";

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户被加入角色。</zh-CN>
        ///   <en>User was added to a role.</en>
        /// </lang>
        /// </summary>
        public const string RoleAdded = "RoleAdded";

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户被移出角色。</zh-CN>
        ///   <en>User was removed from a role.</en>
        /// </lang>
        /// </summary>
        public const string RoleRemoved = "RoleRemoved";

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织单元已创建。</zh-CN>
        ///   <en>Organization unit was created.</en>
        /// </lang>
        /// </summary>
        public const string OrganizationUnitCreated = "OrganizationUnitCreated";

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织单元已更新。</zh-CN>
        ///   <en>Organization unit was updated.</en>
        /// </lang>
        /// </summary>
        public const string OrganizationUnitUpdated = "OrganizationUnitUpdated";

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工主数据已创建。</zh-CN>
        ///   <en>Employee master data was created.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeCreated = "EmployeeCreated";

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工主数据已更新。</zh-CN>
        ///   <en>Employee master data was updated.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeUpdated = "EmployeeUpdated";

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户账号与员工已绑定。</zh-CN>
        ///   <en>A Portal user was bound to an employee.</en>
        /// </lang>
        /// </summary>
        public const string UserEmployeeBound = "UserEmployeeBound";

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户账号与员工绑定已结束。</zh-CN>
        ///   <en>A Portal-user employee binding was ended.</en>
        /// </lang>
        /// </summary>
        public const string UserEmployeeUnbound = "UserEmployeeUnbound";

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工资料已由当前绑定用户确认。</zh-CN>
        ///   <en>Employee profile was confirmed by the current bound user.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileConfirmed = "EmployeeProfileConfirmed";

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工资料更正请求已提交。</zh-CN>
        ///   <en>Employee-profile correction request was submitted.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileCorrectionRequested = "EmployeeProfileCorrectionRequested";

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工资料更正请求已由管理员处理。</zh-CN>
        ///   <en>Employee-profile correction request was reviewed by an administrator.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileCorrectionReviewed = "EmployeeProfileCorrectionReviewed";
    }
}
