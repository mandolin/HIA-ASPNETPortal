# 主题包部署指南

## 范围

本项目当前只接受由受信任部署流程写入 `src/Portal/App_Themes/` 的主题包。后台管理员只能在已部署、已校验的主题中选择全局主题或为 Tab 设置覆盖，不能上传 ZIP、编辑 CSS、输入外部 URL，也不能让主题自动加载 JavaScript。

这是一条当前安全边界，不表示未来永远排除更丰富的主题运营能力。后续如需开放主题包来源或分发，将先建立独立的可信包机制，覆盖来源证明、审核/签名、许可证、版本兼容、回滚和部署责任，而不是直接扩大后台写文件权限。

## 目录与清单

每个可选择主题位于 `App_Themes/{ThemeName}/`，至少包含：

```text
App_Themes/
  {ThemeName}/
    Default.css
    theme.json
    Images/              # 可选，只能是主题目录内的相对资源
```

`ThemeName` 必须匹配 `^[A-Za-z][A-Za-z0-9_-]{0,63}$`，并与清单中的 `name` 完全一致。当前清单版本为 `schemaVersion: 1`：

```json
{
  "schemaVersion": 1,
  "name": "ExampleTheme",
  "displayName": "示例主题 / Example Theme",
  "version": "1.0.0",
  "minimumPortalVersion": "1.0",
  "inheritsDefault": true,
  "resources": [
    "Default.css"
  ]
}
```

`resources` 必须声明 `Default.css`，所有资源都必须是主题目录内实际存在的相对文件。清单不能含 `script`、`scripts`、`externalUrl` 或 `externalUrls` 字段；`.js` 文件不会作为主题资源通过校验。

## 作用域与选择

门户每页只应用一个 Web Forms 原生 `Theme`。有效值的优先级由高到低为：

1. 当前门户 Tab 的主题覆盖。
2. 数据库运行级全局覆盖。
3. 部署级 `Portal.Theme.Name`。
4. 内置 `Default` 安全回退。

Admin 页面只使用全局主题，通用错误页固定使用最小 Default 样式。每个门户页面的 `body` 会输出 `portal-theme-{theme}` 和 `portal-tab-{id}`，模块容器会输出 `portal-module`、`portal-module-{id}` 与 `portal-pane-{pane}`。主题 CSS 应依赖这些稳定 class 做局部覆盖，不应假定可在同一页叠加多个原生 Theme。

管理员入口为 `Admin/ThemeSettings.aspx`。该页只列出通过部署校验的主题包，所有选择/清除动作均写入现有设置或运营审计记录。

## 发布与回退

1. 在隔离环境审查主题目录、许可证和兼容性后，将完整目录作为发布物部署。
2. 确认 `theme.json`、`Default.css` 及其声明资源都存在，再重启应用或等待应用域按部署刷新。
3. 先访问首页、目标 Tab、Admin 页面和通用错误页；虚拟目录部署还要检查资源路径。
4. 发生问题时，在 `Admin/ThemeSettings.aspx` 清除 Tab 覆盖或选择 `Default`；若包本身有问题，回滚该主题目录的部署版本。

主题 CSS 必须保持 IE9+ 基线：不把 CSS Custom Properties、Grid 或 Flexbox 作为必要机制。
