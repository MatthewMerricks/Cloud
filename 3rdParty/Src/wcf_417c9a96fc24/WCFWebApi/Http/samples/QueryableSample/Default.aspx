<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="QueryableSample.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
</head>
<body style="height: 608px">
    <form id="form1" runat="server">
    <div>
    </div>
    <asp:Label ID="Label1" runat="server" Text="Welcome to Queryable Sample" Style="font-weight: 700"></asp:Label>
    <table>
        <tr>
            <td>
                Format:
            </td>
            <td>
                <asp:RadioButtonList ID="Format" runat="server">
                    <asp:ListItem Selected=True>Xml</asp:ListItem>
                    <asp:ListItem>Json</asp:ListItem>
                </asp:RadioButtonList>
            </td>
        </tr>
    </table>
    <p>
        <asp:Button ID="GetAllContacts" runat="server" OnClick="GetAllContacts_Click" Text="Get All Contacts"
            Width="136px" />
        <asp:TextBox ID="TextBox1" ReadOnly=true runat="server" Width="291px"></asp:TextBox>
    </p>
    <p>
        <asp:Button ID="GetTop3" runat="server" OnClick="GetTop3_Click" Text="Get Top 3 Contacts"
            Width="135px" />
        <asp:TextBox ID="TextBox2" runat="server" Width="291px"></asp:TextBox>
    </p>
    <p>
        <asp:Button ID="PostNewContact" runat="server" OnClick="PostNewContact_Click" Text="Post new Contact"
            Width="135px" />
        <asp:TextBox ID="TextBox3" runat="server" Width="291px">Please enter the Name here</asp:TextBox>
    </p>
    <p>
        <asp:Button ID="GetID5" runat="server" OnClick="GetId5_Click" Text="Get 5th contact"
            Width="135px" />
        <asp:TextBox ID="TextBox4" runat="server" Width="291px"></asp:TextBox>
    </p>
    <p>
        <asp:Button ID="GetID6" runat="server" OnClick="GetId6_Click" Text="Get contact with Id:"
            Width="135px" />
        <asp:TextBox ID="TextBox5" runat="server" Width="291px">Please enter an integer here</asp:TextBox>
    </p>
    <asp:TextBox ID="Result" ReadOnly="true" runat="server" Height="117px" TextMode="MultiLine" Width="530px"></asp:TextBox>
    </form>
    <asp:PlaceHolder ID="ResponseBody" runat="server"></asp:PlaceHolder>
</body>
</html>
