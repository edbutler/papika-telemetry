import os, sys
import json
import uuid
import random
import flask, flask_cors
import datetime, dateutil.parser
import hashlib
from .model import db, User, Session, Event, TaskStart, TaskEvent, UserExperiment

app = flask.Flask(__name__)
app.config.from_object('papika.defaultconfig')
app.config.from_envvar('PAPIKA_CONFIG', silent=True)
db.init_app(app)
cors = flask_cors.CORS(app)
experiments = app.config['PAPIKA_EXPERIMENTS']

PROTOCOL_VERSION = 1

def set_from_dict(self, items):
    for c in self.__table__.columns:
        n = c.name
        if n in items:
            setattr(self, n, items[n])
db.Model.set_from_dict = set_from_dict

def parse_message(request):
    content = request.json
    version = content['version']
    if version != PROTOCOL_VERSION:
        raise RuntimeError('invalid protocol version!')
    data = json.loads(content['data'])
    return data, content

def create_object(obj, server_time, data, required_fields):
    if not required_fields.issubset(frozenset(data.keys())):
        raise ValueError('missing data: ' + str(list(required_fields.difference(data.keys()))))

    # HACK only want to set times for tables with these columns
    if 'client_time' in data:
        data['client_time'] = dateutil.parser.parse(data['client_time'])
        data['server_time'] = server_time

    obj.set_from_dict(data)

    return obj

def create_hash(key, data):
    return hashlib.sha256(data.encode('utf-8') + key).hexdigest()

# we want a custom 500 error handler so CORS headers are set correctly, even on exceptions.
# TODO this apparently actually doesn't work at all, hmmmm
@app.errorhandler(500)
def internal_error(e):
    return "internal error", 500

@app.route('/api/session', methods=['POST'])
def log_session():
    server_time = datetime.datetime.now()
    data, params = parse_message(flask.request)
    required = frozenset(['user_id', 'client_time', 'library_revid', 'detail'])
    obj = create_object(Session(), server_time, data, required)
    obj.id = str(uuid.uuid4())
    obj.release_id = params['release']
    db.session.add(obj)
    db.session.commit()
    session_key = create_hash(app.config['SECRET_KEY'], obj.id)
    return flask.jsonify(session_id=obj.id, session_key=session_key)

@app.route('/api/event', methods=['POST'])
def log_events():
    server_time = datetime.datetime.now()
    data, params = parse_message(flask.request)
    session_id = params['session']
    required = frozenset(['session_sequence_index', 'client_time', 'type_id', 'detail'])
    events = [create_object(Event(), server_time, e, required) for e in data]
    # commit all events first (because we need the ids)
    for obj in events:
        obj.session_id = session_id
        db.session.add(obj)
    db.session.commit()
    # then commit any sub-objects of those events
    for obj, e in zip(events, data):
        if 'task_start' in e:
            o = create_object(TaskStart(), None, e['task_start'], frozenset(['task_id', 'group_id']))
            o.id = obj.id
            db.session.add(o)
        if 'task_event' in e:
            o = create_object(TaskEvent(), None, e['task_event'], frozenset(['task_id', 'task_sequence_index']))
            o.id = obj.id
            db.session.add(o)
    db.session.commit()

    return flask.jsonify(is_success=True)

@app.route('/api/user', methods=['POST'])
def get_user_by_name():
    data, params = parse_message(flask.request)
    username = data['username']

    user = User.query.filter_by(username=username).first()
    if user is None:
        # create a new random user id
        user = User(id=str(uuid.uuid4()), username=username)
        db.session.add(user)
        db.session.commit()

    return flask.jsonify(user_id=user.id)

@app.route('/api/user/get_data', methods=['POST'])
def get_user_savedata():
    data, params = parse_message(flask.request)
    id = data['id']

    user = User.query.filter_by(id=id).first()
    if user is None:
        raise ValueError('no such user!')
    else:
        return flask.jsonify(id=user.id, savedata=user.savedata)

@app.route('/api/user/set_data', methods=['POST'])
def set_user_savedata():
    data, params = parse_message(flask.request)
    id = data['id']
    savedata = data['savedata']

    user = User.query.filter_by(id=id).first()
    if user is None:
        raise ValueError('no such user!')
    else:
        user.savedata = savedata
        db.session.commit()

    return flask.jsonify(is_success=True)

@app.route('/api/experiment', methods=['POST'])
def get_experimental_condition():
    data, params = parse_message(flask.request)
    user_id = data['user_id']
    experiment_id = data['experiment_id']

    pe = UserExperiment.query.filter_by(user_id=user_id, experiment_id=experiment_id).first()
    if pe is None:
        # new user! give them a condition
        # HACK just crash if the experiment doesn't exist
        exper = experiments[experiment_id]
        condition = random.choice(exper['conditions'])
        pe = UserExperiment(user_id=user_id, experiment_id=experiment_id, condition=condition['id'])
        db.session.add(pe)
        db.session.commit()

    return flask.jsonify(condition=pe.condition)

def setup():
    with app.app_context():
        db.create_all()

