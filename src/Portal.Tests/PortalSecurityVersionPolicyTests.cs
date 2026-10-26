using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>守护 P42 修复：Application_AuthenticateRequest 的签退决策必须区分“安全版本真实不匹配”与“数据库版本无法判定（瞬态读取失败/旧库无状态表/状态行缺失，读回 0）”。后者不得签退，否则单例 DbContext 上的并发查询竞态会偶发把活跃会话踢出。</zh-CN>
    ///   <en>Guards the P42 fix: Application_AuthenticateRequest's revoke decision must distinguish a genuine security-version mismatch from an undetermined database version (transient read failure, legacy DB without the state table, or a missing state row, which reads back as 0). The latter must not revoke, otherwise concurrent queries on the singleton DbContext would intermittently sign out active sessions.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>纯函数不连库、不启容器，仅覆盖决策边界，符合项目“测试不打开数据库”的约定。</zh-CN>
    ///   <en>The pure function opens no database and starts no container; it only covers decision boundaries, per the project's "tests do not open a database" convention.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalSecurityVersionPolicyTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>票据不可读（无有效票据）时必然签退，与数据库版本无关。</zh-CN>
        ///   <en>An unreadable ticket (no valid ticket) always revokes, regardless of the database version.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void UnreadableTicket_AlwaysRevokes()
        {
            // <lang>
            //   <zh-CN>即使数据库版本可确定且与票据相同，票据不可读也要签退。</zh-CN>
            //   <en>Even when the database version is determinable and matches, an unreadable ticket still revokes.</en>
            // </lang>
            Assert.IsTrue(PortalAuthenticationService.ShouldRevokeForSecurityVersionMismatch(
                ticketSecurityVersion: 1,
                databaseSecurityVersion: 1,
                ticketSecurityVersionReadable: false));

            // <lang>
            //   <zh-CN>数据库版本无法判定（0）且票据不可读，依旧签退。</zh-CN>
            //   <en>Undetermined database version (0) with an unreadable ticket still revokes.</en>
            // </lang>
            Assert.IsTrue(PortalAuthenticationService.ShouldRevokeForSecurityVersionMismatch(
                ticketSecurityVersion: 1,
                databaseSecurityVersion: 0,
                ticketSecurityVersionReadable: false));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>P42 关键守护：票据可读、数据库版本为 0（无法判定）时不得签退。</zh-CN>
        ///   <en>P42 critical guard: when the ticket is readable and the database version is 0 (undetermined), do not revoke.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ReadableTicket_UndeterminedDatabaseVersion_DoesNotRevoke()
        {
            // <lang>
            //   <zh-CN>这正是单例 DbContext 并发竞态导致读取偶发返回 0 的场景；旧逻辑会把活跃会话踢出，新逻辑放行。</zh-CN>
            //   <en>This is exactly the scenario where the singleton-DbContext race makes the read intermittently return 0; the old logic signed out active sessions, the new logic allows them.</en>
            // </lang>
            Assert.IsFalse(PortalAuthenticationService.ShouldRevokeForSecurityVersionMismatch(
                ticketSecurityVersion: 1,
                databaseSecurityVersion: 0,
                ticketSecurityVersionReadable: true));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>票据可读且数据库版本与票据一致时不签退。</zh-CN>
        ///   <en>When the ticket is readable and the database version matches the ticket, do not revoke.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ReadableTicket_MatchingVersion_DoesNotRevoke()
        {
            Assert.IsFalse(PortalAuthenticationService.ShouldRevokeForSecurityVersionMismatch(
                ticketSecurityVersion: 1,
                databaseSecurityVersion: 1,
                ticketSecurityVersionReadable: true));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>真实失效：数据库版本递增（密码重置/角色变更）且大于票据版本时应签退。</zh-CN>
        ///   <en>Genuine invalidation: a database version incremented (password reset or role change) above the ticket version must revoke.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ReadableTicket_DatabaseVersionAhead_Revokes()
        {
            Assert.IsTrue(PortalAuthenticationService.ShouldRevokeForSecurityVersionMismatch(
                ticketSecurityVersion: 1,
                databaseSecurityVersion: 2,
                ticketSecurityVersionReadable: true));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>真实失效：数据库版本低于票据版本（状态被回退）时也应签退。</zh-CN>
        ///   <en>Genuine invalidation: a database version below the ticket version (state rolled back) must also revoke.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ReadableTicket_DatabaseVersionBehind_Revokes()
        {
            Assert.IsTrue(PortalAuthenticationService.ShouldRevokeForSecurityVersionMismatch(
                ticketSecurityVersion: 2,
                databaseSecurityVersion: 1,
                ticketSecurityVersionReadable: true));
        }
    }
}
