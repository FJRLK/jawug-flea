using System;
using System.Collections.Generic;
using System.Web.UI;

public partial class FleaNear : Page
{
    #region Internals

    public string Lat1 = "Lat1";
    public string Lon1 = "Lon1";
    public string Name1 = "Name1";

    #endregion

    #region Members

    public void MakeJavaScript()
    {
        if (Request.QueryString["Node"] != null)
        {
            if (Request.QueryString["Data"] == null)
            {
                string NodeName = "Node" + Request.QueryString["Node"];
                List<string> DataList = ((List<string>) Application[NodeName]);
                if (DataList != null)
                {
                    foreach (string thisLoc in DataList)
                    {
                        string nodename = thisLoc.Split(',')[1];
                        string lat = thisLoc.Split(',')[2];
                        string lon = thisLoc.Split(',')[3];

                        Response.Write("var position2 = new google.maps.LatLng(" + lat + ", " + lon + ");\r\n");
                        Response.Write("makeMarker(position2, \"" + nodename + "\", \"" + nodename + "\", map);\r\n");
                        Response.Write("makeLine(position1, position2, map);\r\n");
                    }
                }
            }
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request.Params["Lat1"] != null)
            Lat1 = Request.Params["Lat1"];

        if (Request.Params["Lon1"] != null)
            Lon1 = Request.Params["Lon1"];

        if (Request.Params["Name1"] != null)
            Name1 = Request.Params["Name1"];


        if (Request.QueryString["Node"] != null)
        {
            if (Request.QueryString["Data"] != null)
            {
                // Store Mode
                string NodeName = "Node" + Request.QueryString["Node"];
                string DataLoad = Request.QueryString["Data"];
                if (Application[NodeName] == null)
                    Application.Add(NodeName, new List<string>());
                ((List<string>) Application[NodeName]).Add(DataLoad);
            }
            else
            {
                // Display Mode
            }
        }
    }

    #endregion
}