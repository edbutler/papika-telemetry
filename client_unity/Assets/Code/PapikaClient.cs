using UnityEngine;
using System;
using System.Collections.Generic;

namespace Papika
{
    public class PapikaClient : MonoBehaviour
    {
        private ClientArgs config;
        private List<object> eventsToLog;
        private int sessionSequenceCounter;

        private Guid? sessionId;
        private string sessionKey;
        private bool isFlushEventsLocked;
        private int taskIdCounter;

        private bool wasStartSessionCalled;

        public TaskLogger Root { get; private set; }

        // Unity initialization crap.
        private void Awake() {
            this.config = null;
            this.eventsToLog = new List<object>();
            this.sessionSequenceCounter = 1;
            this.sessionId = null;
            this.sessionKey = null;
            this.isFlushEventsLocked = false;
            this.taskIdCounter = 1;
            this.wasStartSessionCalled = false;

            this.Root = new TaskLogger(null, logEvent, getTaskId);
        }

        // FOR THE FIRST TIME IN MY LIFE, I AM GLAD THIS IS A MONOBEHAVIOUR.
        private void Update() {
            flushEventLog();
        }

        public void Initialize(ClientArgs args) {
            this.config = args;
        }

        // Public interface for logging:
        public void QueryUserId(string userName, Action<Guid> onSuccess, Action<string> onFailure) {
            NetworkBackend.QueryUserId(this, this.config, userName, onSuccess, onFailure);
        }

        public void QueryExperimentalCondition(Guid userId, Guid experimentId, Action<int> onSuccess, Action<string> onFailure) {
            NetworkBackend.QueryExperimentalCondition(this, this.config, userId, experimentId, onSuccess, onFailure);
        }

        public void QueryUserData(Guid userId, Action<string> onSuccess, Action<string> onFailure) {
            NetworkBackend.QueryUserData(this, this.config, userId, onSuccess, onFailure);
        }

        public void SetUserData(Guid userId, string savedata, Action<string> onSuccess, Action<string> onFailure) {
            NetworkBackend.SetUserData(this, this.config, userId, savedata, onSuccess, onFailure);
        }

        // IMPLEMENT MEEEEE!
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
                Debug.LogError("Logging session failed: " + s);
            };

            // XXX (kasiu): Revision id is important, but we don't really care right now.
            NetworkBackend.LogSession(this, this.config, userId, detail, "UNHAPPY_UNKNOWN_ID", onSuccess, onFailure);
        }

        // Used as callbacks.
        private void logEvent(Dictionary<string, object> data) {
            data.Add("session_sequence_index", this.sessionSequenceCounter);
            this.eventsToLog.Add(data);
            this.sessionSequenceCounter++;
        }

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
                Debug.LogError("Logging events failed: " + s);
                this.isFlushEventsLocked = false;
            };

            NetworkBackend.LogEvents(this, this.config.BaseUri, eventsArray, this.sessionId.Value, this.sessionKey, onSuccess, onFailure);
        }

        private int getTaskId() {
            return this.taskIdCounter++;
        }
    }

    public class TaskLogger
    {
        private int? taskId;
        private Action<Dictionary<string, object>> logEvent;
        private Func<int> getTaskId;
        private int taskSequenceCounter;

        public TaskLogger(int? taskId, Action<Dictionary<string, object>> logEvent, Func<int> getTaskId) {
            this.taskId = taskId;
            this.logEvent = logEvent;
            this.getTaskId = getTaskId;
            this.taskSequenceCounter = 1;
        }

        public void LogEvent(short category, short type, string detail) {
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

            this.logEvent(eventDict);
        }

        // TODO (kasiu): Clean me up later when I am less dumb.
        public TaskLogger StartTask(Guid group, short category, short type, string detail) {
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

            var taskStartDict = new Dictionary<string, object>();
            var newTaskId = this.getTaskId();
            taskStartDict.Add("task_id", newTaskId);
            taskStartDict.Add("group_id", group);
            eventDict.Add("task_start", taskStartDict);

            this.logEvent(eventDict);
            return new TaskLogger(newTaskId, this.logEvent, this.getTaskId);
        }
    }
}
