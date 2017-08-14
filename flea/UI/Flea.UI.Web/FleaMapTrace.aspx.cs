using System;
using System.Collections.Generic;
using System.Web.UI;

public partial class FleaMapTrace : Page
{
    #region Types

    private class TraceNode
    {
        #region Internals

        public string Hop { get; set; }
        public string Location { get; set; }
        public string Name { get; set; }

        #endregion

        #region Constructor

        public TraceNode(string Hop, string Name, string Location)
        {
            this.Hop = Hop;
            this.Name = Name;
            this.Location = Location;
        }

        #endregion

        #region Members

        public override string ToString()
        {
            return $"[Hop: {Hop} Name: {Name} Location: {Location}]";
        }

        #endregion
    }

    #endregion

    #region Internals

    public string Bit1 = "";
    public string Bit2 = "";
    public string Bit3 = "";
    public string MaxNode = "0";
    public string MinNode = "999";

    #endregion

    #region Members

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request.Params["Id"] != null)
        {
            // Store Mode
            string NodeName = Request.QueryString["Id"];
            string DataHop = Request.QueryString["Hop"];
            string DataName = Request.QueryString["Name"];
            string DataLocation = Request.QueryString["Location"];

            // Check if we already have id
            if (Application[NodeName] == null)
            {
                Application.Add(NodeName, new List<TraceNode>());
            }

            List<TraceNode> StorageSpot = ((List<TraceNode>) Application[NodeName]);
            StorageSpot.Add(new TraceNode(DataHop, DataName, DataLocation));
        }
        else
        {
            // Display Mode
            string NodeName = Request.QueryString["DisplayId"];
            List<TraceNode> StorageSpot = ((List<TraceNode>) Application[NodeName]);
            if (StorageSpot != null)
            {
                foreach (TraceNode thisNode in StorageSpot)
                {
                    Bit1 += "var position" + thisNode.Hop + " = new google.maps.LatLng(" + thisNode.Location + ");\r\n";
                    Bit2 += "makeMarker(position" + thisNode.Hop + ", '" + thisNode.Name + "', '" + thisNode.Name +
                            "', map);\r\n";
                    Bit3 += "position" + thisNode.Hop + ",";
                    if (Convert.ToInt32(thisNode.Hop) < Convert.ToInt32(MinNode)) MinNode = thisNode.Hop;
                    if (Convert.ToInt32(thisNode.Hop) > Convert.ToInt32(MaxNode)) MaxNode = thisNode.Hop;
                }
            }

            if (Bit3.Length > 0)
            {
                Bit3 = Bit3.Substring(0, Bit3.Length - 1);
            }
        }
    }

    #endregion
}