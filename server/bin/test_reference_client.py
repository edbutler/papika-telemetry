#!/usr/bin/env python
import os
import sys
sys.path.append(os.path.join(os.path.dirname(__file__),'..'))

from papika.client import *

def run_test():
    release_id = 'de4b98ad-3f9a-4aa9-ba7a-9f8cd80eab6e'
    release_key = b'\xd5\xc4V\xa9\x1e\xb5\xf6\x9c\xf2eQ\x0fmd0\xb2\xac\x9d\x7f\xc3-\x10\x00\x11\x9b\xb1\x8a\x7f\xbe\xed4\x8b'

    tc = TelemetryClient('http://localhost:5000', release_id, release_key)

    user_id = tc.query_user_id(
        username='pika',
    )
    print("User id: %s" % user_id)

    condition = tc.query_experimental_condition(
        user_id=user_id,
        experiment_id='00000000-0000-0000-0000-000000000000',
    )
    print("Condition: %d" % condition)

    root_logger = tc.log_session(
        user_id=user_id,
        detail={'im':'some data', 'with':[2,'arrays']},
    )

    root_logger.log_event(Event(type_id=23, detail={'Im':['An', 'Event']}))

    task_logger = root_logger.start_task(
        event=Event(type_id=2, detail="I'm a task start"),
        group_id='586c3a14-3659-4975-a28e-d88811a4632b'
    )

    task_events = [
        (10, 'task event 1'),
        (432, 'task event 2'),
        (4, 'task event 3'),
    ]

    for (t, d) in task_events:
        task_logger.log_event(Event(type_id=t, detail=d))

    root_events = [
        (62, {'A Different':[None, False, 'Event']}),
        (23, 'blahblahblablablhablhablhahah'),
    ]

    for (t, d) in root_events:
        root_logger.log_event(Event(type_id=t, detail=d))

    tc.flush_events()

run_test()

