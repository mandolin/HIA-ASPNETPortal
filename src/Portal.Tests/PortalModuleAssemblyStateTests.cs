using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>装配态台账只读解析的稳定单元测试；覆盖封闭两值、受控缺省、fail-closed 与四分离。</zh-CN>
    ///   <en>Stable unit tests for read-only assembly-state ledger parsing; cover the closed two values, controlled default, fail-closed behaviour, and four-way separation.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些测试只读取 Portal.Components 的纯内存契约，不连接数据库、不读取配置或台账文件、不依赖 IIS、HTTP 上下文或装配工具。</zh-CN>
    ///   <en>These tests read only pure in-memory contracts from Portal.Components; they connect to no database, read no config or ledger file, and depend on no IIS, HTTP context, or assembly tool.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalModuleAssemblyStateTests
    {
        /// <summary><lang><zh-CN>全缺省台账的固定文本；空 entries 是合法且推荐的部署基线。</zh-CN><en>Fixed text of an all-default ledger; an empty entries object is the valid, recommended deployment baseline.</en></lang></summary>
        private const string EmptyLedger = "{\"schemaVersion\":1,\"entries\":{}}";

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证词表恰为封闭两值，且拒绝大小写变体与第三值。</zh-CN>
        ///   <en>Verifies the vocabulary is exactly the closed two values and rejects case variants and third values.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Modes_ClosedTwoValuesRejectVariants()
        {
            // <lang>
            //   <zh-CN>两个常量值必须与台账、迁移记录与文档中使用的字面量逐字一致。</zh-CN>
            //   <en>The two constants must match the literals used by the ledger, migration records, and documentation character for character.</en>
            // </lang>
            Assert.AreEqual("CodeBehind", PortalModuleAssemblyModes.CodeBehind);
            Assert.AreEqual("CodeFile", PortalModuleAssemblyModes.CodeFile);

            // <lang>
            //   <zh-CN>精确比较：大小写变体、空白变体、空串与第三值都不是已登记装配态。</zh-CN>
            //   <en>Exact comparison: case variants, whitespace variants, blank strings, and a third value are not registered modes.</en>
            // </lang>
            Assert.IsTrue(PortalModuleAssemblyModes.IsRegistered("CodeBehind"));
            Assert.IsTrue(PortalModuleAssemblyModes.IsRegistered("CodeFile"));
            Assert.IsFalse(PortalModuleAssemblyModes.IsRegistered("codebehind"));
            Assert.IsFalse(PortalModuleAssemblyModes.IsRegistered(" CodeBehind "));
            Assert.IsFalse(PortalModuleAssemblyModes.IsRegistered(string.Empty));
            Assert.IsFalse(PortalModuleAssemblyModes.IsRegistered(null));
            Assert.IsFalse(PortalModuleAssemblyModes.IsRegistered("CodeFileBehind"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证空台账解析成功，且任意受信任包标识都按受控缺省返回 CodeBehind。</zh-CN>
        ///   <en>Verifies an empty ledger parses and any trusted package identifier returns the controlled CodeBehind default.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void EmptyLedger_UsesCodeBehindDefault()
        {
            // <lang>
            //   <zh-CN>承接解析输出；两个 out 参数都必须被断言，避免只验证成功而忽略原因文本。</zh-CN>
            //   <en>Holds parse outputs; both out parameters are asserted so success is not checked while the reason text is ignored.</en>
            // </lang>
            PortalModuleAssemblyState state;
            string reason;
            bool parsed = PortalModuleAssemblyState.TryParse(EmptyLedger, out state, out reason);

            // <lang>
            //   <zh-CN>空 entries 是合法基线，失败反而是回归；此时不应有失败原因。</zh-CN>
            //   <en>An empty entries object is a valid baseline, so failing would be a regression; no failure reason should exist.</en>
            // </lang>
            Assert.IsTrue(parsed, reason);
            Assert.IsNotNull(state);
            Assert.AreEqual(string.Empty, reason);
            Assert.AreEqual(0, state.EntryCount);

            // <lang>
            //   <zh-CN>无论包是否真实存在，缺省都是 CodeBehind；解析层不查询信任链，可解析性由门禁负责。</zh-CN>
            //   <en>The default is CodeBehind regardless of whether a package exists; the parse layer queries no trust chain, and resolvability belongs to the gate.</en>
            // </lang>
            Assert.AreEqual("CodeBehind", state.GetMode("HIA.BusinessApplicationRequest"));
            Assert.AreEqual("CodeBehind", state.GetMode("HIA.NotDeployedPackage"));
            Assert.AreEqual("CodeBehind", state.GetMode(""));
            Assert.AreEqual("CodeBehind", state.GetMode(null));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证显式登记的两个装配态都能被解析并按包标识返回，追溯字段不影响取值。</zh-CN>
        ///   <en>Verifies both explicitly registered modes parse and return per package identifier, with trace fields not affecting values.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ExplicitEntries_ReturnRegisteredModes()
        {
            // <lang>
            //   <zh-CN>固定文本含两个条目、两个装配态与四个追溯字段，覆盖白名单全部字段。</zh-CN>
            //   <en>Fixed text with two entries, both modes, and all four trace fields, covering the entire whitelist.</en>
            // </lang>
            string json = "{\"schemaVersion\":1,\"entries\":{" +
                          "\"HIA.EmployeeProfileConfirm\":{\"assemblyMode\":\"CodeBehind\",\"recordedUtc\":\"2026-11-06T00:00:00Z\",\"recordedBy\":\"deployment\",\"sourceHash\":\"abc\",\"note\":\"baseline\"}," +
                          "\"Legacy.Announcements\":{\"assemblyMode\":\"CodeFile\"}}}";

            // <lang>
            //   <zh-CN>承接解析输出；CodeFile 在本周期虽不落地，词表仍须能表达以便未来复用。</zh-CN>
            //   <en>Holds parse outputs; although CodeFile is not entered this cycle, the vocabulary must still express it for future reuse.</en>
            // </lang>
            PortalModuleAssemblyState state;
            string reason;
            bool parsed = PortalModuleAssemblyState.TryParse(json, out state, out reason);

            // <lang>
            //   <zh-CN>两个条目都应通过校验，且各自返回登记的装配态。</zh-CN>
            //   <en>Both entries should validate and each returns its registered mode.</en>
            // </lang>
            Assert.IsTrue(parsed, reason);
            Assert.AreEqual(2, state.EntryCount);
            Assert.AreEqual("CodeBehind", state.GetMode("HIA.EmployeeProfileConfirm"));
            Assert.AreEqual("CodeFile", state.GetMode("Legacy.Announcements"));

            // <lang>
            //   <zh-CN>未登记包仍走缺省，不因台账里存在别的条目而改变。</zh-CN>
            //   <en>An unregistered package still uses the default and is unaffected by other entries in the ledger.</en>
            // </lang>
            Assert.AreEqual("CodeBehind", state.GetMode("HIA.MyWorkItems"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 fail-closed：非 JSON、空文本、错误版本、未知根字段、错误根形状、缺失 entries、错误装配态与未知条目字段都整体失败。</zh-CN>
        ///   <en>Verifies fail-closed behaviour: non-JSON, blank text, wrong version, unknown root field, wrong root shape, missing entries, invalid mode, and unknown entry field all fail wholesale.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void InvalidLedgers_FailClosedWithoutPartialState()
        {
            // <lang>
            //   <zh-CN>每个用例都是一类真实漂移；失败时必须同时给出 null 快照与稳定原因，不得返回部分结果。</zh-CN>
            //   <en>Each case is one class of real drift; failure must yield both a null snapshot and a stable reason, with no partial result.</en>
            // </lang>
            string[] invalid =
            {
                "not json at all",
                "   ",
                null,
                "{\"schemaVersion\":2,\"entries\":{}}",
                "{\"entries\":{}}",
                "{\"schemaVersion\":1,\"entries\":{},\"extra\":true}",
                "[]",
                "{\"schemaVersion\":1}",
                "{\"schemaVersion\":1,\"entries\":[]}",
                "{\"schemaVersion\":1,\"entries\":{\"HIA.X\":{\"assemblyMode\":\"Debug\"}}}",
                "{\"schemaVersion\":1,\"entries\":{\"HIA.X\":{}}}",
                "{\"schemaVersion\":1,\"entries\":{\"Unknown.X\":{\"assemblyMode\":\"CodeBehind\"}}}",
                "{\"schemaVersion\":1,\"entries\":{\"HIA.X\":\"CodeBehind\"}}",
                "{\"schemaVersion\":1,\"entries\":{\"HIA.X\":{\"assemblyMode\":\"codebehind\"}}}",
                "{\"schemaVersion\":1,\"entries\":{\"HIA.X\":{\"assemblyMode\":\"CodeBehind\",\"extra\":1}}}",
                "{\"schemaVersion\":1,\"entries\":{\"HIA.X\":{\"assemblyMode\":\"CodeBehind\",\"sourceHash\":42}}}"
            };

            // <lang>
            //   <zh-CN>逐个解析并断言同一失败契约；循环内不写文件、不依赖外部状态。</zh-CN>
            //   <en>Parse each and assert the same failure contract; the loop writes no file and depends on no external state.</en>
            // </lang>
            foreach (string json in invalid)
            {
                // <lang>
                //   <zh-CN>承接本用例的解析输出；每次循环重新初始化，避免上一用例结果泄漏。</zh-CN>
                //   <en>Holds this case's parse outputs; reinitialised each iteration so no prior result leaks.</en>
                // </lang>
                PortalModuleAssemblyState state;
                string reason;
                bool parsed = PortalModuleAssemblyState.TryParse(json, out state, out reason);

                // <lang>
                //   <zh-CN>失败是唯一可接受结果，且不得产生可用快照或空原因。</zh-CN>
                //   <en>Failure is the only acceptable result, with neither a usable snapshot nor an empty reason.</en>
                // </lang>
                Assert.IsFalse(parsed, "非法台账必须整体失败：" + (json ?? "<null>"));
                Assert.IsNull(state, "失败时不得返回部分快照：" + (json ?? "<null>"));
                Assert.IsFalse(string.IsNullOrEmpty(reason), "失败必须给出稳定原因：" + (json ?? "<null>"));
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证四分离：解析结果只由台账文本决定，与包启用状态、能力归属、Profile 配置无关。</zh-CN>
        ///   <en>Verifies four-way separation: the parse result depends only on ledger text, not package enablement, capability ownership, or Profile configuration.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Parse_IsIndependentOfOtherModuleStates()
        {
            // <lang>
            //   <zh-CN>同一台账文本解析两次，期间不触碰任何其它状态来源；结果必须逐项一致。</zh-CN>
            //   <en>Parse the same ledger text twice without touching any other state source; results must agree item by item.</en>
            // </lang>
            string json = "{\"schemaVersion\":1,\"entries\":{\"HIA.Help\":{\"assemblyMode\":\"CodeFile\"}}}";

            // <lang>
            //   <zh-CN>承接第一次解析输出，作为纯函数确定性的比较基线。</zh-CN>
            //   <en>Holds the first parse outputs as the determinism baseline for the pure function.</en>
            // </lang>
            PortalModuleAssemblyState first;
            string firstReason;
            Assert.IsTrue(PortalModuleAssemblyState.TryParse(json, out first, out firstReason), firstReason);

            // <lang>
            //   <zh-CN>承接第二次解析输出；若解析层读取了全局配置或数据库，此结果可能不同。</zh-CN>
            //   <en>Holds the second parse outputs; if the parse layer read global config or a database, this result could differ.</en>
            // </lang>
            PortalModuleAssemblyState second;
            string secondReason;
            Assert.IsTrue(PortalModuleAssemblyState.TryParse(json, out second, out secondReason), secondReason);

            // <lang>
            //   <zh-CN>条目数、装配态与原因文本均一致，证明输入只有台账文本。</zh-CN>
            //   <en>Entry count, mode, and reason agree, proving ledger text is the only input.</en>
            // </lang>
            Assert.AreEqual(first.EntryCount, second.EntryCount);
            Assert.AreEqual(first.GetMode("HIA.Help"), second.GetMode("HIA.Help"));
            Assert.AreEqual(firstReason, secondReason);

            // <lang>
            //   <zh-CN>即便存在 CodeFile 条目，未登记包仍按缺省返回，装配态不被其它状态源放大。</zh-CN>
            //   <en>Even with a CodeFile entry, unregistered packages still use the default; mode is not amplified by other state sources.</en>
            // </lang>
            Assert.AreEqual("CodeBehind", first.GetMode("HIA.LeaveRequest"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证部署级开关的纯函数判据：关闭时拒绝含调试态条目的台账，开启时接受，全缺省与仅 CodeBehind 恒可接受。</zh-CN>
        ///   <en>Verifies the deployment switch predicate: off rejects a ledger with debug-mode entries, on accepts it, and all-default or CodeBehind-only ledgers are always acceptable.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void EnvironmentSwitch_RejectsCodeFileWhenDisabled()
        {
            // <lang>
            //   <zh-CN>固定文本含一个调试态条目，作为开关判定的注入样本。</zh-CN>
            //   <en>Fixed text with one debug-mode entry serves as the injected sample for switch evaluation.</en>
            // </lang>
            string withDebug = "{\"schemaVersion\":1,\"entries\":{\"HIA.Help\":{\"assemblyMode\":\"CodeFile\"}}}";

            // <lang>
            //   <zh-CN>固定文本仅含一个显式生产态条目，作为不触发开关的对照样本。</zh-CN>
            //   <en>Fixed text with a single explicit production-mode entry serves as the control sample that must not trigger the switch.</en>
            // </lang>
            string productionOnly = "{\"schemaVersion\":1,\"entries\":{\"HIA.Help\":{\"assemblyMode\":\"CodeBehind\"}}}";

            // <lang>
            //   <zh-CN>承接调试态台账的解析输出；调试态在词表中合法，形状校验不应拦截它。</zh-CN>
            //   <en>Holds the debug ledger's parse outputs; the debug mode is a lawful vocabulary value, so shape validation must not reject it.</en>
            // </lang>
            PortalModuleAssemblyState debugState;
            string debugReason;
            Assert.IsTrue(PortalModuleAssemblyState.TryParse(withDebug, out debugState, out debugReason), debugReason);

            // <lang>
            //   <zh-CN>承接对照台账的解析输出，用于证明判定只针对调试态而非"有条目"。</zh-CN>
            //   <en>Holds the control ledger's parse outputs, proving the predicate targets debug mode rather than mere entry presence.</en>
            // </lang>
            PortalModuleAssemblyState productionState;
            string productionReason;
            Assert.IsTrue(PortalModuleAssemblyState.TryParse(productionOnly, out productionState, out productionReason), productionReason);

            // <lang>
            //   <zh-CN>调试态台账被检出；对照台账与全缺省基线都不应被检出。</zh-CN>
            //   <en>The debug ledger is detected; the control ledger and the all-default baseline must not be.</en>
            // </lang>
            Assert.IsTrue(debugState.HasCodeFileEntries());
            Assert.IsFalse(productionState.HasCodeFileEntries());
            Assert.IsFalse(PortalModuleAssemblyState.Empty.HasCodeFileEntries());

            // <lang>
            //   <zh-CN>开关关闭（缺省）时拒绝调试态台账，开启时接受；其余台账在两种开关下都接受。</zh-CN>
            //   <en>With the switch off (default) the debug ledger is rejected and with it on accepted; other ledgers pass under both settings.</en>
            // </lang>
            Assert.IsFalse(debugState.IsAllowedByEnvironment(false));
            Assert.IsTrue(debugState.IsAllowedByEnvironment(true));
            Assert.IsTrue(productionState.IsAllowedByEnvironment(false));
            Assert.IsTrue(PortalModuleAssemblyState.Empty.IsAllowedByEnvironment(false));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 Empty 工厂给出全缺省基线，且每次返回独立实例、互不污染。</zh-CN>
        ///   <en>Verifies the Empty factory yields an all-default baseline and returns independent instances that cannot pollute each other.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Empty_ReturnsIndependentAllDefaultBaseline()
        {
            // <lang>
            //   <zh-CN>承接两次工厂调用；比较引用与行为，确认没有共享静态可变状态。</zh-CN>
            //   <en>Holds two factory calls; compare references and behaviour to confirm no shared static mutable state exists.</en>
            // </lang>
            PortalModuleAssemblyState first = PortalModuleAssemblyState.Empty;
            PortalModuleAssemblyState second = PortalModuleAssemblyState.Empty;

            // <lang>
            //   <zh-CN>两个实例各自独立，条目数均为 0，且任意包都按缺省返回。</zh-CN>
            //   <en>Both instances are independent, have zero entries, and return the default for any package.</en>
            // </lang>
            Assert.AreNotSame(first, second);
            Assert.AreEqual(0, first.EntryCount);
            Assert.AreEqual(0, second.EntryCount);
            Assert.AreEqual("CodeBehind", first.GetMode("HIA.Help"));
            Assert.AreEqual("CodeBehind", second.GetMode("HIA.Help"));
        }
    }
}
