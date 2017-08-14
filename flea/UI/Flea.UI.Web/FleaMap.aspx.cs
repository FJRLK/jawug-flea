using System;
using System.Web.UI;

public partial class FleaMap : Page
{
    #region Internals

    public string Lat1 = "Lat1";
    public string Lat2 = "Lat2";
    public string Lon1 = "Lon1";
    public string Lon2 = "Lon2";
    public string Name1 = "Name1";
    public string Name2 = "Name2";

    #endregion

    #region Members

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request.Params["Lat1"] != null)
            Lat1 = Request.Params["Lat1"];
        if (Request.Params["Lat2"] != null)
            Lat2 = Request.Params["Lat2"];

        if (Request.Params["Lon1"] != null)
            Lon1 = Request.Params["Lon1"];
        if (Request.Params["Lon2"] != null)
            Lon2 = Request.Params["Lon2"];

        if (Request.Params["Name1"] != null)
            Name1 = Request.Params["Name1"];
        if (Request.Params["Name2"] != null)
            Name2 = Request.Params["Name2"];
    }

    #endregion
}