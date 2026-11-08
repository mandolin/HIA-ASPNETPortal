using System;
using System.IO;
using ASPNET.StarterKit.Portal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <lang>
///   <zh-CN>在线能力包审计测试：事件词表、六要素固定键序、fail-closed 不截断、结果白名单、
///   与装配态审计互不混用，以及「工具不代写 / 运行时包装接线正确」的静态源码断言。</zh-CN>
///   <en>Tests for online capability package audits: event vocabulary, fixed six-element key order, fail-closed non-truncation,
///   outcome whitelist, separation from assembly-state audits, and static source assertions for "tools never write" / correct runtime wiring.</en>
/// </lang>
/// </summary>
[TestClass]
public class PortalOnlinePackageAuditTests
{
    /// <summary><lang><zh-CN>固定 UTC 时间，使摘要字符串可逐字断言。</zh-CN><en>Fixed UTC time so the summary string can be asserted verbatim.</en></lang></summary>
    private static readonly DateTime FixedUtc = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

    /// <summary><lang><zh-CN>合法 SHA-256 十六进制摘要。</zh-CN><en>Valid SHA-256 hex digest.</en></lang></summary>
    private const string Hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    /// <summary>
    /// <lang>
    ///   <zh-CN>分类、四个动作、目标类型与三个结果值均为稳定精确值；分类与既有 ModuleAssembly 区分。</zh-CN>
    ///   <en>Category, four actions, target type, and three outcomes are exact stable values; the category is distinct from the existing ModuleAssembly.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void EventVocabulary_IsStableAndDistinctFromAssemblyState()
    {
        Assert.AreEqual("OnlinePackage", PortalOnlinePackageAuditEvents.Category);
        Assert.AreEqual("OnlinePackageUploadValidated", PortalOnlinePackageAuditEvents.UploadValidated);
        Assert.AreEqual("OnlinePackageInstallConfirmed", PortalOnlinePackageAuditEvents.InstallConfirmed);
        Assert.AreEqual("OnlinePackageStagedDiscarded", PortalOnlinePackageAuditEvents.StagedPackageDiscarded);
        Assert.AreEqual("OnlinePackageRollbackDrill", PortalOnlinePackageAuditEvents.RollbackDrill);
        Assert.AreEqual("OnlinePackageStaging", PortalOnlinePackageAuditEvents.TargetType);
        Assert.AreEqual("Success", PortalOnlinePackageAuditEvents.OutcomeSuccess);
        Assert.AreEqual("Failure", PortalOnlinePackageAuditEvents.OutcomeFailure);
        Assert.AreEqual("Skipped", PortalOnlinePackageAuditEvents.OutcomeSkipped);

        // <lang>
        //   <zh-CN>与装配态审计的分类/动作/目标类型互不重名，证明「互不混用」而非复用同一词表。</zh-CN>
        //   <en>Category, actions, and target type do not collide with assembly-state audits, proving "no mixing" rather than vocabulary reuse.</en>
        // </lang>
        Assert.AreNotEqual("ModuleAssembly", PortalOnlinePackageAuditEvents.Category);
        string[] assemblyActions = { "AssemblyStateCheck", "AssemblyStateRollbackDrill" };
        foreach (string assemblyAction in assemblyActions)
        {
            Assert.IsFalse(PortalOnlinePackageAuditFormatter.IsRegisteredAction(assemblyAction));
        }
        Assert.AreNotEqual("ModuleAssemblyState", PortalOnlinePackageAuditEvents.TargetType);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>六要素固定键序与既有装配态格式器逐字同构，便于离线工具/门禁互印证；时间用秒级 ISO 8601 UTC。</zh-CN>
    ///   <en>The fixed six-element key order is character-for-character isomorphic with the assembly-state formatter for offline-tool/gate corroboration; times use second-precision ISO 8601 UTC.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Format_SixElements_FixedOrderAndMatchesAssemblyShape()
    {
        string online = PortalOnlinePackageAuditFormatter.FormatSixElementSummary(
            "staged-1234.zip", string.Empty, Hash, "admin", FixedUtc, FixedUtc, "V1-V7 passed",
            PortalOnlinePackageAuditEvents.UploadValidated);

        string expected = "pkg=staged-1234.zip;hashBefore=;hashAfter=" + Hash +
                          ";actor=admin;startUtc=2026-11-08T12:00:00Z;endUtc=2026-11-08T12:00:00Z" +
                          ";verification=V1-V7 passed;direction=" + PortalOnlinePackageAuditEvents.UploadValidated;
        Assert.AreEqual(expected, online);
        Assert.IsTrue(online.Length <= PortalOnlinePackageAuditFormatter.MaxSummaryLength);

        // <lang>
        //   <zh-CN>用同一组输入调装配态格式器，除 pkg 主体语义外键序与时间格式完全一致。</zh-CN>
        //   <en>Call the assembly-state formatter with the same inputs; key order and time format match apart from pkg semantics.</en>
        // </lang>
        string assembly = PortalModuleAssemblyAuditFormatter.FormatSixElementSummary(
            "staged-1234.zip", "1.0.0", string.Empty, Hash, "admin", FixedUtc, FixedUtc,
            "V1-V7 passed", PortalOnlinePackageAuditEvents.UploadValidated);
        Assert.IsTrue(assembly.StartsWith("pkg=staged-1234.zip@1.0.0;hashBefore=;", StringComparison.Ordinal));
        Assert.IsTrue(assembly.EndsWith(";direction=" + PortalOnlinePackageAuditEvents.UploadValidated, StringComparison.Ordinal));
        Assert.IsTrue(online.Contains(";startUtc=2026-11-08T12:00:00Z;endUtc=2026-11-08T12:00:00Z;"));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>非 UTC 时间统一转 UTC；空白暂存名退化为 unknown 而非静默省略要素；null 文本兜底为空串。</zh-CN>
    ///   <en>Non-UTC times convert to UTC; a blank staging name degrades to unknown rather than silently dropping the element; null text falls back to empty.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Format_NormalisesUtcAndDegradesBlankFields()
    {
        // <lang>
        //   <zh-CN>本地时间 +08:00 的 20:00 对应 UTC 12:00，验证 ToUniversalTime 生效。</zh-CN>
        //   <en>Local 20:00 at +08:00 maps to 12:00 UTC, verifying ToUniversalTime takes effect.</en>
        // </lang>
        DateTime local = new DateTime(2026, 11, 8, 20, 0, 0, DateTimeKind.Local);
        string summary = PortalOnlinePackageAuditFormatter.FormatSixElementSummary(
            string.Empty, null, null, null, local, FixedUtc, null, null);

        Assert.IsTrue(summary.StartsWith("pkg=unknown;hashBefore=;hashAfter=;actor=;", StringComparison.Ordinal));
        Assert.IsTrue(summary.Contains(";verification=;direction="));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>超过 500 字符时 Format 抛 AuditSummaryTooLongException，绝不截断；异常带在线包上下文便于归因。</zh-CN>
    ///   <en>Above 500 characters Format throws AuditSummaryTooLongException without truncation; the exception carries an online-package context for attribution.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Format_Overflow_ThrowsWithoutTruncating()
    {
        try
        {
            PortalOnlinePackageAuditFormatter.FormatSixElementSummary(
                "staged-1234.zip", string.Empty, Hash, new string('x', 600), FixedUtc, FixedUtc,
                "ok", PortalOnlinePackageAuditEvents.UploadValidated);
            Assert.Fail("超长时应抛出 AuditSummaryTooLongException；静默通过意味着存在静默截断风险。");
        }
        catch (AuditSummaryTooLongException exception)
        {
            Assert.AreEqual(PortalOnlinePackageAuditFormatter.MaxSummaryLength, exception.MaxLength);
            Assert.IsTrue(exception.ActualLength > PortalOnlinePackageAuditFormatter.MaxSummaryLength);
            Assert.IsTrue(exception.Message.Contains("Online-package"), "诊断消息应标明在线包上下文");
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>TryFormat 在正常时与 Format 一致；超长时返回 false 且 summary 为 null，不吞掉其它异常。</zh-CN>
    ///   <en>TryFormat matches Format on success; on overflow returns false with a null summary and swallows no other exception type.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryFormat_DegradesOverflowToFalse()
    {
        string normal;
        Assert.IsTrue(PortalOnlinePackageAuditFormatter.TryFormatSixElementSummary(
            "staged-1234.zip", string.Empty, Hash, "admin", FixedUtc, FixedUtc, "ok",
            PortalOnlinePackageAuditEvents.InstallConfirmed, out normal));
        Assert.AreEqual(
            PortalOnlinePackageAuditFormatter.FormatSixElementSummary(
                "staged-1234.zip", string.Empty, Hash, "admin", FixedUtc, FixedUtc, "ok",
                PortalOnlinePackageAuditEvents.InstallConfirmed),
            normal);

        string overflowed;
        Assert.IsFalse(PortalOnlinePackageAuditFormatter.TryFormatSixElementSummary(
            "staged-1234.zip", string.Empty, Hash, new string('x', 600), FixedUtc, FixedUtc, "ok",
            PortalOnlinePackageAuditEvents.InstallConfirmed, out overflowed));
        Assert.IsNull(overflowed);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>动作与结果都为封闭白名单；未知键、装配态动作与伪造结果一律拒绝，防止自由文本进入审计列。</zh-CN>
    ///   <en>Actions and outcomes are closed whitelists; unknown keys, assembly-state actions, and forged outcomes are refused so free text cannot enter audit columns.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void RegisteredActionsAndOutcomes_AreClosedWhitelists()
    {
        Assert.IsTrue(PortalOnlinePackageAuditFormatter.IsRegisteredAction(PortalOnlinePackageAuditEvents.UploadValidated));
        Assert.IsTrue(PortalOnlinePackageAuditFormatter.IsRegisteredAction(PortalOnlinePackageAuditEvents.InstallConfirmed));
        Assert.IsTrue(PortalOnlinePackageAuditFormatter.IsRegisteredAction(PortalOnlinePackageAuditEvents.StagedPackageDiscarded));
        Assert.IsTrue(PortalOnlinePackageAuditFormatter.IsRegisteredAction(PortalOnlinePackageAuditEvents.RollbackDrill));

        string[] badActions = { null, "", "FreeText", "AssemblyStateCheck", "Module.OnlinePackage.Upload", "Admins" };
        foreach (string bad in badActions)
        {
            Assert.IsFalse(PortalOnlinePackageAuditFormatter.IsRegisteredAction(bad), "应拒绝动作: " + bad);
        }

        Assert.IsTrue(PortalOnlinePackageAuditFormatter.IsRegisteredOutcome(PortalOnlinePackageAuditEvents.OutcomeSuccess));
        Assert.IsTrue(PortalOnlinePackageAuditFormatter.IsRegisteredOutcome(PortalOnlinePackageAuditEvents.OutcomeFailure));
        Assert.IsTrue(PortalOnlinePackageAuditFormatter.IsRegisteredOutcome(PortalOnlinePackageAuditEvents.OutcomeSkipped));

        string[] badOutcomes = { null, "", "Succeeded", "Error", "success" };
        foreach (string bad in badOutcomes)
        {
            Assert.IsFalse(PortalOnlinePackageAuditFormatter.IsRegisteredOutcome(bad), "应拒绝结果: " + bad);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>既有两参数构造仍抛装配态措辞的异常（向后兼容），三参数构造支持在线包上下文。</zh-CN>
    ///   <en>The existing two-argument constructor still throws with assembly-state wording (backward compatible); the three-argument one supports an online-package context.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void OverflowException_RemainsBackwardCompatible()
    {
        AuditSummaryTooLongException legacy = new AuditSummaryTooLongException(600, 500);
        Assert.AreEqual(600, legacy.ActualLength);
        Assert.AreEqual(500, legacy.MaxLength);
        Assert.IsTrue(legacy.Message.Contains("Assembly-state"));

        AuditSummaryTooLongException online = new AuditSummaryTooLongException(600, 500, "Online-package");
        Assert.IsTrue(online.Message.Contains("Online-package"));
        Assert.AreEqual(500, online.MaxLength);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>静态源码断言：运行时包装必须经 `PortalOperationAudit.Record` 写入并显式传 outcome，
    ///   且**不得**出现 SQL 拼接、连接构造、隔离暂存文件读取或装配态/包启用写入（工具不代写的机器证据）。</zh-CN>
    ///   <en>Static source assertions: the runtime wrapper must write through `PortalOperationAudit.Record` and pass outcome explicitly,
    ///   and must **not** contain SQL concatenation, connection construction, staging-file reads, or assembly-state/package-enable writes (machine evidence that tools never write).</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void RuntimeWrapper_WritesOnlyThroughOperationAuditAndToolsNeverWrite()
    {
        // <lang>
        //   <zh-CN>从测试程序集目录向上回溯到仓库根；沿用 PortalContainerLifetimeTests 的 BaseDirectory 做法。</zh-CN>
        //   <en>Walk up from the test assembly directory to the repository root, following the BaseDirectory approach in PortalContainerLifetimeTests.</en>
        // </lang>
        DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        string repositoryRoot = null;
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "master.sln")))
            {
                repositoryRoot = directory.FullName;
                break;
            }
            directory = directory.Parent;
        }
        Assert.IsNotNull(repositoryRoot, "无法定位仓库根（src/master.sln）");

        string wrapperPath = Path.Combine(repositoryRoot, "src", "Portal", "Components", "PortalOnlinePackageAudit.cs");
        Assert.IsTrue(File.Exists(wrapperPath), "运行时包装文件缺失");
        string wrapper = StripComments(File.ReadAllText(wrapperPath));

        Assert.IsTrue(wrapper.Contains("PortalOperationAudit.Record("), "必须经统一审计入口写入");
        Assert.IsTrue(wrapper.Contains("outcome"), "必须显式传入真实结果，不伪造成功");
        Assert.IsFalse(wrapper.Contains("INSERT INTO"), "不得在包装内拼 SQL");
        Assert.IsFalse(wrapper.Contains("SqlConnection"), "不得在包装内构造连接");
        Assert.IsFalse(wrapper.Contains("PortalModuleAssemblyState"), "不得读取装配态台账");
        Assert.IsFalse(wrapper.Contains("PortalModulePackageStates"), "不得写包启用状态");
        Assert.IsFalse(wrapper.Contains("AntiForgeryToken"), "防伪令牌不得进入审计摘要");

        // <lang>
        //   <zh-CN>Components 层的事件与格式器文件只能定义常量与纯函数，不得出现任何写入入口。</zh-CN>
        //   <en>The Components-layer event and formatter files may only define constants and pure functions, with no write entry point.</en>
        // </lang>
        string[] pureFiles =
        {
            Path.Combine(repositoryRoot, "src", "Portal.Components", "PortalOnlinePackageAuditEvents.cs"),
            Path.Combine(repositoryRoot, "src", "Portal.Components", "PortalOnlinePackageAuditFormatter.cs")
        };
        foreach (string purePath in pureFiles)
        {
            Assert.IsTrue(File.Exists(purePath), "Components 文件缺失: " + purePath);
            string pure = StripComments(File.ReadAllText(purePath));
            Assert.IsFalse(pure.Contains("PortalOperationAudit.Record"), "Components 层不得写入审计: " + purePath);
            Assert.IsFalse(pure.Contains("SqlConnection"), "Components 层不得连接数据库: " + purePath);
            Assert.IsFalse(pure.Contains("INSERT INTO"), "Components 层不得生成 SQL: " + purePath);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>剥离 C# 注释后再做静态断言：XML 文档注释里会合法地提到「统一入口」等词，
    ///   若不剥离会把注释误判为写入代码（本项目既有门禁同样采用「注释提及不计」口径）。仅剔除整行注释，
    ///   保留全部可执行语句，断言强度不降低。</zh-CN>
    ///   <en>Strips C# comments before static assertions: XML documentation legitimately mentions the "unified entry point",
    ///   and without stripping, comments would be mistaken for write code (existing gates use the same "comment mentions do not count" rule).
    ///   Only whole-line comments are removed; every executable statement remains, so assertion strength is unchanged.</en>
    /// </lang>
    /// </summary>
    /// <param name="source"><l zh-CN="源文件内容。" en="Source file content." /></param>
    /// <returns><l zh-CN="不含整行注释的代码文本。" en="Code text without whole-line comments." /></returns>
    private static string StripComments(string source)
    {
        // <lang>
        //   <zh-CN>承接逐行筛出的代码行；用不变区域性拼接换行，避免平台差异影响断言。</zh-CN>
        //   <en>Holds code lines filtered one by one; lines are joined with an invariant newline so platform differences cannot affect assertions.</en>
        // </lang>
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        foreach (string line in source.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            // <lang>
            //   <zh-CN>跳过 /// 与 // 开头的整行注释；`<!--`/`<lang>` 内容均位于这些行内，一并被剔除。</zh-CN>
            //   <en>Skip whole-line comments starting with /// or //; `<!--`/`<lang>` content sits inside those lines and is removed too.</en>
            // </lang>
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }
            builder.AppendLine(line);
        }
        return builder.ToString();
    }
}
