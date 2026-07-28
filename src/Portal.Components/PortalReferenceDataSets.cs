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
            var items = new List<ReferenceDataItem>();
            if (string.Equals(referenceSetKey, CollaborationItemType, StringComparison.Ordinal))
            {
                items.Add(CreateItem(CollaborationItemType, GeneralItemType, "通用协同", 10));
                items.Add(CreateItem(CollaborationItemType, "Content", "资料/内容协同", 20));
                items.Add(CreateItem(CollaborationItemType, "Operations", "资源/运维协同", 30));
                items.Add(CreateItem(CollaborationItemType, "Workflow", "业务流程协同", 40));
            }
            else if (string.Equals(referenceSetKey, CollaborationPriority, StringComparison.Ordinal))
            {
                items.Add(CreateItem(CollaborationPriority, NormalPriority, "普通", 10));
                items.Add(CreateItem(CollaborationPriority, "Important", "重要", 20));
            }

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
            canonicalValueKey = string.Empty;
            foreach (ReferenceDataItem item in GetFallbackItems(referenceSetKey))
            {
                if (string.Equals(item.ValueKey, candidateValueKey, StringComparison.OrdinalIgnoreCase))
                {
                    canonicalValueKey = item.ValueKey;
                    return true;
                }
            }

            return false;
        }

        private static ReferenceDataItem CreateItem(string referenceSetKey, string valueKey, string displayName, int sortOrder)
        {
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
