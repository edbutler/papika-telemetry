
from .models import *
import os, sys
from .main import create_app
import argparse
import json
from sqlalchemy.sql import select

def _go(args):
    app = create_app(args)
    with app.app.app_context():

        def dump_event(e):
            return {
                'id': e.id,
                'type': e.type_id,
                'session_sequence': e.session_sequence_index,
                'time': str(e.client_time),
                'detail': json.loads(e.detail) if e.detail != '' else None,
            }

        def dump_task_start(e):
            d = dump_event(e)
            d['task_sequence'] = 0
            return d

        def dump_task_event(e):
            d = dump_event(e)
            d['task_sequence'] = e.task_event.task_sequence_index
            return d

        def dump_session(s):
            all_events = Event.query.filter_by(session_id=s.id).order_by(Event.id)

            non_task_events = []
            tasks = {}

            for e in all_events:
                if e.task_start is not None and e.task_event is None:
                    tasks[e.task_start.task_id] = {
                        'id': e.task_start.task_id,
                        'group': e.task_start.group_id,
                        'events': [dump_task_start(e)]
                    }

                elif e.task_event is not None:
                    tasks[e.task_event.task_id]['events'].append(dump_task_event(e))

                else:
                    non_task_events.append(dump_event(e))

            return {
                'id': s.id,
                'release': s.release_id,
                'time': str(s.client_time),
                'detail': json.loads(s.detail) if s.detail != '' else None,
                'tasks': [v for v in tasks.values()],
                'events': non_task_events
            }


        def dump_user(u):
            return {
                'id': u.id,
                'username': u.username,
                'sessions': [dump_session(s) for s in Session.query.filter_by(user_id=u.id)],
            }

        users = [dump_user(u) for u in User.query.all()]

        print(json.dumps(users, indent=4))

def main():
    _desc = '''
    Dump data.
    '''
    parser = argparse.ArgumentParser(description=_desc)

    parser.add_argument('config', nargs='?', default=None, type=argparse.FileType('r'),
        help="Config file to use for the server. An alternative is to set the PAPIKA_CONFIG environment variable."
    )

    _go(parser.parse_args())
