using ASPNET.StarterKit.Portal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;

namespace ASPNET.StarterKit.Portal.HiaBoundaryProof
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>执行 HIA 外围契约 fixture 验证的控制台入口。</zh-CN>
    ///   <en>Console entry point for validating HIA peripheral-contract fixtures.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该 proof 只将受控 JSON fixture 映射到门户 DTO，再调用正式契约验证器；它不复制第二套业务规则。</zh-CN>
    ///   <en>This proof maps controlled JSON fixtures to portal DTOs and then invokes the real contract validator; it does not duplicate a second rule set.</en>
    /// </lang>
    /// </remarks>
    internal static class Program
    {
        private static readonly ProofCase[] FixtureCases =
        {
            new ProofCase("P3H01.ModuleDescriptor", "valid-module.json", true, "HIA_PERIPHERAL_VALID"),
            new ProofCase("P3H02.HealthDescriptor", "valid-health.json", true, "HIA_PERIPHERAL_VALID"),
            new ProofCase("P3H03.ThemeDescriptor", "valid-theme.json", true, "HIA_PERIPHERAL_VALID"),
            new ProofCase("P3H04.SettingDescriptor", "valid-setting.json", true, "HIA_PERIPHERAL_VALID"),
            new ProofCase("P3H05.DiagnosticReference", "valid-diagnostic-reference.json", true, "HIA_PERIPHERAL_VALID"),
            new ProofCase("P3H06.AbsolutePath", "invalid-absolute-path.json", false, "HIA_PERIPHERAL_PROHIBITED_FIELD"),
            new ProofCase("P3H07.UserIdentity", "invalid-user-identity.json", false, "HIA_PERIPHERAL_PROHIBITED_FIELD"),
            new ProofCase("P3H08.InstanceId", "invalid-instance-id.json", false, "HIA_PERIPHERAL_INVALID_INSTANCE_ID"),
            new ProofCase("P3H09.ContractVersion", "invalid-contract-version.json", false, "HIA_PERIPHERAL_UNSUPPORTED_VERSION")
        };

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取 fixture 目录并执行所有正反例契约验证。</zh-CN>
        ///   <en>Reads the fixture directory and runs all positive and negative contract checks.</en>
        /// </lang>
        /// </summary>
        /// <param name="args">
        /// <l>
        ///   <zh-CN>必须包含 <c>--fixtures</c> 的参数数组。</zh-CN>
        ///   <en>Argument array that must contain <c>--fixtures</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>0 表示全部 fixture 通过，1 表示契约验证失败，2 表示参数无效。</zh-CN>
        ///   <en>Returns 0 when all fixtures pass, 1 for contract proof failure, and 2 for invalid arguments.</en>
        /// </l>
        /// </returns>
        private static int Main(string[] args)
        {
            string fixtureDirectory;
            if (!TryReadFixtureDirectory(args, out fixtureDirectory))
            {
                Console.Error.WriteLine("Usage: Portal.HiaBoundaryProof.exe --fixtures <directory>");
                return 2;
            }

            bool passed = true;
            foreach (ProofCase proofCase in FixtureCases)
            {
                PortalHiaBoundaryValidationResult result;
                bool loaded = TryValidateFixture(fixtureDirectory, proofCase.FileName, out result);
                bool casePassed = loaded && result != null &&
                                  result.IsValid == proofCase.ExpectedValid &&
                                  string.Equals(result.Code, proofCase.ExpectedCode, StringComparison.Ordinal);
                Console.WriteLine((casePassed ? "PASS " : "FAIL ") + proofCase.Id + " - " + (result == null ? "FixtureLoadFailed" : result.Code));
                passed &= casePassed;
            }

            string normalizedInstanceId;
            bool instanceIdPassed =
                PortalHiaBoundaryContract.TryNormalizePortalInstanceId("Portal-Dev_01", out normalizedInstanceId) &&
                string.Equals(normalizedInstanceId, "portal-dev_01", StringComparison.Ordinal) &&
                !PortalHiaBoundaryContract.TryNormalizePortalInstanceId("Portal Production!", out normalizedInstanceId);
            Console.WriteLine((instanceIdPassed ? "PASS " : "FAIL ") + "P3H10.InstanceIdNormalization - " + (instanceIdPassed ? "verified" : "failed"));
            passed &= instanceIdPassed;

            return passed ? 0 : 1;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从命令行读取并校验 fixture 根目录。</zh-CN>
        ///   <en>Reads and validates the fixture root directory from command-line arguments.</en>
        /// </lang>
        /// </summary>
        private static bool TryReadFixtureDirectory(string[] args, out string fixtureDirectory)
        {
            fixtureDirectory = string.Empty;
            if (args == null)
            {
                return false;
            }

            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], "--fixtures", StringComparison.OrdinalIgnoreCase))
                {
                    fixtureDirectory = args[index + 1];
                    break;
                }
            }

            return !string.IsNullOrWhiteSpace(fixtureDirectory) && Directory.Exists(fixtureDirectory);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取单个 fixture、映射 envelope，并调用正式验证器。</zh-CN>
        ///   <en>Reads one fixture, maps the envelope, and invokes the production validator.</en>
        /// </lang>
        /// </summary>
        private static bool TryValidateFixture(
            string fixtureDirectory,
            string fixtureFileName,
            out PortalHiaBoundaryValidationResult result)
        {
            result = null;
            try
            {
                string fixturePath = Path.Combine(fixtureDirectory, fixtureFileName);
                string json = File.ReadAllText(fixturePath);
                var serializer = new JavaScriptSerializer();
                var root = serializer.DeserializeObject(json) as IDictionary<string, object>;
                if (root == null)
                {
                    return false;
                }

                // proof 只将 fixture 映射到门户 DTO，再调用正式验证器；不为测试复制第二套契约规则。
                var envelope = new PortalHiaPeripheralEnvelope
                {
                    Contract = ReadText(root, "contract"),
                    ContractVersion = ReadText(root, "contractVersion"),
                    PortalInstanceId = ReadText(root, "portalInstanceId"),
                    Producer = ReadProducer(root),
                    Kind = ReadText(root, "kind"),
                    OccurredUtc = ReadText(root, "occurredUtc"),
                    Payload = ReadMap(root, "payload"),
                    Metadata = ReadMap(root, "metadata")
                };

                result = PortalHiaBoundaryContract.Validate(envelope);
                return true;
            }
            catch
            {
                // fixture 内容错误只以测试失败体现，避免命令行输出路径或原始 JSON。
                return false;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从 fixture 根对象读取 producer 描述。</zh-CN>
        ///   <en>Reads the producer descriptor from the fixture root object.</en>
        /// </lang>
        /// </summary>
        private static PortalHiaProducerDescriptor ReadProducer(IDictionary<string, object> root)
        {
            IDictionary<string, object> producer = ReadMap(root, "producer");
            return new PortalHiaProducerDescriptor
            {
                Id = ReadText(producer, "id"),
                Version = ReadText(producer, "version")
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定键上的 JSON 对象映射。</zh-CN>
        ///   <en>Reads the JSON object map at the specified key.</en>
        /// </lang>
        /// </summary>
        private static IDictionary<string, object> ReadMap(IDictionary<string, object> source, string key)
        {
            if (source == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            object value;
            return source.TryGetValue(key, out value) ? value as IDictionary<string, object> : null;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>以 invariant culture 读取指定键上的文本值。</zh-CN>
        ///   <en>Reads a text value at the specified key using invariant culture.</en>
        /// </lang>
        /// </summary>
        private static string ReadText(IDictionary<string, object> source, string key)
        {
            if (source == null || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            object value;
            return source.TryGetValue(key, out value)
                ? Convert.ToString(value, CultureInfo.InvariantCulture)
                : string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>一个 HIA 契约 fixture 的期望结果定义。</zh-CN>
        ///   <en>Expected result definition for one HIA contract fixture.</en>
        /// </lang>
        /// </summary>
        private sealed class ProofCase
        {
            /// <summary>
            /// <lang>
            ///   <zh-CN>创建 fixture proof 用例。</zh-CN>
            ///   <en>Creates a fixture proof case.</en>
            /// </lang>
            /// </summary>
            public ProofCase(string id, string fileName, bool expectedValid, string expectedCode)
            {
                Id = id;
                FileName = fileName;
                ExpectedValid = expectedValid;
                ExpectedCode = expectedCode;
            }

            /// <summary>
            /// <l>
            ///   <zh-CN>proof 步骤标识。</zh-CN>
            ///   <en>Proof step identifier.</en>
            /// </l>
            /// </summary>
            public string Id { get; private set; }

            /// <summary>
            /// <l>
            ///   <zh-CN>fixture 文件名。</zh-CN>
            ///   <en>Fixture file name.</en>
            /// </l>
            /// </summary>
            public string FileName { get; private set; }

            /// <summary>
            /// <l>
            ///   <zh-CN>期望验证是否通过。</zh-CN>
            ///   <en>Whether validation is expected to pass.</en>
            /// </l>
            /// </summary>
            public bool ExpectedValid { get; private set; }

            /// <summary>
            /// <l>
            ///   <zh-CN>期望的稳定验证代码。</zh-CN>
            ///   <en>Expected stable validation code.</en>
            /// </l>
            /// </summary>
            public string ExpectedCode { get; private set; }
        }
    }
}
