---
layout: default
lang: zh-CN
parent: 产品概览
permalink: /docs-index/
title: HIA-ASPNETPortal 文档
---
# HIA-ASPNETPortal 文档

本目录是项目的公开维护文档入口，面向使用者、开发者和后续维护人员。内部规划与协作资料不随本站发布。

## 文档索引

- `architecture.md`：系统结构和主要模块说明。
- `dev-guide.md`：本地开发、构建、配置和调试指南。
- `versioning.md`：版本号规则、版本与里程碑的关系、程序集版本联动、发布流程与宣传口径约束。
- `frontend-asset-guide.md`：Web Forms 呈现、主题、模块 CSS、Gulp 与前端资产边界。
- `theme-package-guide.md`：受信任主题包的目录、启用、回退和发布边界。
- `theme-semantic-variables.md`：主题层 `--portal-*` 语义变量的矩阵（7 套主题 × **14 个变量**）、用途速查、使用规则与复现命令。
- `theme-module-contract.md`：模块外观契约（主题层与模块层的分工、七族外观类与所用变量、`var()` 回退值的真实含义与已审计差异）。
- `module-development-guide.md`：受信任部署模块包的开发、注册、启停和移除边界。
- `product-overview.md`：产品概览与定位（是什么、给谁用、能力地图、边界、兼容性与许可证、适用场景）。
- `user-guide.md`：运行和使用入口说明，含门户配置流程、管理员常用操作与故障排除。
- `testing-checklist.md`：构建、测试、手工验证和发布前检查清单。
- `documentation-artifacts-guide.md`：公开文档、JSDoc、XML 文档和生成物的输入输出边界。
- `documentation-review-checklist.md`：文档复核**人工走查清单**（正确性 / 双语对等 / 读者可达性 / 不误信门禁），对应自动化门禁无法证明的部分。
- `contracts/`：技术契约目录 —— 边界判定（无对外 REST/GraphQL）、[模块契约]({{ site.baseurl }}/contracts-module-manifest/)（`module.json`）、[配置契约]({{ site.baseurl }}/contracts-app-settings/)（`appSettings`）；入口见 [contracts/README.md]({{ site.baseurl }}/contracts-readme/)。
- `site-visual-baseline.md`：**官网视觉基线**（配色 / 字体 / 排版层级 / 代码块与表格取色 / 导航骨架），是 `site/` 取值的依据。
- `documentation-registry.md`：7 个文档族的适用性登记（适用对象、维护责任、更新触发条件）与已登记缺口。
- `documentation-map.md`：7 族文档的导航地图、阅读路径与缺口指向，用于按角色快速定位文档。
- `deployment-checklist.md`：SQL Server、IIS、外置配置和发布后回归清单。
- `deployment-rollback-guide.md`：文件、配置、数据库和证据回滚准备说明。
- `release-notes-template.md`：版本号、变更、验证摘要、延期补证和回滚入口模板。
- `operations-runbook.md`：运维入口、例行检查、日志维护、审计查询、备份提醒和证据包说明。
- `deployment-default-credentials.md`：默认账号、初始化脚本样例口令和旧凭据治理说明。
- `p12-sample-business-flow.md`：员工资料更正样板业务路径、样例数据 SQL 和 验收入口。
- `business-application-state-machine.md`：业务申请的状态词汇、允许迁移与 fail-closed 写入约束。
- `collaboration-item-state-machine.md`：协同事项的状态词汇、允许迁移与处理角色语义。
- `audit-retention-policy.md`：运营审计表的保留期原则与依据。
- `font-policy-and-audit.md`：字体使用原则、许可证边界和当前审计结果。
- `third-party-dependencies.md`：新增第三方依赖的用途、许可证和分发边界。

## 使用说明

- 本站文档同时提供中文与英文两个版本，可随时切换。
- 文档按「产品概览 / 评估 / 快速上手 / 学习 / 参考 / 开发 / 运维 / 变更」八个分区组织，见[文档地图]({{ site.baseurl }}/documentation-map/)。
