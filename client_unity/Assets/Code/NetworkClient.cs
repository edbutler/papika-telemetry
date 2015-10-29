using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Papika {
    /// <summary>
    /// The Unity client for the Papika library.
    /// </summary>
    public class NetworkClient : MonoBehaviour
    {
        private static int PROTOCOL_VERSION = 2;
        private static Dictionary<string, string> headers = null;
        private static string REVISION_ID = "UNKNOWN_REVISION_ID";

        // Public client functions
        public void QueryUserId(Uri baseUri, string username, Guid releaseId, string releaseKey, Action<Guid> onSuccess, Action<string> onFailure) {
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

            SendNonSessionRequest(new Uri(baseUri, "/api/user"), data, releaseId, releaseKey, callback, onFailure);
        }

        public void QueryExperimentalCondition(Uri baseUri, Guid userId, Guid experimentId, Guid releaseId, string releaseKey, Action<int> onSuccess, Action<string> onFailure) {
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

            SendNonSessionRequest(new Uri(baseUri, "/api/experiment"), data, releaseId, releaseKey, callback, onFailure);
        }

        public void QueryUserData(Uri baseUri, Guid userId, Guid releaseId, string releaseKey, Action<string> onSuccess, Action<string> onFailure) {
            var data = new Dictionary<string, object>();
            data.Add("id", userId);

            // TODO (kasiu): Process the user data here. Currently unimplemented.

            SendNonSessionRequest(new Uri(baseUri, "/api/user/get_data"), data, releaseId, releaseKey, onSuccess, onFailure);
        }

        public void SaveUserData(Uri baseUri, Guid userId, object savedata, Guid releaseId, string releaseKey, Action<string> onSuccess, Action<string> onFailure) {
            var data = new Dictionary<string, object>();
            data.Add("id", userId);
            data.Add("savedata", MicroJSON.Serialize(savedata));

            // We don't need to implement a special callback here, since we don't get anything back.
            SendNonSessionRequest(new Uri(baseUri, "/api/user/set_data"), data, releaseId, releaseKey, onSuccess, onFailure);
        }

        public void LogSession(Uri baseUri, Guid userId, Dictionary<string, object> detail, string revisionId, Guid releaseId, string releaseKey, Action<Guid, string> onSuccess, Action<string> onFailure) {
            var data = new Dictionary<string, object>();
            data.Add("user_id", userId);
            data.Add("release_id", releaseId);
            // XXX (kasiu): Need to check if this is the right DateTime string to send. I THINK THIS IS WRONG BUT I DON'T GIVE A FOOBAR RIGHT NOW. FIXME WHEN THE SERVER SCREAMS.
            data.Add("client_time", DateTime.Now.ToString());
            data.Add("detail", MicroJSON.Serialize(detail));
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

            SendNonSessionRequest(new Uri(baseUri, "/api/session"), data, releaseId, releaseKey, callback, onFailure);
        }

        public void LogEvents(Uri baseUri, object[] events, Guid sessionId, string sessionKey, Action<string> onSuccess, Action<string> onFailure) {
            SendSessionRequest(new Uri(baseUri, "/api/event"), events, sessionId, sessionKey, onSuccess, onFailure);
        }

        /// <summary>
        /// Sends a non-session request.
        /// </summary>
        private void SendNonSessionRequest(Uri url, Dictionary<string, object> data, Guid releaseId, string releaseKey, Action<string> onSuccess, Action<string> onFailure) {
            var values = new Dictionary<string, object>();
            values.Add("version", PROTOCOL_VERSION);
            values.Add("data", MicroJSON.Serialize(data));
            values.Add("release", releaseId);
            values.Add("checksum", string.Empty);
            var jsonString = MicroJSON.Serialize(values);

            StartCoroutine(SendPostRequest(url, jsonString, onSuccess, onFailure));
        }

        ///// <summary>
        ///// Sends a session request (tied to a given session id).
        ///// </summary>
        private void SendSessionRequest(Uri url, object[] data, Guid sessionId, string sessionKey, Action<string> onSuccess, Action<string> onFailure) {
            var values = new Dictionary<string, object>();
            values.Add("version", PROTOCOL_VERSION);
            values.Add("data", MicroJSON.Serialize(data));
            values.Add("session", sessionId.ToString());
            values.Add("checksum", string.Empty);

            StartCoroutine(SendPostRequest(url, MicroJSON.Serialize(values), onSuccess, onFailure));
        }

        /// <summary>
        /// Sends a post request with JSON data using Unity's WWW class.
        /// </summary>
        /// Action<string, bool> is response-text and whether or not the response was successful.
        private IEnumerator SendPostRequest(Uri url, string data, Action<string> onSuccess, Action<string> onFailure) {
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