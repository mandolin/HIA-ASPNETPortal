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
        public PortalHealthSnapshot(
            DateTime generatedUtc,
            IList<PortalHealthCheckResult> checks,
            IList<PortalSettingHealthInfo> settings)
        {
            GeneratedUtc = generatedUtc;
            Checks = checks ?? new List<PortalHealthCheckResult>();
            Settings = settings ?? new List<PortalSettingHealthInfo>();
            OverallStatus = CalculateOverallStatus(Checks);
        }

        /// <summary>
        /// <l>
        ///   <zh-CN>快照生成 UTC 时间。</zh-CN>
        ///   <en>Snapshot generation UTC time.</en>
        /// </l>
        /// </summary>
        public DateTime GeneratedUtc { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>汇总状态。</zh-CN>
        ///   <en>Overall status.</en>
        /// </l>
        /// </summary>
        public PortalHealthStatus OverallStatus { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>健康检查结果。</zh-CN>
        ///   <en>Health check results.</en>
        /// </l>
        /// </summary>
        public IList<PortalHealthCheckResult> Checks { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>设置 registry 状态。</zh-CN>
        ///   <en>Settings registry state.</en>
        /// </l>
        /// </summary>
        public IList<PortalSettingHealthInfo> Settings { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按 Error、Warning、Unknown、Healthy 的优先级汇总单项检查结果。</zh-CN>
        ///   <en>Aggregates individual check results by Error, Warning, Unknown, and Healthy priority.</en>
        /// </lang>
        /// </summary>
        private static PortalHealthStatus CalculateOverallStatus(IEnumerable<PortalHealthCheckResult> checks)
        {
            bool hasUnknown = false;
            bool hasWarning = false;

            foreach (PortalHealthCheckResult check in checks)
            {
                if (check.Status == PortalHealthStatus.Error)
                {
                    return PortalHealthStatus.Error;
                }

                if (check.Status == PortalHealthStatus.Warning)
                {
                    hasWarning = true;
                }

                if (check.Status == PortalHealthStatus.Unknown)
                {
                    hasUnknown = true;
                }
            }

            if (hasWarning)
            {
                return PortalHealthStatus.Warning;
            }

            return hasUnknown ? PortalHealthStatus.Unknown : PortalHealthStatus.Healthy;
        }
    }
}
