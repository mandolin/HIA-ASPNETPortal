using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>裁定强密码哈希的目标 PBKDF2 迭代次数，以及存量凭据是否需要重哈希。</zh-CN>
    ///   <en>Decides the target PBKDF2 iteration count for strong password hashes and whether stored credentials need rehashing.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类不依赖 System.Web，也不反向依赖 Web 配置读取器：Web 启动期通过 ConfigureTargetProvider 注入当前配置值，未注入或注入失败一律回落到硬下限 MinimumIterationCount（600000），确保哈希强度只增不减，且登录流程不受配置故障影响。</zh-CN>
    ///   <en>This class depends on neither System.Web nor the Web configuration resolver: Web startup injects the current configured value through ConfigureTargetProvider, and missing or failing injection always falls back to the hard lower bound MinimumIterationCount (600000), so hashing strength only increases and sign-in is never affected by configuration faults.</en>
    /// </lang>
    /// </remarks>
    public static class PortalPasswordIterationPolicy
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>允许的最低迭代次数硬下限，等于组件当前默认成本；配置值低于它时一律回落。</zh-CN>
        ///   <en>Hard lower bound for the iteration count, equal to the component's current default cost; lower configured values always fall back to it.</en>
        /// </lang>
        /// </summary>
        public const int MinimumIterationCount = 600000;

        /// <summary>
        /// <lang>
        ///   <zh-CN>保护运行期目标提供器引用的同步锁，避免启动配置与并发校验读取到部分状态。</zh-CN>
        ///   <en>Synchronization lock protecting the runtime target-provider reference so startup configuration and concurrent validation cannot observe partial state.</en>
        /// </lang>
        /// </summary>
        private static readonly object TargetProviderLock = new object();

        /// <summary>
        /// <lang>
        ///   <zh-CN>由 Web 启动期注入的目标迭代次数提供器。</zh-CN>
        ///   <en>Target iteration-count provider injected during Web startup.</en>
        /// </lang>
        /// </summary>
        private static Func<int> targetProvider;

        /// <summary>
        /// <lang>
        ///   <zh-CN>配置运行期目标迭代次数提供器。</zh-CN>
        ///   <en>Configures the runtime target iteration-count provider.</en>
        /// </lang>
        /// </summary>
        /// <param name="provider">
        /// <l>
        ///   <zh-CN>返回当前目标迭代次数的委托；传入 <c>null</c> 会恢复硬下限。</zh-CN>
        ///   <en>Delegate returning the current target iteration count; <c>null</c> restores the hard lower bound.</en>
        /// </l>
        /// </param>
        public static void ConfigureTargetProvider(Func<int> provider)
        {
            // <lang>
            //   <zh-CN>在同步边界内原子替换提供器；传入 null 明确恢复组件层硬下限。</zh-CN>
            //   <en>Replace the provider atomically within the synchronization boundary; null explicitly restores the component-layer hard lower bound.</en>
            // </lang>
            lock (TargetProviderLock)
            {
                targetProvider = provider;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析当前目标迭代次数；低于硬下限或提供器故障时回落到硬下限。</zh-CN>
        ///   <en>Resolves the current target iteration count; values below the hard lower bound or provider failures fall back to the bound.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>不小于硬下限的目标迭代次数。</zh-CN>
        ///   <en>Target iteration count that is never below the hard lower bound.</en>
        /// </l>
        /// </returns>
        public static int ResolveTargetIterationCount()
        {
            // <lang>
            //   <zh-CN>复制提供器引用以缩短锁持有时间，并让外部提供器在锁外执行。</zh-CN>
            //   <en>Copy the provider reference to shorten lock hold time and execute external providers outside the lock.</en>
            // </lang>
            Func<int> provider;
            lock (TargetProviderLock)
            {
                provider = targetProvider;
            }

            // <lang>
            //   <zh-CN>未注入提供器时直接使用硬下限，与组件当前默认成本一致。</zh-CN>
            //   <en>Without an injected provider the hard lower bound is used, matching the component's current default cost.</en>
            // </lang>
            if (provider == null)
            {
                return MinimumIterationCount;
            }

            try
            {
                int configured = provider();

                // <lang>
                //   <zh-CN>低于硬下限的配置一律回落，避免在线配置把哈希成本降到不安全值。</zh-CN>
                //   <en>Configuration below the hard lower bound always falls back, preventing online configuration from lowering hashing cost to an unsafe value.</en>
                // </lang>
                if (configured < MinimumIterationCount)
                {
                    return MinimumIterationCount;
                }

                return configured;
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>提供器故障按 fail-safe 回落到硬下限，且不记录可能包含敏感配置上下文的异常。</zh-CN>
                //   <en>Fail safely back to the hard lower bound when the provider faults, without logging an exception that could contain sensitive configuration context.</en>
                // </lang>
                return MinimumIterationCount;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断给定凭据是否需按当前目标成本重哈希。</zh-CN>
        ///   <en>Determines whether a stored credential needs rehashing to the current target cost.</en>
        /// </lang>
        /// </summary>
        /// <param name="storedIterationCount">
        /// <l>
        ///   <zh-CN>凭据记录中已持久化的迭代次数。</zh-CN>
        ///   <en>Iteration count persisted on the credential record.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>凭据有效且低于目标成本时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the credential is valid and below the target cost.</en>
        /// </l>
        /// </returns>
        public static bool NeedsRehash(int storedIterationCount)
        {
            return NeedsRehash(storedIterationCount, ResolveTargetIterationCount());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按显式目标成本判断是否需要重哈希；已持久化次数非正（缺失或损坏）时不触发，交由既有校验路径处理。</zh-CN>
        ///   <en>Determines rehash need against an explicit target cost; a non-positive persisted count never triggers rehash and is left to the existing validation path.</en>
        /// </lang>
        /// </summary>
        /// <param name="storedIterationCount">
        /// <l>
        ///   <zh-CN>凭据记录中已持久化的迭代次数。</zh-CN>
        ///   <en>Iteration count persisted on the credential record.</en>
        /// </l>
        /// </param>
        /// <param name="targetIterationCount">
        /// <l>
        ///   <zh-CN>目标迭代次数。</zh-CN>
        ///   <en>Target iteration count.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已持久化次数为正且低于目标时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the persisted count is positive and below the target.</en>
        /// </l>
        /// </returns>
        public static bool NeedsRehash(int storedIterationCount, int targetIterationCount)
        {
            // <lang>
            //   <zh-CN>非正值代表凭据缺失或损坏，不在此猜测兼容语义，也不触发额外写入。</zh-CN>
            //   <en>A non-positive value means the credential is missing or damaged; no compatibility semantics are guessed here and no extra write is triggered.</en>
            // </lang>
            return storedIterationCount > 0 && storedIterationCount < targetIterationCount;
        }
    }
}
