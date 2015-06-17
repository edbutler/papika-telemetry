
# Reference client for telemetry system.

import datetime
import urllib.parse
import urllib.request
from uuid import UUID
import json
import requests
import hashlib

PROTOCOL_VERSION = 1

# currently unused, but might bring this back later?
def create_checksum(params):
    salt = 'ML6AZJgPqtyMosJUT9pSPAWWigL0D1YkbVzJ44KAUi6eukyfoHRJhQl8ead3m9b'
    params['checksum'] = hashlib.sha256((params['data'] + salt).encode('utf-8')).hexdigest()
    params['release'] = str(UUID('de4b98ad-3f9a-4aa9-ba7a-9f8cd80eab6e'))

def send_post_request(url, params):
    req = requests.post(url, data=json.dumps(params), headers={'Content-Type':'application/json'})
    return req.json()

def log_session(player_id, release_id, detail):
    data = {
        'player_id':str(player_id),
        'release_id':str(release_id),
        'client_time':str(datetime.datetime.now()),
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


session_id = log_session(
    player_id=UUID('748ad5bd-8645-4a08-a8e0-f3521a9e4413'),
    release_id=UUID('de4b98ad-3f9a-4aa9-ba7a-9f8cd80eab6e'),
    detail={'im':'some data', 'with':[2,'arrays']}
)

events = [
    Event(23, {'Im':['An', 'Event']}),
    Event(62, {'A Different':[None, False, 'Event']}),
    Event(23, 'blahblahblablablhablhablhahah'),
]

log_events(session_id, events)

