<%@ Page Language="C#" AutoEventWireup="true" Inherits="FleaWeb.Default" Codebehind="Default.aspx.cs" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
<form id="form1" runat="server">
    <div>

        Welcome To Flea&#39;s Command Website:<br/>
        <br/>
        Select Command:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
        <asp:DropDownList ID="ddlCommandList" runat="server" Height="16px" Width="169px">
            <asp:ListItem>8BALL</asp:ListItem>
            <asp:ListItem>ANA</asp:ListItem>
            <asp:ListItem>ANNOUNCE</asp:ListItem>
            <asp:ListItem>CHECKFREQ</asp:ListItem>
            <asp:ListItem>CHECKPORT</asp:ListItem>
            <asp:ListItem>DNS</asp:ListItem>
            <asp:ListItem>DNSFIND</asp:ListItem>
            <asp:ListItem>EXCUSE</asp:ListItem>
            <asp:ListItem>GOOGLE</asp:ListItem>
            <asp:ListItem>GREET</asp:ListItem>
            <asp:ListItem>GSG</asp:ListItem>
            <asp:ListItem>IPCALC</asp:ListItem>
            <asp:ListItem>KML</asp:ListItem>
            <asp:ListItem>LOC</asp:ListItem>
            <asp:ListItem>LOS</asp:ListItem>
            <asp:ListItem>LOS2</asp:ListItem>
            <asp:ListItem>MAP</asp:ListItem>
            <asp:ListItem>MAKEWORD</asp:ListItem>
            <asp:ListItem>MAPTRACE</asp:ListItem>
            <asp:ListItem>MEMBERS</asp:ListItem>
            <asp:ListItem>MEMSTATUS</asp:ListItem>
            <asp:ListItem>MYSQLTEST</asp:ListItem>
            <asp:ListItem>NATRULE</asp:ListItem>
            <asp:ListItem>NEAR</asp:ListItem>
            <asp:ListItem>PING</asp:ListItem>
            <asp:ListItem>RADIUS</asp:ListItem>
            <asp:ListItem>RBCP</asp:ListItem>
            <asp:ListItem>REFRESHDB</asp:ListItem>
            <asp:ListItem>REGISTER</asp:ListItem>
            <asp:ListItem>ROUTE</asp:ListItem>
            <asp:ListItem>RTRACE</asp:ListItem>
            <asp:ListItem>SCOREBOARD</asp:ListItem>
            <asp:ListItem>SERVICES</asp:ListItem>
            <asp:ListItem>TESTKIT</asp:ListItem>
            <asp:ListItem>TIME</asp:ListItem>
            <asp:ListItem>TRACE</asp:ListItem>
            <asp:ListItem>UNREGISTER</asp:ListItem>
            <asp:ListItem>UPTIME</asp:ListItem>
            <asp:ListItem>WEATHER</asp:ListItem>
            <asp:ListItem>WHATSMYIP</asp:ListItem>
            <asp:ListItem>WGET</asp:ListItem>
            <asp:ListItem>WORDS</asp:ListItem>
        </asp:DropDownList>
        &nbsp;&nbsp;&nbsp;&nbsp; Enter Parameters:
        <asp:TextBox ID="txtParms" runat="server" Width="320px"></asp:TextBox>
        &nbsp;&nbsp;
        <asp:Button ID="btnGo" runat="server" Text="Go"/>
        <br/>
        <br/>
        Results:<br/>
        <br/>
        <asp:Label ID="lblResults" runat="server"></asp:Label>
        <br/>
        <br/>
        <br/>
        <br/>
        <br/>
        <br/>
        <br/>
        <br/>
        <br/>
        <br/>
        <br/>
        <br/>
        <br/>

    </div>
</form>
</body>
</html>