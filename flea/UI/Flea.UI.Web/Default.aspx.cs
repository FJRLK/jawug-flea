using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Flea.Logic.CommandUnit;
using Flea.Logic.IrcConnectionUnit;

namespace FleaWeb
{
    public partial class Default : Page
    {
        #region Members

        protected void Page_Load(object sender, EventArgs e)
        {
            //Response.Redirect("fleaup.aspx");
            FleaCommands myCommands = new FleaCommands(false, false);
            string[] Params = (txtParms.Text + "          ").Split(' ');
            switch (ddlCommandList.SelectedValue)
            {
                case "8BALL":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.Display8BallMessage());
                }
                    break;
                case "ANA":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.CreateLosAnalysisLink(Params[0], Params[1]));
                }
                    break;
                case "CHECKPORT":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.CheckTcpPort(Params[0], Params[1]));
                }
                    break;
                case "DNS":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.ResolveDns(Params[0]));
                }
                    break;
                case "DNSFIND":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.SearchForDnsEntry(Params[0]));
                }
                    break;
                case "EXCUSE":
                {
                    lblResults.Text = myCommands.MakeExcuse();
                }
                    break;
                case "GOOGLE":
                {
                    lblResults.Text = myCommands.MakeGoogleLuckyUrl(txtParms.Text);
                }
                    break;
                case "GSG":
                {
                    lblResults.Text = myCommands.DisplayGsgLink();
                }
                    break;
                case "IPCALC":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.CalcIpRange(Params[0]));
                }
                    break;
                case "KML":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.DisplayKmlLink());
                }
                    break;
                case "LOC":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.SearchForLocation(txtParms.Text));
                }
                    break;
                case "LOS":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.MakeLosLink(Params[0], Params[1]));
                }
                    break;
                case "LOS2":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.CreateLos2LinkUrl(Params[0], Params[1]));
                }
                    break;
                case "MAP":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.CreateMapUrl(Params[0], Params[1]));
                }
                    break;
                case "MAPTRACE":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.DoMapTrace(txtParms.Text));
                }
                    break;
                case "MEMBERS":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.DisplayMemberList());
                }
                    break;
                case "MYSQLTEST":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.TestMySqlLinks());
                }
                    break;
                case "NATRULE":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.DisplatNatRuleSyntax());
                }
                    break;
                case "NEAR":
                {
                    lblResults.Text =
                        ConvertIrcMessageToHtml(myCommands.DoNearAnalysis(Params[0], Params[1],
                            Convert.ToInt32(Params[2])));
                }
                    break;
                case "PING":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.Ping(Params[0], Convert.ToInt16(Params[1])));
                }
                    break;
                case "RADIUS":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.CheckRadiusServers());
                }
                    break;
                case "RBCP":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.GenerateRbcpUrl(Params[0]));
                }
                    break;
                case "REFRESHDB":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.RefreshDb());
                }
                    break;
                case "REGISTER":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.MakeDnsEntry(Params[0], Params[1]));
                }
                    break;
                case "RTRACE":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.ExecuteRemoteTrace(Params[0], Params[1]));
                }
                    break;
                case "SERVICES":
                {
                    lblResults.Text = myCommands.DisplayServicesLink();
                }
                    break;
                case "TESTKIT":
                {
                    lblResults.Text = myCommands.DisplayTestKitLink();
                }
                    break;
                case "TIME":
                {
                    lblResults.Text = myCommands.DisplayTime();
                }
                    break;
                case "TRACE":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.TraceRoute(Params[0]));
                }
                    break;
                case "UNREGISTER":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.RemoveDnsEntry(Params[0], Params[1]));
                }
                    break;
                case "WEATHER":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.PostWeatherLinks());
                }
                    break;
                case "WGET":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.TestWebLink(Params[0]));
                }
                    break;
                case "WORDS":
                {
                    lblResults.Text = ConvertIrcMessageToHtml(myCommands.DisplayWordList());
                }
                    break;
                default:
                    lblResults.Text = "Not implemented!";
                    break;
            }
        }

        /// <summary>
        ///     Converts the irc message to HTML.
        /// </summary>
        /// <param name="messageList">The message list.</param>
        /// <returns></returns>
        private static string ConvertIrcMessageToHtml(IEnumerable<IrcMessage> messageList)
        {
            return messageList.Aggregate("", (current, s) => current + (s + "<br/>"));
        }

        #endregion
    }
}