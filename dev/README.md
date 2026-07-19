# 开发工具入口

根级 `dev/` 只保留可供项目开发者直接使用的公共脚本和说明。

## 当前内容

- `scripts/`：NuGet 还原、解决方案构建和 IIS Express 启停脚本。
- `documentation/jsdoc/`：独立的 HIA JSDoc pilot 配置、依赖锁定和输出验证器。
- `notify/`：历史通知快照。2026-07-19 起不再作为 HIA-Documentation-Sys 的新通知投递入口。

内部开发状态、路线图、阶段计划、任务与修复记录已经迁移到独立私有仓库 `work-zone/dev/`，不进入公开项目仓库。

## HIA 文档化通知

HIA-Documentation-Sys 的目标项目通知机制已改为目标项目主动读取。查看通知时使用：

```powershell
dev/scripts/Get-HiaDocumentationNotifications.ps1
```

默认读取同级 `../HIA-Documentation-Sys/work-zone/notify/`。如本机目录不同，可传入 `-HiaDocumentationRoot`。
