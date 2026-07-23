# HIA-ASPNETPortal AI 开发规范

## Shell 与编码

- Windows 下默认使用 PowerShell 7：`C:\Program Files\PowerShell\7\pwsh.exe`。
- 项目文本统一使用 UTF-8 无 BOM；需要入库的文件尽量保持 CRLF。
- PowerShell 读写文本时显式指定 UTF-8，并避免会写入 BOM 的旧版默认行为。

## 项目边界

- 本项目保持 .NET Framework 4.8 与 ASP.NET Web Forms 路线，现阶段不迁移 ASP.NET Core。
- Visual Studio 是主开发路径，VSCode 是自动化、AI 协作和调试增强路径。
- VSCode 任务、脚本和配置不得破坏 `.sln`、`.csproj`、`.csproj.user`、VS Task Runner 或既有 Visual Studio 调试流程。
- 前端继续保留原 Gulp 绑定；自动化使用独立 `assets:build` 等任务。
- 修改真实环境配置前，必须区分模板与本地配置；不得记录或提交真实连接串、密码、Token、证书等敏感信息。

## 文档与仓库边界

- 根项目仓库保存源码、构建脚本、发布材料、用户文档和二次开发文档。
- `work-zone/` 是独立私有 Git 仓库，保存内部计划、阶段状态、任务、研究草稿、ADR、AI 日志和交接材料。
- 内部规划默认放在 `work-zone/dev/plans/`，短期任务放在 `work-zone/dev/tasks/`，ADR 放在 `work-zone/docs/adr/`。
- 根级 `ai/` 仅保留迁移说明；根级 `dev/` 只保留公共脚本和入口说明；根级 `docs/` 只保留整理后的公开文档。
- 不为 `work-zone/` 创建 submodule、符号链接或 junction；两个仓库分别提交。
- `work-zone/` 中同样不得保存任何密钥、令牌、Cookie、生产凭据或私有证书。
- 根级 `.serena/` 是本机符号索引和项目记忆状态，默认忽略，不作为正式文档或公开提交内容。

## AI 工作区

- `work-zone/ai/share/` 是共享区，所有 AI 可读写。
- `work-zone/ai/codex/`、`work-zone/ai/qoder/`、`work-zone/ai/comate/` 分别由对应 AI 维护。
- 不直接修改其他 AI 的专属目录；交接、审查和冲突讨论统一写入 `work-zone/ai/share/`。
- 修改已有文档或代码前先读取现状并检查 Git 状态，不覆盖用户或其他协作者的未提交改动。

## 设计与开发

- 重要任务先读取相关设计、任务、ADR 和目标代码，再开始修改。
- 涉及架构、协议、解析规则、公共 API、文档体系等基础设计时，先调查官方规范、成熟实现和生态惯例，并把关键依据记录到研究文档、计划或 ADR。
- 如果后续实现依赖尚未明确的概念、边界或协议，应主动提醒用户先细化并形成草案或 ADR。
- 新增第三方依赖前记录用途与许可证；优先 MIT、Apache-2.0、BSD、ISC、SIL OFL 等宽松许可证。
- 新增或结构性修改 public/protected API、配置契约、安全/路径/异常逻辑和复杂流程时，必须补充准确、完整的中英双语 XML/JSDoc 注释；旧代码采用 touch-improve，优先补本次触达的高风险语义。
- 注释以语义风险覆盖为准，不逐行复述赋值、循环和条件；参数、返回、异常、副作用、回退、兼容和安全限制按适用范围说明，并与实现和测试在同一提交中更新。
- HIA-Documentation-Sys DotNetDoc 已支持 C# XML documentation 中的 `<lang>` 与 `<l>` locale 标记；P15 起新增或重点补强的 C# XML 注释优先使用标准 XML 文档 block 内的 `<lang><zh-CN>...</zh-CN><en>...</en></lang>` 或字段内 `<l>` 形式。既有“中文 / English”双语段落作为历史兼容输入保留，按 P15 注释样例和优先级逐步迁移，不机械批量改写。
- P15 起新增或重点补强的节点内部代码块注释也采用内嵌 `<lang>` 双语块；ASPX/ASCX/Master 标记层优先使用不会输出到客户端的 `<%-- ... --%>`，并在复杂结构、权限/安全边界、数据绑定和兼容性区域说明用途与限制。
- 改动保持小步、聚焦和可验证；旧 Web Forms 行为不确定时先核查，不凭现代框架经验直接推断。
- 测试覆盖与风险相匹配；至少执行相关构建、静态扫描或手工回归，并记录未验证风险。

## 字体规则

- 普通文字字体不随项目分发，样式使用开源字体优先栈并保留 generic fallback。
- 不主动引用 Verdana、Arial、Helvetica、Times New Roman、Courier New、Consolas、Segoe UI、Microsoft YaHei 等专有或授权边界不清的字体。
- 图标字体仅在许可证清楚且保留必要声明时使用；新增或更新字体资产前必须补许可证审计。

## Codex 日志与提交提醒

- 每次会话在 `work-zone/ai/codex/chatlog/YYYY-MM-DD/主题-HHmmss.md` 记录可公开的工程过程，并在最终回复附上日志链接。
- 日志按时间片记录工作内容、数字序号进度说明、执行命令、关键输出、失败重试、验证结果和后续事项；不记录逐字私有思维链。
- 当改动文件较多或存在较多未提交内容时，提醒用户考虑分批提交，但不主动执行 `git add` 或 `git commit`。
- 提交提醒要区分两个仓库：项目代码和公开文档提交到主仓库，内部资料与日志提交到 WorkZone 私有仓库。

## 任务账本与反循环协议

- 长任务开始、上下文恢复、阶段切换或继续推进前，必须先读取根目录 `TASK_STATE.md`、最新 `work-zone/dev/CURRENT_STATUS_YYYY-MM-DD.md`、`work-zone/dev/plans/W-anp-INDEX.md` 和相关阶段计划。
- 继续动手前同时核对主仓库与 WorkZone 的 `git status --short`，并识别哪些是既有残留、哪些是本轮相关改动。
- `TASK_STATE.md` 记录动态状态：当前目标、完成条件、当前里程碑、已完成项及验证证据、失败尝试、唯一下一步、最后代码状态和连续无进展次数。
- 阶段完成、进入新阶段、改变下一步唯一动作、出现失败重试或准备长时间暂停时，必须更新 `TASK_STATE.md`。
- 不得重复执行已标记为 `completed`、`abandoned` 或 `deferred` 的动作，除非用户明确要求复核。
- 同一命令或同一方案连续失败 2 次，或连续 2 轮没有新代码、测试结果、文档证据或用户确认时，必须暂停并报告，不得继续循环。
- 达到可验证完成条件后，应写入结果/closeout 或任务账本，再进入下一讨论节点；不要因为上下文缺失而重新开始已完成阶段。
- `TASK_STATE.md` 不记录密码、连接串、Token、Cookie、证书私钥、生产配置或敏感截图；详细内部依据放在 WorkZone 计划、证据或日志中。
