using System;
using System.Configuration;
using System.Net;
using TorrentMakerUnit;

namespace TorrentMakerTestHarness
{
    class Program
    {
        #region Members

        private static void Main(string[] args)
        {
            string username = ConfigurationManager.AppSettings["TrackerUsername"];
            string password = ConfigurationManager.AppSettings["TrackerPassword"];
            string trackerUrl = ConfigurationManager.AppSettings["TrackerUrl"];
            string folderToScan = ConfigurationManager.AppSettings["FolderToScan"];
            string fileTypes = ConfigurationManager.AppSettings["FileTypesToUpload"];
            string folderUsername = ConfigurationManager.AppSettings["FolderUserName"];
            string folderPassword = ConfigurationManager.AppSettings["FolderPassword"];
            string pathToUTorrent = ConfigurationManager.AppSettings["PathToUTorrent"];
            string whereToPutTorrents = ConfigurationManager.AppSettings["WhereToPutTorrents"];
            bool trackerEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["UploadToTracker"]);

            TorrentMakerWorker myTorrentMakerUnit = new TorrentMakerWorker(username, password, trackerUrl,
                whereToPutTorrents,
                trackerEnabled);
            if (folderUsername != "")
                myTorrentMakerUnit.ScanAndUploadFolder(folderToScan, fileTypes.Split(';'),
                    new NetworkCredential(folderUsername, folderPassword), pathToUTorrent);
            else
                myTorrentMakerUnit.ScanAndUploadFolder(folderToScan, fileTypes.Split(';'), null, pathToUTorrent);
        }

        #endregion
    }
}