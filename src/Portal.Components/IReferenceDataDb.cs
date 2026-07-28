using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>读取受治理业务参考数据目录的只读契约。</zh-CN>
    ///   <en>Read-only contract for the governed business reference-data catalog.</en>
    /// </lang>
    /// </summary>
    public interface IReferenceDataDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>尝试读取指定集合中允许新事实使用的目录值。</zh-CN>
        ///   <en>Tries to read catalog values in one set that may be used by new facts.</en>
        /// </lang>
        /// </summary>
        /// <param name="referenceSetKey">
        /// <l><zh-CN>稳定参考集合键。</zh-CN><en>Stable reference-set key.</en></l>
        /// </param>
        /// <param name="items">
        /// <l><zh-CN>成功时返回已启用、已排序的值；失败时返回空集合。</zh-CN><en>Returns active ordered values on success; returns an empty collection on failure.</en></l>
        /// </param>
        /// <returns>
        /// <l><zh-CN>目录表可读取时为 <c>true</c>；未部署或读取失败时为 <c>false</c>。</zh-CN><en><c>true</c> when the catalog table is readable; <c>false</c> when it is undeployed or cannot be read.</en></l>
        /// </returns>
        bool TryGetActiveItems(string referenceSetKey, out IList<ReferenceDataItem> items);
    }
}
