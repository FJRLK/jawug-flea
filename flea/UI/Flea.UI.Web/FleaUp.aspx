<%@ Page Language="C#" AutoEventWireup="true" Inherits="FleaWeb.FleaUp" Codebehind="FleaUp.aspx.cs" %>

<!-- !DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd" -->

<html xmlns="http://www.w3.org/1999/xhtml" style="height: 100%; width: 100%;">
<head runat="server">
    <title>Flea's Map Display</title>
    <script type="text/javascript"
            src="http://maps.google.com/maps/api/js?sensor=false">

    </script>
    <script type="text/javascript" src="js/label.js"></script>
    <script type="text/javascript">

        function makeMarker(p_position, p_title, p_label, p_color, p_map) {
            var iconurl = 'http://maps.google.com/mapfiles/' + p_color;
            var marker = new google.maps.Marker({
                position: p_position,
                map: p_map,
                title: p_title,
                label: p_label,
                icon: iconurl
            });
            var label = new Label({
                map: p_map
            });
            label.bindTo('position', marker, 'position');
            label.bindTo('text', marker, 'label');
        }

        function calcMidPoint(p_position1, p_position2) {

            var midX = (p_position1.lat() + p_position2.lat()) / 2.0;
            var midY = (p_position1.lng() + p_position2.lng()) / 2.0;
            return new google.maps.LatLng(midX, midY);
        }


        function initialize() {

            var midpoint = new google.maps.LatLng(-26.083090, 27.995060);
            var myOptions = {
                zoom: 10,
                center: midpoint,
                mapTypeId: google.maps.MapTypeId.TERRAIN //ROADMAP
            };
            var map = new google.maps.Map(
                document.getElementById("map_canvas"),
                myOptions);

            <% GetAllMarkers(); %>

            map.controls[google.maps.ControlPosition.RIGHT_BOTTOM].push(document.getElementById('legend'));

        }

    </script>
    <style>
        #legend {
            background: white;
            font-size: 10pt;
            padding: 10px;
        }
    </style>
</head>
<body onload="initialize()" style="height: 100%; width: 100%;">
<form id="form1" runat="server">
    <div style="height: 100%; width: 100%;">
        <div id="map_canvas" style="height: 98%; width: 98%;"></div>
    </div>
    <div id="legend">
        <img src="http://maps.google.com/mapfiles/kml/pal3/icon47.png" height="12px"/><img src="http://maps.google.com/mapfiles/kml/pal3/icon39.png" height="12px"/>Highsite Online/Offline<br/>
        <img src="http://maps.google.com/mapfiles/ms/icons/green-dot.png" height="12px"/><img src="http://maps.google.com/mapfiles/ms/icons/red-dot.png" height="12px"/>Client Node Online/Offline<br/>
        <img src="http://maps.google.com/mapfiles/ms/icons/yellow-dot.png" height="12px"/><img src="http://maps.google.com/mapfiles/ms/icons/orange-dot.png" height="12px"/>Client Node (Guess location) On/Off<br/>
    </div>
</form>
</body>
</html>