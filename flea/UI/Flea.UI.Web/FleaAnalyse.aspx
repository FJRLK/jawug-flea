<%@ Page Language="C#" AutoEventWireup="true" Inherits="FleaAnalyse" Codebehind="FleaAnalyse.aspx.cs" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" style="width:100%; height:100%">
<head runat="server">
    <title></title>
</head>
<body style="width:100%; height:100%">
    <form id="form1" runat="server" style="width:100%; height:100%">
    <div>
    
        <table style="width:100%; height:100%">
            <tr>
                <td>
                    &nbsp;</td>
                <td>
                    &nbsp;</td>
                <td>
                    &nbsp;</td>
            </tr>
            <tr>
                <td>
                    WUG.ZA.NET LOS TOOL<br />(Thanks Xarion)</td>
                <td>
                    &nbsp;</td>
                <td>
                    ALTERNATE LOS TOOL<br />(Thanks heywhatsthat.com)</td>
            </tr>
            <tr>
                <td>
                    &nbsp;</td>
                <td>
                    &nbsp;</td>
                <td>
                    &nbsp;</td>
            </tr>
            <tr>
                <td height="310px" width="505px">
                    <iframe src="http://www.wug.za.net/newlos2.php?fl=<% = Id1 %>&tl=<% = Id2 %>" width="505px" height="310px" ></iframe>
                    &nbsp;
                </td>
                <td>
                    &nbsp;</td>
                <td height="310px">
                <iframe src="http://www.heywhatsthat.com/bin/profile.cgi?src=profiler&axes=1&curvature=0&metric=1&pt0=<% = Lat1 %>,<% = Lon1 %>,ff0000&pt1=<% = Lat2 %>,<% = Lon2 %>,00c000"  width="100%" height="310px" ></iframe>
                    &nbsp;</td>
            </tr>
            <tr>
                <td>
                    &nbsp;</td>
                <td>
                    &nbsp;</td>
                <td>
                    &nbsp;</td>
            </tr>
            <tr>
                <td >
                    MAP</td>
            </tr>
            <tr>
                <td colspan="3">
                <iframe src="http://flea.wolfen.za.net/FleaMap.aspx?Lat1=<% = Lat1 %>&Lat2=<% = Lat2 %>&Lon1=<% = Lon1 %>&Lon2=<% = Lon2 %>&Name1=<% = Name1 %>&Name2=<% = Name2 %>" width="100%" height="500px"> </iframe>
                    </td>
            </tr>
        </table>
    
    </div>
    </form>
</body>
</html>
