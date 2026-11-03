/* <lang>
  <zh-CN>门户一级导航种子（`W-anp-P266`）：把导航对齐到已确认的产品形态——首页 / 帮助 / 我的待办 / 请假与报销 / 业务办理 / 我的信息，
  管理员再加企业能力工作台与平台管理。执行内容：注册这些页签所需的产品模块定义与模块包启用状态、建立或改造页签、
  把模块实例挂到 `ContentPane`、把不属于目标形态的页签归入 `Test.` 前缀（由默认排除前缀统一隐藏）、并校准导航可见性设置。

  幂等性：可重复执行，不重复建页签/模块定义/模块实例，不删除任何业务数据。
  执行时机：在 `Portal_CreateDB.sql` → `Portal_LoadConfig.sql` → `Portal_LoadData.sql` 之后；既有库升级同样可直接执行。
  边界：本脚本只管**导航与模块挂载**，不改首页演示内容（Starter Kit 演示模块的下线见备忘 D20、计划随 v2.5 处理）。
  </zh-CN>
  <en>Portal first-level navigation seed (`W-anp-P266`): aligns navigation with the confirmed product shape — Home / Help /
  My Work Items / Leave &amp; Expense / Business Requests / My Profile, plus Enterprise Capability Workbench and Platform Admin for
  administrators. It registers the product module definitions and package enablement those tabs need, creates or reshapes the
  tabs, mounts module instances into `ContentPane`, moves any non-target tab under the `Test.` prefix (so the default excluded
  prefixes hide it), and calibrates the navigation visibility settings.

  Idempotent: safe to re-run; it never duplicates tabs, module definitions, or module instances, and never deletes business data.
  Run order: after `Portal_CreateDB.sql` to `Portal_LoadConfig.sql` to `Portal_LoadData.sql`; existing databases can run it too.
  Boundary: this script only owns **navigation and module mounting**. It does not touch the home-page demo content
  (retiring the Starter Kit demo modules is tracked as memo D20 and planned with v2.5).
  </en>
</lang> */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @PortalId INT = 1;
DECLARE @Actor NVARCHAR(100) = N'navigation-seed';

/* <lang><zh-CN>前置表检查：缺表说明基础建库尚未完成，直接报错返回，避免在残缺库上写入半套导航。</zh-CN><en>Prerequisite table check: a missing table means the base database build has not run; fail and return rather than writing half a navigation model onto an incomplete schema.</en></lang> */
IF OBJECT_ID(N'[dbo].[PortalCfg_Tabs]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[PortalCfg_Modules]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[PortalCfg_ModuleDefinitions]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[PortalCfg_ModulePackageStates]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[PortalCfg_SystemSettings]', N'U') IS NULL
BEGIN
    RAISERROR(N'Navigation seed requires the portal base schema. Run Portal_CreateDB.sql, Portal_LoadConfig.sql and Portal_LoadData.sql first.', 16, 1);
    RETURN;
END

/* <lang>
  <zh-CN>目标表：每行代表"某页签上的一个模块"；`DesktopSource` 为空的行只校准页签本身（首页、平台管理），不挂新模块。
  同一 `NavKey` 出现多行表示该页签承载多个模块（请假与报销两个、我的信息两个），`ModuleOrder` 决定同页内的先后。</zh-CN>
  <en>Target table: each row is "one module on one tab"; rows with an empty `DesktopSource` only calibrate the tab itself
  (Home, Platform Admin) and mount nothing new. Several rows sharing a `NavKey` mean the tab hosts multiple modules
  (two for Leave &amp; Expense, two for My Profile), with `ModuleOrder` deciding their order inside the pane.</en>
</lang> */
DECLARE @Target TABLE
(
    NavKey NVARCHAR(100) NOT NULL,
    TabName NVARCHAR(100) NOT NULL,
    TabNameEn NVARCHAR(100) NOT NULL,
    MobileTabName NVARCHAR(100) NOT NULL,
    TabOrder INT NOT NULL,
    AccessRoles NVARCHAR(250) NOT NULL,
    ModuleOrder INT NOT NULL,
    ModuleTitle NVARCHAR(150) NOT NULL,
    DesktopSource NVARCHAR(250) NOT NULL,
    FriendlyName NVARCHAR(150) NOT NULL,
    PackageId NVARCHAR(100) NOT NULL
);

INSERT INTO @Target (NavKey, TabName, TabNameEn, MobileTabName, TabOrder, AccessRoles, ModuleOrder, ModuleTitle, DesktopSource, FriendlyName, PackageId)
VALUES
    (N'Home',                N'首页',         N'Home',                N'首页',         1,  N'All Users;', 0, N'',               N'',                                                                          N'', N''),
    (N'Help',                N'帮助',         N'Help',                N'帮助',         3,  N'All Users;', 1, N'帮助与说明',     N'DesktopModules/Help/Help.ascx',                                              N'帮助与说明 / Help', N'HIA.Help'),
    (N'MyWorkItems',         N'我的待办',     N'My Work Items',       N'我的待办',     5,  N'All Users;', 1, N'我的待办',       N'DesktopModules/MyWorkItems/MyWorkItems.ascx',                                N'我的待办 / My To-Do Items', N'HIA.MyWorkItems'),
    (N'LeaveAndExpense',     N'请假与报销',   N'Leave & Expense',     N'请假与报销',   7,  N'All Users;', 1, N'请假申请',       N'DesktopModules/LeaveRequest/LeaveRequest.ascx',                              N'请假申请 / Leave Request', N'HIA.LeaveRequest'),
    (N'LeaveAndExpense',     N'请假与报销',   N'Leave & Expense',     N'请假与报销',   7,  N'All Users;', 2, N'费用报销',       N'DesktopModules/ExpenseReimbursement/ExpenseReimbursement.ascx',              N'费用报销 / Expense Reimbursement', N'HIA.ExpenseReimbursement'),
    (N'BusinessRequests',    N'业务办理',     N'Business Requests',   N'业务办理',     9,  N'All Users;', 1, N'业务申请',       N'DesktopModules/BusinessApplicationRequest/BusinessApplicationRequest.ascx',  N'业务申请 / Business Application Request', N'HIA.BusinessApplicationRequest'),
    (N'MyProfile',           N'我的信息',     N'My Profile',          N'我的信息',     11, N'All Users;', 1, N'员工资料确认',   N'DesktopModules/EmployeeProfileConfirm/EmployeeProfileConfirm.ascx',           N'员工资料确认 / Employee Profile Confirm', N'HIA.EmployeeProfileConfirm'),
    (N'MyProfile',           N'我的信息',     N'My Profile',          N'我的信息',     11, N'All Users;', 2, N'员工资料更正申请', N'DesktopModules/EmployeeProfileCorrectionRequest/EmployeeProfileCorrectionRequest.ascx', N'员工资料更正请求 / Employee Profile Correction Request', N'HIA.EmployeeProfileCorrectionRequest'),
    (N'CapabilityWorkbench', N'企业能力工作台', N'Enterprise Capability Workbench', N'企业能力工作台', 13, N'Admins;', 1, N'企业能力工作台', N'DesktopModules/EnterpriseCapabilityWorkbench/EnterpriseCapabilityWorkbench.ascx', N'企业能力工作台 / Enterprise Capability Workbench', N'HIA.EnterpriseCapabilityWorkbench'),
    (N'Admin',               N'平台管理',     N'Platform Admin',      N'平台管理',     15, N'Admins;',    0, N'',               N'',                                                                          N'', N'');

/* <lang><zh-CN>循环工作变量：在游标外一次性声明，循环内只重新赋值（同一批次内不能重复 DECLARE 同名变量）。</zh-CN><en>Loop working variables are declared once outside the cursor and only reassigned inside the loop, since a single batch cannot re-declare the same variable.</en></lang> */
DECLARE @NavKey NVARCHAR(100), @TabName NVARCHAR(100), @TabNameEn NVARCHAR(100), @MobileTabName NVARCHAR(100);
DECLARE @TabOrder INT, @AccessRoles NVARCHAR(250), @ModuleOrder INT, @ModuleTitle NVARCHAR(150);
DECLARE @DesktopSource NVARCHAR(250), @FriendlyName NVARCHAR(150), @PackageId NVARCHAR(100);
DECLARE @TabId INT, @ModuleDefId INT, @ModuleId INT, @NextModuleOrder INT;

/* <lang>
  <zh-CN>已解析页签映射：同一 `NavKey` 的多行（请假与报销、我的信息各两行）必须落到**同一个**页签，
  否则会按各自模块的旧宿主页签分别解析出两个同名页签，出现重复导航。首行解析后登记，后续行直接复用。</zh-CN>
  <en>Resolved-tab map: rows sharing a `NavKey` (two each for Leave &amp; Expense and My Profile) must land on the **same** tab,
  otherwise each row would resolve through its own legacy host tab and produce two identically named tabs, duplicating navigation.
  The first row registers the resolution and later rows reuse it.</en>
</lang> */
DECLARE @Resolved TABLE (NavKey NVARCHAR(100) PRIMARY KEY, TabId INT NOT NULL);

BEGIN TRANSACTION;

DECLARE navCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT NavKey, TabName, TabNameEn, MobileTabName, TabOrder, AccessRoles, ModuleOrder, ModuleTitle, DesktopSource, FriendlyName, PackageId
    FROM @Target
    ORDER BY TabOrder, ModuleOrder;

OPEN navCursor;
FETCH NEXT FROM navCursor INTO @NavKey, @TabName, @TabNameEn, @MobileTabName, @TabOrder, @AccessRoles, @ModuleOrder, @ModuleTitle, @DesktopSource, @FriendlyName, @PackageId;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @TabId = NULL;
    SET @ModuleDefId = NULL;
    SET @ModuleId = NULL;

    /* <lang><zh-CN>1) 模块定义：按桌面入口路径认定义（路径是模块包身份，改名不改路径），缺失才插入，已存在不动其 FriendlyName，避免覆盖运维既有命名。</zh-CN><en>1) Module definition: identify by desktop entry path (the path is package identity; renames keep it), insert only when missing, and leave an existing FriendlyName untouched so operator naming is never overwritten.</en></lang> */
    IF @DesktopSource <> N''
    BEGIN
        /* <lang><zh-CN>同一桌面入口可能存在多条历史定义（实测 `LeaveRequest` 同时有 `1041` 与 `1043`），这里优先选**已被模块实例引用**的那条，避免在页签上再挂一个重复模块。</zh-CN><en>A desktop entry can have several historical definitions (measured: `LeaveRequest` exists as both `1041` and `1043`); prefer the one **already referenced by a module instance** so the tab never receives a duplicate module.</en></lang> */
        SELECT TOP (1) @ModuleDefId = d.ModuleDefId
        FROM PortalCfg_ModuleDefinitions d
        WHERE d.DesktopSourceFile = @DesktopSource
        ORDER BY CASE WHEN EXISTS (SELECT 1 FROM PortalCfg_Modules m WHERE m.ModuleDefId = d.ModuleDefId) THEN 0 ELSE 1 END,
                 d.ModuleDefId;

        IF @ModuleDefId IS NULL
        BEGIN
            INSERT INTO PortalCfg_ModuleDefinitions (FriendlyName, DesktopSourceFile, MobileSourceFile)
            VALUES (@FriendlyName, @DesktopSource, N'');
            SET @ModuleDefId = CAST(SCOPE_IDENTITY() AS INT);
        END

        IF @PackageId <> N''
        BEGIN
            IF EXISTS (SELECT 1 FROM PortalCfg_ModulePackageStates WHERE PackageId = @PackageId)
            BEGIN
                UPDATE PortalCfg_ModulePackageStates
                SET IsEnabled = 1,
                    UpdatedBy = @Actor,
                    UpdatedUtc = SYSUTCDATETIME()
                WHERE PackageId = @PackageId;
            END
            ELSE
            BEGIN
                INSERT INTO PortalCfg_ModulePackageStates (PackageId, IsEnabled, Note, UpdatedBy, UpdatedUtc)
                VALUES (@PackageId, 1, N'Enabled by the navigation seed.', @Actor, SYSUTCDATETIME());
            END
        END
    END

    /* <lang>
      <zh-CN>2) 定位页签：按三级优先匹配，确保在"页签名与 NavigationKey 都是测试名"的既有库上也能命中正确页签而不是另建一个。
      ① NavigationKey 精确命中；② 已有模块实例挂着目标模块的桌面入口（最可靠的身份）；③ 页签中文或英文名命中。</zh-CN>
      <en>2) Locate the tab with three levels of precedence so an existing database whose tab names and NavigationKeys are both
      test names still resolves to the right row instead of creating a duplicate: (1) exact NavigationKey; (2) the tab already hosts
      a module instance on the target desktop entry (the most reliable identity); (3) the tab's Chinese or English name matches.</en>
    </lang> */
    /* <lang><zh-CN>先看本 `NavKey` 是否已有解析结果；有则直接复用，绝不为同一目标页签再解析出第二个页签。</zh-CN><en>Check for an existing resolution of this `NavKey` first and reuse it, never resolving a second tab for the same target.</en></lang> */
    SELECT @TabId = r.TabId
    FROM @Resolved r
    WHERE r.NavKey = @NavKey;

    IF @TabId IS NULL
    SELECT TOP (1) @TabId = t.TabId
    FROM PortalCfg_Tabs t
    WHERE (t.PortalId = @PortalId OR t.PortalId IS NULL)
      AND (
            t.NavigationKey = @NavKey
            OR (@DesktopSource <> N'' AND EXISTS (
                    SELECT 1
                    FROM PortalCfg_Modules m
                    JOIN PortalCfg_ModuleDefinitions d ON d.ModuleDefId = m.ModuleDefId
                    WHERE m.TabId = t.TabId AND d.DesktopSourceFile = @DesktopSource))
            OR t.TabName = @TabName
            OR t.TabNameEn = @TabNameEn
          )
    ORDER BY
        CASE
            WHEN t.NavigationKey = @NavKey THEN 0
            WHEN @DesktopSource <> N'' AND EXISTS (
                    SELECT 1
                    FROM PortalCfg_Modules m
                    JOIN PortalCfg_ModuleDefinitions d ON d.ModuleDefId = m.ModuleDefId
                    WHERE m.TabId = t.TabId AND d.DesktopSourceFile = @DesktopSource) THEN 1
            WHEN t.TabName = @TabName THEN 2
            ELSE 3
        END,
        t.TabId;

    /* <lang><zh-CN>3) 建或改页签：显示名、英文名、移动端名、顺序、访问角色、NavigationKey 一次写全；多语言附加列（fr/es）不覆盖，保留既有译名。</zh-CN><en>3) Create or reshape the tab: display name, English name, mobile name, order, access roles, and NavigationKey are written together; the extra localized columns (fr/es) are left as-is to preserve existing translations.</en></lang> */
    IF @TabId IS NULL
    BEGIN
        INSERT INTO PortalCfg_Tabs (TabName, TabNameEn, TabOrder, AccessRoles, ShowMobile, MobileTabName, MobileTabNameEn, NavigationKey, PortalId)
        VALUES (@TabName, @TabNameEn, @TabOrder, @AccessRoles, 0, @MobileTabName, @TabNameEn, @NavKey, @PortalId);
        SET @TabId = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE PortalCfg_Tabs
        SET TabName = @TabName,
            TabNameEn = @TabNameEn,
            TabOrder = @TabOrder,
            AccessRoles = @AccessRoles,
            MobileTabName = @MobileTabName,
            MobileTabNameEn = @TabNameEn,
            NavigationKey = @NavKey,
            /* <lang><zh-CN>顺手把历史上缺 PortalId 的页签归位到目标门户：这些页签由早期夹具脚本插入时未写 PortalId，按门户过滤时会被漏掉。</zh-CN><en>Also place legacy tabs that lack PortalId onto the target portal: early fixture scripts inserted them without PortalId, so portal-scoped filtering silently skipped them.</en></lang> */
            PortalId = @PortalId
        WHERE TabId = @TabId;
    END

    /* <lang><zh-CN>登记解析结果，供同一 `NavKey` 的后续行复用（首行登记后，第二行不再重新定位）。</zh-CN><en>Record the resolution for later rows sharing this `NavKey`; once the first row registers it, the second row no longer re-resolves.</en></lang> */
    IF NOT EXISTS (SELECT 1 FROM @Resolved WHERE NavKey = @NavKey)
    BEGIN
        INSERT INTO @Resolved (NavKey, TabId) VALUES (@NavKey, @TabId);
    END

    /* <lang><zh-CN>4) 模块实例：同页签同定义只保留一个实例，缺失才插入；`ContentPane` 是门户外壳的正文区，模块标题随目标表校准。</zh-CN><en>4) Module instance: keep exactly one instance per tab and definition, inserting only when absent; `ContentPane` is the portal shell's body pane and the module title follows the target table.</en></lang> */
    IF @DesktopSource <> N'' AND @ModuleDefId IS NOT NULL
    BEGIN
        SELECT TOP (1) @ModuleId = ModuleId
        FROM PortalCfg_Modules
        WHERE TabId = @TabId AND ModuleDefId = @ModuleDefId
        ORDER BY ModuleId;

        IF @ModuleId IS NULL
        BEGIN
            SELECT @NextModuleOrder = ISNULL(MAX(ModuleOrder), 0) + 1
            FROM PortalCfg_Modules
            WHERE TabId = @TabId AND PaneName = N'ContentPane';

            INSERT INTO PortalCfg_Modules (ModuleTitle, ModuleOrder, EditRoles, PaneName, ShowMobile, CacheTimeout, ModuleDefId, TabId)
            VALUES (@ModuleTitle, @NextModuleOrder, N'Admins;', N'ContentPane', 0, 0, @ModuleDefId, @TabId);
        END
        ELSE
        BEGIN
            UPDATE PortalCfg_Modules
            SET ModuleTitle = @ModuleTitle,
                PaneName = N'ContentPane'
            WHERE ModuleId = @ModuleId;
        END
    END

    FETCH NEXT FROM navCursor INTO @NavKey, @TabName, @TabNameEn, @MobileTabName, @TabOrder, @AccessRoles, @ModuleOrder, @ModuleTitle, @DesktopSource, @FriendlyName, @PackageId;
END

CLOSE navCursor;
DEALLOCATE navCursor;

/* <lang>
  <zh-CN>5) 归一化非目标页签：把不属于目标形态的页签统一加 `Test.` 前缀，由默认排除前缀一次性隐藏。
  比逐个拉黑更稳——新出现的测试页签只要沿用该前缀就自动隐藏，不会再次漏进导航；已带该前缀的跳过，保证幂等。
  本步骤只改 NavigationKey，不改页签名、不删页签、不动其模块数据，演示内容仍可在后台恢复查看。</zh-CN>
  <en>5) Normalize non-target tabs: prefix every tab outside the target shape with `Test.` so the default excluded prefixes hide
  them in one pass. This is steadier than blacklisting one by one — any new test tab that reuses the prefix hides itself and can
  never leak back into navigation; tabs already carrying the prefix are skipped, keeping the step idempotent. Only NavigationKey
  changes: names, rows, and module data are untouched, so demo content stays recoverable from the admin side.</en>
</lang> */
UPDATE t
SET t.NavigationKey = N'Test.' + COALESCE(NULLIF(t.NavigationKey, N''), t.TabName)
FROM PortalCfg_Tabs AS t
WHERE (t.PortalId = @PortalId OR t.PortalId IS NULL)
  AND (t.NavigationKey IS NULL OR t.NavigationKey NOT LIKE N'Test.%')
  AND NOT EXISTS (SELECT 1 FROM @Target x WHERE x.NavKey = t.NavigationKey);

/* <lang>
  <zh-CN>6) 导航可见性设置校准：匿名只看到首页与帮助；四个业务入口登录后可见；排除前缀覆盖全部测试命名段。
  `HiddenNavigationKeys` 刻意不改：历史部署可能在其中登记了页签名，直接清空会改变其既有行为，
  而归一化后的 `Test.` 前缀已足以隐藏这些页签，两者不冲突。</zh-CN>
  <en>6) Calibrate navigation visibility: anonymous visitors see only Home and Help; the four business entries appear after
  sign-in; the excluded prefixes cover every test naming segment. `HiddenNavigationKeys` is deliberately left alone: past
  deployments may have registered tab names there, and clearing it would change their behaviour, while the normalized
  `Test.` prefix already hides those tabs — the two do not conflict.</en>
</lang> */
IF EXISTS (SELECT 1 FROM PortalCfg_SystemSettings WHERE SettingKey = N'Portal.Navigation.ExcludedNavigationKeyPrefixes')
BEGIN
    UPDATE PortalCfg_SystemSettings
    SET SettingValue = N'Test.,P22-Test-,P19-Test-,P64-Test-,P65-Test-,P74-Test-,P20-Test-,G1P2-Test-',
        UpdatedBy = @Actor,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE SettingKey = N'Portal.Navigation.ExcludedNavigationKeyPrefixes';
END
ELSE
BEGIN
    INSERT INTO PortalCfg_SystemSettings (SettingKey, SettingValue, ValueType, SourceLevel, CanDelete, UpdatedBy, UpdatedUtc)
    VALUES (N'Portal.Navigation.ExcludedNavigationKeyPrefixes', N'Test.,P22-Test-,P19-Test-,P64-Test-,P65-Test-,P74-Test-,P20-Test-,G1P2-Test-', N'String', N'Database', 1, @Actor, SYSUTCDATETIME());
END

IF EXISTS (SELECT 1 FROM PortalCfg_SystemSettings WHERE SettingKey = N'Portal.Navigation.AnonymousHiddenNavigationKeys')
BEGIN
    UPDATE PortalCfg_SystemSettings
    SET SettingValue = N'MyWorkItems,LeaveAndExpense,BusinessRequests,MyProfile',
        UpdatedBy = @Actor,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE SettingKey = N'Portal.Navigation.AnonymousHiddenNavigationKeys';
END
ELSE
BEGIN
    INSERT INTO PortalCfg_SystemSettings (SettingKey, SettingValue, ValueType, SourceLevel, CanDelete, UpdatedBy, UpdatedUtc)
    VALUES (N'Portal.Navigation.AnonymousHiddenNavigationKeys', N'MyWorkItems,LeaveAndExpense,BusinessRequests,MyProfile', N'String', N'Database', 1, @Actor, SYSUTCDATETIME());
END

COMMIT TRANSACTION;

/* <lang><zh-CN>对账输出：目标页签与挂载模块一览，供执行日志留证与人工核对（顺序即最终导航顺序）。</zh-CN><en>Reconciliation output: the target tabs and mounted modules for the run log and manual review; the order is the final navigation order.</en></lang> */
SELECT t.TabOrder,
       t.TabId,
       t.NavigationKey,
       t.TabName,
       t.TabNameEn,
       t.AccessRoles,
       ISNULL(d.FriendlyName, N'(tab only)') AS ModuleDefinition,
       ISNULL(m.ModuleTitle, N'') AS ModuleTitle,
       ISNULL(m.ModuleId, -1) AS ModuleId
FROM PortalCfg_Tabs t
LEFT JOIN PortalCfg_Modules m ON m.TabId = t.TabId
LEFT JOIN PortalCfg_ModuleDefinitions d ON d.ModuleDefId = m.ModuleDefId
WHERE t.PortalId = @PortalId
  AND EXISTS (SELECT 1 FROM @Target x WHERE x.NavKey = t.NavigationKey)
ORDER BY t.TabOrder, m.ModuleOrder, m.ModuleId;

SELECT SettingKey, SettingValue
FROM PortalCfg_SystemSettings
WHERE SettingKey IN (N'Portal.Navigation.AnonymousHiddenNavigationKeys', N'Portal.Navigation.ExcludedNavigationKeyPrefixes', N'Portal.Navigation.HiddenNavigationKeys')
ORDER BY SettingKey;
