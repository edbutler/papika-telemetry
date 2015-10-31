using System;

namespace Papika
{
    /// <summary>
    /// A container for common client arguments.
    /// </summary>
    public class ClientArgs
    {
        public Uri BaseUri { get; private set; }
        public Guid ReleaseId { get; private set; }
        public string ReleaseKey { get; private set; }

        public ClientArgs(Uri baseUri, Guid releaseId, string releaseKey) {
            BaseUri = baseUri;
            ReleaseId = releaseId;
            ReleaseKey = releaseKey;
        }

        // Eric thinks this is dumb. People might be tempted to use this internal hacky-sack. MMM HACKY-SACK. I was bad at this game.
        public ClientArgs(Uri newUri, ClientArgs oldArgs) {
            BaseUri = newUri;
            ReleaseId = oldArgs.ReleaseId;
            ReleaseKey = oldArgs.ReleaseKey;
        }
    }
}
