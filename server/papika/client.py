
# Reference client for telemetry system.

import os, sys
import subprocess
import datetime
from uuid import UUID
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
def create_checksum(params):
    salt = 'ML6AZJgPqtyMosJUT9pSPAWWigL0D1YkbVzJ44KAUi6eukyfoHRJhQl8ead3m9b'
    params['checksum'] = hashlib.sha256((params['data'] + salt).encode('utf-8')).hexdigest()
    params['release'] = str(UUID('de4b98ad-3f9a-4aa9-ba7a-9f8cd80eab6e'))

def send_post_request(url, params):
    req = requests.post(url, data=json.dumps(params), headers={'Content-Type':'application/json'})
    return req.json()

def query_user(username):
    data = {
        'username':username,
    }
    params = {
        'version': PROTOCOL_VERSION,
        'data': data,
    }
    result = send_post_request("http://localhost:5000/api/user", params)
    return result['user_id']

def query_experiment(user_id, experiment_id):
    data = {
        'user_id':str(user_id),
        'experiment_id':str(experiment_id),
    }
    params = {
        'version': PROTOCOL_VERSION,
        'data': data,
    }
    result = send_post_request("http://localhost:5000/api/experiment", params)
    return result['condition']

def log_session(user_id, release_id, detail):
    data = {
        'user_id':str(user_id),
        'release_id':str(release_id),
        'client_time':str(datetime.datetime.now()),
        'library_revid': revid,
        'detail':json.dumps(detail),
    }
    params = {
        'version': PROTOCOL_VERSION,
        'data': data,
    }
    result = send_post_request("http://localhost:5000/api/session", params)
    return result['session_id']

counter = 0

class Event(object):
    def __init__(self, type_id, detail):
        global counter
        counter += 1
        self.data = {
            'type_id': type_id,
            'session_sequence_index': counter,
            'client_time': str(datetime.datetime.now()),
            'detail': json.dumps(detail),
        }

def log_events(session_id, events):
    params = {
        'version': PROTOCOL_VERSION,
        'data': [e.data for e in events],
        'session': session_id,
    }
    result = send_post_request("http://localhost:5000/api/event", params)

user_id = query_user('pika')

condition = query_experiment(
    user_id=user_id,
    experiment_id=UUID('00000000-0000-0000-0000-000000000000'),
)
print('Condition:' + str(condition))

session_id = log_session(
    user_id=user_id,
    release_id=UUID('de4b98ad-3f9a-4aa9-ba7a-9f8cd80eab6e'),
    detail={'im':'some data', 'with':[2,'arrays']}
)

events = [
    Event(23, {'Im':['An', 'Event']}),
    Event(62, {'A Different':[None, False, 'Event']}),
    Event(23, 'blahblahblablablhablhablhahah'),
]

log_events(session_id, events)

