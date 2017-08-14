using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web.UI;

namespace FleaWeb
{
    public partial class FleaUp : Page
    {
        #region Types

        private class BbLink
        {
            #region Internals

            public string Identity { get; private set; }
            private string LinkUp { get; set; }
            public string LoopBack { get; private set; }
            public string PeerAsn { get; private set; }
            public string RemoteIdentity { get; set; }
            public string RemoteLo0 { get; set; }

            #endregion

            #region Constructor

            public BbLink(string identity, string loopBack, string peerAsn, string linkUp)
            {
                Identity = identity;
                LoopBack = loopBack;
                PeerAsn = peerAsn;
                LinkUp = linkUp;
            }

            #endregion
        }

        private class MapNode
        {
            #region Internals

            public string CoOrds { get; set; }
            public string HoverText { get; set; }
            public string HsLocation { get; set; }
            public string Identity { get; set; }
            public string Ip { get; set; }
            public bool IsAccurateLocation { get; set; }
            public bool IsHighSite { get; set; }
            public bool IsOnline { get; set; }
            public string Label { get; set; }
            public string LastSeen { get; set; }
            public string Location { get; set; }

            #endregion

            #region Constructor

            public MapNode(string ip, string identity, string lastSeen, string location, string hsLocation,
                string coOrds)
            {
                Ip = ip;
                Identity = identity;
                LastSeen = lastSeen;
                Location = location;
                HsLocation = hsLocation;
                CoOrds = coOrds;
            }

            #endregion
        }

        #endregion

        #region Internals

        private readonly Dictionary<string, MapNode> _CoOrdLookup = new Dictionary<string, MapNode>();

        private readonly Dictionary<string, MapNode> _IdentityLookup = new Dictionary<string, MapNode>();
        private readonly Dictionary<string, MapNode> _LocationLookup = new Dictionary<string, MapNode>();
        private readonly Dictionary<string, MapNode> _SsidLookup = new Dictionary<string, MapNode>();
        int _TotalLines = 0;
        private int _TotalMapNodes = 0;

        public float GpsOffSet = 0F;

        #endregion

        #region Members

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void GetAllMarkers()
        {
            string physicalLocation = ConfigurationManager.AppSettings["PhysicalLocation"];

            KmlProcessor myKml = new KmlProcessor(physicalLocation);


            string hsData = SafeFetchUrlWithFileBackup("http://droid.russian.za.net:85/ros/highsite", "highsite.txt");
            string clientData = SafeFetchUrlWithFileBackup("http://droid.russian.za.net:85/ros/client", "client.txt");
            string ubntData =
                SafeFetchUrlWithFileBackup(
                    "http://droid.russian.za.net:85/ros/ubnt.php?order_by=wlanOpmode&sorting=desc", "ubnt.txt");
            string peerData = SafeFetchUrlWithFileBackup("http://droid.russian.za.net:85/ros/peerlatency.php",
                "peer.txt");

            MakeNodes(myKml, hsData, 19, 0, 1, 2, 12, 18, 18, "1", false);
            MakeNodes(myKml, clientData, 18, 0, 1, 4, 14, 17, 2, "1", true);

            MakeNodesUbnt(myKml, ubntData, 11, 0, 1, 2, 5, 9, 10, 7, 6, 3, 4);

            string[] hsCells = peerData.Split(new string[] {"<td"}, StringSplitOptions.RemoveEmptyEntries);
            const int TableWidth = 15;
            List<BbLink> linkList = new List<BbLink>();
            for (int ii = 1; ii < hsCells.Length; ii = ii + TableWidth)
            {
                if (ii + TableWidth >= hsCells.Length) continue;
                string identity = ExtractCellContents(hsCells[ii + 1]);
                string loopBack = ExtractCellContents(hsCells[ii + 3]);
                string peerAsn = ExtractCellContents(hsCells[ii + 5]);
                string linkUp = ExtractCellContents(hsCells[ii + 8]);

                BbLink myBbLink = new BbLink(identity, loopBack, peerAsn, linkUp);
                linkList.Add(myBbLink);
            }

            foreach (BbLink thisLink in linkList)
            {
                thisLink.RemoteLo0 = ("172.16.250." + thisLink.PeerAsn.Substring(2)).Replace(".0", ".")
                    .Replace(".0", ".");
                thisLink.RemoteIdentity = "Not found";
                BbLink target = linkList.Find(thisOne => thisOne.LoopBack == thisLink.RemoteLo0);
                if (target == null) continue;
                thisLink.RemoteIdentity = target.Identity;

                if (!_IdentityLookup.ContainsKey(thisLink.Identity.ToLowerInvariant()) ||
                    !_IdentityLookup.ContainsKey(thisLink.RemoteIdentity.ToLowerInvariant())) continue;
                string sourceGps = _IdentityLookup[thisLink.Identity.ToLowerInvariant()].CoOrds;
                string targetGps = _IdentityLookup[thisLink.RemoteIdentity.ToLowerInvariant()].CoOrds;

                Drawline(sourceGps, targetGps);
            }
        }

        private string SafeFetchUrlWithFileBackup(string urlToFix, string filename)
        {
            WebClient myWebClient = new WebClient();
            string data = "";
            try
            {
                data = myWebClient.DownloadString(urlToFix);
                using (StreamWriter myWriter = new StreamWriter(Server.MapPath($"~/{filename}"), true))
                {
                    myWriter.WriteLine(data); // Write the file.
                }
                return data;
            }
            catch (Exception)
            {
                data = File.ReadAllText(Server.MapPath($"~/{filename}"));
                return data;
            }
        }

        private void MakeNodesUbnt(KmlProcessor MyKml, string HSData, int TableWidth, int IdPos, int IPPos,
            int IdentityPos, int LocationPos, int UpdownPos, int LinkUpDownPos, int LocationLatPos, int LocationLongPos,
            int SSIDPos, int ApModePos)
        {
            string[] HSCells = HSData.Split(new string[] {"<td"}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < HSCells.Length; i = i + TableWidth)
            {
                if (i + TableWidth < HSCells.Length)
                {
                    string Id = ExtractCellContents(HSCells[i + IdPos]);
                    string IP = ExtractCellContents(HSCells[i + IPPos]);
                    string Identity = ExtractCellContents(HSCells[i + IdentityPos]);
                    string UpDown = ExtractCellContents(HSCells[i + UpdownPos]);
                    string LinkState = ExtractCellContents(HSCells[i + LinkUpDownPos]);
                    string LocationLat = ExtractCellContents(HSCells[i + LocationLatPos]).Trim();
                    string LocationLong = ExtractCellContents(HSCells[i + LocationLongPos]).Trim();
                    string LocationName = ExtractCellContents(HSCells[i + LocationPos]).Trim();
                    string Gps = LocationLat + "," + LocationLong;
                    string SSID = ExtractCellContents(HSCells[i + SSIDPos]).Trim();
                    string ApMode = ExtractCellContents(HSCells[i + ApModePos]).Trim();

                    MapNode MySite = new MapNode(IP, Identity, "Unknown", LocationName, "", Gps);
                    if (!_IdentityLookup.ContainsKey(Identity.ToLowerInvariant()))
                    {
                        _IdentityLookup.Add(Identity.ToLowerInvariant(), MySite);
                    }

                    if (MySite.Location == "") MySite.Location = MySite.Identity;

                    // try fix co-ords by location name from hist
                    if (MySite.CoOrds == "0.000000,0.000000")
                    {
                        if (_LocationLookup.ContainsKey(MySite.Location.ToLowerInvariant()))
                            MySite.CoOrds = _LocationLookup[MySite.Location.ToLowerInvariant()].CoOrds;
                    }

                    // try fix co-ords by location name from KML
                    string KMLCoOrds = GetGPSCoOrds(MyKml, MySite.Location.ToLowerInvariant());
                    if (KMLCoOrds != "0,0")
                    {
                        MySite.CoOrds = KMLCoOrds;
                    }


                    if (MySite.Location != "" && MySite.CoOrds != ",")
                    {
                        MySite.IsOnline = ((UpDown == "UP") && (LinkState == "UP"));
                        MySite.IsHighSite = (ApMode == "ap");
                        MySite.IsAccurateLocation = true;
                        MySite.Label = MySite.Location;
                        MySite.HoverText = MySite.Identity + " (*)";
                        if (!MySite.IsHighSite) MySite.HoverText += "\\n Connect To:" + SSID + "\\n IP:" + IP;
                        else MySite.HoverText += "\\n Broadcasts:" + SSID + "\\n IP:" + IP;
                        if (MySite.IsHighSite) MySite.HoverText = MySite.HoverText.Replace("(*)", "(ubnt hs)");
                        else MySite.HoverText = MySite.HoverText.Replace("(*)", "(ubnt client)");
                        MySite.HoverText += "\\n Unit: " + UpDown + " Link: " + LinkState;
                        // Merge duplicate nodes on same spot
                        MySite = MergeDups(MySite);

                        // store ssid for lookup
                        if (!_SsidLookup.ContainsKey(SSID))
                            _SsidLookup.Add(SSID, MySite);

                        MakeMapNode(MySite.IsOnline, MySite.IsHighSite, MySite.IsAccurateLocation, MySite.CoOrds,
                            MySite.Label, MySite.HoverText);


                        if (ApMode != "ap")
                        {
                            Drawline(MySite.CoOrds, _SsidLookup[SSID].CoOrds, "#0000FF");
                        }
                    }
                }
            }
        }

        private void MakeNodes(KmlProcessor MyKml, string HSData, int TableWidth, int IdPos, int IPPos, int IdentityPos,
            int LastSeenPos, int LocationPos, int HSLocationPos, string ExitValue, bool LineToHS)
        {
            string[] HSCells = HSData.Split(new string[] {"<td"}, StringSplitOptions.RemoveEmptyEntries);
            string PrevHSLocation = "";
            bool first = true;
            float GPSOffset = -0.004F;
            float UnknownGPSOffset = 0F;
            for (int i = 1; i < HSCells.Length; i = i + TableWidth)
            {
                if (i + TableWidth < HSCells.Length)
                {
                    string Id = ExtractCellContents(HSCells[i + IdPos]);
                    if ((Id == ExitValue) && !first) break;
                    string IP = ExtractCellContents(HSCells[i + IPPos]);
                    string Identity = ExtractCellContents(HSCells[i + IdentityPos]);
                    string LastSeen = ExtractCellContents(HSCells[i + LastSeenPos]);
                    string Location = ExtractCellContents(HSCells[i + LocationPos]).Trim();
                    string HSLocation = ExtractCellContents(HSCells[i + HSLocationPos]).Trim();
                    string Gps = GetGPSCoOrds(MyKml, Location);
                    string SSID = ExtractCellContents(HSCells[i + 3]).Trim();
                    string WLANIP = ExtractCellContents(HSCells[i + 5]).Trim() + "," +
                                    ExtractCellContents(HSCells[i + 6]).Trim();
                    string PostFix = "";

                    //ManualOverride(MyKml, Identity, ref Location, ref Gps);

                    if (PrevHSLocation != HSLocation) GPSOffset = -0.004F;
                    PrevHSLocation = HSLocation;

                    if (Location == "")
                    {
                        // Try using the Identity
                        string SearchString = Identity.ToLowerInvariant();
                        Gps = GetCoords(MyKml, Gps, SearchString.Replace("jawug", ""));
                        Gps = GetCoords(MyKml, Gps, SearchString);

                        // Try using the KML with the identity
                        if (Gps == "0,0")
                        {
                            Gps = GetGPSCoOrds(MyKml, SearchString);
                            if (Gps == "0,0") Gps = GetGPSCoOrds(MyKml, SearchString.Replace("jawug", ""));
                        }

                        // Default it to near the highsite
                        if (Gps == "0,0")
                        {
                            if (_IdentityLookup.ContainsKey(HSLocation.ToLowerInvariant()) && HSLocation != "")
                            {
                                MapNode Result = _IdentityLookup[HSLocation.ToLowerInvariant()];
                                Gps = GetGPSCoOrds(MyKml, Result.Location, GPSOffset);
                                GPSOffset += 0.00175F;
                                PostFix = "*";
                            }
                        }
                    }

                    if (Gps == "0,0")
                    {
                        Gps = (-26 + UnknownGPSOffset).ToString(CultureInfo.InvariantCulture) + ", 28";
                        UnknownGPSOffset += 0.00175F;
                    }

                    MapNode MySite = new MapNode(IP, Identity, LastSeen, Location, HSLocation, Gps);
                    if (!_IdentityLookup.ContainsKey(Identity.ToLowerInvariant()))
                    {
                        _IdentityLookup.Add(Identity.ToLowerInvariant(), MySite);
                    }

                    if (MySite.Location == "") MySite.Location = MySite.Identity;
                    MySite.Location = MySite.Location + PostFix;

                    if (MySite.Location != "*" && MySite.Location != "")
                    {
                        MySite.IsOnline = (Convert.ToDateTime(LastSeen) > DateTime.Now.AddHours(-48));
                        MySite.IsHighSite = !LineToHS;
                        MySite.IsAccurateLocation = (PostFix != "*");
                        MySite.Label = MySite.Location;
                        MySite.HoverText = MySite.Identity + " (*)\\n Last seen:" + LastSeen;
                        if (!MySite.IsHighSite) MySite.HoverText += "\\n Connect To:" + SSID + "\\n IP:" + WLANIP;
                        else MySite.HoverText += "\\n IP:" + IP;
                        if (MySite.IsHighSite) MySite.HoverText = MySite.HoverText.Replace("(*)", "(identity)");
                        else MySite.HoverText = MySite.HoverText.Replace("(*)", "(radio)");

                        // Merge duplicate nodes on same spot
                        MySite = MergeDups(MySite);

                        MakeMapNode(MySite.IsOnline, MySite.IsHighSite, MySite.IsAccurateLocation, MySite.CoOrds,
                            MySite.Label, MySite.HoverText);

                        if (_IdentityLookup.ContainsKey(HSLocation.ToLowerInvariant()) && HSLocation != "")
                        {
                            Drawline(Gps, _IdentityLookup[HSLocation.ToLowerInvariant()].CoOrds);
                        }

                        first = false;
                    }
                }
            }
        }

        private MapNode MergeDups(MapNode MySite)
        {
            MapNode AlreadyFound = null;

            if (!_LocationLookup.ContainsKey(MySite.Location.ToLowerInvariant()))
            {
                _LocationLookup.Add(MySite.Location.ToLowerInvariant(), MySite);
            }
            else AlreadyFound = _LocationLookup[MySite.Location.ToLowerInvariant()];

            if (!_CoOrdLookup.ContainsKey(MySite.CoOrds.ToLowerInvariant()))
            {
                _CoOrdLookup.Add(MySite.CoOrds.ToLowerInvariant(), MySite);
            }
            else AlreadyFound = _CoOrdLookup[MySite.CoOrds.ToLowerInvariant()];

            // Merge Needed
            if (AlreadyFound != null)
            {
                AlreadyFound.HsLocation += "," + MySite.HsLocation;
                AlreadyFound.Identity += "," + MySite.Identity;
                AlreadyFound.HoverText += "\\n\\n" + MySite.HoverText;
                AlreadyFound.Ip += "," + MySite.Ip;
                AlreadyFound.IsHighSite = AlreadyFound.IsHighSite || MySite.IsHighSite;
                AlreadyFound.IsOnline = AlreadyFound.IsOnline && MySite.IsOnline;
                if (MySite.LastSeen != "Unknown")
                    AlreadyFound.LastSeen =
                        new DateTime(Math.Min(Convert.ToDateTime(MySite.LastSeen).Ticks,
                            Convert.ToDateTime(AlreadyFound.LastSeen).Ticks)).ToString();
                MySite = AlreadyFound;
            }
            return MySite;
        }

        private void MakeMapNode(bool IsOnline, bool IsHighSite, bool IsAccurateLocation, string GPSAddress,
            string Label, string HoverText)
        {
            // Calc Color
            string IconUrl = "?";
            if (!IsHighSite)
            {
                // Client Colors
                if (!IsOnline && IsAccurateLocation) IconUrl = "ms/icons/red-dot.png";
                if (!IsOnline && !IsAccurateLocation) IconUrl = "ms/icons/orange-dot.png";
                if (IsOnline && !IsAccurateLocation) IconUrl = "ms/icons/yellow-dot.png";
                if (IsOnline && IsAccurateLocation) IconUrl = "ms/icons/green-dot.png";
            }
            else
            {
                // HS Colors
                if (IsOnline) IconUrl = "kml/pal3/icon47.png";
                else IconUrl = "kml/pal3/icon39.png";
            }

            _TotalMapNodes++;
            string MakePosition = "var position" + _TotalMapNodes.ToString() + " = new google.maps.LatLng(" + GPSAddress +
                                  ");\n";
            string MakeMarker = "makeMarker(position" + _TotalMapNodes.ToString() + ", \"" + HoverText + "\", \"" +
                                Label +
                                "\", \"" + IconUrl + "\", map);\n";
            Response.Write(MakePosition);
            Response.Write(MakeMarker);
        }

        private string GetCoords(KmlProcessor MyKml, string Gps, string SearchString)
        {
            if (_IdentityLookup.ContainsKey(SearchString))
            {
                MapNode Result = _IdentityLookup[SearchString];
                Gps = GetGPSCoOrds(MyKml, Result.Location);
            }
            return Gps;
        }


        private void Drawline(string Gps, string Gps2, string Color)
        {
            _TotalLines++;
            string Pos1 = "var position" + _TotalLines.ToString() + "_1 = new google.maps.LatLng(" + Gps + ");\n";
            string Pos2 = "var position" + _TotalLines.ToString() + "_2 = new google.maps.LatLng(" + Gps2 + ");\n";
            string Line1 = "var lineCoordinates" + _TotalLines.ToString() + " = [ position" + _TotalLines.ToString() +
                           "_1, position" + _TotalLines.ToString() + "_2  ]; \n";
            string Line2 = "var theLine" + _TotalLines.ToString() + " = new google.maps.Polyline({  \n";
            string Line3 = "  path: lineCoordinates" + _TotalLines.ToString() + ", strokeColor: \"" + Color +
                           "\", strokeOpacity: 1.0, strokeWeight: 2 }); \n";
            string Line4 = "  theLine" + _TotalLines.ToString() + ".setMap(map); \n";

            Response.Write(Pos1);
            Response.Write(Pos2);
            Response.Write(Line1);
            Response.Write(Line2);
            Response.Write(Line3);
            Response.Write(Line4);


            //string MakePosition = "var position" + i.ToString() + " = new google.maps.LatLng(" + Gps + ");\n";
            //string MakeMarker = "makeMarker(position" + i.ToString() + ", \"" + MySite.Identity + "\", \"" + MySite.Location + "\", \"" + Color + "\", map);\n";
            //Response.Write(MakePosition);
            //Response.Write(MakeMarker);
        }


        private void Drawline(string Gps, string Gps2)
        {
            Drawline(Gps, Gps2, "#FF0000");
        }

        private void ManualOverride(KmlProcessor MyKml, string Identity, ref string Location, ref string Gps)
        {
            switch (Identity)
            {
                case "Bryanpark3":
                    Location = "(BP) Bryanpark";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "DarkShadow":
                    Location = "Darkshadow";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "cAtilre1":
                    Location = "cAtilre(HS)";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "cAtilre2":
                    Location = "cAtilre(HS)";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "GElston750":
                    Location = "GElston";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "IVO":
                    Location = "Ivo";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "LR":
                    Location = "(LR)LinksfieldRidge";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "Moerbei Nest":
                    Location = "Moerbei nest";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "MoerBeiSXTbbAsh":
                    Location = "Moerbei nest";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "NC1":
                    Location = "(NC) Northcliff";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "Nivec":
                    Location = "KDM";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "Scully":
                    Location = "(SCU) Scully";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "TF":
                    Location = "(TF) The Forum";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "TOOLBOY":
                    Location = "Toolboy";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
                case "xxxxxx":
                    Location = "Toolboy";
                    Gps = GetGPSCoOrds(MyKml, Location);
                    break;
            }
        }

        private string GetGPSCoOrds(KmlProcessor MyKml, string GPSSearch)
        {
            return GetGPSCoOrds(MyKml, GPSSearch, 0F);
        }

        private string GetGPSCoOrds(KmlProcessor MyKml, string GPSSearch, float OffSet)
        {
            string gps = "0,0";
            if (GPSSearch != "")
            {
                if (MyKml.coOrds.ContainsKey(GPSSearch.ToLowerInvariant()))
                {
                    gps = MyKml.coOrds[GPSSearch.ToLowerInvariant()];
                }
            }
            if (OffSet != 0)
            {
                string[] CoOrds = gps.Split(',');
                CoOrds[0] =
                    (Convert.ToSingle(CoOrds[0], CultureInfo.InvariantCulture) - 0.002F).ToString(
                        CultureInfo.InvariantCulture);
                CoOrds[1] =
                    (Convert.ToSingle(CoOrds[1], CultureInfo.InvariantCulture) + OffSet).ToString(
                        CultureInfo.InvariantCulture);
                gps = CoOrds[0] + "," + CoOrds[1];
            }
            return gps;
        }

        private string ExtractCellContents(string CellContent)
        {
            CellContent = CellContent.Substring(CellContent.IndexOf('>') + 1).Replace("<b>", "");
            CellContent = CellContent.Substring(0, CellContent.IndexOf('<'));
            return CellContent.Trim();
        }

        #endregion
    }
}