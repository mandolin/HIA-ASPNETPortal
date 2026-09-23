namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项参与人操作的轻量结果。</zh-CN>
    ///   <en>Lightweight result of an enterprise collaboration-item participant operation.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>只承载成败与低敏提示，不暴露 SQL、连接或异常细节。</zh-CN>
    ///   <en>Carries only success and a low-sensitivity message, without SQL, connection, or exception detail.</en>
    /// </lang>
    /// </remarks>
    public sealed class CollaborationItemParticipantResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建参与人操作结果。</zh-CN>
        ///   <en>Creates a participant-operation result.</en>
        /// </lang>
        /// </summary>
        /// <param name="success"><l zh-CN="操作是否成功。" en="Whether the operation succeeded." /></param>
        /// <param name="message"><l zh-CN="低敏结果提示。" en="Low-sensitivity result message." /></param>
        public CollaborationItemParticipantResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }

        /// <summary><lang><zh-CN>操作是否成功。</zh-CN><en>Whether the operation succeeded.</en></lang></summary>
        public bool Success { get; private set; }

        /// <summary><lang><zh-CN>低敏结果提示。</zh-CN><en>Low-sensitivity result message.</en></lang></summary>
        public string Message { get; private set; }
    }
}
