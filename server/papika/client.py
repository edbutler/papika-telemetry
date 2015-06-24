
# Reference client for telemetry system.

import os, sys
import subprocess
import datetime
from uuid import UUID, uuid4
import json
import requests
import hashlib

PROTOCOL_VERSION = 1

localdir = os.path.abspath(os.path.dirname(__file__))

def check_output(args, cwd):
    '''Wrapper for subprocess.check_output that converts result to unicode'''
    result = subprocess.check_output(args, cwd=cwd)
    return result.decode('utf-8')

revid = check_output(['git', 'rev-parse', 'HEAD'], localdir).strip()
status = check_output(['git', 'status', '--porcelain'], localdir).strip()
if len(status) > 0:
    revid += "+"

# currently unused, but might bring this back later?
def create_checksum(message, key):
    return hashlib.sha256(message.encode('utf-8') + key).hexdigest()

def send_post_request(url, params):
    req = requests.post(url, data=json.dumps(params), headers={'Content-Type':'application/json'})
    return req.json()

def send_nonsession_request(url, data, release_id, release_key):
    data = json.dumps(data)
    return send_post_request(url, {
        'version': PROTOCOL_VERSION,
        'data': data,
        'release': release_id,
        'checksum': create_checksum(data, release_key),
    })

def send_session_request(url, data, session_id, session_key):
    data = json.dumps(data)
    return send_post_request(url, {
        'version': PROTOCOL_VERSION,
        'data': data,
        'session': session_id,
        'checksum': create_checksum(data, session_key),
    })

def query_user(username, release_id, release_key):
    data = {
        'username':username,
    }
    result = send_nonsession_request("http://localhost:5000/api/user", data, release_id, release_key)
    return result['user_id']

def query_experiment(user_id, experiment_id, release_id, release_key):
    data = {
        'user_id': user_id,
        'experiment_id': experiment_id,
    }
    result = send_nonsession_request("http://localhost:5000/api/experiment", data, release_id, release_key)
    return result['condition']

def log_session(user_id, detail, release_id, release_key):
    data = {
        'user_id': user_id,
        'client_time':str(datetime.datetime.now()),
        'library_revid': revid,
        'detail': json.dumps(detail),
    }
    result = send_nonsession_request("http://localhost:5000/api/session", data, release_id, release_key)
    return result['session_id'], result['session_key']

def log_events(events, session_id, session_key):
    data = [e.data for e in events]
    send_session_request("http://localhost:5000/api/event", data, session_id, session_key)

event_counter = 0

class Event:
    def __init__(self, type_id, detail):
        global event_counter
        event_counter += 1
        self.data = {
            'type_id': type_id,
            'session_sequence_index': event_counter,
            'client_time': str(datetime.datetime.now()),
            'detail': json.dumps(detail),
        }

task_counter = 0

class TaskStart(Event):
    def __init__(self, group_id, type_id, detail):
        super().__init__(type_id, detail)
        global task_counter
        task_counter += 1
        self.task_id = task_counter
        self.data['task_start'] = {
            'task_id': self.task_id,
            'group_id': group_id,
        }

class TaskEvent(Event):
    def __init__(self, task_id, seq_index, type_id, detail):
        super().__init__(type_id, detail)
        self.data['task_event'] = {
            'task_id': task_id,
            'task_sequence_index': seq_index
        }

def run_test():
    release_id = 'de4b98ad-3f9a-4aa9-ba7a-9f8cd80eab6e'
    release_key = b'\xd5\xc4V\xa9\x1e\xb5\xf6\x9c\xf2eQ\x0fmd0\xb2\xac\x9d\x7f\xc3-\x10\x00\x11\x9b\xb1\x8a\x7f\xbe\xed4\x8b'

    user_id = query_user(
        username='pika',
        release_id=release_id,
        release_key=release_key
    )

    condition = query_experiment(
        user_id=user_id,
        experiment_id='00000000-0000-0000-0000-000000000000',
        release_id=release_id,
        release_key=release_key
    )
    print('Condition:' + str(condition))

    session_id, session_key = log_session(
        user_id=user_id,
        detail={'im':'some data', 'with':[2,'arrays']},
        release_id=release_id,
        release_key=release_key
    )
    session_key = bytes.fromhex(session_key)

    task = TaskStart(
        group_id='586c3a14-3659-4975-a28e-d88811a4632b',
        type_id=2,
        detail="I'm a task start"
    )
    task_events = [
        TaskEvent(task.task_id, 1, 10, 'task event 1'),
        TaskEvent(task.task_id, 2, 432, 'task event 2'),
        TaskEvent(task.task_id, 3, 4, 'task event 3'),
    ]

    events = [
        Event(23, {'Im':['An', 'Event']}),
        Event(62, {'A Different':[None, False, 'Event']}),
        Event(23, 'blahblahblablablhablhablhahah'),
    ]

    log_events(
        events=events + [task] + task_events,
        session_id=session_id,
        session_key=session_key
    )

run_test()

