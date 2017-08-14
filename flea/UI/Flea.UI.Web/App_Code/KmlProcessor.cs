using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using ICSharpCode.SharpZipLib.Zip;
using log4net;

/// <summary>
///     Summary description for KmlProcessor
/// </summary>
public class KmlProcessor
{
    #region Static

    private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    #endregion

    #region Internals

    private readonly string _pathPrefix;

    public readonly Dictionary<string, string> coOrds = new Dictionary<string, string>();

    #endregion

    #region Constructor

    public KmlProcessor(string pathPrefix)
    {
        _pathPrefix = pathPrefix;
        RefreshDb();
    }

    #endregion

    #region Members

    private void RefreshDb()
    {
        WebClient myWebClient = new WebClient();
        try
        {
            myWebClient.DownloadFile("http://www.wug.za.net/newkmz.php?wug=1&download=Download+KMZ",
                $"{_pathPrefix}kmz_1.zip.tmp");
            myWebClient.DownloadFile("http://www.wug.za.net/newkmz.php?wug=15&download=Download+KMZ",
                $"{_pathPrefix}kmz_2.zip.tmp");
            try
            {
                File.Delete($"{_pathPrefix}kmz_1.zip");
                File.Delete($"{_pathPrefix}kmz_2.zip");
            }
            catch
            {
            }
            File.Move($"{_pathPrefix}kmz_1.zip.tmp", $"{_pathPrefix}kmz_1.zip");
            File.Move($"{_pathPrefix}kmz_2.zip.tmp", $"{_pathPrefix}kmz_2.zip");
        }
        catch (Exception ee)
        {
            Log.ErrorFormat(
                "Failed to download KML: {0}. System will attempt to use previously downloaded files where possible",
                ee.Message);
        }

        ExtractDocKml(_pathPrefix + "kmz_1.zip", _pathPrefix + "doc1.kml");
        ExtractDocKml(_pathPrefix + "kmz_2.zip", _pathPrefix + "doc2.kml");

        ConvertKmltoXml(_pathPrefix + "doc1.kml", _pathPrefix + "doc1.xml");
        ConvertKmltoXml(_pathPrefix + "doc2.kml", _pathPrefix + "doc2.xml");

        RunXslTransform(_pathPrefix + "doc1.xml", _pathPrefix + "CoOrd.xslt", _pathPrefix + "result1.dat");
        RunXslTransform(_pathPrefix + "doc2.xml", _pathPrefix + "CoOrd.xslt", _pathPrefix + "result2.dat");

        coOrds.Clear();

        LoadCoordsFromFile(_pathPrefix + "result1.dat");
        LoadCoordsFromFile(_pathPrefix + "result2.dat");
    }

    private void LoadCoordsFromFile(string inputFile)
    {
        using (StreamReader sr = new StreamReader(inputFile))
        {
            string line;
            int thisLine = 0;
            while ((line = sr.ReadLine()) != null)
            {
                thisLine++;
                string[] thisNode = line.Split(new string[] {"/*888*/"}, StringSplitOptions.RemoveEmptyEntries);
                if (thisNode.Length == 3)
                {
                    string[] locsplit = thisNode[2].Split(new char[] {'&', ':'});
                    if (locsplit.Length > 2)
                    {
                        string[] coordssplit = thisNode[1].Replace(",0", "").Split(',');
                        string theName = thisNode[0].Trim();
                        if (!coOrds.ContainsKey(theName.ToLowerInvariant()))
                        {
                            string secondVal = coordssplit[0];
                            while (secondVal.StartsWith("0")) secondVal = secondVal.Substring(1);
                            coOrds.Add(theName.ToLowerInvariant(), coordssplit[1] + "," + secondVal);
                        }
                    }
                }
            }
        }
    }

    private static void RunXslTransform(string inputFile, string transformFile, string outputFile)
    {
        XPathDocument myXPathDoc = new XPathDocument(inputFile);
        XslCompiledTransform myXslTrans = new XslCompiledTransform();
        myXslTrans.Load(transformFile);
        XmlTextWriter myWriter = new XmlTextWriter(outputFile, null);
        myXslTrans.Transform(myXPathDoc, null, myWriter);
        myWriter.Close();
    }

    private static void ConvertKmltoXml(string inputFile, string outputFile)
    {
        using (StreamWriter sw = new StreamWriter(outputFile))
        {
            using (StreamReader sr = new StreamReader(inputFile))
            {
                string line;
                int thisLine = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    thisLine++;
                    if (thisLine == 2) sw.WriteLine("<kml>");
                    else sw.WriteLine(line.Replace("& ", "&amp; "));
                }
            }
        }
    }

    private static void ExtractDocKml(string inputFile, string outputFile)
    {
        FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.ReadWrite);

        fs.Seek(5, SeekOrigin.Begin);
        fs.WriteByte(0);
        fs.WriteByte(1);
        fs.Seek(0, SeekOrigin.Begin);

        ZipFile zf = new ZipFile(fs);
        ZipEntry ze = zf.GetEntry("doc.kml");
        ze.ForceZip64();
        Stream s = zf.GetInputStream(ze);
        //s.Length
        FileStream newFile = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
        byte[] buffer = new byte[10000];
        int size = 1;
        while (size > 0)
        {
            size = s.Read(buffer, 0, buffer.Length);
            newFile.Write(buffer, 0, size);
        }
        fs.Close();
        zf.Close();
        s.Close();
        newFile.Close();
    }

    #endregion
}