using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Papika;

/// <summary>
/// A test component that invokes the backend protocol implementation directly to test functionality.
/// </summary>
public class NetworkBackendClientTest : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The server used for development. Used when the game is run in the Unity editor.")]
    private string DevServer = "http://localhost:5000";

    [SerializeField]
    [Tooltip("The server used for production builds. Used outside of the editor by default.")]
    private string ProdServer;

    private Uri serverUri;

    /// <summary>
    /// Unity Awake()
    /// </summary>
    private void Awake () {
#if UNITY_EDITOR
        this.serverUri = new Uri(DevServer);
#else
        this.serverUri = new Uri(ProdServer);
#endif

        // Set up some dummy values for testing.
        var releaseId = Guid.NewGuid();
        var releaseKey = Guid.NewGuid().ToString();
        var clientArgs = new ClientArgs(serverUri, releaseId, releaseKey);

        Debug.Log("Testing the telemetry server");

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
            NetworkBackend.QueryUserData(this, clientArgs, userId, onSuccess, onFailure);
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

            var events = new object[]{eventDict};
            NetworkBackend.LogEvents(this, serverUri, events, g, s, setOnLogEventOnSuccess, onFailure);
        };

        Action<Guid> setUserIdOnSuccess = g => {
            userId = g;
            Debug.Log("Got successful user id: " + g.ToString());

            // TESTING QUERY EXPERIMENTAL CONDITION
            Debug.Log("Testing that QueryExperimentalCondition works...");
            NetworkBackend.QueryExperimentalCondition(this, clientArgs, userId, new Guid("00000000-0000-0000-0000-000000000000"), setExperimentalConditionOnSuccess, onFailure);

            // TESTING SAVE/QUERY USER DATA
            Debug.Log("Testing that SaveUserData works...");
            var saveData = new Dictionary<string, object>();
            saveData.Add("I'm some", new object[] {"save", "data"});
            NetworkBackend.SetUserData(this, clientArgs, userId, MicroJSON.Serialize(saveData), setUserDataOnSuccess, onFailure);

            // TESTING LOG SESSION
            Debug.Log("Testing that LogSession works...");
            var sessionData = new Dictionary<string, object>();
            sessionData.Add("i'm", "some_data");
            sessionData.Add("with", new object[] { 2, "arrays" });

            NetworkBackend.LogSession(this, clientArgs, userId, MicroJSON.Serialize(sessionData), "UNHAPPY ID", setOnLogSessionOnSuccess, onFailure);
        };

        Debug.Log("Testing that QueryUserId works...");
        NetworkBackend.QueryUserId(this, clientArgs, "dedennehblehs", setUserIdOnSuccess, onFailure);
	}
}