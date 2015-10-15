using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PapikaTelemetryClient : MonoBehaviour
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

        Debug.Log("testing the telemetry server");
        StartCoroutine(PapikaUnityClient.QueryUserId(server, "dedennehblehs", Guid.NewGuid(), Guid.NewGuid().ToString()));
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