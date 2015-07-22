
# Reference client for telemetry system.
# Doesn't have any of the error handling, etc. a real client should have.
# But it does follow the same basic interface.

import os, sys
import subprocess
import datetime
from uuid import UUID, uuid4
import json
import requests
import hashlib

PROTOCOL_VERSION = 1

_localdir = os.path.abspath(os.path.dirname(__file__))

def _check_output(args, cwd):
    '''Wrapper for subprocess.check_output that converts result to unicode'''
    result = subprocess.check_output(args, cwd=cwd)
    return result.decode('utf-8')
_revid = ''
try:
    _revid = _check_output(['git', 'rev-parse', 'HEAD'], _localdir).strip()
    _status = _check_output(['git', 'status', '--porcelain'], _localdir).strip()
    if len(_status) > 0:
        _revid += "+"
except subprocess.CalledProcessError: pass

# PROTOCOL IMPLEMENTATION
#############################################################################

# currently unused, but might bring this back later?
def _create_checksum(message, key):
    return hashlib.sha256(message.encode('utf-8') + key).hexdigest()

def _send_post_request(url, params):
    req = requests.post(url, data=json.dumps(params), headers={'Content-Type':'application/json'})
    return req.json()

def _send_nonsession_request(url, data, release_id, release_key):
    data = json.dumps(data)
    return _send_post_request(url, {
        'version': PROTOCOL_VERSION,
        'data': data,
        'release': release_id,
        'checksum': _create_checksum(data, release_key),
    })

def _send_session_request(url, data, session_id, session_key):
    data = json.dumps(data)
    return _send_post_request(url, {
        'version': PROTOCOL_VERSION,
        'data': data,
        'session': session_id,
        'checksum': _create_checksum(data, session_key),
    })

def _query_user(base_uri, username, release_id, release_key):
    data = {
        'username':username,
    }
    result = _send_nonsession_request(base_uri + "/api/user", data, release_id, release_key)
    return result['user_id']

def _query_user_savedata(base_uri, user_id, release_id, release_key):
    data = {
        'id': user_id,
    }
    result = _send_nonsession_request(base_uri + "/api/user/get_data", data, release_id, release_key)
    return result['savedata']

def _send_user_savedata(base_uri, user_id, savedata, release_id, release_key):
    data = {
        'id': user_id,
        'savedata': savedata
    }
    result = _send_nonsession_request(base_uri + "/api/user/set_data", data, release_id, release_key)

def _query_experiment(base_uri, user_id, experiment_id, release_id, release_key):
    data = {
        'user_id': user_id,
        'experiment_id': experiment_id,
    }
    result = _send_nonsession_request(base_uri + "/api/experiment", data, release_id, release_key)
    return result['condition']

def _log_session(base_uri, user_id, detail, release_id, release_key):
    data = {
        'user_id': user_id,
        'client_time':str(datetime.datetime.now()),
        'library_revid': _revid,
        'detail': json.dumps(detail),
    }
    result = _send_nonsession_request(base_uri + "/api/session", data, release_id, release_key)
    return result['session_id'], result['session_key']

def _log_events(base_uri, events, session_id, session_key):
    data = [e for e in events]
    _send_session_request(base_uri + "/api/event", data, session_id, session_key)

# CLIENT INTERFACE
#############################################################################

class Event:
    def __init__(self, type_id, detail):
        self.type_id = type_id
        self.detail = json.dumps(detail)

class TaskLogger:
    def __init__(self, tc, task_id):
        self.telemetry_client = tc
        self.task_id = task_id
        self._task_counter = 0

    def _create_data(self, event):
        tc = self.telemetry_client

        tc._session_counter += 1
        data = {
            'type_id': event.type_id,
            'session_sequence_index': tc._session_counter,
            'client_time': str(datetime.datetime.now()),
            'detail': event.detail,
        }

        if self.task_id is not None:
            self._task_counter += 1
            data['task_event'] = {
                'task_id': self.task_id,
                'task_sequence_index': self._task_counter
            }

        return data

    def start_task(self, event, group_id):
        tc = self.telemetry_client
        tc._task_counter += 1
        task_id = tc._task_counter

        data = self._create_data(event)
        data['task_start'] = {
            'task_id': task_id,
            'group_id': group_id,
        }
        self.telemetry_client._events_to_log.append(data)

        return TaskLogger(tc, task_id)

    def log_event(self, event):
        data = self._create_data(event)
        self.telemetry_client._events_to_log.append(data)

class TelemetryClient:
    def __init__(self, base_uri, release_id, release_key):
        self.base_uri = base_uri
        self.release_id = release_id
        self.release_key = release_key
        self._session_counter = 0
        self._task_counter = 0
        self._events_to_log = []

    def query_user_id(self, username):
        return _query_user(self.base_uri, username, self.release_id, self.release_key)

    def query_experimental_condition(self, user_id, experiment_id):
        return _query_experiment(self.base_uri, user_id, experiment_id, self.release_id, self.release_key)

    def query_user_savedata(self, user_id):
        data = _query_user_savedata(self.base_uri, user_id, self.release_id, self.release_key)
        return json.loads(data) if data is not None else None

    def send_user_savedata(self, user_id, savedata):
        return _send_user_savedata(self.base_uri, user_id, json.dumps(savedata), self.release_id, self.release_key)

    def log_session(self, user_id, detail):
        session_id, session_key = _log_session(self.base_uri, user_id, detail, self.release_id, self.release_key)
        self.session_id = session_id
        self.session_key = bytes.fromhex(session_key)
        return TaskLogger(self, None)

    def flush_events(self):
        _log_events(self.base_uri, self._events_to_log, self.session_id, self.session_key)
        self._events_to_log = []

