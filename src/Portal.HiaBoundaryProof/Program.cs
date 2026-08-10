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
        /// <summary>
        /// <lang>
        ///   <zh-CN>按稳定顺序登记所有受控外围契约 fixture 及其期望结果。</zh-CN>
        ///   <en>Registers all controlled peripheral-contract fixtures and their expected results in stable order.</en>
        /// </lang>
        /// </summary>
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
            // <lang>
            //   <zh-CN>从命令行提取受控 fixture 根目录；缺少目录时只报告用法错误，不触碰真实环境。</zh-CN>
            //   <en>Extract the controlled fixture root from the command line; when absent, report usage only and never touch a real environment.</en>
            // </lang>
            string fixtureDirectory;
            if (!TryReadFixtureDirectory(args, out fixtureDirectory))
            {
                Console.Error.WriteLine("Usage: Portal.HiaBoundaryProof.exe --fixtures <directory>");
                return 2;
            }

            // <lang>
            //   <zh-CN>累计所有 fixture 和规范化检查的结果，保持 proof 以单一退出码收口。</zh-CN>
            //   <en>Accumulate every fixture and normalization result so the proof closes with one exit code.</en>
            // </lang>
            bool passed = true;

            // <lang>
            //   <zh-CN>按固定顺序遍历 fixture，保持 proof 输出与登记表一一对应。</zh-CN>
            //   <en>Traverse fixtures in fixed order so proof output remains one-to-one with the registry.</en>
            // </lang>
            foreach (ProofCase proofCase in FixtureCases)
            {
                // <lang>
                //   <zh-CN>当前受控用例定义文件名、预期有效性和稳定错误码，供本地离线验证。</zh-CN>
                //   <en>The current controlled case supplies the file name, expected validity, and stable error code for offline verification.</en>
                // </lang>
                PortalHiaBoundaryValidationResult result;

                // <lang>
                //   <zh-CN>读取并验证 fixture；失败时保留 null 结果，让输出只暴露稳定 proof 状态。</zh-CN>
                //   <en>Read and validate the fixture; retain a null result on failure so output exposes only stable proof state.</en>
                // </lang>
                bool loaded = TryValidateFixture(fixtureDirectory, proofCase.FileName, out result);

                // <lang>
                //   <zh-CN>同时比较加载状态、有效性和稳定代码，避免只凭进程未抛异常判定通过。</zh-CN>
                //   <en>Compare load state, validity, and stable code together instead of treating an exception-free process as proof success.</en>
                // </lang>
                bool casePassed = loaded && result != null &&
                                  result.IsValid == proofCase.ExpectedValid &&
                                  string.Equals(result.Code, proofCase.ExpectedCode, StringComparison.Ordinal);
                Console.WriteLine((casePassed ? "PASS " : "FAIL ") + proofCase.Id + " - " + (result == null ? "FixtureLoadFailed" : result.Code));
                passed &= casePassed;
            }

            // <lang>
            //   <zh-CN>保存实例标识规范化的输出，验证合法值转小写且非法值被拒绝。</zh-CN>
            //   <en>Store normalization output to verify lowercase canonicalization for valid input and rejection of invalid input.</en>
            // </lang>
            string normalizedInstanceId;

            // <lang>
            //   <zh-CN>将合法与非法样例合并为一个边界断言，确保 proof 复用正式契约实现。</zh-CN>
            //   <en>Combine valid and invalid samples into one boundary assertion so the proof reuses the production contract.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>先将输出初始化为空，保证参数缺失或格式错误时不会泄露调用方路径。</zh-CN>
            //   <en>Initialize the output to empty so missing or malformed arguments never leak a caller path.</en>
            // </lang>
            fixtureDirectory = string.Empty;
            if (args == null)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>只扫描能取得后继值的参数位置，避免读取 --fixtures 末尾的越界项。</zh-CN>
            //   <en>Scan only positions with a following value to avoid reading past a trailing --fixtures switch.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>把受控目录和固定文件名组合为单个 fixture 路径；路径来自 proof 参数，不扩展外部搜索。</zh-CN>
                //   <en>Combine the controlled directory and fixed file name into one fixture path; the path comes from proof arguments without external discovery.</en>
                // </lang>
                string fixturePath = Path.Combine(fixtureDirectory, fixtureFileName);

                // <lang>
                //   <zh-CN>以文本形式读取 fixture，随后交给框架 serializer 做结构映射。</zh-CN>
                //   <en>Read the fixture as text and then hand structural mapping to the framework serializer.</en>
                // </lang>
                string json = File.ReadAllText(fixturePath);

                // <lang>
                //   <zh-CN>使用无外部配置的 JavaScriptSerializer 将 JSON 映射为普通字典。</zh-CN>
                //   <en>Use JavaScriptSerializer without external configuration to map JSON into ordinary dictionaries.</en>
                // </lang>
                var serializer = new JavaScriptSerializer();

                // <lang>
                //   <zh-CN>要求根对象为字段字典；非对象 fixture 直接作为 proof 失败处理。</zh-CN>
                //   <en>Require a dictionary root object; treat non-object fixtures as proof failures immediately.</en>
                // </lang>
                var root = serializer.DeserializeObject(json) as IDictionary<string, object>;
                if (root == null)
                {
                    return false;
                }

                // <lang>
                //   <zh-CN>只将 fixture 映射到门户 DTO，再调用正式验证器；不为测试复制第二套契约规则。</zh-CN>
                //   <en>Map the fixture only to portal DTOs and invoke the production validator; do not copy a second contract rule set for tests.</en>
                // </lang>
                // <lang>
                //   <zh-CN>保留字段缺失为 null/空值，让正式验证器负责统一失败码和隐私边界。</zh-CN>
                //   <en>Preserve missing fields as null or empty values so the production validator owns failure codes and privacy boundaries.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>fixture 内容错误只以测试失败体现，避免命令行输出路径或原始 JSON。</zh-CN>
                //   <en>Represent fixture-content errors only as test failures and avoid printing paths or raw JSON to the command line.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>从根对象读取 producer 子对象；缺失或类型不符由后续验证器判定无效。</zh-CN>
            //   <en>Read the producer child object from the root; the validator decides when it is missing or mistyped.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>暂存键值以便只做字典查询和安全类型转换，不改变 fixture 原始对象。</zh-CN>
            //   <en>Hold the keyed value for dictionary lookup and safe type conversion without mutating the fixture object.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>暂存文本字段的原始对象，再以 invariant culture 转换为稳定字符串。</zh-CN>
            //   <en>Hold the raw text-field object and convert it to a stable string using invariant culture.</en>
            // </lang>
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
