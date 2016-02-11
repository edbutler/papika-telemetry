/**!
 * Papika telemetry client (Unity) library.
 * Copyright 2015 Kristin Siu (kasiu).
 * Revision Id: UNKNOWN_REVISION_ID
 */
using UnityEngine;
using System;
using System.Collections.Generic;
using Papika;

/// <summary>
/// A test component that invokes the backend protocol implementation directly.
/// This is a good way to test if your build works in Unity (i.e. Unity's WWW
/// classes are behaving for your platform).
/// Simply attach this to an empty game object in a scene, build, and run.
/// (Assumes you have the Papika server running somewhere when you do.)
/// </summary>
public class NetworkBackendClientTest : MonoBehaviour
{
    // Change these values to wherever your test server is currently living.
    [SerializeField]
    [Tooltip("The server used for development. Used when the game is run in the Unity editor.")]
    private string DevServer = "http://localhost:5000";

    [SerializeField]
    [Tooltip("The server used for production builds. Used outside of the editor by default.")]
    private string ProdServer = "http://localhost:5000";

    private Uri serverUri;

    /// <summary>
    /// Unity Awake()
    /// </summary>
    private void Awake () {
#if UNITY_EDITOR
        this.serverUri = new Uri(DevServer);
#else
        // Otherwise, try and call this method from a UI button or something by attaching the function below.
        this.serverUri = new Uri(ProdServer);
#endif

        TestTelemetryServer();
	}

    /// <summary>
    /// Tests the telemetry server by writing some hilarious dummy values.
    /// Left public to invoke externally if you desire.
    /// </summary>
    public void TestTelemetryServer() {
        Debug.Log("Testing the telemetry server");
        // Set up some dummy values for testing.
        var releaseId = Guid.NewGuid();
        var releaseKey = Guid.NewGuid().ToString();
        var clientArgs = new ClientArgs(serverUri, releaseId, releaseKey);

        // TESTING QUERY USER ID ----> and some other stuff.
        var userId = Guid.Empty;
        var experimentalCondition = -1;

        Action<string> onSuccess = s => {
            Debug.Log("OnSuccess: " + s);
        };

        Action<string> onFailure = s => {
            Debug.LogError("OnFailure: " + s);
        };

        Action<int> setExperimentalConditionOnSuccess = i => {
            experimentalCondition = i;
            Debug.Log("Got successful user experiment condition: " + i);
        };

        Action<string> setUserDataOnSuccess = s => {
            // Probably could do something with data? Nothing gets returned, so we don't care.

            // TESTING QUERY DATA
            Debug.Log("Testing that QueryUserData works...");
            UnityBackend.QueryUserData(this, clientArgs, userId, onSuccess, onFailure);
        };

        Action<string> setOnLogEventOnSuccess = s => {
            Debug.Log("Succesfully logged event: " + s);
        };

        Action<Guid, string> setOnLogSessionOnSuccess = (g, s) => {
            Debug.Log("Got session id: " + g.ToString());
            Debug.Log("Got session key: " + s);

            // TESTING LOG EVENTS (just going to log some root events because fuck tasks for now)
            var eventDetail = new Dictionary<string, object>();
            eventDetail.Add("PANCAKES", "HAMSTERS");

            var eventDict = new Dictionary<string, object>();
            eventDict.Add("category_id", 42);
            eventDict.Add("type_id", 666);
            eventDict.Add("session_sequence_index", 1);
            eventDict.Add("client_time", DateTime.Now.ToString());
            eventDict.Add("detail", MicroJSON.Serialize(eventDetail));

            var events = new object[] { eventDict };
            UnityBackend.LogEvents(this, serverUri, events, g, s, setOnLogEventOnSuccess, onFailure);
        };

        Action<Guid> setUserIdOnSuccess = g => {
            userId = g;
            Debug.Log("Got successful user id: " + g.ToString());

            // TESTING QUERY EXPERIMENTAL CONDITION
            Debug.Log("Testing that QueryExperimentalCondition works...");
            UnityBackend.QueryExperimentalCondition(this, clientArgs, userId, new Guid("00000000-0000-0000-0000-000000000000"), setExperimentalConditionOnSuccess, onFailure);

            // TESTING SAVE/QUERY USER DATA
            Debug.Log("Testing that SaveUserData works...");
            var saveData = new Dictionary<string, object>();
            saveData.Add("I'm some", new object[] { "save", "data" });
            UnityBackend.SetUserData(this, clientArgs, userId, MicroJSON.Serialize(saveData), setUserDataOnSuccess, onFailure);

            // TESTING LOG SESSION
            Debug.Log("Testing that LogSession works...");
            var sessionData = new Dictionary<string, object>();
            sessionData.Add("i'm", "some_data");
            sessionData.Add("with", new object[] { 2, "arrays" });

            UnityBackend.LogSession(this, clientArgs, userId, MicroJSON.Serialize(sessionData), "UNHAPPY ID", setOnLogSessionOnSuccess, onFailure);
        };

        Debug.Log("Testing that QueryUserId works...");
        UnityBackend.QueryUserId(this, clientArgs, "dedennehblehs", setUserIdOnSuccess, onFailure);
    }
}