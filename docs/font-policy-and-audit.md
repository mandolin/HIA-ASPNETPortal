# 字体政策与审计

## 使用原则

- 项目不随站点分发普通文字字体文件，避免不必要的许可证、包体积和缓存维护成本。
- 页面样式只主动引用许可证明确的开源字体，并保留 `sans-serif` 或 `monospace` 通用回退。
- 不主动引用 Verdana、Arial、Helvetica、Times New Roman、Courier New、Consolas、Segoe UI、Microsoft YaHei 等专有或授权边界不清的字体。
- 若未来需要随项目分发字体文件，必须同时提交原始许可证、版权声明、来源、版本和发布审计记录。

## 当前字体栈

普通界面文字：

```css
font-family: "Noto Sans SC", "Source Han Sans SC", "Sarasa Gothic SC", "Liberation Sans", sans-serif;
```

等宽内容：

```css
font-family: "Sarasa Mono SC", "Noto Sans Mono CJK SC", "Source Han Mono SC", "Liberation Mono", monospace;
```

这些名称只是本地字体候选，不会触发网络下载。客户端未安装时由浏览器使用通用字体回退，因此不会影响站点启动。

## 许可证依据

| 字体系列 | 许可证 | 官方依据 |
| --- | --- | --- |
| Noto | Open Font License | [Noto 官方使用说明](https://notofonts.github.io/noto-docs/website/use/) |
| Source Han Sans / Mono | SIL Open Font License 1.1 | [Source Han Sans LICENSE](https://github.com/adobe-fonts/source-han-sans/blob/master/LICENSE.txt) |
| Sarasa Gothic / Mono | SIL Open Font License 1.1 | [Sarasa Gothic LICENSE](https://github.com/be5invis/Sarasa-Gothic/blob/main/LICENSE) |
| Liberation Sans / Mono | SIL Open Font License 1.1 | [Liberation Fonts LICENSE](https://github.com/liberationfonts/liberation-fonts/blob/main/LICENSE) |

## 2026-07-10 审计结果

- 主项目 Git 当前没有追踪 `.eot`、`.otf`、`.ttf`、`.woff` 或 `.woff2` 字体二进制。
- 默认主题已将 Verdana、Helvetica 和 Lucida Console 引用替换为上述开源字体栈。
- 后台角色控件和文档上传控件已移除 Verdana、Arial 的内联绑定，统一继承主题字体。
- 旧 `src/Setup/SystemReqs.rtf` 未被项目引用，且其字体表包含多种 Office/Windows 专有字体；现有 `SystemReqs.md` 已保留同一份历史需求内容，因此删除冗余 RTF。
- 未跟踪的生成文档中存在 Font Awesome Free 6.1.1 图标字体，其 CSS 头部已声明字体使用 SIL OFL 1.1、代码使用 MIT、图标使用 CC BY 4.0；该目录目前仍按生成物处理，不纳入本次主仓库字体分发。
