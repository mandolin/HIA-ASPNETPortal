using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>组织子树展开纯函数的契约测试，锁定组织范围语义的 fail-closed、深度上限与环路终止行为。</zh-CN>
    ///   <en>Contract tests for the organization subtree expansion pure function, pinning the fail-closed, depth-cap, and cycle-termination behavior of organization scope.</en>
    /// </lang>
    /// </summary>
    [TestClass]
    public class PortalOrganizationTreeExpanderTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>子树应包含根节点与全部后代。</zh-CN>
        ///   <en>A subtree must contain the root and all of its descendants.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ExpandSubtreeIds_IncludesRootAndDescendants()
        {
            // <lang>
            //   <zh-CN>构造三层组织：1 为根，2/3 为其子，4 为 2 的子。</zh-CN>
            //   <en>Build a three-level organization: 1 is the root, 2 and 3 are its children, and 4 is a child of 2.</en>
            // </lang>
            IList<PortalOrganizationTreeEdge> edges = new List<PortalOrganizationTreeEdge>
            {
                new PortalOrganizationTreeEdge(1, null),
                new PortalOrganizationTreeEdge(2, 1),
                new PortalOrganizationTreeEdge(3, 1),
                new PortalOrganizationTreeEdge(4, 2),
                new PortalOrganizationTreeEdge(9, null)
            };

            IList<int> subtree = PortalOrganizationTreeExpander.ExpandSubtreeIds(edges, 1, 5);

            Assert.AreEqual(4, subtree.Count, "子树应包含根节点及其三个后代。");
            Assert.IsTrue(subtree.Contains(1), "子树必须包含根节点。");
            Assert.IsTrue(subtree.Contains(4), "子树必须包含孙级后代。");
            Assert.IsFalse(subtree.Contains(9), "另一棵树的节点不得进入本子树。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>深度上限必须限制展开层数；根节点深度为零。</zh-CN>
        ///   <en>The depth cap must bound the expansion levels; the root has depth zero.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ExpandSubtreeIds_RespectsMaxDepth()
        {
            IList<PortalOrganizationTreeEdge> edges = new List<PortalOrganizationTreeEdge>
            {
                new PortalOrganizationTreeEdge(1, null),
                new PortalOrganizationTreeEdge(2, 1),
                new PortalOrganizationTreeEdge(3, 2)
            };

            IList<int> capped = PortalOrganizationTreeExpander.ExpandSubtreeIds(edges, 1, 1);

            Assert.IsTrue(capped.Contains(1), "深度上限内必须包含根节点。");
            Assert.IsTrue(capped.Contains(2), "深度上限为一时应包含第一层后代。");
            Assert.IsFalse(capped.Contains(3), "第二层后代超出深度上限，不应出现。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>根节点非正值时按 fail-closed 返回空集合，不退化为全量。</zh-CN>
        ///   <en>A non-positive root fails closed with an empty set instead of degrading to all rows.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ExpandSubtreeIds_NonPositiveRoot_ReturnsEmpty()
        {
            IList<PortalOrganizationTreeEdge> edges = new List<PortalOrganizationTreeEdge>
            {
                new PortalOrganizationTreeEdge(1, null)
            };

            IList<int> zero = PortalOrganizationTreeExpander.ExpandSubtreeIds(edges, 0, 5);
            IList<int> negative = PortalOrganizationTreeExpander.ExpandSubtreeIds(edges, -3, 5);

            Assert.AreEqual(0, zero.Count, "根节点为零应返回空集合。");
            Assert.AreEqual(0, negative.Count, "根节点为负应返回空集合。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>根节点不在边集合中时返回空集合，避免凭空生成孤立范围。</zh-CN>
        ///   <en>A root absent from the edge set yields an empty set so no isolated scope is invented.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ExpandSubtreeIds_UnknownRoot_ReturnsEmpty()
        {
            IList<PortalOrganizationTreeEdge> edges = new List<PortalOrganizationTreeEdge>
            {
                new PortalOrganizationTreeEdge(1, null),
                new PortalOrganizationTreeEdge(2, 1)
            };

            IList<int> subtree = PortalOrganizationTreeExpander.ExpandSubtreeIds(edges, 99, 5);

            Assert.AreEqual(0, subtree.Count, "未知根节点必须返回空集合。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>边集合为空引用或空集合时返回空集合。</zh-CN>
        ///   <en>A null or empty edge set yields an empty set.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ExpandSubtreeIds_NullOrEmptyEdges_ReturnsEmpty()
        {
            IList<int> fromNull = PortalOrganizationTreeExpander.ExpandSubtreeIds(null, 1, 5);
            IList<int> fromEmpty = PortalOrganizationTreeExpander.ExpandSubtreeIds(new List<PortalOrganizationTreeEdge>(), 1, 5);

            Assert.AreEqual(0, fromNull.Count, "空引用边集合应返回空集合。");
            Assert.AreEqual(0, fromEmpty.Count, "空边集合应返回空集合。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>深度上限为非正值时返回空集合，不返回根节点。</zh-CN>
        ///   <en>A non-positive depth cap yields an empty set and does not even return the root.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ExpandSubtreeIds_NonPositiveMaxDepth_ReturnsEmpty()
        {
            IList<PortalOrganizationTreeEdge> edges = new List<PortalOrganizationTreeEdge>
            {
                new PortalOrganizationTreeEdge(1, null)
            };

            IList<int> subtree = PortalOrganizationTreeExpander.ExpandSubtreeIds(edges, 1, 0);

            Assert.AreEqual(0, subtree.Count, "深度上限为零应返回空集合。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>即使数据出现环路（写入侧防环被绕过），展开也必须终止且不重复计入节点。</zh-CN>
        ///   <en>Even when the data contains a cycle (write-side guard bypassed), expansion must terminate and must not count a node twice.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ExpandSubtreeIds_Cycle_TerminatesWithoutDuplicates()
        {
            // <lang>
            //   <zh-CN>构造 2→3→2 的环路，展开器不得无限递归。</zh-CN>
            //   <en>Build a 2→3→2 cycle; the expander must not recurse forever.</en>
            // </lang>
            IList<PortalOrganizationTreeEdge> edges = new List<PortalOrganizationTreeEdge>
            {
                new PortalOrganizationTreeEdge(1, null),
                new PortalOrganizationTreeEdge(2, 1),
                new PortalOrganizationTreeEdge(3, 2),
                new PortalOrganizationTreeEdge(2, 3)
            };

            IList<int> subtree = PortalOrganizationTreeExpander.ExpandSubtreeIds(edges, 1, 10);

            Assert.AreEqual(3, subtree.Count, "环路不得导致节点被重复计入。");
            Assert.IsTrue(subtree.Contains(1) && subtree.Contains(2) && subtree.Contains(3), "环路下的三个节点仍应各出现一次。");
        }
    }
}
