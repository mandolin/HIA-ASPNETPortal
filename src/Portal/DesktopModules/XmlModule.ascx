<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.XmlModule" CodeBehind="XmlModule.ascx.cs" AutoEventWireup="True" %>
<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<portal:title EditText="Edit" EditUrl="~/DesktopModules/EditXml.aspx" runat="server" id=Title1 />

<span class="Normal">
    <asp:xml id="xml1" runat="server" />
</span>