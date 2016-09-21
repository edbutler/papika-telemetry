
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

        def dump_user_no_entry(uid):
            return {
                'id': uid,
                'sessions': [dump_session(s) for s in Session.query.filter_by(user_id=uid)],
            }

        if args.mode == 'user':
            users = [dump_user(u) for u in User.query.all()]
        elif args.mode == 'session':
            users = [dump_user_no_entry(u[0]) for u in db.session.query(Session.user_id).distinct()]
        else:
            raise ValueError("invalid mode!")

        print(json.dumps(users, indent=4))

def main():
    _desc = '''
    Dump data.
    '''
    parser = argparse.ArgumentParser(description=_desc)

    parser.add_argument('config', nargs='?', default=None, type=argparse.FileType('r'),
        help="Config file to use for the server. An alternative is to set the PAPIKA_CONFIG environment variable."
    )

    mut = parser.add_mutually_exclusive_group(required=True)
    mut.add_argument('-u', '--user', action='store_const', dest='mode', const='user',
        help="Use the user table to group and dump sessions. If you use the user table at all, this is probably what you want."
    )
    mut.add_argument('-s', '--session', action='store_const', dest='mode', const='session',
        help="Use the session table to group and dump sessions. If you do not use the user table, this option will instead select all sessions, grouping by user id."
    )

    _go(parser.parse_args())
