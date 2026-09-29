using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>组织邻接表的一条父子边，供子树展开使用。</zh-CN>
    ///   <en>One parent-child edge of the organization adjacency list, used for subtree expansion.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型只承载展开所需的两个标识，不含名称、状态或权限信息，因此不能用于授权判断。</zh-CN>
    ///   <en>This type carries only the two identifiers needed for expansion and no name, status, or permission data, so it must never be used for authorization decisions.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalOrganizationTreeEdge
    {
        /// <summary><lang><zh-CN>组织单元标识；非正值表示无效边。</zh-CN><en>Organization unit identifier; a non-positive value marks an invalid edge.</en></lang></summary>
        public int OrganizationUnitId { get; private set; }

        /// <summary><lang><zh-CN>父组织单元标识；为空表示根节点。</zh-CN><en>Parent organization unit identifier; null denotes a root node.</en></lang></summary>
        public int? ParentOrganizationUnitId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用组织单元标识与父标识初始化一条边。</zh-CN>
        ///   <en>Initializes an edge with an organization unit identifier and its parent identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="organizationUnitId"><l><zh-CN>组织单元标识。</zh-CN><en>The organization unit identifier.</en></l></param>
        /// <param name="parentOrganizationUnitId"><l><zh-CN>父组织单元标识；可为空。</zh-CN><en>The parent organization unit identifier; may be null.</en></l></param>
        public PortalOrganizationTreeEdge(int organizationUnitId, int? parentOrganizationUnitId)
        {
            OrganizationUnitId = organizationUnitId;
            ParentOrganizationUnitId = parentOrganizationUnitId;
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>组织邻接表的纯函数子树展开器，不访问数据库、不依赖会话或权限。</zh-CN>
    ///   <en>Pure-function subtree expander for the organization adjacency list; it touches no database and depends on no session or permission.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>把展开做成纯函数是为了让"组织范围"这一安全相关语义可以在没有数据库的环境中被单测覆盖；数据层仍然负责取数、停用过滤与失败处理。展开按广度优先进行，并用深度上限与"已访问"集合双重保护，即使写入侧的防环被绕过也不会无限递归。</zh-CN>
    ///   <en>Expansion is a pure function so that the security-relevant "organization scope" semantics can be unit tested without a database; the data layer still owns retrieval, inactive filtering, and failure handling. Expansion is breadth-first with both a depth cap and a visited set, so a malformed cycle cannot cause unbounded recursion even if the write-side cycle guard were bypassed.</en>
    /// </lang>
    /// </remarks>
    public static class PortalOrganizationTreeExpander
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>返回以指定组织为根的子树标识集合（含根节点），深度不超过上限。</zh-CN>
        ///   <en>Returns the identifier set of the subtree rooted at the specified organization (root included), limited by the depth cap.</en>
        /// </lang>
        /// </summary>
        /// <param name="edges">
        /// <l>
        ///   <zh-CN>组织邻接表的父子边集合；可为空引用或空集合。</zh-CN>
        ///   <en>The parent-child edges of the organization adjacency list; may be null or empty.</en>
        /// </l>
        /// </param>
        /// <param name="rootOrganizationUnitId">
        /// <l>
        ///   <zh-CN>子树根节点标识；非正值或不在边集合中时返回空集合（fail-closed，不退化为全量）。</zh-CN>
        ///   <en>The subtree root identifier; a non-positive value or one absent from the edge set yields an empty set (fail-closed, never degrading to all rows).</en>
        /// </l>
        /// </param>
        /// <param name="maxDepth">
        /// <l>
        ///   <zh-CN>允许的最大后代层数；非正值返回空集合。根节点深度为零。</zh-CN>
        ///   <en>Maximum allowed descendant levels; a non-positive value yields an empty set. The root has depth zero.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>子树标识列表（含根节点，按访问顺序）；无命中或入参无效时为空列表。</zh-CN>
        ///   <en>The subtree identifier list (root included, in visit order); empty when nothing matches or the input is invalid.</en>
        /// </l>
        /// </returns>
        public static IList<int> ExpandSubtreeIds(
            IEnumerable<PortalOrganizationTreeEdge> edges,
            int rootOrganizationUnitId,
            int maxDepth)
        {
            IList<int> result = new List<int>();

            // <lang>
            //   <zh-CN>入参无效时按 fail-closed 返回空集合：既不返回全量，也不把"范围无法判定"伪装成命中全部。</zh-CN>
            //   <en>Invalid input fails closed with an empty set: it neither returns everything nor disguises an undecidable scope as a full match.</en>
            // </lang>
            if (edges == null || rootOrganizationUnitId <= 0 || maxDepth <= 0)
            {
                return result;
            }

            // <lang>
            //   <zh-CN>先收集有效节点，再建子索引；根节点不在已知节点中时直接返回空，避免凭空生成一个孤立范围。</zh-CN>
            //   <en>Collect valid nodes first, then build the child index; when the root is unknown, return empty instead of inventing an isolated scope.</en>
            // </lang>
            HashSet<int> known = new HashSet<int>();
            Dictionary<int, List<int>> children = new Dictionary<int, List<int>>();
            foreach (PortalOrganizationTreeEdge edge in edges)
            {
                if (edge == null || edge.OrganizationUnitId <= 0)
                {
                    continue;
                }

                known.Add(edge.OrganizationUnitId);
            }

            if (!known.Contains(rootOrganizationUnitId))
            {
                return result;
            }

            foreach (PortalOrganizationTreeEdge edge in edges)
            {
                if (edge == null || edge.OrganizationUnitId <= 0 || !edge.ParentOrganizationUnitId.HasValue)
                {
                    continue;
                }

                int parentId = edge.ParentOrganizationUnitId.Value;
                if (parentId <= 0)
                {
                    continue;
                }

                List<int> siblings;
                if (!children.TryGetValue(parentId, out siblings))
                {
                    siblings = new List<int>();
                    children.Add(parentId, siblings);
                }

                siblings.Add(edge.OrganizationUnitId);
            }

            // <lang>
            //   <zh-CN>广度优先展开：深度上限控制层数，visited 集合同时防止重访与环路造成的无限递归。</zh-CN>
            //   <en>Breadth-first expansion: the depth cap bounds levels, and the visited set prevents both revisits and unbounded recursion from cycles.</en>
            // </lang>
            Dictionary<int, int> depths = new Dictionary<int, int>();
            Queue<int> pending = new Queue<int>();
            depths.Add(rootOrganizationUnitId, 0);
            pending.Enqueue(rootOrganizationUnitId);

            while (pending.Count > 0)
            {
                int current = pending.Dequeue();
                result.Add(current);

                int currentDepth = depths[current];
                if (currentDepth >= maxDepth)
                {
                    continue;
                }

                List<int> childIds;
                if (!children.TryGetValue(current, out childIds))
                {
                    continue;
                }

                foreach (int childId in childIds)
                {
                    if (depths.ContainsKey(childId))
                    {
                        continue;
                    }

                    depths.Add(childId, currentDepth + 1);
                    pending.Enqueue(childId);
                }
            }

            return result;
        }
    }
}
