using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Resources;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>把协同事项的参与人集合渲染为人可读的展示文本：角色键映射为本地化角色名。</zh-CN>
    ///   <en>Renders a collaboration item's participant set as human-readable display text, mapping role keys to localized role names.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P74.4 起供前台工作台与后台协同事项页共用。此前两处各自把 `<c>ParticipantRoleKey</c>` 原样拼进界面（形如 `张三 (Collaborator)`），把内部标识符暴露给终端用户，且空集合与空字段回退为硬编码英文 `(none)`。本类统一处理三件事：① 已知角色键 → 本地化角色名（词表来自 <see cref="PortalCollaborationItemParticipantRoles"/>）；② **未知角色键 → 回退为原始键**，不臆造译文（与全局既有做法一致）；③ 空集合与空字段 → 本地化占位文本。角色词表是**领域事实**而非某个页面的私有词条，故角色名与占位文案使用 `Collaboration_*` / `Common_*` 共享键，不按页面各造一套。输出文本**不在此处编码**，由标记层的编码绑定（`&lt;%#: %&gt;`）负责，避免二次编码。</zh-CN>
    ///   <en>Shared by the front-end workbench and the Admin collaboration page starting with P74.4. Both sites previously interpolated `<c>ParticipantRoleKey</c>` verbatim (for example `Zhang (Collaborator)`), exposing an internal identifier to end users, and fell back to a hard-coded English `(none)` for empty sets and blank fields. This type centralizes three things: (1) a known role key becomes a localized role name (the vocabulary comes from <see cref="PortalCollaborationItemParticipantRoles"/>); (2) an **unknown role key falls back to the raw key** rather than inventing a translation, matching established practice elsewhere; (3) an empty set or blank field becomes the localized placeholder. The role vocabulary is a **domain fact** rather than one page's private wording, so role names and the placeholder use shared `Collaboration_*` / `Common_*` keys instead of per-page copies. Output is **not encoded here**; the markup's encoded binding (`&lt;%#: %&gt;`) owns that, which avoids double encoding.</en>
    /// </lang>
    /// </remarks>
    public static class PortalCollaborationParticipantText
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>把单个角色键转换为本地化角色名；未知键回退为去空白后的原始键，空值返回空串。</zh-CN>
        ///   <en>Converts a single role key to its localized role name; an unknown key falls back to the trimmed raw key and a blank value yields an empty string.</en>
        /// </lang>
        /// </summary>
        /// <param name="participantRoleKey">
        /// <l>
        ///   <zh-CN>参与角色键，通常来自 <see cref="PortalCollaborationItemParticipantRoles"/>。</zh-CN>
        ///   <en>Participant role key, normally from <see cref="PortalCollaborationItemParticipantRoles"/>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>本地化角色名；未知键为原始键，空值为空串。</zh-CN>
        ///   <en>The localized role name; the raw key when unknown, or an empty string when blank.</en>
        /// </l>
        /// </returns>
        public static string GetRoleDisplayText(string participantRoleKey)
        {
            // <lang>
            //   <zh-CN>先做去空白与空值判定，避免把仅含空格的键当成"未知角色"而回退成一个不可读的空白高亮。</zh-CN>
            //   <en>Trim and check for blank first so a whitespace-only key is not treated as an unknown role and echoed back as unreadable whitespace.</en>
            // </lang>
            string roleKey = participantRoleKey == null ? string.Empty : participantRoleKey.Trim();
            if (roleKey.Length == 0)
            {
                return string.Empty;
            }

            // <lang>
            //   <zh-CN>已知角色按封闭词表匹配并取共享资源键；比较使用 Ordinal，避免区域性规则把不同键视为相等。</zh-CN>
            //   <en>Known roles match the closed vocabulary and resolve to shared resource keys; comparison is ordinal so culture rules cannot equate distinct keys.</en>
            // </lang>
            if (string.Equals(roleKey, PortalCollaborationItemParticipantRoles.Collaborator, StringComparison.Ordinal))
            {
                return lang.Collaboration_ParticipantRole_Collaborator;
            }

            if (string.Equals(roleKey, PortalCollaborationItemParticipantRoles.Watcher, StringComparison.Ordinal))
            {
                return lang.Collaboration_ParticipantRole_Watcher;
            }

            // <lang>
            //   <zh-CN>未知角色一律回退原始键，不臆造译文：数据层若引入新角色，界面至少仍显示其真实标识而不是错误的角色名。</zh-CN>
            //   <en>An unknown role always falls back to the raw key instead of inventing a translation: if the data layer introduces a new role, the UI at least shows its real identifier rather than a wrong role name.</en>
            // </lang>
            return roleKey;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把参与人集合渲染为展示文本；空集合返回本地化占位文本。</zh-CN>
        ///   <en>Renders the participant set as display text; an empty set returns the localized placeholder.</en>
        /// </lang>
        /// </summary>
        /// <param name="participants">
        /// <l>
        ///   <zh-CN>参与人集合，可为 <c>null</c>；条目或其用户名为空时按占位文本呈现。</zh-CN>
        ///   <en>The participant collection, which may be <c>null</c>; a blank entry or blank user name renders as the placeholder.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>"用户名（本地化角色名）" 以逗号连接的文本；空集合为占位文本。</zh-CN>
        ///   <en>Comma-joined "user name (localized role name)" text, or the placeholder when the set is empty.</en>
        /// </l>
        /// </returns>
        public static string BuildParticipantsText(IList<CollaborationItemParticipantInfo> participants)
        {
            if (participants == null || participants.Count == 0)
            {
                return lang.Common_NonePlaceholder;
            }

            StringBuilder builder = new StringBuilder();
            foreach (CollaborationItemParticipantInfo participant in participants)
            {
                // <lang>
                //   <zh-CN>用户名缺失时用占位文本，保持"用户名（角色）"的形状不塌陷成孤立的括号。</zh-CN>
                //   <en>A missing user name uses the placeholder so the "user name (role)" shape does not collapse into a bare pair of brackets.</en>
                // </lang>
                string userName = participant == null || string.IsNullOrWhiteSpace(participant.UserName)
                    ? lang.Common_NonePlaceholder
                    : participant.UserName.Trim();
                string roleText = participant == null
                    ? string.Empty
                    : GetRoleDisplayText(participant.ParticipantRoleKey);

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                // <lang>
                //   <zh-CN>括号与分隔符由资源格式串提供：不同语言的全角/半角标点不同，硬编码会破坏本地化排版。</zh-CN>
                //   <en>Brackets and spacing come from the resource format string: full-width and half-width punctuation differ per language, and hard-coding would break localized typography.</en>
                // </lang>
                builder.Append(string.Format(
                    CultureInfo.CurrentCulture,
                    lang.Collaboration_ParticipantDisplayFormat,
                    userName,
                    roleText));
            }

            return builder.ToString();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把参与人集合渲染为**逐人一行**的展示序列；空集合返回仅含占位文本的单元素序列。</zh-CN>
        ///   <en>Renders the participant set as **one line per person**; an empty set returns a single placeholder line.</en>
        /// </lang>
        /// </summary>
        /// <param name="participants">
        /// <l>
        ///   <zh-CN>参与人集合，可为 <c>null</c>；条目或其用户名为空时按占位文本呈现。</zh-CN>
        ///   <en>The participant collection, which may be <c>null</c>; a blank entry or blank user name renders as the placeholder.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>每项为"用户名（本地化角色名）"的只读序列；空集合为仅含占位文本的序列。</zh-CN>
        ///   <en>A read-only sequence whose items are "user name (localized role name)", or a sequence holding only the placeholder when the set is empty.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>与 <see cref="BuildParticipantsText"/> **共用同一条格式串** <c>Collaboration_ParticipantDisplayFormat</c>，
        ///   因此"逗号连接"与"逐人一行"两种呈现不会出现本地化排版的漂移 —— 这正是 <c>P74.4</c> 引入共享渲染器要防的问题。
        ///   本方法是**新增**而非替换：逗号串仍被前台与后台的两处只读展示使用，逐行呈现按需选用，互不影响。</zh-CN>
        ///   <en>This method **shares the same format string** <c>Collaboration_ParticipantDisplayFormat</c> with
        ///   <see cref="BuildParticipantsText"/>, so the "comma-joined" and "one line per person" renderings cannot drift apart
        ///   in localized typography — exactly what the shared renderer introduced in P74.4 was meant to prevent. It is
        ///   **added rather than replacing**: the comma-joined text is still used by the two read-only displays in the front
        ///   office and the admin area, while the per-line rendering is opted into where needed.</en>
        /// </lang>
        /// </remarks>
        public static IList<string> BuildParticipantLines(IList<CollaborationItemParticipantInfo> participants)
        {
            if (participants == null || participants.Count == 0)
            {
                return new[] { lang.Common_NonePlaceholder };
            }

            List<string> lines = new List<string>(participants.Count);
            foreach (CollaborationItemParticipantInfo participant in participants)
            {
                string userName = participant == null || string.IsNullOrWhiteSpace(participant.UserName)
                    ? lang.Common_NonePlaceholder
                    : participant.UserName.Trim();
                string roleText = participant == null
                    ? string.Empty
                    : GetRoleDisplayText(participant.ParticipantRoleKey);

                lines.Add(string.Format(
                    CultureInfo.CurrentCulture,
                    lang.Collaboration_ParticipantDisplayFormat,
                    userName,
                    roleText));
            }

            return lines;
        }
    }
}
