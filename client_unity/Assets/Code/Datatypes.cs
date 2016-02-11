/**!
 * Papika telemetry client (Unity) library.
 * Copyright 2015 Kristin Siu (kasiu).
 * Revision Id: UNKNOWN_REVISION_ID
 */
using System;

namespace Papika
{
    /// <summary>
    /// A container for common client arguments.
    /// </summary>
    public class ClientArgs
    {
        /// <summary>
        /// The uri to the server.
        /// </summary>
        public Uri BaseUri { get; private set; }

        /// <summary>
        /// The release id.
        /// </summary>
        public Guid ReleaseId { get; private set; }

        /// <summary>
        /// The release key (used for encryption, etc.)
        /// </summary>
        public string ReleaseKey { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ClientArgs(Uri baseUri, Guid releaseId, string releaseKey) {
            BaseUri = baseUri;
            ReleaseId = releaseId;
            ReleaseKey = releaseKey;
        }

        /// <summary>
        /// Constructor.
        /// This one is used internally to copy over non-Uri args and really
        /// shouldn't be used outside of backend code.
        /// </summary>
        public ClientArgs(Uri newUri, ClientArgs oldArgs) {
            BaseUri = newUri;
            ReleaseId = oldArgs.ReleaseId;
            ReleaseKey = oldArgs.ReleaseKey;
        }
    }
}
