<%@ Page Language="C#" AutoEventWireup="true" Inherits="FleaMap" Codebehind="FleaMap.aspx.cs" %>

<!-- !DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd" -->

<html xmlns="http://www.w3.org/1999/xhtml" style="width:100%; height:100%">
<head runat="server">
    <title>Flea's Map Display</title>
<script type="text/javascript"
    src="http://maps.google.com/maps/api/js?sensor=false">
</script>
<script type="text/javascript" src="js/label.js"></script>
<script type="text/javascript">

    function makeMarker(p_position, p_title, p_label, p_map) {
        var marker = new google.maps.Marker({
            position: p_position,
            map: p_map,
            title: p_title,
            label: p_label
        });
        var label = new Label({
            map: p_map
        });
        label.bindTo('position', marker, 'position');
        label.bindTo('text', marker, 'label');
    }

    function calcMidPoint(p_position1, p_position2) {

        var midX = (p_position1.lat() + p_position2.lat())/2.0;
        var midY = (p_position1.lng() + p_position2.lng()) / 2.0;
        return new google.maps.LatLng(midX, midY);
    }


    function initialize() {
        var position1 = new google.maps.LatLng(<% = Lat1 %>, <% = Lon1 %>);
        var position2 = new google.maps.LatLng(<% = Lat2 %>, <% = Lon2 %>);
        var midpoint = calcMidPoint(position1, position2);
        var myOptions = {
            zoom: 14,
            center: midpoint,
            mapTypeId: google.maps.MapTypeId.TERRAIN
        };
        var map = new google.maps.Map(
        document.getElementById("map_canvas"),
        myOptions);

        makeMarker(position1, "<% = Name1 %>", "<% = Name1 %>", map);
        makeMarker(position2, "<% = Name2 %>", "<% = Name2 %>", map);

        var lineCoordinates = [ position1, position2  ];
        var theLine = new google.maps.Polyline({
            path: lineCoordinates,
            strokeColor: "#FF0000",
            strokeOpacity: 1.0,
            strokeWeight: 2
        });

        theLine.setMap(map);
    }
 
</script>
</head>
<body onload="initialize()" style="width:100%; height:100%">
    <form id="form1" runat="server">
    <div style="width:100%; height:100%">
        <div id="map_canvas" style="width:95%; height:95%"></div>
    </div>
    </form>
</body>
</html>



