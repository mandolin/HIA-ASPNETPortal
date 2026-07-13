# 第三方依赖审计

本文件记录本项目在 AI 协作治理建立后新引入或新升级的第三方依赖。历史依赖会在后续文档化周期中逐步补全，不代表未列出的旧包已完成许可证审计。

## 依赖清单

| 依赖 | 锁定版本 | 用途 | 许可证/依据 | 当前边界 |
| --- | --- | --- | --- | --- |
| `System.Data.SQLite.Core` | `1.0.119.0` | `W-anp-P3.3` 的 .NET Framework 4.7 SQLite ADO.NET capability proof。 | package 元数据链接至 SQLite Public Domain 版权声明；其 net46 build target 也声明为 Public Domain。 | 仅由独立 proof 项目使用，不进入门户主业务运行路径。 |

## 使用与分发约束

1. proof 项目使用 packages.config 恢复包；`src/packages/` 仍由现有忽略规则排除，不提交二进制包。
2. SQLite 运行时文件随 proof 输出到 `temp/provider-proof/`，不进入站点 `bin`、发布包或门户正常启动路径。
3. 生成的 `.sqlite` 文件只用于开发/测试，脚本会默认重新创建；不得放入仓库、WorkZone 或外置生产配置目录。
4. 若未来将 SQLite、MySQL、PostgreSQL 等 provider 用于真实门户能力，必须新增对应版本、许可证、native 运行时和部署说明审计，并完成独立 ADR 与回归。

## 外部依据

1. [System.Data.SQLite.Core 1.0.119 NuGet 包页](https://www.nuget.org/packages/System.Data.SQLite.Core/1.0.119)
2. [SQLite 版权与许可说明](https://www.sqlite.org/copyright.html)
