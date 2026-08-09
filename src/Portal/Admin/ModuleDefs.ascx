<%@ Control 
Inherits="ASPNET.StarterKit.Portal.ModuleDefs" 
CodeBehind="ModuleDefs.ascx.cs" 
Language="c#" 
AutoEventWireup="True" %>

<%@ Register 
TagPrefix="ASPNETPortal" 
TagName="Title" 
Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title runat="server" id="Title1" />

<%--
    <lang>
        <zh-CN>Legacy 桥接入口只展示既有定义并引导到受信任模块目录，不恢复在线手填路径。</zh-CN>
        <en>The legacy bridge entry only lists existing definitions and routes users to the trusted module catalog; online manual path entry is not restored.</en>
    </lang>
--%>
<div class="portal-admin-page portal-legacy-admin-module portal-legacy-module-defs">
    <div class="portal-admin-header">
        <div class="portal-admin-heading">
            <h2 class="Head portal-admin-title">Legacy Module Definitions</h2>
            <p class="Normal portal-admin-subtitle">Existing module definitions are listed here; new business modules should be registered from trusted deployed packages.</p>
        </div>
        <div class="portal-admin-actions">
            <%--
                <lang>
                    <zh-CN>此入口只转到受信任的模块目录；不在旧控件中开放定义路径或程序集等高风险输入。</zh-CN>
                    <en>This entry routes only to the trusted module catalog; the legacy control does not expose high-risk definition-path or assembly inputs.</en>
                </lang>
            --%>
            <asp:LinkButton
                ID="AddDefBtn"
                CssClass="portal-button portal-button-primary"
                Text="Open Module Catalog"
                CausesValidation="False"
                OnClick="AddDef_Click"
                runat="server" />
        </div>
    </div>

    <div class="portal-admin-section">
        <div class="portal-section-header">
            <h3 class="Head portal-section-title">Existing Definitions</h3>
        </div>
        <div class="portal-chip-list-wrap">
            <%--
                <lang>
                    <zh-CN>DataList 使用 ModuleDefID 作为命令键并保留服务器事件，展示名称采用编码绑定以避免把定义元数据当作标记输出。</zh-CN>
                    <en>The DataList keeps ModuleDefID as the command key and retains server events; the display name uses encoded binding so definition metadata is not emitted as markup.</en>
                </lang>
            --%>
            <asp:DataList
                ID="defsList"
                CssClass="portal-chip-list portal-legacy-list"
                RepeatColumns="2"
                DataKeyField="ModuleDefID"
                OnItemCommand="DefsList_ItemCommand"
                runat="server">
                <ItemTemplate>
                    <%--
                        <lang>
                            <zh-CN>每个条目仅提供受编码的友好名称和编辑命令，实际定义读取、授权与保存仍由 code-behind 决定。</zh-CN>
                            <en>Each item exposes only an encoded friendly name and an edit command; definition loading, authorization, and persistence remain decided by the code-behind.</en>
                        </lang>
                    --%>
                    <div class="portal-chip-item">
                        <asp:Label
                            Text='<%#: DataBinder.Eval(Container.DataItem, "FriendlyName") %>'
                            CssClass="Normal portal-chip-text"
                            runat="server" />
                        <asp:LinkButton
                            Text="Edit Definition"
                            CommandName="edit"
                            CssClass="portal-button portal-button-secondary portal-button-compact"
                            CausesValidation="False"
                            runat="server" />
                    </div>
                </ItemTemplate>
            </asp:DataList>
        </div>
    </div>
</div>
