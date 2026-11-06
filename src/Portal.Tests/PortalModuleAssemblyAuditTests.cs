using ASPNET.StarterKit.Portal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

/// <summary>
/// <lang>
///   <zh-CN>PortalModuleAssemblyAuditFormatter 的六要素格式器测试。重点是「不得静默截断」的 fail-closed 行为，
///   以及 Summary 字段顺序与离线工具 Invoke-PortalModuleAssemblyCheck.ps1 同源。</zh-CN>
///   <en>Tests for PortalModuleAssemblyAuditFormatter's six-element formatter. Focus: the fail-closed "never silently truncate"
///   behaviour and that the Summary field order matches the offline tool Invoke-PortalModuleAssemblyCheck.ps1.</en>
/// </lang>
/// </summary>
[TestClass]
public class PortalModuleAssemblyAuditTests
{
    private static readonly DateTime FixedUtc = new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc);
    private const string BeforeHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string AfterHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    /// <summary>
    /// <lang>
    ///   <zh-CN>正常输入生成紧凑格式且不超过 Summary 上限；字段顺序固定为 pkg/hashBefore/hashAfter/actor/startUtc/endUtc/verification/direction。</zh-CN>
    ///   <en>Normal input yields the compact format within the Summary limit; field order is fixed as pkg/hashBefore/hashAfter/actor/startUtc/endUtc/verification/direction.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Format_SixElements_WithinLimitAndFixedOrder()
    {
        string summary = PortalModuleAssemblyAuditFormatter.FormatSixElementSummary(
            "HIA.Help", "1.2.3", BeforeHash, AfterHash, "deployment-offline-check",
            FixedUtc, FixedUtc, "V1-V7 passed", "CheckOnly");

        // <lang>
        //   <zh-CN>预期字符串与离线工具同口径：targetId=packageId@version，时间用秒级 ISO 8601。</zh-CN>
        //   <en>Expected string shares the offline tool's contract: targetId=packageId@version and second-precision ISO 8601 time.</en>
        // </lang>
        string expected = "pkg=HIA.Help@1.2.3;hashBefore=" + BeforeHash +
                          ";hashAfter=" + AfterHash +
                          ";actor=deployment-offline-check;startUtc=2026-11-07T00:00:00Z;endUtc=2026-11-07T00:00:00Z;verification=V1-V7 passed;direction=CheckOnly";
        Assert.AreEqual(expected, summary);
        Assert.IsTrue(summary.Length <= PortalModuleAssemblyAuditFormatter.MaxSummaryLength);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>version 为空时 targetId 退化为 packageId@unknown，而非省略装配态事实（避免静默丢要素）。</zh-CN>
    ///   <en>When version is blank, targetId degrades to packageId@unknown instead of omitting the assembly-state fact (avoids silently dropping an element).</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Format_BlankVersion_DegradesToUnknownNotOmitted()
    {
        string summary = PortalModuleAssemblyAuditFormatter.FormatSixElementSummary(
            "HIA.Help", string.Empty, BeforeHash, AfterHash, "actor",
            FixedUtc, FixedUtc, "ok", "CheckOnly");

        Assert.IsTrue(summary.StartsWith("pkg=HIA.Help@unknown;", StringComparison.Ordinal));
        Assert.IsTrue(summary.Length <= PortalModuleAssemblyAuditFormatter.MaxSummaryLength);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>超过 500 字符时 Format 抛出 AuditSummaryTooLongException，绝不截断；这是「不得静默截断」的硬性保证。</zh-CN>
    ///   <en>Above 500 characters Format throws AuditSummaryTooLongException rather than truncating; this is the hard guarantee behind "never silently truncate".</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Format_Overflow_ThrowsDoNotTruncate()
    {
        // <lang>
        //   <zh-CN>用超长 actor 把摘要推过 500 字符；null 参数也要兜底为空串，不能靠外部约束。当前 MSTest 版本无 ExpectedException 特性，改用 try/catch 断言。</zh-CN>
        //   <en>An over-long actor pushes the summary past 500; null arguments must also fall back to empty, not rely on external guarantees. The current MSTest build lacks ExpectedException, so assert via try/catch.</en>
        // </lang>
        try
        {
            PortalModuleAssemblyAuditFormatter.FormatSixElementSummary(
                "HIA.Help", "1.2.3", BeforeHash, AfterHash, new string('x', 600),
                FixedUtc, FixedUtc, "ok", "CheckOnly");
            Assert.Fail("超长时应抛出 AuditSummaryTooLongException，但未被抛出（静默通过意味着静默截断风险）。");
        }
        catch (AuditSummaryTooLongException)
        {
            // <lang>
            //   <zh-CN>命中预期异常，证明未走截断路径。</zh-CN>
            //   <en>The expected exception was caught, proving the truncation path was not taken.</en>
            // </lang>
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>TryFormat 在超长时返回 false 且 summary 为 null，给调用方一个显式分支去走审计表扩展方案，而非异常或截断。</zh-CN>
    ///   <en>TryFormat returns false with a null summary on overflow, giving the caller an explicit branch to the audit-table extension path instead of an exception or truncation.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryFormat_Overflow_ReturnsFalseAndNull()
    {
        string summary;
        bool ok = PortalModuleAssemblyAuditFormatter.TryFormatSixElementSummary(
            "HIA.Help", "1.2.3", BeforeHash, AfterHash, new string('y', 600),
            FixedUtc, FixedUtc, "ok", "CheckOnly", out summary);

        Assert.IsFalse(ok);
        Assert.IsNull(summary);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>正常输入经 TryFormat 成功且结果与 Format 一致，证明两条路径语义等价。</zh-CN>
    ///   <en>Normal input succeeds via TryFormat and matches Format, proving the two paths are semantically equivalent.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryFormat_Success_MatchesFormat()
    {
        string viaFormat = PortalModuleAssemblyAuditFormatter.FormatSixElementSummary(
            "HIA.Help", "1.2.3", BeforeHash, AfterHash, "actor", FixedUtc, FixedUtc, "ok", "CheckOnly");
        string viaTry;
        bool ok = PortalModuleAssemblyAuditFormatter.TryFormatSixElementSummary(
            "HIA.Help", "1.2.3", BeforeHash, AfterHash, "actor", FixedUtc, FixedUtc, "ok", "CheckOnly", out viaTry);

        Assert.IsTrue(ok);
        Assert.AreEqual(viaFormat, viaTry);
    }
}
