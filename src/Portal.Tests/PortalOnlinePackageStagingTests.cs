using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using ASPNET.StarterKit.Portal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <lang>
///   <zh-CN>在线能力包隔离暂存器测试：成功解压、Zip Slip 拒绝、危险类型拒绝、清单缺失拒绝、条目数上限拒绝与失败零残留。</zh-CN>
///   <en>Tests for the isolated staging class: successful extraction, Zip Slip rejection, dangerous-type rejection, missing-manifest rejection, entry-count rejection, and zero residue on failure.</en>
/// </lang>
/// </summary>
/// <remarks>
/// <lang>
///   <zh-CN>夹具用 .NET Framework 4.8 自带的 ZipArchive 在磁盘临时目录构造真实 ZIP，不使用任何第三方测试依赖；
///   每个测试自建临时包与隔离根并在 finally 中清理，避免污染仓库与互相干扰。</zh-CN>
///   <en>Fixtures are real ZIPs built with the .NET Framework 4.8 ZipArchive in a temporary directory, with no third-party test dependency;
///   each test creates its own archive and isolation root and cleans them up in a finally block, so neither the repository nor other tests are polluted.</en>
/// </lang>
/// </remarks>
[TestClass]
public class PortalOnlinePackageStagingTests
{
    /// <summary><lang><zh-CN>当前测试创建的全部临时包文件路径，供 finally 逐一清理。</zh-CN><en>All temporary archive paths created by the current test, each removed by the finally-equivalent cleanup.</en></lang></summary>
    private readonly System.Collections.Generic.List<string> _archivePaths = new System.Collections.Generic.List<string>();

    /// <summary><lang><zh-CN>当前测试使用的隔离根目录。</zh-CN><en>Isolation root used by the current test.</en></lang></summary>
    private string _isolationRoot;

    /// <summary>
    /// <lang>
    ///   <zh-CN>为每个测试创建独立的临时隔离根；路径为绝对路径，满足暂存器输入要求。</zh-CN>
    ///   <en>Creates a distinct temporary isolation root per test; its absolute path satisfies the staging input requirement.</en>
    /// </lang>
    /// </summary>
    [TestInitialize]
    public void SetUp()
    {
        _isolationRoot = Path.Combine(Path.GetTempPath(), "p276-root-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_isolationRoot);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>删除本测试创建的包与隔离根，清理失败不影响测试结论。</zh-CN>
    ///   <en>Deletes the archive and isolation root created by this test; a cleanup failure does not change the test verdict.</en>
    /// </lang>
    /// </summary>
    [TestCleanup]
    public void TearDown()
    {
        try
        {
            foreach (string archive in _archivePaths)
            {
                if (File.Exists(archive))
                {
                    File.Delete(archive);
                }
            }
            if (!string.IsNullOrEmpty(_isolationRoot) && Directory.Exists(_isolationRoot))
            {
                Directory.Delete(_isolationRoot, true);
            }
        }
        catch (IOException)
        {
            // <lang>
            //   <zh-CN>临时目录清理失败不改变断言结果；由操作系统临时目录策略最终回收。</zh-CN>
            //   <en>A failed temporary-directory cleanup does not change assertions; the OS temp policy eventually reclaims it.</en>
            // </lang>
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>在临时目录构造一个真实 ZIP 包；每个条目写入短文本，条目名可为穿越或危险形态。</zh-CN>
    ///   <en>Builds a real ZIP in the temp directory; each entry receives short text and may carry a traversal or dangerous name.</en>
    /// </lang>
    /// </summary>
    /// <param name="entryNames"><l zh-CN="要写入的 ZIP 条目名序列。" en="ZIP entry names to write." /></param>
    /// <returns><l zh-CN="压缩包的绝对路径。" en="Absolute path of the compressed archive." /></returns>
    private string CreateArchive(params string[] entryNames)
    {
        // <lang>
        //   <zh-CN>使用 CreateNew 确保每次构造新文件，不覆盖既有夹具。</zh-CN>
        //   <en>CreateNew guarantees a fresh file per fixture and never overwrites an existing one.</en>
        // </lang>
        string path = Path.Combine(Path.GetTempPath(), "p276-pack-" + Guid.NewGuid().ToString("N") + ".zip");
        using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, false))
        {
            foreach (string entryName in entryNames)
            {
                // <lang>
                //   <zh-CN>ZipArchive.CreateEntry 不校验条目名，因此可构造穿越与危险类型夹具来验证暂存器。</zh-CN>
                //   <en>ZipArchive.CreateEntry does not validate names, so traversal and dangerous-type fixtures can be built to exercise the staging class.</en>
                // </lang>
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
                byte[] payload = Encoding.UTF8.GetBytes("fixture");
                using (Stream entryStream = entry.Open())
                {
                    entryStream.Write(payload, 0, payload.Length);
                }
            }
        }
        _archivePaths.Add(path);
        return path;
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>断言隔离根下没有任何隔离子目录残留（失败隔离的可机检证据）。</zh-CN>
    ///   <en>Asserts no isolated subdirectory remains under the isolation root (machine-checkable evidence of failure isolation).</en>
    /// </lang>
    /// </summary>
    private void AssertNoResidue()
    {
        Assert.AreEqual(0, Directory.GetDirectories(_isolationRoot).Length, "失败后不应留下隔离子目录");
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>含清单与静态资源的合法包成功解压到全新隔离子目录，清单文件真实存在。</zh-CN>
    ///   <en>A valid package with a manifest and a static resource extracts into a fresh isolated subdirectory and the manifest really exists.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryStage_AcceptsValidPackage()
    {
        string archive = CreateArchive("module.json", "Styles/theme.css", "Images/logo.png");

        string extracted;
        string reason;
        Assert.IsTrue(PortalOnlinePackageStaging.TryStage(archive, _isolationRoot, out extracted, out reason), reason);
        Assert.AreEqual(string.Empty, reason);
        Assert.IsTrue(Directory.Exists(extracted));
        Assert.IsTrue(File.Exists(Path.Combine(extracted, "module.json")));
        Assert.IsTrue(File.Exists(Path.Combine(extracted, "Styles", "theme.css")));
        Assert.IsTrue(extracted.StartsWith(Path.GetFullPath(_isolationRoot), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>父级跳转（Zip Slip）条目被拒绝，隔离目录整体清理，不写入隔离根之外的任何文件。</zh-CN>
    ///   <en>A parent-traversal (Zip Slip) entry is rejected, the isolated directory is removed wholesale, and nothing is written outside the isolation root.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryStage_RejectsZipSlipAndCleansUp()
    {
        string archive = CreateArchive("module.json", "../../evil.txt");

        string extracted;
        string reason;
        Assert.IsFalse(PortalOnlinePackageStaging.TryStage(archive, _isolationRoot, out extracted, out reason));
        Assert.IsNull(extracted);
        Assert.IsFalse(string.IsNullOrEmpty(reason));
        AssertNoResidue();
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>包内携带 Web.config 等危险类型时拒绝，且不留半解压内容（防 per-directory 配置覆盖）。</zh-CN>
    ///   <en>A package carrying Web.config or other dangerous types is rejected with no partially extracted content (preventing per-directory config overrides).</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryStage_RejectsDangerousEntryTypeAndCleansUp()
    {
        string archive = CreateArchive("module.json", "Web.config");

        string extracted;
        string reason;
        Assert.IsFalse(PortalOnlinePackageStaging.TryStage(archive, _isolationRoot, out extracted, out reason));
        Assert.IsFalse(string.IsNullOrEmpty(reason));
        AssertNoResidue();
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>清单缺失的包即使条目全部合法也拒绝，避免「解压成功」被误当作「可进入安装流程」。</zh-CN>
    ///   <en>A package without a manifest is rejected even when every entry is valid, so extraction success is not mistaken for installation readiness.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryStage_RejectsMissingManifestAndCleansUp()
    {
        string archive = CreateArchive("Styles/theme.css", "README.md");

        string extracted;
        string reason;
        Assert.IsFalse(PortalOnlinePackageStaging.TryStage(archive, _isolationRoot, out extracted, out reason));
        Assert.IsFalse(string.IsNullOrEmpty(reason));
        AssertNoResidue();
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>深层同名 module.json 不被当作根目录清单，防止把嵌套文件冒充包清单。</zh-CN>
    ///   <en>A same-named module.json deep in the tree is not accepted as the root manifest, preventing a nested file from impersonating it.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryStage_RejectsNestedManifestOnly()
    {
        string archive = CreateArchive("Nested/module.json");

        string extracted;
        string reason;
        Assert.IsFalse(PortalOnlinePackageStaging.TryStage(archive, _isolationRoot, out extracted, out reason));
        AssertNoResidue();
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>超过条目数上限的包在解压前被拒绝（zip bomb 预算接线），且无残留。</zh-CN>
    ///   <en>A package above the entry-count cap is rejected before extraction (zip-bomb budget wiring), with no residue.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryStage_RejectsExcessiveEntryCountAndCleansUp()
    {
        // <lang>
        //   <zh-CN>构造刚好超过上限的条目名数组，全部写入极小内容，避免测试依赖大内存。</zh-CN>
        //   <en>Build just over the cap with tiny payloads so the test needs no large memory allocation.</en>
        // </lang>
        string[] names = new string[PortalOnlinePackagePolicy.MaximumEntryCount + 1];
        names[0] = "module.json";
        for (int i = 1; i < names.Length; i++)
        {
            names[i] = "Assets/f" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".txt";
        }
        string archive = CreateArchive(names);

        string extracted;
        string reason;
        Assert.IsFalse(PortalOnlinePackageStaging.TryStage(archive, _isolationRoot, out extracted, out reason));
        Assert.IsFalse(string.IsNullOrEmpty(reason));
        AssertNoResidue();
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>非 ZIP 内容、相对隔离根、缺失包路径等输入一律拒绝且不抛异常到调用方。</zh-CN>
    ///   <en>Non-ZIP content, a relative isolation root, and a missing archive path are all rejected without throwing to the caller.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryStage_RejectsMalformedInputsWithoutThrowing()
    {
        string extracted;
        string reason;

        // <lang>
        //   <zh-CN>写入非 ZIP 字节，验证 InvalidDataException 被收敛为稳定失败短语。</zh-CN>
        //   <en>Write non-ZIP bytes to verify InvalidDataException converges into a stable failure phrase.</en>
        // </lang>
        string malformed = Path.Combine(Path.GetTempPath(), "p276-bad-" + Guid.NewGuid().ToString("N") + ".zip");
        File.WriteAllText(malformed, "not a zip");
        _archivePaths.Add(malformed);
        Assert.IsFalse(PortalOnlinePackageStaging.TryStage(malformed, _isolationRoot, out extracted, out reason));
        Assert.IsFalse(string.IsNullOrEmpty(reason));
        AssertNoResidue();

        string validArchive = CreateArchive("module.json");
        Assert.IsFalse(PortalOnlinePackageStaging.TryStage(validArchive, "relative/root", out extracted, out reason));
        Assert.IsFalse(PortalOnlinePackageStaging.TryStage(Path.Combine(Path.GetTempPath(), "p276-absent-" + Guid.NewGuid().ToString("N") + ".zip"),
                                                          _isolationRoot, out extracted, out reason));
        Assert.IsFalse(PortalOnlinePackageStaging.TryStage(null, _isolationRoot, out extracted, out reason));
        AssertNoResidue();
    }
}
