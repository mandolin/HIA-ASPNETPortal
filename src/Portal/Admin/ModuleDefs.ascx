<%-- 用户控件声明 --%>
<%@ Control 
Inherits="ASPNET.StarterKit.Portal.ModuleDefs" 
CodeBehind="ModuleDefs.ascx.cs" 
Language="c#" 
AutoEventWireup="True" %>

<%-- 注册一个自定义标签，用于显示标题 --%>
<%@ Register 
TagPrefix="ASPNETPortal" 
TagName="Title" 
Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title runat="server" id="Title1" />

<%-- 中文 / English: Legacy 桥接入口只展示既有定义并引导到受信任模块目录，不恢复在线手填路径。 --%>
<div class="portal-admin-page portal-legacy-admin-module portal-legacy-module-defs">
    <div class="portal-admin-header">
        <div class="portal-admin-heading">
            <h2 class="Head portal-admin-title">Legacy Module Definitions</h2>
            <p class="Normal portal-admin-subtitle">Existing module definitions are listed here; new business modules should be registered from trusted deployed packages.</p>
        </div>
        <div class="portal-admin-actions">
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
            <asp:DataList
                ID="defsList"
                CssClass="portal-chip-list portal-legacy-list"
                RepeatColumns="2"
                DataKeyField="ModuleDefID"
                OnItemCommand="DefsList_ItemCommand"
                runat="server">
                <ItemTemplate>
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
