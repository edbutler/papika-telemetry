/**!
 * Papika telemetry client (Unity) library.
 * Copyright 2015 Kristin Siu (kasiu).
 * Revision Id: UNKNOWN_REVISION_ID
 */
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Papika
{
    /// <summary>
    /// The backend protocol implementation for the Papika client library.
    /// Implemented as a static class because it's mostly full of utility
    /// functions used by the corresponding PapikaClient Unity component.
    /// </summary>
    public static class UnityBackend
    {
        // XXX (kasiu): This number is currently arbitrarily set to 2.
        private static int PROTOCOL_VERSION = 2;
        private static Dictionary<string, string> headers = null;

        /// <summary>
        /// Queries the user id.
        /// </summary>
        public static void QueryUserId(MonoBehaviour mb, ClientArgs args, string username, Action<Guid> onSuccess, Action<string> onFailure) {
            var data = new Dictionary<string, object>();
            data.Add("username", username);
            Action<string> callback = s => {
                // HACK (kasiu): Get the Guid out without real JSON parsing.
                var split = s.Split(':');
                if (split.Length != 2) {
                    onFailure(string.Format("QueryUserId received ill-formatted JSON: {0}", s));
                }
                var s2 = split[1];
                var startIndex = s2.IndexOf('"') + 1; // +1 past the first quote
                var endIndex = s2.IndexOf('"', startIndex);
                var guid = s2.Substring(startIndex, endIndex - startIndex);
                onSuccess(new Guid(guid));
            };

            var newArgs = new ClientArgs(new Uri(args.BaseUri, "/api/user"), args);
            SendNonSessionRequest(mb, newArgs, data, callback, onFailure);
        }

        /// <summary>
        /// Queries for an experimental condition (integer), which is assigned on success.
        /// </summary>
        public static void QueryExperimentalCondition(MonoBehaviour mb, ClientArgs args, Guid userId, Guid experimentId, Action<int> onSuccess, Action<string> onFailure) {
            var data = new Dictionary<string, object>();
            data.Add("user_id", userId);
            data.Add("experiment_id", experimentId);
            Action<string> callback = s => {
                // HACK (kasiu): Again, more delightful condition retrieval without the pain of real JSON parsing.
                var split = s.Split(':');
                if (split.Length != 2) {
                    onFailure(string.Format("QueryExperimentalCondition received ill-formatted JSON: {0}", s));
                }

                var conditionStr = split[1].Replace("}", "").Trim();
                onSuccess(int.Parse(conditionStr));
            };

            var newArgs = new ClientArgs(new Uri(args.BaseUri, "/api/experiment"), args);
            SendNonSessionRequest(mb, newArgs, data, callback, onFailure);
        }

        /// <summary>
        /// Queries for any saved user data.
        /// </summary>
        public static void QueryUserData(MonoBehaviour mb, ClientArgs args, Guid userId, Action<string> onSuccess, Action<string> onFailure) {
            var data = new Dictionary<string, object>();
            data.Add("id", userId);

            // TODO (kasiu): Process the user data here?

            var newArgs = new ClientArgs(new Uri(args.BaseUri, "/api/user/get_data"), args.ReleaseId, args.ReleaseKey);
            SendNonSessionRequest(mb, newArgs, data, onSuccess, onFailure);
        }

        /// <summary>
        /// Sets user data.
        /// </summary>
        public static void SetUserData(MonoBehaviour mb, ClientArgs args, Guid userId, string savedata, Action<string> onSuccess, Action<string> onFailure) {
            var data = new Dictionary<string, object>();
            data.Add("id", userId);
            data.Add("savedata", savedata);

            // We don't need to implement a special callback here, since we don't get anything back.
            var newArgs = new ClientArgs(new Uri(args.BaseUri, "/api/user/set_data"), args);
            SendNonSessionRequest(mb, newArgs, data, onSuccess, onFailure);
        }

        /// <summary>
        /// Logs a session and returns session information on success.
        /// </summary>
        public static void LogSession(MonoBehaviour mb, ClientArgs args, Guid userId, string detail, string revisionId, Action<Guid, string> onSuccess, Action<string> onFailure) {
            var data = new Dictionary<string, object>();
            data.Add("user_id", userId);
            data.Add("release_id", args.ReleaseId);
            // XXX (kasiu): Need to check if this is the right DateTime string to send. I THINK THIS IS WRONG BUT I DON'T GIVE A FOOBAR RIGHT NOW. FIXME WHEN THE SERVER SCREAMS.
            data.Add("client_time", DateTime.Now.ToString());
            data.Add("detail", detail);
            data.Add("library_revid", revisionId);

            // Processing to get session_id
            Action<string> callback = s => {
                var s2 = s.Replace("{", "").Replace("}", "").Replace("\"", "").Trim();
                var split = s2.Split(',');
                if (split.Length != 2) {
                    onFailure(string.Format("LogSession received ill-formatted JSON: {0}", s));
                }
                var sessionId = split[0].Split(':')[1].Trim();
                var sessionKey = split[1].Split(':')[1].Trim();

                onSuccess(new Guid(sessionId), sessionKey);
            };

            var newArgs = new ClientArgs(new Uri(args.BaseUri, "/api/session"), args);
            SendNonSessionRequest(mb, newArgs, data, callback, onFailure);
        }

        /// <summary>
        /// Logs an array of events.
        /// </summary>
        public static void LogEvents(MonoBehaviour mb, Uri baseUri, object[] events, Guid sessionId, string sessionKey, Action<string> onSuccess, Action<string> onFailure) {
            SendSessionRequest(mb, new Uri(baseUri, "/api/event"), events, sessionId, sessionKey, onSuccess, onFailure);
        }

        /// <summary>
        /// Sends a non-session request.
        /// </summary>
        private static void SendNonSessionRequest(MonoBehaviour mb, ClientArgs args, Dictionary<string, object> data, Action<string> onSuccess, Action<string> onFailure) {
            var values = new Dictionary<string, object>();
            values.Add("version", PROTOCOL_VERSION);
            values.Add("data", MicroJSON.Serialize(data));
            values.Add("release", args.ReleaseId);
            values.Add("checksum", string.Empty);
            var jsonString = MicroJSON.Serialize(values);

            mb.StartCoroutine(SendPostRequest(args.BaseUri, jsonString, onSuccess, onFailure));
        }

        ///// <summary>
        ///// Sends a session request (tied to a given session id).
        ///// </summary>
        private static void SendSessionRequest(MonoBehaviour mb, Uri url, object[] data, Guid sessionId, string sessionKey, Action<string> onSuccess, Action<string> onFailure) {
            var values = new Dictionary<string, object>();
            values.Add("version", PROTOCOL_VERSION);
            values.Add("data", MicroJSON.Serialize(data));
            values.Add("session", sessionId.ToString());
            values.Add("checksum", string.Empty);

            mb.StartCoroutine(SendPostRequest(url, MicroJSON.Serialize(values), onSuccess, onFailure));
        }

        /// <summary>
        /// Sends a post request with JSON data using Unity's WWW class.
        /// </summary>
        /// Action<string, bool> is response-text and whether or not the response was successful.
        private static IEnumerator SendPostRequest(Uri url, string data, Action<string> onSuccess, Action<string> onFailure) {
            if (headers == null) {
                initializeHeaders();
            }

            var www = new WWW(url.AbsoluteUri, System.Text.Encoding.UTF8.GetBytes(data), headers);
            yield return www;

            // Unity treats errors as strings (as opposed to explicit error codes).
            if (!string.IsNullOrEmpty(www.error)) {
                // Handle errors here.
#if UNITY_EDITOR
                Debug.LogError(string.Format("Post request error: {0}", www.error));
#endif
                onFailure(www.error);
            } else {
#if UNITY_EDITOR
                Debug.Log(string.Format("Post request response: {0}", www.text));
#endif
                onSuccess(www.text);
            }
        }

        /// <summary>
        /// Initializes the headers, just in case this hasn't been done already.
        /// </summary>
        private static void initializeHeaders() {
            headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
        }
    }
}