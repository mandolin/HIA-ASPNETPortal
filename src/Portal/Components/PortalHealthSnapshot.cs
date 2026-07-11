using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 一次系统健康检查快照。
    /// Snapshot of one system health check run.
    /// </summary>
    public sealed class PortalHealthSnapshot
    {
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
        /// 快照生成 UTC 时间。
        /// Snapshot generation UTC time.
        /// </summary>
        public DateTime GeneratedUtc { get; private set; }

        /// <summary>
        /// 汇总状态。
        /// Overall status.
        /// </summary>
        public PortalHealthStatus OverallStatus { get; private set; }

        /// <summary>
        /// 健康检查结果。
        /// Health check results.
        /// </summary>
        public IList<PortalHealthCheckResult> Checks { get; private set; }

        /// <summary>
        /// 设置 registry 状态。
        /// Settings registry state.
        /// </summary>
        public IList<PortalSettingHealthInfo> Settings { get; private set; }

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
