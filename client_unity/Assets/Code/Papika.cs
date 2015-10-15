using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public static class PapikaUnityClient
{
    public static int PROTOCOL_VERSION = 2;
    private static Dictionary<string, string> headers = null;

    // Public client functions
    public static IEnumerator QueryUserId(Uri baseUri, string username, Guid releaseId, string releaseKey) {
        var data = new Dictionary<string, object>();
        data.Add("username", username);
        return SendNonSessionRequest(new Uri(baseUri, "/api/user"), data, releaseId, releaseKey);
    }

    public static IEnumerator QueryExperimentalCondition(Uri baseUri, Guid userId, Guid experimentId, Guid releaseId, string releaseKey) {
        var data = new Dictionary<string, object>();
        data.Add("user_id", userId);
        data.Add("experiment_id", experimentId);
        return SendNonSessionRequest(new Uri(baseUri, "/api/experiment"), data, releaseId, releaseKey);
    }

    public static IEnumerator QueryUserData(Uri baseUri, Guid userId, Guid releaseId, string releaseKey) {
        var data = new Dictionary<string, object>();
        data.Add("id", userId);
        return SendNonSessionRequest(new Uri(baseUri, "/api/user/get_data"), data, releaseId, releaseKey);
    }

    public static IEnumerator SaveUserData(Uri baseUri, Guid userId, object savedata, Guid releaseId, string releaseKey) {
        var data = new Dictionary<string, object>();
        data.Add("id", userId);
        data.Add("savedata", savedata);
        return SendNonSessionRequest(new Uri(baseUri, "/api/user/set_data"), data, releaseId, releaseKey);
    }

    public static IEnumerator LogSession(Uri baseUri, Guid userId, Dictionary<string, object> detail, Guid revisionId, Guid releaseId, string releaseKey) {
        var data = new Dictionary<string, object>();
        data.Add("user_id", userId);
        data.Add("release_id", releaseId);
        // XXX (kasiu): Need to check if this is the right DateTime string to send. I THINK THIS IS WRONG BUT I DON'T GIVE A FOOBAR RIGHT NOW. FIXME WHEN THE SERVER SCREAMS.
        data.Add("client_time", DateTime.Now.ToString());
        data.Add("detail", MicroJSON.Serialize(detail));
        data.Add("library_revid", revisionId);
        return SendNonSessionRequest(new Uri(baseUri, "/api/session"), data, releaseId, releaseKey);
    }

    // XXX (kasiu): Please fix the second parameter to be a list of events.
    public static IEnumerator LogEvents(Uri baseUri, object[] events, Guid sessionId, string sessionKey) {
        return SendSessionRequest(new Uri(baseUri, "/api/event"), events, sessionId, sessionKey);
    }

    /// <summary>
    /// Sends a non-session request.
    /// </summary>
    private static IEnumerator SendNonSessionRequest(Uri url, Dictionary<string, object> data, Guid releaseId, string releaseKey) {
        var values = new Dictionary<string, object>();
        values.Add("version", PROTOCOL_VERSION);
        values.Add("data", MicroJSON.Serialize(data));
        values.Add("release", releaseId);
        values.Add("checksum", string.Empty);
        var jsonString = MicroJSON.Serialize(values);

        Debug.Log(jsonString);
        Debug.Log(string.Format("Sending nonsession request to {0}", url.ToString()));
        return SendPostRequest(url, jsonString);
    }

    /// <summary>
    /// Sends a session request (tied to a given session id).
    /// </summary>
    private static IEnumerator SendSessionRequest(Uri url, object[] data, Guid sessionId, string sessionKey) {
        var values = new Dictionary<string, object>();
        values.Add("version", PROTOCOL_VERSION);
        values.Add("data", MicroJSON.Serialize(data));
        values.Add("session", sessionId.ToString());
        values.Add("checksum", string.Empty);
        return SendPostRequest(url, MicroJSON.Serialize(values));
    }

    /// <summary>
    /// Sends a post request with JSON data using Unity's WWW class.
    /// </summary>
    private static IEnumerator SendPostRequest(Uri url, string data) {
        Debug.Log("Sending a post request without headers initialized?");
        if (headers == null) {
            initializeHeaders();
        }

        Debug.Log("Sending a post request to " + url.AbsoluteUri);
        var www = new WWW(url.AbsoluteUri, System.Text.Encoding.UTF8.GetBytes(data), headers);
        yield return www;

        // Unity treats errors as strings (as opposed to explicit error codes).
        if (!string.IsNullOrEmpty(www.error)) {
            // Handle errors here.
            Debug.Log(www.error);
        } else {
            Debug.Log(www.text);
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