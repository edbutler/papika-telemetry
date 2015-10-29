using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Papika;

public class NetworkClientTest : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The server used for development. Used when the game is run in the Unity editor.")]
    private string DevServer = "http://localhost:5000";

    [SerializeField]
    [Tooltip("The server used for production builds. Used outside of the editor by default.")]
    private string ProdServer;

    // XXX (kasiu): Eventually log events.
    //private List<RetryWWW> events;

    private int sessionSequenceCounter;
    private int taskIdCounter;

    private Uri server;

    /// <summary>
    /// Unity Awake()
    /// </summary>
    private void Awake () {
#if UNITY_EDITOR
        this.server = new Uri(DevServer);
#else
        this.server = new Uri(ProdServer);
#endif

        this.sessionSequenceCounter = 1;
        this.taskIdCounter = 1;


        // Set up some dummy values for testing.
        var releaseId = Guid.NewGuid();
        var releaseKey = Guid.NewGuid().ToString();
        var networkClient = GetComponent<NetworkClient>();

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
            networkClient.QueryUserData(server, userId, releaseId, releaseKey, onSuccess, onFailure);
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
            networkClient.LogEvents(server, events, g, s, setOnLogEventOnSuccess, onFailure);
        };

        Action<Guid> setUserIdOnSuccess = g => {
            userId = g;
            Debug.Log("Got successful user id: " + g.ToString());

            // TESTING QUERY EXPERIMENTAL CONDITION
            Debug.Log("Testing that QueryExperimentalCondition works...");
            networkClient.QueryExperimentalCondition(server, userId, new Guid("00000000-0000-0000-0000-000000000000"), releaseId, releaseKey, setExperimentalConditionOnSuccess, onFailure);

            // TESTING SAVE/QUERY USER DATA
            Debug.Log("Testing that SaveUserData works...");
            var saveData = new Dictionary<string, object>();
            saveData.Add("I'm some", new object[] {"save", "data"});
            networkClient.SaveUserData(server, userId, saveData, releaseId, releaseKey, setUserDataOnSuccess, onFailure);

            // TESTING LOG SESSION
            Debug.Log("Testing that LogSession works...");
            var sessionData = new Dictionary<string, object>();
            sessionData.Add("i'm", "some_data");
            sessionData.Add("with", new object[] { 2, "arrays" });

            networkClient.LogSession(server, userId, sessionData, "UNHAPPY ID", releaseId, releaseKey, setOnLogSessionOnSuccess, onFailure);
        };

        Debug.Log("Testing that QueryUserId works...");
        networkClient.QueryUserId(server, "dedennehblehs", releaseId, releaseKey, setUserIdOnSuccess, onFailure);
	}

    /// <summary>
    /// Unity Update()
    /// </summary>
	private void Update () {
	    // TODO (kasiu): Exponential backoff resending the events.
	}

#region PUBLIC FUNCTIONS
    public void LogEvent(int eventId, string data) {

    }
#endregion
}