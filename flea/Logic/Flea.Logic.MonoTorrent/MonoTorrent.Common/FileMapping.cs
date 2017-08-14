namespace MonoTorrent.Common
{
    public struct FileMapping
    {
        string source;
        string destination;

        public string Source
        {
            get { return source; }
        }

        public string Destination
        {
            get { return destination; }
        }

        public FileMapping(string source, string destination)
        {
            this.source = source;
            this.destination = destination;
        }
    }
}