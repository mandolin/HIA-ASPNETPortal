using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>一次系统健康检查快照。</zh-CN>
    ///   <en>Snapshot of one system health check run.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalHealthSnapshot
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建系统健康检查快照并计算汇总状态。</zh-CN>
        ///   <en>Creates a system-health snapshot and calculates the overall status.</en>
        /// </lang>
        /// </summary>
        /// <param name="generatedUtc">
        /// <l>
        ///   <zh-CN>本次检查生成时间，使用 UTC 以便跨部署比较。</zh-CN>
        ///   <en>UTC generation time for comparing checks across deployments.</en>
        /// </l>
        /// </param>
        /// <param name="checks">
        /// <l>
        ///   <zh-CN>单项健康结果集合；为 <c>null</c> 时归一为空集合。</zh-CN>
        ///   <en>Individual health results; <c>null</c> normalizes to an empty collection.</en>
        /// </l>
        /// </param>
        /// <param name="settings">
        /// <l>
        ///   <zh-CN>设置健康行集合；为 <c>null</c> 时归一为空集合。</zh-CN>
        ///   <en>Setting-health rows; <c>null</c> normalizes to an empty collection.</en>
        /// </l>
        /// </param>
        public PortalHealthSnapshot(
            DateTime generatedUtc,
            IList<PortalHealthCheckResult> checks,
            IList<PortalSettingHealthInfo> settings)
        {
            // <lang>
            //   <zh-CN>保存调用方提供的 UTC 时间，不在模型层改用本地时间或重新生成时间。</zh-CN>
            //   <en>Retain the caller-provided UTC time without converting it to local time or generating another timestamp in the model.</en>
            // </lang>
            GeneratedUtc = generatedUtc;

            // <lang>
            //   <zh-CN>把空结果集合归一为空列表，保证快照枚举安全且不共享隐式 null 状态。</zh-CN>
            //   <en>Normalize a null result collection to an empty list so snapshot enumeration is safe and never relies on implicit null state.</en>
            // </lang>
            Checks = checks ?? new List<PortalHealthCheckResult>();

            // <lang>
            //   <zh-CN>把空设置集合归一为空列表，保持健康页可渲染且不伪造设置定义。</zh-CN>
            //   <en>Normalize a null setting collection to an empty list so the health page can render without inventing setting definitions.</en>
            // </lang>
            Settings = settings ?? new List<PortalSettingHealthInfo>();

            // <lang>
            //   <zh-CN>只根据已保存的单项结果计算汇总状态，保持错误优先级集中在快照模型。</zh-CN>
            //   <en>Calculate the overall status only from the retained individual results, keeping priority centralized in the snapshot model.</en>
            // </lang>
            OverallStatus = CalculateOverallStatus(Checks);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>快照生成 UTC 时间。</zh-CN>
        ///   <en>Snapshot generation UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime GeneratedUtc { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>汇总状态。</zh-CN>
        ///   <en>Overall status.</en>
        /// </lang>
        /// </summary>
        public PortalHealthStatus OverallStatus { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>健康检查结果。</zh-CN>
        ///   <en>Health check results.</en>
        /// </lang>
        /// </summary>
        public IList<PortalHealthCheckResult> Checks { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置 registry 状态。</zh-CN>
        ///   <en>Settings registry state.</en>
        /// </lang>
        /// </summary>
        public IList<PortalSettingHealthInfo> Settings { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按 Error、Warning、Unknown、Healthy 的优先级汇总单项检查结果。</zh-CN>
        ///   <en>Aggregates individual check results by Error, Warning, Unknown, and Healthy priority.</en>
        /// </lang>
        /// </summary>
        /// <param name="checks">
        /// <l>
        ///   <zh-CN>待汇总的单项结果序列；调用方保证其非空。</zh-CN>
        ///   <en>Individual results to aggregate; callers provide a non-null sequence.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>按 Error、Warning、Unknown、Healthy 顺序计算的汇总状态。</zh-CN>
        ///   <en>Overall status calculated in Error, Warning, Unknown, Healthy order.</en>
        /// </l>
        /// </returns>
        private static PortalHealthStatus CalculateOverallStatus(IEnumerable<PortalHealthCheckResult> checks)
        {
            // <lang>
            //   <zh-CN>分别记录警告和未知状态，等遍历结束后再按优先级决定非错误结果。</zh-CN>
            //   <en>Track warning and unknown states separately so non-error priority can be decided after traversal.</en>
            // </lang>
            bool hasUnknown = false;

            // <lang>
            //   <zh-CN>警告标志只表示至少一个单项需要关注，不覆盖更高优先级的 Error。</zh-CN>
            //   <en>The warning flag means at least one item needs attention and never overrides the higher-priority Error.</en>
            // </lang>
            bool hasWarning = false;

            // <lang>
            //   <zh-CN>按输入序列逐项读取状态；本 helper 不修改结果对象或集合。</zh-CN>
            //   <en>Read each status from the input sequence without modifying result objects or the collection.</en>
            // </lang>
            foreach (PortalHealthCheckResult check in checks)
            {
                // <lang>
                //   <zh-CN>错误是终止性最高状态，立即返回以避免后续低优先级结果掩盖关键故障。</zh-CN>
                //   <en>Error is the highest terminal state, so return immediately to prevent later lower-priority results from masking a critical fault.</en>
                // </lang>
                if (check.Status == PortalHealthStatus.Error)
                {
                    return PortalHealthStatus.Error;
                }

                // <lang>
                //   <zh-CN>记录警告事实但继续遍历，以便发现后续 Error。</zh-CN>
                //   <en>Record a warning and continue so a later Error can still be discovered.</en>
                // </lang>
                if (check.Status == PortalHealthStatus.Warning)
                {
                    hasWarning = true;
                }

                // <lang>
                //   <zh-CN>记录未知事实但继续遍历；Warning 在最终优先级中高于 Unknown。</zh-CN>
                //   <en>Record an unknown state and continue; Warning has higher final priority than Unknown.</en>
                // </lang>
                if (check.Status == PortalHealthStatus.Unknown)
                {
                    hasUnknown = true;
                }
            }

            // <lang>
            //   <zh-CN>没有 Error 时，任一 Warning 使汇总状态为 Warning。</zh-CN>
            //   <en>When no Error exists, any Warning makes the overall status Warning.</en>
            // </lang>
            if (hasWarning)
            {
                return PortalHealthStatus.Warning;
            }

            // <lang>
            //   <zh-CN>无 Error/Warning 时保留 Unknown；只有所有项目均为 Healthy 才返回 Healthy。</zh-CN>
            //   <en>Preserve Unknown when no Error or Warning exists; return Healthy only when every item is Healthy.</en>
            // </lang>
            return hasUnknown ? PortalHealthStatus.Unknown : PortalHealthStatus.Healthy;
        }
    }
}
