using ASPNET.StarterKit.Portal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;

namespace ASPNET.StarterKit.Portal.HiaBoundaryProof
{
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

        private static PortalHiaProducerDescriptor ReadProducer(IDictionary<string, object> root)
        {
            IDictionary<string, object> producer = ReadMap(root, "producer");
            return new PortalHiaProducerDescriptor
            {
                Id = ReadText(producer, "id"),
                Version = ReadText(producer, "version")
            };
        }

        private static IDictionary<string, object> ReadMap(IDictionary<string, object> source, string key)
        {
            if (source == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            object value;
            return source.TryGetValue(key, out value) ? value as IDictionary<string, object> : null;
        }

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

        private sealed class ProofCase
        {
            public ProofCase(string id, string fileName, bool expectedValid, string expectedCode)
            {
                Id = id;
                FileName = fileName;
                ExpectedValid = expectedValid;
                ExpectedCode = expectedCode;
            }

            public string Id { get; private set; }

            public string FileName { get; private set; }

            public bool ExpectedValid { get; private set; }

            public string ExpectedCode { get; private set; }
        }
    }
}
