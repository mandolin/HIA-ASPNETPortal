---
layout: default
lang: zh-CN
parent: 参考
permalink: /third-party-dependencies/
title: 第三方依赖审计
---
# 第三方依赖审计

本文件记录本项目在 AI 协作治理建立后新引入或新升级的第三方依赖。历史依赖会在后续文档化周期中逐步补全，不代表未列出的旧包已完成许可证审计。

## 依赖清单

| 依赖 | 锁定版本 | 用途 | 许可证/依据 | 当前边界 |
| --- | --- | --- | --- | --- |
| `System.Data.SQLite.Core` | `1.0.119.0` | .NET Framework 4.8 SQLite ADO.NET capability proof。 | package 元数据链接至 SQLite Public Domain 版权声明；其 net46 build target 也声明为 Public Domain。 | 仅由独立 proof 项目使用，不进入门户主业务运行路径。 |
| `Stub.System.Data.SQLite.Core.NetFramework` | `1.0.119.0` | 上一行的同族安装期 stub（net48 target 的 native 运行时安放辅助），与主包同版本锁定。 | 与主包同源：SQLite Public Domain 版权声明。 | 仅随 proof 项目恢复，不进入门户主业务运行路径。 |
| `playwright` | `1.62.1` | L2 运行期证据/基线门禁（模块运行期、性能基线等）的浏览器自动化运行时。 | Apache-2.0（package.json 声明）。 | **开发期依赖、不入库**：位于 `temp/node_modules/playwright`，经 `PORTAL_PLAYWRIGHT_MODULE` 指向；不进入站点 `bin`、发布包或任何运行路径。 |

## 机器可读基线与变更铁律

`dev/dependency-baseline.json` 是**已审计依赖集**的机器快照（含全部 packages.config 声明与上述开发期依赖）。依赖集任何增删或版本变更必须三处同步：①本文件登记；②重估漏洞面（L0 门禁自动扫描 NuGet 漏洞数据源）；③有意更新该基线。缺任何一处，门禁 `Test-PortalDependencyVulnerabilityBaseline.ps1` 即失败（default-deny：不允许未经审计的依赖变更）。

## 使用与分发约束

1. proof 项目使用 packages.config 恢复包；`src/packages/` 仍由现有忽略规则排除，不提交二进制包。
2. SQLite 运行时文件随 proof 输出到 `temp/provider-proof/`，不进入站点 `bin`、发布包或门户正常启动路径。
3. 生成的 `.sqlite` 文件只用于开发/测试，脚本会默认重新创建；不得放入仓库、内部目录或外置生产配置目录。
4. 若未来将 SQLite、MySQL、PostgreSQL 等 provider 用于真实门户能力，必须新增对应版本、许可证、native 运行时和部署说明审计，并完成独立 ADR 与回归。

## 外部依据

1. [System.Data.SQLite.Core 1.0.119 NuGet 包页](https://www.nuget.org/packages/System.Data.SQLite.Core/1.0.119)
2. [SQLite 版权与许可说明](https://www.sqlite.org/copyright.html)
