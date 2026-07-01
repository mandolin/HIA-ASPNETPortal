<%@ Page Language="c#" CodeBehind="DiscussDetails.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.DiscussDetails" MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %> <%-- 导入命名空间 --%>

<%--主要内容区域--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table cellspacing="0" cellpadding="0" width="600">
        <tr>
            <td align="left">
                <span class="Head">Message Detail</span> <%-- 显示“消息详情”的标题 --%>
            </td>
            <td align="right">
                <asp:Panel ID="ButtonPanel" runat="server"> <%-- 按钮面板 --%>
                    <a class="CommandButton" id="prevItem" title="Previous Message" runat="server">
                        <img src='<%=Global.GetApplicationPath(Request) + "/images/rew.gif"%>' border="0"></a>&nbsp; <%-- 上一条消息按钮 --%>
                    <a class="CommandButton" id="nextItem" title="Next Message" runat="server">
                        <img src='<%=Global.GetApplicationPath(Request) + "/images/fwd.gif"%>' border="0"></a>&nbsp; <%-- 下一条消息按钮 --%>
                    <asp:LinkButton ID="ReplyBtn" runat="server" EnableViewState="false" CssClass="CommandButton"
                        Text="Reply to this Message" OnClick="ReplyBtn_Click"></asp:LinkButton> <%-- 回复按钮 --%>
                </asp:Panel>
            </td>
        </tr>
        <tr>
            <td colspan="2">
                <hr noshade size="1"> <%-- 分割线 --%>
            </td>
        </tr>
    </table>

    <%-- 编辑面板 --%>
    <asp:Panel ID="EditPanel" runat="server" Visible="false">
        <table cellspacing="0" cellpadding="4" width="600" border="0">
            <tr valign="top">
                <td class="SubHead" width="150">
                    Title: <%-- 标题标签 --%>
                </td>
                <td rowspan="4">
                    &nbsp;
                </td>
                <td width="*">
                    <asp:TextBox ID="TitleField" runat="server" MaxLength="100" Columns="40" Width="500"
                        CssClass="NormalTextBox"></asp:TextBox> <%-- 输入框用于编辑标题 --%>
                </td>
            </tr>
            <tr valign="top">
                <td class="SubHead">
                    Body: <%-- 内容标签 --%>
                </td>
                <td width="*">
                    <asp:TextBox ID="BodyField" runat="server" Columns="59" Width="500" Rows="15" TextMode="Multiline"></asp:TextBox> <%-- 多行输入框用于编辑内容 --%>
                </td>
            </tr>
            <tr valign="top">
                <td>
                    &nbsp;
                </td>
                <td>
                    <asp:LinkButton class="CommandButton" ID="updateButton" runat="server" Text="Submit"
                        OnClick="UpdateBtn_Click"></asp:LinkButton> <%-- 提交按钮 --%>
                    &nbsp;
                    <asp:LinkButton class="CommandButton" ID="cancelButton" runat="server" Text="Cancel"
                        CausesValidation="False" OnClick="CancelBtn_Click"></asp:LinkButton> <%-- 取消按钮 --%>
                    &nbsp;
                </td>
            </tr>
            <tr valign="top">
                <td class="SubHead">
                    Original Message: <%-- 原始消息标签 --%>
                </td>
                <td>
                    &nbsp;
                </td>
            </tr>
        </table>
    </asp:Panel>

    <%-- 消息内容展示区 --%>
    <table cellspacing="0" cellpadding="4" width="600" border="0">
        <tr valign="top">
            <td class="Message" align="left">
                <b>Subject: </b> <%-- 主题 --%>
                <asp:Label ID="Subject" runat="server"></asp:Label>
                <br>
                <b>Author: </b> <%-- 发布者 --%>
                <asp:Label ID="CreatedByUser" runat="server"></asp:Label>
                <br>
                <b>Date: </b> <%-- 发布日期 --%>
                <asp:Label ID="CreatedDate" runat="server"></asp:Label>
                <br>
                <br>
                <asp:Label ID="Body" runat="server"></asp:Label> <%-- 消息正文 --%>
            </td>
        </tr>
    </table>
</asp:Content>