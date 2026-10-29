---
layout: default
lang: zh-CN
parent: 学习
nav_order: 5
permalink: /operator-handbook/
title: 运营者手册：把门户用起来
---
# 运营者手册：把门户用起来

这份手册给**站点运营者**：不写代码，只管内容、页签和可见性。要改模块行为、看源码的，去「参考」分区。

## 你要先有权限

- 后台页面的入口都在门户的 `/Admin/` 下（下面提到的路径都是**门户内**路径，不是文档站路径）。
- 每个请求都会重新校验权限，不是登录一次就长期放行。
- 看不到某个后台页，通常是**没有该页的授权角色**，不是页面坏了。

## 发一条公告

公告是首页中间那块「门户公告」。入口在**承载公告的页签上**：模块标题栏右侧的「Add New Announcement」。

### 新建

1. 打开承载公告的页签，点标题栏右侧的**「Add New Announcement」**。
2. 填**标题**（必填，100 字以内）。
3. 填**描述**（必填，长度不限）。
4. 填**过期日期**（必填，格式要能被解析成日期，例如 `12/31/2026`）。
5. 需要跳转再填**详情链接**与**移动端链接**（都可留空，各 100 字以内）。
6. 保存。返回模块后，新公告出现在列表里。

描述和链接的格式**不会被当场校验**：链接填错了不会报错，只会在点击时出问题。

### 改一条、或让它下线

- 改：找到条目，点右侧的**编辑入口**（没有编辑权限时这个入口**根本不出现**，不是点了报错）。
- 下线：把**过期日期**改成一个已经过去的日期并保存。到期后公告不再显示。
- 删：编辑页底部有删除按钮。

**没有"永不过期"开关**。想让一条公告长期挂着，就把过期日期设成足够远的将来，并定期回来改。

### 公告对谁可见

每条公告有一个条目级的**游客可见**属性，**默认不对匿名访客显示** —— 匿名访客只看到标了游客可见的条目。

目前这个属性**没有后台勾选项**，只能由部署侧在数据层设置。也就是说：想让一条公告对外可见，你需要走部署侧这一步，而不是在后台勾一下。

## 调整页签

### 改页签的名字、授权角色、移动端可见性

「页签名称与布局」页（`/Admin/TabLayout.aspx`）管这些：

- 页签名与版式
- **授权角色**（勾选哪些角色能看到这个页签）
- 移动端可见性

改授权角色会落库并写一条审计（归类 `TabAdministration` / `Update`）。新建页签的默认授权角色是「All Users」。

### 页签排序目前没有后台入口

页签的**排序**与**删除**原本在「旧版页签管理」页里，但那个页面**没有宿主页**，进不去。也就是说页签顺序目前只能由部署侧在数据层调整。

### 只让登录用户看到某个页签

有些页签要"登录可见、匿名不可见"，这不靠授权角色（匿名本来就不在任何角色里），而是靠**系统设置**里的导航可见性项：

| 设置键 | 作用 |
| --- | --- |
| `Portal.Navigation.AnonymousHiddenNavigationKeys` | 列出"仅对匿名隐藏"的页签键（逗号分隔） |
| `Portal.Navigation.HiddenTabIds` | 显式隐藏的页签 |
| `Portal.Navigation.HideTabsWithoutRenderableModules` | 隐藏"模块全被档位禁用"的页签 |
| `Portal.Navigation.ExcludedTabIdPrefixes` | 按前缀排除的页签 |

## 模块的启停与设置

- **模块包目录**（`/Admin/ModuleCatalog.aspx`）：看有哪些模块包可用。
- **模块类型定义**（`/Admin/ModuleDefinitions.aspx`）：看模块类型（定义）与档位的关系。
- **模块设置**（`/Admin/ModuleSettings.aspx`）：改某个**实例**的元数据，并指定该实例的**编辑角色**（谁能编辑它 —— 公告那条编辑权限就靠它）。
- 想让某个模块在某个页签上出现/消失，改的是**页签与模块的挂载关系**，不是模块本身。

## 站点设置

**门户名**（侧栏显示的那串字）目前**没有**可达的后台入口 —— 负责它的「旧版站点设置」控件没有宿主页。要改它，得由部署侧在数据层改。

主题外观在「主题设置」页（`/Admin/ThemeSettings.aspx`）：全局主题，以及按页签覆盖。

## 后台页面速查

| 页面 | 路径 | 干什么 |
| --- | --- | --- |
| 业务申请 | `/Admin/BusinessApplications.aspx` | 审核业务申请 |
| 员工资料更正请求 | `/Admin/EmployeeProfileCorrectionRequests.aspx` | 审核资料更正 |
| 企业协同事项 | `/Admin/CollaborationItems.aspx` | 处理协同事项 |
| 业务待办 | `/Admin/WorkItems.aspx` | 看待办并执行动作 |
| 员工目录 | `/Admin/EmployeeDirectory.aspx` | 组织、员工、账号绑定 |
| 用户员工绑定 | `/Admin/UserEmployeeBindingEdit.aspx` | 把账号绑到某个员工 |
| 角色成员 | `/Admin/SecurityRoles.aspx` | 给角色加/减成员 |
| 能力权限 | `/Admin/CapabilityPermissionMatrix.aspx` | 业务能力的权限矩阵 |
| 运营审计 | `/Admin/OperationAudits.aspx` | 谁在什么时候做了什么 |
| 监控日志 | `/Admin/DiagnosticsLogs.aspx` | 运行期日志 |
| 系统健康状态 | `/Admin/SystemHealth.aspx` | 健康检查 |

## 这页没讲的事

- 公告字段的完整校验规则 → [公告模块页]({{ site.baseurl }}/modules/announcements/)
- 页签可见性的完整设置项 → [系统设置契约]({{ site.baseurl }}/contracts-app-settings/)
- 模块包与档位的机制 → [模块清单契约]({{ site.baseurl }}/contracts-module-manifest/)
- 怎么加一个自己的模块 → [模块开发指南]({{ site.baseurl }}/module-development/)

<!-- build-stamp: C182CE0A5394 -->
