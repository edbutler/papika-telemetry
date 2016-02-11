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
/// A test component that invokes the PapikaClient component directly.
/// This is a good way to test that the PapikaClient component is behaving
/// as intended.
/// First create an empty game object and attach the PapikaClient component.
/// Then attach this component, build, and run.
/// (Assumes you have the Papika server running somewhere when you do.)
/// </summary>
public class NetworkFrontendClientTest : MonoBehaviour
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
    /// Unity Start()
    /// </summary>
	private void Start () {
#if UNITY_EDITOR
        this.serverUri = new Uri(DevServer);
#else
        this.serverUri = new Uri(ProdServer);
#endif

        // Set up some dummy values for testing.
        var releaseId = Guid.NewGuid();
        var releaseKey = Guid.NewGuid().ToString();
        var clientArgs = new ClientArgs(serverUri, releaseId, releaseKey);

        // TESTING QUERY USER ID ----> and some other stuff.
        var userId = Guid.Empty;
        var experimentalCondition = -1;

        var pClient = this.gameObject.GetComponent<PapikaClient>();
        if (pClient == null) {
            throw new NullReferenceException("Please make sure the PapikaClient component is also attached to the game object with this component.");
        }

        pClient.Initialize(clientArgs);

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
            pClient.QueryUserData(userId, onSuccess, onFailure);
        };

        Action<Guid> setUserIdOnSuccess = g => {
            userId = g;
            Debug.Log("Got successful user id: " + g.ToString());

            // TESTING QUERY EXPERIMENTAL CONDITION
            Debug.Log("Testing that QueryExperimentalCondition works...");
            pClient.QueryExperimentalCondition(userId, new Guid("00000000-0000-0000-0000-000000000000"), setExperimentalConditionOnSuccess, onFailure);

            // TESTING SAVE/QUERY USER DATA
            Debug.Log("Testing that SaveUserData works...");
            var saveData = new Dictionary<string, object>();
            saveData.Add("I'm some", new object[] { "save", "data (again)" });
            pClient.SetUserData(userId, MicroJSON.Serialize(saveData), setUserDataOnSuccess, onFailure);

            // TESTING LOG SESSION
            Debug.Log("Testing that LogSession works...");
            var sessionData = new Dictionary<string, object>();
            sessionData.Add("i'm", "some_data");
            sessionData.Add("with", new object[] { 2, "arrays (maybe)" });

            pClient.StartSession(userId, MicroJSON.Serialize(sessionData));

            // TESTING LOG EVENTS (just going to log some root events)
            // XXX (kasiu): Does not test task logging yet.
            Debug.Log("Testing that LogEvent works...");
            var eventDetail = new Dictionary<string, object>();
            eventDetail.Add("WAFFLES", "HAMSTERS");
            pClient.Root.LogEvent(42, 667, MicroJSON.Serialize(eventDetail));
        };

        pClient.QueryUserId("I love cheesecake", setUserIdOnSuccess, onFailure);
	}
}
