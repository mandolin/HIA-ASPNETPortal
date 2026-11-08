using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ASPNET.StarterKit.Portal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <lang>
///   <zh-CN>在线能力包安全闸门测试：SHA-256 完整性、RSA-SHA256 真实验签、扫描器 fail-closed 降级，
///   以及「白名单 + 完整性 + 签名 + 扫描」四道判据的联合判定与各拒真场景。</zh-CN>
///   <en>Tests for the online package security gate: SHA-256 integrity, real RSA-SHA256 verification, fail-closed scanner
///   degradation, and joint allow-list + integrity + signature + scan decisions with every rejection scenario.</en>
/// </lang>
/// </summary>
[TestClass]
public class PortalOnlinePackageSecurityTests
{
    /// <summary><lang><zh-CN>固定包内容，使哈希与签名断言可复现。</zh-CN><en>Fixed package content so hash and signature assertions are reproducible.</en></lang></summary>
    private static readonly byte[] PackageContent = Encoding.UTF8.GetBytes("HIA online capability package fixture v1");

    /// <summary><lang><zh-CN>测试用 2048 位 RSA 密钥对，仅在本测试进程内生成，不持久化、不入库。</zh-CN><en>Test-only 2048-bit RSA key pair generated in-process, never persisted or committed.</en></lang></summary>
    private static string _publicKeyXml;
    private static string _privateKeyXml;

    /// <summary>
    /// <lang>
    ///   <zh-CN>类初始化时生成一次性密钥对；若 4.8 加密 API 不可用，测试会以明确失败暴露而非静默跳过。</zh-CN>
    ///   <en>Generates a one-off key pair at class init; if a 4.8 crypto API is unavailable, tests fail explicitly rather than silently skipping.</en>
    /// </lang>
    /// </summary>
    [ClassInitialize]
    public static void CreateKeyPair(TestContext context)
    {
        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
        {
            try
            {
                _publicKeyXml = rsa.ToXmlString(false);
                _privateKeyXml = rsa.ToXmlString(true);
            }
            finally
            {
                rsa.PersistKeyInCsp = false;
            }
        }
    }

    /// <summary><lang><zh-CN>返回固定内容的独立副本，避免测试之间共享可变数组。</zh-CN><en>Returns an independent copy of the fixed content so tests share no mutable array.</en></lang></summary>
    private static byte[] Content()
    {
        return (byte[])PackageContent.Clone();
    }

    /// <summary><lang><zh-CN>返回固定内容的期望 SHA-256 摘要。</zh-CN><en>Returns the expected SHA-256 digest of the fixed content.</en></lang></summary>
    private static string ExpectedHash()
    {
        return PortalOnlinePackageIntegrity.ComputeSha256Hex(PackageContent);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>可配置的测试扫描器：返回指定判定或抛出指定异常，用于覆盖 Clean/Infected/Unavailable/异常。</zh-CN>
    ///   <en>Configurable test scanner returning a chosen verdict or throwing, covering Clean/Infected/Unavailable/exception.</en>
    /// </lang>
    /// </summary>
    private sealed class StubScanner : IPortalOnlinePackageScanner
    {
        public PortalOnlinePackageScanVerdict Verdict = PortalOnlinePackageScanVerdict.Clean;
        public bool ThrowOnScan;
        public int CallCount;

        public PortalOnlinePackageScanVerdict Scan(string stagingName, byte[] content)
        {
            CallCount++;
            if (ThrowOnScan)
            {
                throw new InvalidOperationException("scanner unavailable");
            }
            return Verdict;
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>以合法参数调用联合判定门，便于各测试只改动一个变量。</zh-CN>
    ///   <en>Calls the joint gate with valid parameters so each test varies exactly one input.</en>
    /// </lang>
    /// </summary>
    private static bool Allow(out string reason, IPortalOnlinePackageScanner scanner = null,
                              string clientFileName = "package.zip", byte[] content = null,
                              string expectedHash = null, string publicKeyXml = null,
                              string signature = null, bool requireSignature = false,
                              string stagingName = "staged-1234.zip")
    {
        return PortalOnlinePackageSecurityGate.TryAllowInstall(
            clientFileName,
            content ?? Content(),
            expectedHash ?? ExpectedHash(),
            publicKeyXml,
            signature,
            requireSignature,
            scanner ?? new StubScanner(),
            stagingName,
            out reason);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>哈希口径：64 位小写十六进制，与离线工具 Get-FileSha256 同口径；字节与流两条路径结果一致。</zh-CN>
    ///   <en>Hash format: 64 lowercase hex characters, identical to the offline Get-FileSha256; byte and stream paths agree.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Sha256_IsLowercase64HexAndMatchesAcrossByteAndStream()
    {
        string fromBytes = PortalOnlinePackageIntegrity.ComputeSha256Hex(Content());
        string fromStream;
        using (MemoryStream stream = new MemoryStream(Content()))
        {
            fromStream = PortalOnlinePackageIntegrity.ComputeSha256Hex(stream);
        }

        Assert.AreEqual(64, fromBytes.Length);
        Assert.AreEqual(fromBytes, fromBytes.ToLowerInvariant());
        Assert.AreEqual(fromBytes, fromStream);
        Assert.IsTrue(PortalOnlinePackageIntegrity.IsSha256Hex(fromBytes));

        // <lang>
        //   <zh-CN>空数组也有合法摘要且不同于 null（null 抛异常）。</zh-CN>
        //   <en>An empty array has a valid digest distinct from null (null throws).</en>
        // </lang>
        Assert.AreEqual(64, PortalOnlinePackageIntegrity.ComputeSha256Hex(new byte[0]).Length);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>摘要格式严格：长度、字符集、空白、冒号与 Unicode 同形字符都拒绝；null 内容抛异常而非静默通过。</zh-CN>
    ///   <en>Strict digest format: length, character set, whitespace, colons, and Unicode look-alikes reject; null content throws instead of passing silently.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Sha256_RejectsMalformedDigestsAndNullContent()
    {
        string[] bad =
        {
            null, "", new string('a', 63), new string('a', 65), new string('g', 64),
            " " + new string('a', 63), new string('a', 63) + ":", new string('ａ', 64)
        };
        foreach (string value in bad)
        {
            Assert.IsFalse(PortalOnlinePackageIntegrity.IsSha256Hex(value), "应拒绝: " + value);
        }
        Assert.IsTrue(PortalOnlinePackageIntegrity.IsSha256Hex(new string('A', 64)), "大写十六进制也是合法摘要形态");

        try
        {
            PortalOnlinePackageIntegrity.ComputeSha256Hex((byte[])null);
            Assert.Fail("null 内容应抛 ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>完整性：一致通过；内容被篡改、期望哈希缺失/畸形、null 内容都拒绝（承担 4.8 无 CRC 自动校验职责）。</zh-CN>
    ///   <en>Integrity: matching hash passes; tampered content, missing/malformed expected hash, and null content reject (carrying the duty absent from 4.8 CRC validation).</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Integrity_RejectsTamperingAndMissingExpectedHash()
    {
        string reason;
        Assert.IsTrue(PortalOnlinePackageIntegrity.TryVerifyIntegrity(Content(), ExpectedHash(), out reason), reason);

        byte[] tampered = Encoding.UTF8.GetBytes("tampered package content");
        Assert.IsFalse(PortalOnlinePackageIntegrity.TryVerifyIntegrity(tampered, ExpectedHash(), out reason));
        Assert.IsFalse(string.IsNullOrEmpty(reason));

        Assert.IsFalse(PortalOnlinePackageIntegrity.TryVerifyIntegrity(Content(), null, out reason));
        Assert.IsFalse(PortalOnlinePackageIntegrity.TryVerifyIntegrity(Content(), "short", out reason));
        Assert.IsFalse(PortalOnlinePackageIntegrity.TryVerifyIntegrity(null, ExpectedHash(), out reason));

        // <lang>
        //   <zh-CN>服务端记录大小写不同不改变安全结论（OrdinalIgnoreCase）。</zh-CN>
        //   <en>Casing differences in the server record do not change the verdict (OrdinalIgnoreCase).</en>
        // </lang>
        Assert.IsTrue(PortalOnlinePackageIntegrity.TryVerifyIntegrity(Content(), ExpectedHash().ToUpperInvariant(), out reason), reason);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>真实 RSA-SHA256 验签：有效签名通过；内容篡改、换公钥、坏 Base64、缺材料都拒绝（4.8 加密 API 编译实测）。</zh-CN>
    ///   <en>Real RSA-SHA256 verification: a valid signature passes; tampered content, a different key, bad Base64, and missing material reject (compiled proof of 4.8 crypto APIs).</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Signature_VerifiesRealRsaSha256AndRejectsTampering()
    {
        string signature = PortalOnlinePackageIntegrity.SignPackage(Content(), _privateKeyXml);
        string reason;

        Assert.IsTrue(PortalOnlinePackageIntegrity.TryVerifySignature(Content(), _publicKeyXml, signature, out reason), reason);
        Assert.IsTrue(PortalOnlinePackageIntegrity.HasSignatureMaterial(_publicKeyXml, signature));

        Assert.IsFalse(PortalOnlinePackageIntegrity.TryVerifySignature(
            Encoding.UTF8.GetBytes("tampered"), _publicKeyXml, signature, out reason));
        Assert.IsFalse(PortalOnlinePackageIntegrity.TryVerifySignature(Content(), _publicKeyXml, "not-base64!!", out reason));
        Assert.IsFalse(PortalOnlinePackageIntegrity.TryVerifySignature(Content(), null, signature, out reason));
        Assert.IsFalse(PortalOnlinePackageIntegrity.TryVerifySignature(Content(), _publicKeyXml, null, out reason));
        Assert.IsFalse(PortalOnlinePackageIntegrity.TryVerifySignature(null, _publicKeyXml, signature, out reason));
        Assert.IsFalse(PortalOnlinePackageIntegrity.TryVerifySignature(Content(), "<bad/>", signature, out reason));

        // <lang>
        //   <zh-CN>换一把公钥后原签名必须无效，证明验证确实绑定具体密钥。</zh-CN>
        //   <en>A different public key must invalidate the original signature, proving verification is bound to a specific key.</en>
        // </lang>
        using (RSACryptoServiceProvider other = new RSACryptoServiceProvider(2048))
        {
            try
            {
                Assert.IsFalse(PortalOnlinePackageIntegrity.TryVerifySignature(
                    Content(), other.ToXmlString(false), signature, out reason));
            }
            finally
            {
                other.PersistKeyInCsp = false;
            }
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>四道判据全部通过才放行；任一失败即拒绝且不回显不可信文本。</zh-CN>
    ///   <en>All four criteria must pass; any failure rejects and no untrusted text is echoed.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void JointGate_AllowsOnlyWhenAllFourCriteriaPass()
    {
        string reason;
        Assert.IsTrue(Allow(out reason), reason);
        Assert.AreEqual(string.Empty, reason);

        // <lang>
        //   <zh-CN>白名单：非 .zip / 超限文件名。</zh-CN>
        //   <en>Allow-list: non-.zip or unsafe file name.</en>
        // </lang>
        Assert.IsFalse(Allow(out reason, clientFileName: "package.exe"));
        Assert.IsFalse(Allow(out reason, clientFileName: "../package.zip"));
        Assert.IsFalse(string.IsNullOrEmpty(reason));

        // <lang>
        //   <zh-CN>完整性：内容被替换或期望哈希被改写。</zh-CN>
        //   <en>Integrity: substituted content or a changed expected hash.</en>
        // </lang>
        Assert.IsFalse(Allow(out reason, content: Encoding.UTF8.GetBytes("swapped package")));
        Assert.IsFalse(Allow(out reason, expectedHash: new string('b', 64)));

        // <lang>
        //   <zh-CN>暂存名缺失。</zh-CN>
        //   <en>Missing staging name.</en>
        // </lang>
        Assert.IsFalse(Allow(out reason, stagingName: " "));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>签名可选语义：要求签名但缺材料即拒绝；不要求但提供了无效签名仍必须拒绝（伪造签名不得被忽略）。</zh-CN>
    ///   <en>Optional-signature semantics: required-but-missing rejects; optional-but-supplied-invalid also rejects so a forged signature is never ignored.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void JointGate_SignatureIsOptionalButNeverIgnoredWhenSupplied()
    {
        string validSignature = PortalOnlinePackageIntegrity.SignPackage(Content(), _privateKeyXml);
        string reason;

        Assert.IsTrue(Allow(out reason, publicKeyXml: _publicKeyXml, signature: validSignature,
                            requireSignature: true), reason);
        Assert.IsFalse(Allow(out reason, requireSignature: true), "要求签名但未提供材料");
        Assert.IsFalse(Allow(out reason, publicKeyXml: _publicKeyXml, signature: "not-base64!!"),
                       "可选签名但提供了伪造签名");
        Assert.IsFalse(Allow(out reason, publicKeyXml: null, signature: validSignature),
                       "只给签名不给公钥");
        Assert.IsFalse(Allow(out reason, publicKeyXml: _publicKeyXml, signature: null),
                       "只给公钥不给签名（材料不完整同样不得静默放行）");

        // <lang>
        //   <zh-CN>有效签名但内容被篡改时，完整性或签名至少一道拒绝。</zh-CN>
        //   <en>With a valid signature but tampered content, integrity or signature (at least one) rejects.</en>
        // </lang>
        Assert.IsFalse(Allow(out reason, content: Encoding.UTF8.GetBytes("tampered"),
                             publicKeyXml: _publicKeyXml, signature: validSignature));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>扫描 fail-closed：null 适配、默认不可用实现、Infected、Unavailable、适配层抛异常、
    ///   枚举外伪造值一律拒绝；只有显式 Clean 放行。</zh-CN>
    ///   <en>Scanner fail-closed: a null adapter, the default unavailable implementation, Infected, Unavailable, an adapter
    ///   exception, and forged out-of-enum values all reject; only explicit Clean allows.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void JointGate_ScannerIsFailClosed()
    {
        string reason;

        Assert.IsFalse(Allow(out reason, scanner: new PortalOnlinePackageUnavailableScanner()));
        Assert.IsFalse(Allow(out reason, scanner: new StubScanner { Verdict = PortalOnlinePackageScanVerdict.Infected }));
        Assert.IsFalse(Allow(out reason, scanner: new StubScanner { Verdict = PortalOnlinePackageScanVerdict.Unavailable }));
        Assert.IsFalse(Allow(out reason, scanner: new StubScanner { ThrowOnScan = true }));
        Assert.IsFalse(Allow(out reason, scanner: new StubScanner { Verdict = (PortalOnlinePackageScanVerdict)999 }));
        Assert.IsTrue(Allow(out reason, scanner: new StubScanner { Verdict = PortalOnlinePackageScanVerdict.Clean }), reason);

        // <lang>
        //   <zh-CN>null 扫描器需要显式传参绕过默认值包装，单独断言。</zh-CN>
        //   <en>A null scanner must bypass the default-wrapper argument, so assert it explicitly.</en>
        // </lang>
        Assert.IsFalse(PortalOnlinePackageSecurityGate.TryAllowInstall(
            "package.zip", Content(), ExpectedHash(), null, null, false, null, "staged-1234.zip", out reason));
        Assert.IsFalse(string.IsNullOrEmpty(reason));
        Assert.IsFalse(reason.Contains("scanner unavailable"), "拒绝原因不得回显扫描器内部消息");
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>判定短路：前置判据失败时不得调用扫描器，避免对已拒绝内容做无意义或危险的扫描。</zh-CN>
    ///   <en>Short-circuit: when an earlier criterion fails, the scanner must not run, avoiding useless or risky scans of already-rejected content.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void JointGate_ShortCircuitsBeforeScanningOnEarlierFailure()
    {
        StubScanner scanner = new StubScanner { Verdict = PortalOnlinePackageScanVerdict.Clean };
        string reason;

        Assert.IsFalse(Allow(out reason, scanner: scanner, clientFileName: "package.exe"));
        Assert.AreEqual(0, scanner.CallCount);

        Assert.IsFalse(Allow(out reason, scanner: scanner, expectedHash: new string('c', 64)));
        Assert.AreEqual(0, scanner.CallCount);

        Assert.IsTrue(Allow(out reason, scanner: scanner), reason);
        Assert.AreEqual(1, scanner.CallCount);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>超限包（>10 MiB）在白名单阶段即拒绝，且不触发哈希/签名/扫描的后续工作。</zh-CN>
    ///   <en>An over-sized package (>10 MiB) is rejected at the allow-list stage without later hash/signature/scan work.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void JointGate_OversizedPackageRejectedAtAllowList()
    {
        StubScanner scanner = new StubScanner();
        byte[] oversized = new byte[PortalOnlinePackagePolicy.MaximumPackageBytes + 1];
        string reason;

        Assert.IsFalse(Allow(out reason, scanner: scanner, content: oversized));
        Assert.AreEqual(0, scanner.CallCount);
        Assert.IsFalse(string.IsNullOrEmpty(reason));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>FormatByteLength 用不变区域性，保证证据文本稳定。</zh-CN>
    ///   <en>FormatByteLength uses invariant culture so evidence text stays stable.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void FormatByteLength_IsInvariantCultureStable()
    {
        Assert.AreEqual("10485760", PortalOnlinePackageIntegrity.FormatByteLength(10485760L));
        Assert.AreEqual("0", PortalOnlinePackageIntegrity.FormatByteLength(0L));
    }
}
