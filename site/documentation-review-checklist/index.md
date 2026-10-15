---
layout: default
lang: zh-CN
parent: 参考
permalink: /documentation-review-checklist/
title: 文档复核走查清单
---
# 文档复核走查清单

> **P21 `W-anp-P127` 交付物**。这是**人工走查清单**，对应文档验证门禁中"自动化无法证明"的部分。
> 自动化门禁只证明结构（链接可达、引用存在、双语结构齐备、索引覆盖）；**语义是否正确、示例是否跑得通、表述是否误导读者，必须靠人**。

## 一、什么时候要走查

| 触发时机 | 走查范围 |
| --- | --- |
| 新增或大改公开文档 | 该文档全量 |
| 改变接口 / 配置 / 流程的代码 | 引用到它的**所有**文档 |
| 发版前 | 产品族（`product-overview.md`、`user-guide.md`）全量 |
| 发现文档被读者反馈有误 | 该文档 + 同类表述的其它文档 |

## 二、走查项

### A. 正确性（最重要）

- [ ] 文档中的**路径、命令、参数、键名**与当前源码一致（命令示例的路径由 `Test-PortalDocumentationExamples.ps1` 保证存在，但**参数与输出需人工核对**）
- [ ] 引用的**默认值 / 版本号 / 数量**不是复制来的旧快照（改代码时是否同步？）
- [ ] 声明的**边界与"不做的事"**仍然成立（例如"无对外 REST API"是否仍成立）
- [ ] **成熟度口径**未被无意夸大（例如不得把 L10 达成写成"生产就绪"）

### B. 双语对等

- [ ] 根 `README.md` 的中文节与 `## English` 节**事实一致**（版本号、能力清单、成熟度口径逐项对齐）
- [ ] 代码注释中的 `<lang>` 块**中文与英文含义对等**，不是单向翻译（语义对等，不是逐词对应）
- [ ] 术语译法统一（同一概念不混用多种译名）

### C. 读者可达性

- [ ] 新读者能按 [documentation-map.md](documentation-map.md) 的**读者路径**找到需要的文档，不会迷路
- [ ] 文档间的链接形成闭环，不存在"被引用但不被索引"的孤岛
- [ ] 示例**可直接复制运行**（或明确写出前置条件）

### D. 与门禁的关系（避免误信）

- [ ] 未把"门禁通过"当作"文档内容正确"的证据 —— 门禁只覆盖结构
- [ ] 若文档含**教学占位**（如 `HIA.YourModule`），已让读者能识别它是占位而非真实路径

## 三、已有自动化覆盖（不要重复人工）

| 检查 | 门禁 |
| --- | --- |
| Markdown 相对链接有效性 | `Test-PortalPublicDocumentation.ps1` |
| 公开文档索引进度 | `Test-PortalPublicDocumentation.ps1` |
| 私有仓库链接 / 绝对路径 / 凭据形态（`docs/`） | `Test-PortalPublicDocumentation.ps1` |
| 私有仓库名 / 凭据形态 / 绝对路径 / 内部计划路径（**站点发布面 `site/`**） | `Test-PortalSitePublicSurface.ps1` |
| 命令示例引用的脚本 / SQL 路径存在 | `Test-PortalDocumentationExamples.ps1` |
| README 中英双语结构 | `Test-PortalDocumentationExamples.ps1` |
| 版本一致性（CHANGELOG / git tag / 程序集） | `Test-PortalVersionConsistency.ps1` |
| 代码 XML 文档覆盖率 | `Test-PortalXmlDocumentation.ps1` |
| 注释 `<lang>` 双语结构（源码） | `Test-PortalRopCommentBaseline.ps1` |

## 四、记录要求

走查发现的问题按两类处置：

1. **可即时修正**（错别字、过期数字、坏链）→ 直接改，并在提交信息说明；
2. **需决策或超范围** → 登记为延期项，写明**所有者、判定依据、再入条件**，不静默搁置。

走查结论（含"无问题"）应记录在当周期的**内部完成记录**中，**不得只做不记**。
