using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using log4net;
using MonoTorrent.Common;

namespace TorrentMakerUnit
{
    public class TorrentMakerWorker
    {
        #region Static

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Internals

        private readonly CookieContainer _cookieBox;
        private readonly string _password;
        private readonly string _trackerBaseUrl;
        private readonly bool _trackerEnabled;

        private readonly string _userName;
        private readonly string _whereToPutTorrents;

        #endregion

        #region Constructor

        public TorrentMakerWorker(string userName, string password, string trackerBaseUrl, string whereToPutTorrents,
            bool trackerEnabled)
        {
            _userName = userName;
            _password = password;
            _cookieBox = new CookieContainer();
            _trackerBaseUrl = trackerBaseUrl;
            if (!whereToPutTorrents.EndsWith("\\")) whereToPutTorrents += "\\";
            _whereToPutTorrents = whereToPutTorrents;
            _trackerEnabled = trackerEnabled;
        }

        #endregion

        #region Members

        private static string StreamToString(Stream stream)
        {
            //stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }


        private void LoginToTracker()
        {
            Log.Info("Logging into tracker...");
            bool madeIt = false;
            int Try = 0;

            while (!madeIt)
            {
                try
                {
                    byte[] dataToSend = Encoding.UTF8.GetBytes($"uid={_userName}&pwd={_password}");

                    HttpWebRequest myWebRequest =
                        (HttpWebRequest) WebRequest.Create(_trackerBaseUrl + "/index.php?page=login");
                    myWebRequest.AllowAutoRedirect = false;
                    myWebRequest.Method = "POST";
                    myWebRequest.ContentType = "application/x-www-form-urlencoded";
                    myWebRequest.Referer = _trackerBaseUrl;
                    myWebRequest.ContentLength = dataToSend.Length;
                    myWebRequest.Headers.Set("Origin", _trackerBaseUrl);
                    myWebRequest.Headers.Set("Cache-Control", "max-age=0");
                    myWebRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                    myWebRequest.UserAgent =
                        "User-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
                    myWebRequest.CookieContainer = _cookieBox;
                    myWebRequest.GetRequestStream().Write(dataToSend, 0, dataToSend.Length);

                    HttpWebResponse myLoginResponse = (HttpWebResponse) myWebRequest.GetResponse();
                    CookieCollection myCookies = myLoginResponse.Cookies;
                    string response = StreamToString(myLoginResponse.GetResponseStream());
                    madeIt = true;
                }
                catch (WebException e)
                {
                    Try++;
                    Log.ErrorFormat("Failed to connect to tracker: {0}. Retry... #{1}", e.Message, Try);
                    Thread.Sleep(4000);
                }
            }
            Log.Info("Connected to tracker!");
        }

        private Stream Upload(string url, string fileName, Stream torrentFileStream)
        {
            using (HttpClientHandler handler = new HttpClientHandler() {CookieContainer = _cookieBox})
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    using (MultipartFormDataContent formData = new MultipartFormDataContent())
                    {
                        client.Timeout = new TimeSpan(0, 0, 2, 0);

                        HttpContent userIdBit = new StringContent("");
                        HttpContent torrentFileBit = new StreamContent(torrentFileStream);
                        HttpContent categoryBit = new StringContent("16");
                        HttpContent fileNameBit = new StringContent(fileName.Replace(".torrent", ""));
                        HttpContent fontChangeBit = new StringContent("");
                        HttpContent infoBit = new StringContent(fileName);
                        HttpContent anonBit = new StringContent("false");

                        formData.Add(userIdBit, "user_id");
                        formData.Add(torrentFileBit, "torrent", fileName);
                        formData.Add(categoryBit, "category");
                        formData.Add(fileNameBit, "filename");
                        formData.Add(fontChangeBit, "fontchange");
                        formData.Add(fontChangeBit, "fontchange");
                        formData.Add(infoBit, "info");
                        formData.Add(anonBit, "anonymous");

                        HttpResponseMessage response = client.PostAsync(url, formData).Result;
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception("Error response:" + response.StatusCode);
                        }
                        return response.Content.ReadAsStreamAsync().Result;
                    }
                }
            }
        }

        private void UploadTorrentToTracker(FileSystemInfo thisFile)
        {
            bool madeIt = false;

            while (!madeIt)
            {
                try
                {
                    Log.InfoFormat("Uploading to tracker... {0}", thisFile.Name);
                    string destinationUrl = _trackerBaseUrl + "/index.php?page=upload";
                    FileStream torrentFileStream = new FileStream(thisFile.FullName, FileMode.Open);
                    Stream dataResponse = Upload(destinationUrl, thisFile.Name, torrentFileStream);
                    string responseString = StreamToString(dataResponse);

                    //PID system active get your torrent with your PID
                    //<br /><a href="download.php?id=8ef24b933afbb2955fbd6646b2da4bb4091aede9&f=Born+to+Raise+Hell+%282010%29.avi.torrent.torrent">Download</a><
                    Regex myRegex = new Regex("download\\.php\\?id=.*\\\"");
                    Match myMatch = myRegex.Match(responseString);
                    string downloadUrl = myMatch.Value.Replace("\"", "");
                    if (downloadUrl != "")
                    {
                        Log.InfoFormat("Upload Success! Downloading it again: {0}", downloadUrl);
                        bool isdone = false;
                        while (!isdone)
                        {
                            try
                            {
                                using (CookieAwareWebClient client = new CookieAwareWebClient())
                                {
                                    client.SetCookieContainer(_cookieBox);
                                    client.DownloadFile(_trackerBaseUrl + "/" + downloadUrl, thisFile.FullName);
                                    isdone = true;
                                }
                            }
                            catch (Exception ee4)
                            {
                                Log.ErrorFormat("Failed downloading .torrent. Retry.. {0}", ee4.Message);
                            }
                        }
                    }

                    madeIt = true;
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("Failed to upload to tracker: {0}. Retry in 10 seconds...", e.Message);
                    Thread.Sleep(10000);
                }
            }
            Log.Info("Uploaded to tracker!");
        }

        public void ScanAndUploadFolder(string path, IEnumerable<string> fileFilter, NetworkCredential accessCredential,
            string pathToUTorrent)
        {
            if (_trackerEnabled) LoginToTracker();

            NetworkConnection myNetworkConnection = null;
            if (accessCredential != null)
            {
                myNetworkConnection = new NetworkConnection(path, accessCredential);
            }

            TorrentCreator myTorrentCreator = SetUpTorrentCreator();
            DirectoryInfo root = new DirectoryInfo(path);
            List<string> fileFilters = fileFilter.ToList();

            FileSystemEnumerable fileList = new FileSystemEnumerable(root, fileFilters, SearchOption.AllDirectories);
            foreach (FileSystemInfo thisDataFile in fileList)
            {
                Console.Clear();
                Log.InfoFormat("Processing file:{0}", thisDataFile.FullName);

                if (!new FileInfo(_whereToPutTorrents + $"{thisDataFile.Name}.torrent").Exists)
                {
                    // Make the torrent
                    TorrentFileSource myTorrentfileSource = new TorrentFileSource(thisDataFile.FullName);
                    myTorrentCreator.Create(myTorrentfileSource,
                        _whereToPutTorrents + $"{thisDataFile.Name}.torrent");

                    Log.InfoFormat("Processing file:{0} Torrent Created..", thisDataFile.Name);
                }


                // Upload to the tracker
                if (_trackerEnabled)
                    UploadTorrentToTracker(
                        new FileInfo(_whereToPutTorrents + $"{thisDataFile.Name}.torrent"));
                lock (myTorrentCreator)
                {
                    Console.SetCursorPosition(1, 3);
                    Log.InfoFormat("Done..");
                }

                // Add torrent to utorrent
                string theFileName = _whereToPutTorrents + $"{thisDataFile.Name}.torrent";
                string thePath = Path.GetDirectoryName(thisDataFile.FullName);
                ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(pathToUTorrent,
                    $"/DIRECTORY \"{thePath}\" \"{theFileName}\"")
                {
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Log.InfoFormat("Starting uTorrent...");
                Process uTorrentProcess = Process.Start(myProcessStartInfo);
                int sleeps = 0;
                while (uTorrentProcess != null && !uTorrentProcess.HasExited)
                {
                    sleeps++;
                    Thread.Sleep(1000);
                    try
                    {
                        if (!uTorrentProcess.HasExited && sleeps > 10) uTorrentProcess.Kill();
                        Log.InfoFormat(".");
                    }
                    catch (Exception)
                    {
                    }
                }
                Log.InfoFormat("Fed it to uTorrent...");
            }

            myNetworkConnection?.Dispose();
        }


        private TorrentCreator SetUpTorrentCreator()
        {
            TorrentCreator myTorrentCreator = new TorrentCreator();
            myTorrentCreator.Hashed += delegate(object o, TorrentCreatorEventArgs e)
            {
                lock (myTorrentCreator)
                {
                    Console.SetCursorPosition(1, 3);
                    Log.InfoFormat("Current File is {0:0.000}% hashed     ", e.FileCompletion);
                    //Logger.WriteLine("Overall {0:0.000}% hashed     ", e.OverallCompletion);
                    Log.InfoFormat("Total data to hash: {0:##,###} kb  {1:##,###} Mb  ", e.OverallSize/1024,
                        e.OverallSize/(1024*1014));
                }
            };
            myTorrentCreator.PieceLength = 1048576;
            myTorrentCreator.Announce = _trackerBaseUrl + "/announce.php";
            myTorrentCreator.CreatedBy = "TorrentGadget";
            myTorrentCreator.Comment = "Torrent Created by Wolfen's Fancy Gadget";
            myTorrentCreator.Private = true;
            return myTorrentCreator;
        }

        #endregion
    }
}