namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：集中保存门户运营审计的稳定分类、动作和目标类型，避免页面层散写字符串。
    ///
    /// English: Centralizes stable Portal operations-audit categories, actions, and target types so page code does not
    /// scatter string literals.
    /// </summary>
    public static class PortalOperationAuditEvents
    {
        /// <summary>
        /// 中文：用户生命周期分类，覆盖注册、审核、资料状态和账号启停等动作。
        ///
        /// English: User-lifecycle category covering registration, review, profile status, and account enable/disable actions.
        /// </summary>
        public const string UserLifecycleCategory = "UserLifecycle";

        /// <summary>
        /// 中文：安全凭据分类，覆盖密码重置和旧凭据升级等动作。
        ///
        /// English: Security-credential category covering password resets and legacy-credential upgrades.
        /// </summary>
        public const string SecurityCredentialsCategory = "SecurityCredentials";

        /// <summary>
        /// 中文：用户管理分类，覆盖角色成员关系等仍属于后台管理而非生命周期状态的动作。
        ///
        /// English: User-administration category for role memberships and other administrative actions that are not
        /// lifecycle-status changes.
        /// </summary>
        public const string UserAdministrationCategory = "UserAdministration";

        /// <summary>
        /// 中文：企业目录分类，覆盖员工和组织主数据维护动作。
        ///
        /// English: Enterprise-directory category covering employee and organization master-data maintenance actions.
        /// </summary>
        public const string EnterpriseDirectoryCategory = "EnterpriseDirectory";

        /// <summary>
        /// 中文：审计目标类型：门户用户。
        ///
        /// English: Audit target type for a Portal user.
        /// </summary>
        public const string UserTargetType = "User";

        /// <summary>
        /// 中文：审计目标类型：组织单元。
        ///
        /// English: Audit target type for an organization unit.
        /// </summary>
        public const string OrganizationUnitTargetType = "OrganizationUnit";

        /// <summary>
        /// 中文：审计目标类型：员工。
        ///
        /// English: Audit target type for an employee.
        /// </summary>
        public const string EmployeeTargetType = "Employee";

        /// <summary>
        /// 中文：自主注册已提交。
        ///
        /// English: Self-registration was submitted.
        /// </summary>
        public const string RegistrationSubmitted = "RegistrationSubmitted";

        /// <summary>
        /// 中文：注册申请已批准。
        ///
        /// English: Registration request was approved.
        /// </summary>
        public const string RegistrationApproved = "RegistrationApproved";

        /// <summary>
        /// 中文：注册申请已拒绝。
        ///
        /// English: Registration request was rejected.
        /// </summary>
        public const string RegistrationRejected = "RegistrationRejected";

        /// <summary>
        /// 中文：用户资料已更新。
        ///
        /// English: User profile was updated.
        /// </summary>
        public const string ProfileUpdated = "ProfileUpdated";

        /// <summary>
        /// 中文：账号已被管理员禁用。
        ///
        /// English: Account was disabled by an administrator.
        /// </summary>
        public const string UserDisabled = "UserDisabled";

        /// <summary>
        /// 中文：账号已由管理员恢复启用。
        ///
        /// English: Account was restored by an administrator.
        /// </summary>
        public const string UserRestored = "UserRestored";

        /// <summary>
        /// 中文：管理员重置了密码。
        ///
        /// English: Password was reset by an administrator.
        /// </summary>
        public const string PasswordReset = "PasswordReset";

        /// <summary>
        /// 中文：旧凭据在登录成功后升级。
        ///
        /// English: Legacy credential was upgraded after a successful sign-in.
        /// </summary>
        public const string LegacyCredentialUpgraded = "LegacyCredentialUpgraded";

        /// <summary>
        /// 中文：用户被加入角色。
        ///
        /// English: User was added to a role.
        /// </summary>
        public const string RoleAdded = "RoleAdded";

        /// <summary>
        /// 中文：用户被移出角色。
        ///
        /// English: User was removed from a role.
        /// </summary>
        public const string RoleRemoved = "RoleRemoved";

        /// <summary>
        /// 中文：组织单元已创建。
        ///
        /// English: Organization unit was created.
        /// </summary>
        public const string OrganizationUnitCreated = "OrganizationUnitCreated";

        /// <summary>
        /// 中文：组织单元已更新。
        ///
        /// English: Organization unit was updated.
        /// </summary>
        public const string OrganizationUnitUpdated = "OrganizationUnitUpdated";

        /// <summary>
        /// 中文：员工主数据已创建。
        ///
        /// English: Employee master data was created.
        /// </summary>
        public const string EmployeeCreated = "EmployeeCreated";

        /// <summary>
        /// 中文：员工主数据已更新。
        ///
        /// English: Employee master data was updated.
        /// </summary>
        public const string EmployeeUpdated = "EmployeeUpdated";
    }
}
