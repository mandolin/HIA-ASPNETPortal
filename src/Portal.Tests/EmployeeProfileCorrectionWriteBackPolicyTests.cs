using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>资料更正回写策略的契约测试，锁定服务端字段白名单、映射行为与"需人工处理"边界。</zh-CN>
    ///   <en>Contract tests for the profile-correction write-back policy, pinning the server-side field allow list, the mapping behavior, and the "manual handling" boundary.</en>
    /// </lang>
    /// </summary>
    [TestClass]
    public class EmployeeProfileCorrectionWriteBackPolicyTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>四个已知字段应被识别，未知字段应被拒绝。</zh-CN>
        ///   <en>The four known fields must be recognized and unknown ones rejected.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsKnownField_RecognizesKnownAndRejectsUnknown()
        {
            Assert.IsTrue(EmployeeProfileCorrectionWriteBackPolicy.IsKnownField("DisplayName"), "显示名应为已知字段。");
            Assert.IsTrue(EmployeeProfileCorrectionWriteBackPolicy.IsKnownField("PreferredName"), "偏好称呼应为已知字段。");
            Assert.IsTrue(EmployeeProfileCorrectionWriteBackPolicy.IsKnownField("WorkEmail"), "工作邮箱应为已知字段。");
            Assert.IsTrue(EmployeeProfileCorrectionWriteBackPolicy.IsKnownField("OrganizationDisplayName"), "组织显示名应为已知字段。");
            Assert.IsFalse(EmployeeProfileCorrectionWriteBackPolicy.IsKnownField("Salary"), "未知字段不得被识别。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>自动回写白名单只含可直接映射的三个字段；组织显示名不得自动回写。</zh-CN>
        ///   <en>The automatic write-back allow list contains only the three directly mappable fields; the organization display name must not be applied automatically.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanAutoApply_ExcludesOrganizationDisplayName()
        {
            Assert.IsTrue(EmployeeProfileCorrectionWriteBackPolicy.CanAutoApply("DisplayName"), "显示名应可自动回写。");
            Assert.IsTrue(EmployeeProfileCorrectionWriteBackPolicy.CanAutoApply("PreferredName"), "偏好称呼应可自动回写。");
            Assert.IsTrue(EmployeeProfileCorrectionWriteBackPolicy.CanAutoApply("WorkEmail"), "工作邮箱应可自动回写。");
            Assert.IsFalse(
                EmployeeProfileCorrectionWriteBackPolicy.CanAutoApply("OrganizationDisplayName"),
                "组织显示名需映射为组织标识且存在重名歧义，不得自动回写。");
            Assert.IsFalse(EmployeeProfileCorrectionWriteBackPolicy.CanAutoApply("Salary"), "未知字段不得自动回写。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>只有已知且不可自动回写的字段才被标记为"需人工处理"，避免审核通过后静默不生效。</zh-CN>
        ///   <en>Only a known field that cannot be applied automatically is flagged as requiring manual handling, preventing silent no-ops after approval.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void RequiresManualHandling_OnlyForKnownButNotAutoFields()
        {
            Assert.IsTrue(
                EmployeeProfileCorrectionWriteBackPolicy.RequiresManualHandling("OrganizationDisplayName"),
                "组织显示名应被标记为需人工处理。");
            Assert.IsFalse(
                EmployeeProfileCorrectionWriteBackPolicy.RequiresManualHandling("DisplayName"),
                "可自动回写的字段不应被标记为需人工处理。");
            Assert.IsFalse(
                EmployeeProfileCorrectionWriteBackPolicy.RequiresManualHandling("Salary"),
                "未知字段不属于人工处理范畴（应直接拒绝）。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>三个白名单字段的建议值应被写入保存请求的对应属性。</zh-CN>
        ///   <en>Proposed values for the three allow-listed fields must be written to the matching save-request property.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryApply_WritesAllowListedFields()
        {
            EmployeeSaveRequest request = new EmployeeSaveRequest();

            bool displayNameApplied = EmployeeProfileCorrectionWriteBackPolicy.TryApply(request, "DisplayName", " 张三 ");
            bool preferredNameApplied = EmployeeProfileCorrectionWriteBackPolicy.TryApply(request, "PreferredName", "小张");
            bool workEmailApplied = EmployeeProfileCorrectionWriteBackPolicy.TryApply(request, "WorkEmail", " zhang@example.com ");

            Assert.IsTrue(displayNameApplied && preferredNameApplied && workEmailApplied, "白名单字段应写入成功。");
            Assert.AreEqual("张三", request.DisplayName, "显示名应写入并裁剪空白。");
            Assert.AreEqual("小张", request.PreferredName, "偏好称呼应写入。");
            Assert.AreEqual("zhang@example.com", request.WorkEmail, "工作邮箱应写入并裁剪空白。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>未知字段不得写入任何属性（服务端二次校验，不信任调用方字段名）。</zh-CN>
        ///   <en>An unknown field must not write any property (server-side re-validation never trusts a caller-supplied field name).</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryApply_RejectsUnknownField()
        {
            EmployeeSaveRequest request = new EmployeeSaveRequest();

            bool applied = EmployeeProfileCorrectionWriteBackPolicy.TryApply(request, "Salary", "100000");

            Assert.IsFalse(applied, "未知字段必须被拒绝。");
            Assert.IsNull(request.DisplayName, "被拒绝的写入不得改动任何属性。");
            Assert.IsNull(request.PreferredName, "被拒绝的写入不得改动任何属性。");
            Assert.IsNull(request.WorkEmail, "被拒绝的写入不得改动任何属性。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>空建议值、空请求与需人工处理字段都应被拒绝，避免制造无意义的主数据更新。</zh-CN>
        ///   <en>Blank proposed values, null requests, and manually handled fields must all be rejected so no meaningless master-data update is created.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryApply_RejectsBlankValueNullRequestAndManualField()
        {
            EmployeeSaveRequest request = new EmployeeSaveRequest();

            bool blankApplied = EmployeeProfileCorrectionWriteBackPolicy.TryApply(request, "DisplayName", "   ");
            bool manualApplied = EmployeeProfileCorrectionWriteBackPolicy.TryApply(request, "OrganizationDisplayName", "研发中心");
            bool nullRequestApplied = EmployeeProfileCorrectionWriteBackPolicy.TryApply(null, "DisplayName", "张三");

            Assert.IsFalse(blankApplied, "空白建议值应被拒绝。");
            Assert.IsFalse(manualApplied, "需人工处理字段不应被自动写入。");
            Assert.IsFalse(nullRequestApplied, "空请求应被拒绝。");
            Assert.IsNull(request.DisplayName, "被拒绝的写入不得改动属性。");
        }
    }
}
