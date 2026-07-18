<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.ImageModule" CodeBehind="ImageModule.ascx.cs" AutoEventWireup="True" %>
<%@ Register TagPrefix="Portal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<portal:title EditText="Edit" EditUrl="~/DesktopModules/EditImage.aspx" runat="server" id=Title1 />

<div class="portal-content-media">
    <asp:image id="Image1" CssClass="portal-content-image" runat="server" />
</div>
