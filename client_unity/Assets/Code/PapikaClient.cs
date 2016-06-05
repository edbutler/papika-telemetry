/**!
 * Papika telemetry client (Unity) library.
 * Copyright 2015 Kristin Siu (kasiu).
 * Revision Id: UNKNOWN_REVISION_ID
 */
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Papika
{
    /// <summary>
    /// Unity component that acts as the frontend client implementation for the Papika library.
    /// </summary>
    public class PapikaClient : MonoBehaviour
    {
        // Note: These editor parameters are optional and may also be set by
        //       building the ClientArgs and passing it into Initialize.
        [SerializeField]
        [Tooltip("The base uri to the server.")]
        private string BaseUri;

        [SerializeField]
        [Tooltip("The release id (string representation).")]
        private string ReleaseId;

        [SerializeField]
        [Tooltip("The release key.")]
        private string ReleaseKey;

        private ClientArgs config;
        private List<object> eventsToLog;
        private int sessionSequenceCounter;

        private Guid? sessionId;
        private string sessionKey;
        private bool isFlushEventsLocked;
        private int taskIdCounter;

        private bool wasStartSessionCalled;

        /// <summary>
        /// The root event logger.
        /// Use this to launch tasks and log root-level events.
        /// </summary>
        public TaskLogger Root { get; private set; }

        /// <summary>
        /// Unity Awake()
        /// </summary>
        private void Awake() {
            // this may be called multiple times if scene gets reloaded, don't reinitialize!

            // Process editor parameters or assume user will call initialize with them later.
            if (this.config == null && !(string.IsNullOrEmpty(BaseUri) || string.IsNullOrEmpty(ReleaseId) || string.IsNullOrEmpty(ReleaseKey))) {
                this.config = new ClientArgs(new Uri(BaseUri), new Guid(ReleaseId), ReleaseKey);
            }

            if (this.eventsToLog == null) {
                this.eventsToLog = new List<object>();
                this.sessionSequenceCounter = 1;
                this.sessionId = null;
                this.sessionKey = null;
                this.isFlushEventsLocked = false;
                this.taskIdCounter = 1;
                this.wasStartSessionCalled = false;
                this.Root = new TaskLogger(null, logEvent, getTaskId);
            }
        }

        /// <summary>
        /// Unity Update()
        /// </summary>
        private void Update() {
            // FOR THE FIRST TIME IN MY LIFE, I AM GLAD THIS IS A MONOBEHAVIOUR
            // (so we don't have to schedule flushing the event log).
            flushEventLog();
        }

        // PUBLIC CLIENT FUNCTIONS!

        /// <summary>
        /// Sets the client-side configuration arguments.
        /// This should be called in a Unity Start() call, probably.
        /// </summary>
        public void Initialize(ClientArgs args) {
            this.config = args;
        }

        /// <summary>
        /// Queries for the user id based on a given user name.
        /// </summary>
        public void QueryUserId(string userName, Action<Guid> onSuccess, Action<string> onFailure) {
            UnityBackend.QueryUserId(this, this.config, userName, onSuccess, onFailure);
        }

        /// <summary>
        /// Queries the experimental condition for a given user (and assigns one if it doesn't exist).
        /// </summary>
        public void QueryExperimentalCondition(Guid userId, Guid experimentId, Action<int> onSuccess, Action<string> onFailure) {
            UnityBackend.QueryExperimentalCondition(this, this.config, userId, experimentId, onSuccess, onFailure);
        }

        /// <summary>
        /// Queries for the user data of a given user.
        /// </summary>
        public void QueryUserData(Guid userId, Action<string> onSuccess, Action<string> onFailure) {
            UnityBackend.QueryUserData(this, this.config, userId, onSuccess, onFailure);
        }

        public void SetUserData(Guid userId, string savedata, Action<string> onSuccess, Action<string> onFailure) {
            UnityBackend.SetUserData(this, this.config, userId, savedata, onSuccess, onFailure);
        }

        /// <summary>
        /// Starts a logging session.
        /// Should only be called once - at the start of a new logging session per application start.
        /// </summary>
        public void StartSession(Guid userId, string detail) {
            if (this.wasStartSessionCalled) {
                throw new InvalidOperationException("Session already logged.");
            }

            if (detail == null) {
                throw new ArgumentException("Null detail parameter.");
            }

            Action<Guid, string> onSuccess = (g, s) => {
                this.sessionId = g;
                this.sessionKey = s;
            };

            Action<string> onFailure = s => {
//#if UNITY_EDITOR
//                Debug.LogError("Logging session failed: " + s);
//#endif
            };

            // XXX (kasiu): Revision id is important, but we don't really care right now.
            UnityBackend.LogSession(this, this.config, userId, detail, "UNKNOWN_REVISION_ID", onSuccess, onFailure);
            this.wasStartSessionCalled = true;
        }

        /// <summary>
        /// Flushes the list of queued events by sending them to the server.
        /// </summary>
        private void flushEventLog() {
            if (this.sessionId == null || this.eventsToLog.Count == 0 || this.isFlushEventsLocked) {
                return;
            }

            this.isFlushEventsLocked = true;
            var eventsArray = this.eventsToLog.ToArray();

            Action<string> onSuccess = s => {
                // Clear the log.
                this.eventsToLog.RemoveRange(0, eventsArray.Length);
                this.isFlushEventsLocked = false;
            };

            Action<string> onFailure = s => {
//#if UNITY_EDITOR
//                Debug.LogError("Logging events failed: " + s);
//#endif
                // TODO (kasiu): Implement exponential backoff or something fancy, eventually.
                this.isFlushEventsLocked = false;
            };

            UnityBackend.LogEvents(this, this.config.BaseUri, eventsArray, this.sessionId.Value, this.sessionKey, onSuccess, onFailure);
        }

        // CALLBACK FUNCTIONS!

        /// <summary>
        /// Callback for logging an event.
        /// Writes session index and adds to the list of events to log.
        /// </summary>
        private void logEvent(Dictionary<string, object> data) {
            data.Add("session_sequence_index", this.sessionSequenceCounter);
            this.eventsToLog.Add(data);
            this.sessionSequenceCounter++;
        }

        /// <summary>
        /// Returns the next task id.
        /// </summary>
        private int getTaskId() {
            return this.taskIdCounter++;
        }
    }

    /// <summary>
    /// The task and event logger.
    /// </summary>
    public class TaskLogger
    {
        private int? taskId;
        private Action<Dictionary<string, object>> logEvent;
        private Func<int> getTaskId;
        private int taskSequenceCounter;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TaskLogger(int? taskId, Action<Dictionary<string, object>> logEvent, Func<int> getTaskId) {
            this.taskId = taskId;
            this.logEvent = logEvent;
            this.getTaskId = getTaskId;
            this.taskSequenceCounter = 1;
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        public void LogEvent(short category, short type, string detail) {
            var eventDict = buildEventDictionary(category, type, detail);

            this.logEvent(eventDict);
        }

        /// <summary>
        /// Starts a new task and returns a dedicated logger for that task.
        /// </summary>
        public TaskLogger StartTask(Guid group, short category, short type, string detail) {
            var eventDict = buildEventDictionary(category, type, detail);

            var taskStartDict = new Dictionary<string, object>();
            var newTaskId = this.getTaskId();
            taskStartDict.Add("task_id", newTaskId);
            taskStartDict.Add("group_id", group);
            eventDict.Add("task_start", taskStartDict);

            this.logEvent(eventDict);
            return new TaskLogger(newTaskId, this.logEvent, this.getTaskId);
        }

        /// <summary>
        /// Builds the event object.
        /// </summary>
        private Dictionary<string, object> buildEventDictionary(short category, short type, string detail) {
            var eventDict = new Dictionary<string, object>();
            eventDict.Add("category_id", category);
            eventDict.Add("type_id", type);
            // PapikaClient should add the session_sequence_index.
            eventDict.Add("client_time", DateTime.Now.ToString());
            eventDict.Add("detail", detail);

            if (this.taskId != null) {
                var taskEventDict = new Dictionary<string, object>();
                taskEventDict.Add("task_id", this.taskId);
                taskEventDict.Add("task_sequence_index", this.taskSequenceCounter);
                this.taskSequenceCounter++;
                eventDict.Add("task_event", taskEventDict);
            }

            return eventDict;
        }
    }
}
