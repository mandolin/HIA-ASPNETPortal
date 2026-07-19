# 通知目录历史说明

`dev/notify/` 仅保留 2026-07-19 之前由外部项目主动写入本项目的历史通知快照。

从 2026-07-19 起，HIA-Documentation-Sys 不再主动写入目标项目仓库的 `dev/notify/`。本项目需要主动读取：

```text
../HIA-Documentation-Sys/work-zone/notify/
```

推荐使用只读脚本查看最近通知：

```powershell
dev/scripts/Get-HiaDocumentationNotifications.ps1
```

如 HIA-Documentation-Sys 不在本项目同级目录，可显式指定路径：

```powershell
dev/scripts/Get-HiaDocumentationNotifications.ps1 -HiaDocumentationRoot "D:\path\to\HIA-Documentation-Sys"
```

本目录后续不再作为新通知投递入口；如确需保存某条通知的吸收结论，应写入 `work-zone/dev/plans/`、`work-zone/dev/tasks/` 或相关 ADR，而不是复制通知原文。
