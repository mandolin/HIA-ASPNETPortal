using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项首批参考集合及未部署目录时的受限兼容值。</zh-CN>
    ///   <en>First collaboration-item reference sets and restricted compatibility values for an undeployed catalog.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些值只在 `PortalBiz_ReferenceData` 尚未部署或不可读时提供回退。目录可读后，调用方必须以目录的已启用值为准，不能用该回退绕过停用。</zh-CN>
    ///   <en>These values are a fallback only while `PortalBiz_ReferenceData` is undeployed or unreadable. Once the catalog is readable, callers must use its active values and must not use this fallback to bypass deactivation.</en>
    /// </lang>
    /// </remarks>
    public static class PortalReferenceDataSets
    {
        /// <summary><lang><zh-CN>协同事项类型集合键。</zh-CN><en>Collaboration-item type set key.</en></lang></summary>
        public const string CollaborationItemType = "CollaborationItemType";

        /// <summary><lang><zh-CN>协同事项优先级集合键。</zh-CN><en>Collaboration-item priority set key.</en></lang></summary>
        public const string CollaborationPriority = "CollaborationPriority";

        /// <summary><lang><zh-CN>默认通用协同类型键。</zh-CN><en>Default general-collaboration type key.</en></lang></summary>
        public const string GeneralItemType = "General";

        /// <summary><lang><zh-CN>默认普通优先级键。</zh-CN><en>Default normal-priority key.</en></lang></summary>
        public const string NormalPriority = "Normal";

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回指定集合的 P22 兼容目录副本。</zh-CN>
        ///   <en>Returns a P22-compatible catalog copy for one reference set.</en>
        /// </lang>
        /// </summary>
        /// <param name="referenceSetKey">
        /// <l><zh-CN>稳定参考集合键。</zh-CN><en>Stable reference-set key.</en></l>
        /// </param>
        /// <returns>
        /// <l><zh-CN>可安全由调用方枚举或修改的独立列表。</zh-CN><en>An independent list that callers may safely enumerate or modify.</en></l>
        /// </returns>
        public static IList<ReferenceDataItem> GetFallbackItems(string referenceSetKey)
        {
            // <lang>
            //   <zh-CN>每次调用都创建独立列表，调用方可以绑定、排序或追加占位项，而不会污染全局兼容种子。</zh-CN>
            //   <en>Each call creates an independent list so callers may bind, sort, or append placeholder items without mutating the global compatibility seeds.</en>
            // </lang>
            var items = new List<ReferenceDataItem>();
            // <lang>
            //   <zh-CN>协同事项类型回退只保留 P22 已接受的低敏业务类别，供目录表未部署时维持表单可用。</zh-CN>
            //   <en>The collaboration-item type fallback keeps only the low-sensitivity business categories accepted by P22, preserving form availability while the catalog table is undeployed.</en>
            // </lang>
            if (string.Equals(referenceSetKey, CollaborationItemType, StringComparison.Ordinal))
            {
                items.Add(CreateItem(CollaborationItemType, GeneralItemType, "通用协同", 10));
                items.Add(CreateItem(CollaborationItemType, "Content", "资料/内容协同", 20));
                items.Add(CreateItem(CollaborationItemType, "Operations", "资源/运维协同", 30));
                items.Add(CreateItem(CollaborationItemType, "Workflow", "业务流程协同", 40));
            }
            // <lang>
            //   <zh-CN>优先级回退保持最小集合，避免在目录治理尚不可读时引入未审计的业务级别。</zh-CN>
            //   <en>The priority fallback keeps the smallest set, avoiding unaudited business levels while catalog governance is not yet readable.</en>
            // </lang>
            else if (string.Equals(referenceSetKey, CollaborationPriority, StringComparison.Ordinal))
            {
                items.Add(CreateItem(CollaborationPriority, NormalPriority, "普通", 10));
                items.Add(CreateItem(CollaborationPriority, "Important", "重要", 20));
            }

            // <lang>
            //   <zh-CN>未知集合返回空副本，由调用方按业务上下文决定是否拒绝或展示无可选值。</zh-CN>
            //   <en>Unknown sets return an empty copy, leaving callers to reject the value or show no choices according to their business context.</en>
            // </lang>
            return items;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从 P22 兼容目录解析输入值对应的规范稳定键。</zh-CN>
        ///   <en>Resolves an input value to its canonical stable key from the P22-compatible catalog.</en>
        /// </lang>
        /// </summary>
        /// <param name="referenceSetKey">
        /// <l><zh-CN>稳定参考集合键。</zh-CN><en>Stable reference-set key.</en></l>
        /// </param>
        /// <param name="candidateValueKey">
        /// <l><zh-CN>待解析的输入值键。</zh-CN><en>Input value key to resolve.</en></l>
        /// </param>
        /// <param name="canonicalValueKey">
        /// <l><zh-CN>成功时返回规范稳定键；失败时为空。</zh-CN><en>Returns the canonical stable key on success; otherwise empty.</en></l>
        /// </param>
        /// <returns>
        /// <l><zh-CN>输入属于该兼容集合时为 <c>true</c>。</zh-CN><en><c>true</c> when the input belongs to the compatible set.</en></l>
        /// </returns>
        public static bool TryResolveFallbackValue(string referenceSetKey, string candidateValueKey, out string canonicalValueKey)
        {
            // <lang>
            //   <zh-CN>输出键先置空，确保解析失败时不会保留调用方传入的旧值或上一轮结果。</zh-CN>
            //   <en>The output key starts empty so a failed resolution cannot retain an old caller value or a previous result.</en>
            // </lang>
            canonicalValueKey = string.Empty;
            // <lang>
            //   <zh-CN>解析只遍历当前集合的兼容副本；目录可读时调用方不得走这条路径绕过停用值。</zh-CN>
            //   <en>Resolution scans only the compatibility copy for the current set; callers must not use this path to bypass deactivated values when the catalog is readable.</en>
            // </lang>
            foreach (ReferenceDataItem item in GetFallbackItems(referenceSetKey))
            {
                // <lang>
                //   <zh-CN>值键比较允许大小写差异，但返回值保持种子中的规范大小写，便于事实表持久化稳定键。</zh-CN>
                //   <en>Value-key comparison accepts casing differences, but the returned value keeps the seed's canonical casing for stable fact-table persistence.</en>
                // </lang>
                if (string.Equals(item.ValueKey, candidateValueKey, StringComparison.OrdinalIgnoreCase))
                {
                    canonicalValueKey = item.ValueKey;
                    return true;
                }
            }

            // <lang>
            //   <zh-CN>未命中表示候选值不属于受限兼容集合，调用方应按输入无效处理。</zh-CN>
            //   <en>A miss means the candidate does not belong to the restricted compatibility set and should be treated by callers as invalid input.</en>
            // </lang>
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建一条受限兼容参考数据项。</zh-CN>
        ///   <en>Creates one restricted compatibility reference-data item.</en>
        /// </lang>
        /// </summary>
        /// <param name="referenceSetKey">
        /// <l>
        ///   <zh-CN>该值所属的稳定集合键。</zh-CN>
        ///   <en>Stable set key that owns this value.</en>
        /// </l>
        /// </param>
        /// <param name="valueKey">
        /// <l>
        ///   <zh-CN>集合内稳定值键。</zh-CN>
        ///   <en>Stable value key within the set.</en>
        /// </l>
        /// </param>
        /// <param name="displayName">
        /// <l>
        ///   <zh-CN>面向用户展示的低敏名称。</zh-CN>
        ///   <en>Low-sensitivity name displayed to users.</en>
        /// </l>
        /// </param>
        /// <param name="sortOrder">
        /// <l>
        ///   <zh-CN>兼容集合内的稳定排序值。</zh-CN>
        ///   <en>Stable ordering value within the compatibility set.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>标记为启用和系统种子的参考数据项。</zh-CN>
        ///   <en>A reference-data item marked as active and system-seeded.</en>
        /// </l>
        /// </returns>
        private static ReferenceDataItem CreateItem(string referenceSetKey, string valueKey, string displayName, int sortOrder)
        {
            // <lang>
            //   <zh-CN>兼容项不分配数据库标识，也不携带说明字段，避免被误解为已经持久化的运营目录记录。</zh-CN>
            //   <en>Compatibility items do not assign database identifiers or descriptions, avoiding confusion with persisted operational catalog records.</en>
            // </lang>
            return new ReferenceDataItem
            {
                ReferenceSetKey = referenceSetKey,
                ValueKey = valueKey,
                DisplayName = displayName,
                SortOrder = sortOrder,
                IsActive = true,
                IsSystemSeed = true
            };
        }
    }
}
