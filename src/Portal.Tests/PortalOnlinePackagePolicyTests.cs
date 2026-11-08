using System;
using System.Collections.Generic;
using ASPNET.StarterKit.Portal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <lang>
///   <zh-CN>在线能力包隔离上传策略测试：文件白名单、体积上限、条目名穿越拒绝、条目类型白名单、解压预算与服务端口生成存储名。</zh-CN>
///   <en>Tests for the isolated-upload policy: file allow-list, size caps, entry-name traversal rejection, entry-type allow-list, decompressed budget, and server-generated storage names.</en>
/// </lang>
/// </summary>
[TestClass]
public class PortalOnlinePackagePolicyTests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>合法 .zip 文件在体积上限内通过；输出原因保持为空。</zh-CN>
    ///   <en>A valid .zip within the size cap passes and the reason stays empty.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void ValidatePackageFile_AcceptsZipWithinLimit()
    {
        string reason;
        Assert.IsTrue(PortalOnlinePackagePolicy.TryValidatePackageFile("collaboration-pack.zip", 1024, out reason));
        Assert.AreEqual(string.Empty, reason);
        Assert.IsTrue(PortalOnlinePackagePolicy.TryValidatePackageFile("pack.ZIP", PortalOnlinePackagePolicy.MaximumPackageBytes, out reason));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>超限、零字节与负体积都拒绝，且拒绝原因不回显原始文件名。</zh-CN>
    ///   <en>Over-limit, zero-byte, and negative sizes are rejected, and no rejection reason echoes the raw file name.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void ValidatePackageFile_RejectsBadSizes()
    {
        string reason;
        Assert.IsFalse(PortalOnlinePackagePolicy.TryValidatePackageFile("secret-name.zip", PortalOnlinePackagePolicy.MaximumPackageBytes + 1, out reason));
        Assert.IsFalse(reason.Contains("secret-name"));
        Assert.IsFalse(PortalOnlinePackagePolicy.TryValidatePackageFile("pack.zip", 0, out reason));
        Assert.IsFalse(PortalOnlinePackagePolicy.TryValidatePackageFile("pack.zip", -1, out reason));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>非 .zip、双扩展名、空名、含冒号（NTFS 备用数据流）与含路径分隔符的文件名都拒绝。</zh-CN>
    ///   <en>Non-.zip, double-extension, blank, colon-bearing (NTFS alternate data stream), and path-separator file names are all rejected.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void ValidatePackageFile_RejectsDisallowedNames()
    {
        string[] rejected =
        {
            "pack.exe", "pack.zip.aspx", "pack", "", "pack.zip:stream",
            "../pack.zip", "folder\\pack.zip", "pack?.zip", "pack*.zip"
        };

        foreach (string name in rejected)
        {
            string reason;
            Assert.IsFalse(PortalOnlinePackagePolicy.TryValidatePackageFile(name, 2048, out reason), "应拒绝: " + name);
            Assert.IsFalse(string.IsNullOrEmpty(reason));
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>合法相对条目名通过；目录条目以 '/' 结尾亦通过。</zh-CN>
    ///   <en>Valid relative entry names pass; directory entries ending in '/' also pass.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void EntryName_AcceptsRelativePaths()
    {
        string reason;
        Assert.IsTrue(PortalOnlinePackagePolicy.IsAllowedEntryName("module.json", out reason));
        Assert.IsTrue(PortalOnlinePackagePolicy.IsAllowedEntryName("Styles/theme.css", out reason));
        Assert.IsTrue(PortalOnlinePackagePolicy.IsAllowedEntryName("Styles/", out reason));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>父级跳转、盘符、冒号、控制字符、前导点隐藏文件与尾随点都被拒绝（名字层 Zip Slip 防护）。</zh-CN>
    ///   <en>Parent traversal, drive prefixes, colons, control characters, leading-dot hidden files, and trailing dots are rejected (name-level Zip Slip defence).</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void EntryName_RejectsTraversalAndUnsafeShapes()
    {
        string[] rejected =
        {
            "../Web.config", "a/../../evil.txt", "/absolute/file.txt", "C:\\evil.txt",
            "file.txt:$DATA", ".hidden", "folder/.", "trailing.", "a\\..\\b.txt", "a\0b.txt", ""
        };

        foreach (string name in rejected)
        {
            string reason;
            Assert.IsFalse(PortalOnlinePackagePolicy.IsAllowedEntryName(name, out reason), "应拒绝: " + name);
            Assert.IsFalse(string.IsNullOrEmpty(reason));
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>清单、静态资源、用户控件与文本类型允许；.config/.aspx/.dll/.js/.zip 等危险或嵌套类型拒绝。</zh-CN>
    ///   <en>Manifest, static resources, user controls, and text are allowed; dangerous or nested types such as .config/.aspx/.dll/.js/.zip are rejected.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void EntryExtension_EnforcesHardAllowList()
    {
        string[] allowed =
        {
            "module.json", "Styles/theme.css", "Images/logo.png", "Widget.ascx", "README.md", "LICENSE.txt"
        };
        foreach (string name in allowed)
        {
            string reason;
            Assert.IsTrue(PortalOnlinePackagePolicy.IsAllowedEntryExtension(name, out reason), "应允许: " + name);
        }

        string[] rejected =
        {
            "Web.config", "Default.aspx", "bin/evil.dll", "script.js", "nested.zip", "noextension", "pack.zip.aspx"
        };
        foreach (string name in rejected)
        {
            string reason;
            Assert.IsFalse(PortalOnlinePackagePolicy.IsAllowedEntryExtension(name, out reason), "应拒绝: " + name);
            Assert.IsFalse(string.IsNullOrEmpty(reason));
        }

        string directoryReason;
        Assert.IsTrue(PortalOnlinePackagePolicy.IsAllowedEntryExtension("Styles/", out directoryReason));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>条目数、单条目上限与解压后累计上限都在解压前拒绝，且不发生溢出回绕。</zh-CN>
    ///   <en>Entry count, per-entry cap, and cumulative decompressed cap are all rejected before extraction, with no overflow wrap-around.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void DecompressedBudget_RejectsBombs()
    {
        string reason;

        Assert.IsTrue(PortalOnlinePackagePolicy.TryValidateDecompressedBudget(new List<long> { 100L, 200L }, out reason));

        // <lang>
        //   <zh-CN>空条目列表不可能是合法包。</zh-CN>
        //   <en>An empty entry list cannot be a valid package.</en>
        // </lang>
        Assert.IsFalse(PortalOnlinePackagePolicy.TryValidateDecompressedBudget(new List<long>(), out reason));
        Assert.IsFalse(PortalOnlinePackagePolicy.TryValidateDecompressedBudget(null, out reason));

        // <lang>
        //   <zh-CN>条目数超过硬上限。</zh-CN>
        //   <en>Entry count exceeds the hard cap.</en>
        // </lang>
        List<long> tooMany = new List<long>();
        for (int i = 0; i <= PortalOnlinePackagePolicy.MaximumEntryCount; i++)
        {
            tooMany.Add(1L);
        }
        Assert.IsFalse(PortalOnlinePackagePolicy.TryValidateDecompressedBudget(tooMany, out reason));

        // <lang>
        //   <zh-CN>单条目声明长度超过上限。</zh-CN>
        //   <en>One entry declares a length above the per-entry cap.</en>
        // </lang>
        Assert.IsFalse(PortalOnlinePackagePolicy.TryValidateDecompressedBudget(
            new List<long> { PortalOnlinePackagePolicy.MaximumSingleEntryBytes + 1L }, out reason));

        // <lang>
        //   <zh-CN>多个合法单条目累加后超过总量上限。</zh-CN>
        //   <en>Several individually valid entries exceed the total cap in aggregate.</en>
        // </lang>
        List<long> oversizedTotal = new List<long>();
        long chunk = PortalOnlinePackagePolicy.MaximumSingleEntryBytes;
        for (int i = 0; i < 5; i++)
        {
            oversizedTotal.Add(chunk);
        }
        Assert.IsFalse(PortalOnlinePackagePolicy.TryValidateDecompressedBudget(oversizedTotal, out reason));

        // <lang>
        //   <zh-CN>checked 加法下溢出必须成为失败而不是回绕成小数。</zh-CN>
        //   <en>Overflow under checked addition must fail rather than wrap to a small number.</en>
        // </lang>
        Assert.IsFalse(PortalOnlinePackagePolicy.TryValidateDecompressedBudget(
            new List<long> { long.MaxValue, long.MaxValue }, out reason));

        Assert.IsFalse(PortalOnlinePackagePolicy.TryValidateDecompressedBudget(new List<long> { -1L }, out reason));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>服务端生成的存储名与隔离子目录名不依赖用户输入，且多次调用互不相同。</zh-CN>
    ///   <en>Server-generated storage and isolated-directory names depend on no user input and differ across calls.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void GeneratedNames_AreServerControlledAndUnique()
    {
        string first = PortalOnlinePackagePolicy.CreateStorageFileName();
        string second = PortalOnlinePackagePolicy.CreateStorageFileName();

        Assert.AreNotEqual(first, second);
        Assert.IsTrue(first.EndsWith(PortalOnlinePackagePolicy.PackageExtension, StringComparison.Ordinal));
        Assert.IsFalse(first.Contains("/"));
        Assert.IsFalse(first.Contains("\\"));
        Assert.IsFalse(first.Contains(":"));

        Assert.AreNotEqual(PortalOnlinePackagePolicy.CreateIsolatedDirectoryName(),
                           PortalOnlinePackagePolicy.CreateIsolatedDirectoryName());
        Assert.IsTrue(PortalOnlinePackagePolicy.CreateIsolatedDirectoryName().StartsWith("pkg-", StringComparison.Ordinal));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>Content-Type 只作提示：大小写不敏感匹配，但 null 与伪造类型都返回 false，不被当作安全依据。</zh-CN>
    ///   <en>Content-Type is a hint only: case-insensitive match, while null and spoofed types return false and are never a security basis.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void ContentType_IsHintOnly()
    {
        Assert.IsTrue(PortalOnlinePackagePolicy.MatchesDeclaredContentType("application/zip"));
        Assert.IsTrue(PortalOnlinePackagePolicy.MatchesDeclaredContentType("APPLICATION/ZIP"));
        Assert.IsFalse(PortalOnlinePackagePolicy.MatchesDeclaredContentType(null));
        Assert.IsFalse(PortalOnlinePackagePolicy.MatchesDeclaredContentType("application/x-msdownload"));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>常量与预设计稿中对管理员可见的约束一致（10 MiB / 200 条目 / 64 MiB 解压后），防止设计与实现漂移。</zh-CN>
    ///   <en>Constants match the constraints shown to administrators in the prototype (10 MiB / 200 entries / 64 MiB decompressed), preventing design-implementation drift.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Limits_MatchPublishedDesignConstraints()
    {
        Assert.AreEqual(10485760, PortalOnlinePackagePolicy.MaximumPackageBytes);
        Assert.AreEqual(200, PortalOnlinePackagePolicy.MaximumEntryCount);
        Assert.AreEqual(67108864L, PortalOnlinePackagePolicy.MaximumTotalDecompressedBytes);
        Assert.AreEqual(".zip", PortalOnlinePackagePolicy.PackageExtension);
        Assert.AreEqual("~/App_Data/OnlinePackages/Staging", PortalOnlinePackagePolicy.StagingVirtualDirectory);
    }
}
