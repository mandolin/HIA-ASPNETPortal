# HIA-ASPNETPortal 文档

本目录是项目的公开维护文档入口，面向使用者、开发者和后续维护人员。内部计划、阶段状态、ADR 和 AI 协作资料统一保存在独立私有仓库 `work-zone/`。

## 文档索引

- `architecture.md`：系统结构和主要模块说明。
- `dev-guide.md`：本地开发、构建、配置和调试指南。
- `frontend-asset-guide.md`：Web Forms 呈现、主题、模块 CSS、Gulp 与前端资产边界。
- `theme-package-guide.md`：受信任主题包的目录、启用、回退和发布边界。
- `module-development-guide.md`：受信任部署模块包的开发、注册、启停和移除边界。
- `user-guide.md`：运行和使用入口说明。
- `testing-checklist.md`：构建、测试、手工验证和发布前检查清单。
- `documentation-artifacts-guide.md`：公开文档、JSDoc、XML 文档和生成物的输入输出边界。
- `deployment-checklist.md`：SQL Server、IIS、外置配置和发布后回归清单。
- `font-policy-and-audit.md`：字体使用原则、许可证边界和当前审计结果。
- `third-party-dependencies.md`：新增第三方依赖的用途、许可证和分发边界。

## 当前约定

- 根目录 `docs/` 只保存适合公开的稳定文档。
- 内部规划放在 `work-zone/dev/plans/`，内部 ADR 放在 `work-zone/docs/adr/`。
- `src/Documentation/` 和 `src/Portal.Components.Data/Documentation/` 暂按生成文档输出处理，不作为维护文档入口。
- 新增正式文档默认使用中文，必要时保留英文技术名词。
