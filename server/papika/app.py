import os, sys
import json
import uuid
import random
import flask, flask.ext.sqlalchemy, flask_cors
from sqlalchemy.dialects.postgresql import UUID
import datetime, dateutil.parser
import hashlib

app = flask.Flask(__name__)
app.config.from_object('papika.defaultconfig')
app.config.from_envvar('PAPIKA_CONFIG', silent=True)
db = flask.ext.sqlalchemy.SQLAlchemy(app)
cors = flask_cors.CORS(app)
experiments = app.config['PAPIKA_EXPERIMENTS']

PROTOCOL_VERSION = 1

def set_from_dict(self, items):
    for c in self.__table__.columns:
        n = c.name
        if n in items:
            setattr(self, n, items[n])
db.Model.set_from_dict = set_from_dict

class Session(db.Model):
    id = db.Column(UUID, primary_key=True)
    # user id, specified by client, used to match up sessions with the same user
    user_id = db.Column(UUID, nullable=False)
    # the application version
    release_id = db.Column(UUID, nullable=False)
    # wall time the server received the request to log this event
    server_time = db.Column(db.DateTime(timezone=True), nullable=False)
    # wall time of the client computer when the event was generated
    client_time = db.Column(db.DateTime(timezone=True), nullable=False)
    # blob of json data
    detail = db.Column(db.Unicode, nullable=False)
    # the VCS revision id of the telemetry library (uniquely identifying which version is running)
    library_revid = db.Column(db.Unicode, nullable=False)

class Event(db.Model):
    id = db.Column(db.BigInteger, primary_key=True)
    session_id = db.Column(Session.id.type, db.ForeignKey('session.id'), nullable=False)
    # index of this event in all events this session (e.g., 1st event is 1, 2nd is 2...)
    session_sequence_index = db.Column(db.Integer, nullable=False)
    # wall time the server received the request to log this event
    server_time = db.Column(db.DateTime(timezone=True), nullable=False)
    # wall time of the client computer when the event was generated
    client_time = db.Column(db.DateTime(timezone=True), nullable=False)
    # the "type" of the event. This is entirely application specific, and is used
    # by aplications to partition events into different kinds.
    type_id = db.Column(db.SmallInteger, nullable=False)
    # blob of json data
    detail = db.Column(db.Unicode, nullable=False)

# a type of event which marks the start of a group of events.
# other events can be associated with this event.
# used to group events that are part of the same game level, problem, puzzle, task, quest, etc.
class TaskStart(db.Model):
    __tablename__ = 'task_start'
    id = db.Column(Event.id.type, db.ForeignKey('event.id'), primary_key=True)
    # client-selected id for this task. all events within the task will share this id.
    task_id = db.Column(db.Integer, nullable=False)
    # aka level id, problem id, quest id, puzzle id. whatever the things you are tracing are.
    group_id = db.Column(UUID, nullable=False)

class TaskEvent(db.Model):
    __tablename__ = 'task_event'
    id = db.Column(Event.id.type, db.ForeignKey('event.id'), primary_key=True)
    # client-selected id for this task. all events within the task will share this id.
    task_id = db.Column(TaskStart.task_id.type, nullable=False)
    # index of this event in all events this task (e.g., 1st event is 1, 2nd is 2...)
    task_sequence_index = db.Column(db.Integer, nullable=False)

# mapping from indenifiable usernames to (presumably anonymous) user ids.
class User(db.Model):
    # the user id, will be generated
    id = db.Column(UUID, primary_key=True)
    # the username
    username = db.Column(db.Unicode, unique=True, nullable=False)

# many-to-many mapping of users to experimental condition
class UserExperiment(db.Model):
    __tablename__ = 'user_experiment'
    # user id, might not be in the user table if the client is generating its own user ids
    user_id = db.Column(UUID, primary_key=True)
    # experiement id, specified in the config file
    experiment_id = db.Column(UUID, primary_key=True)
    # condition to which they are assigned, chocies are specified in the config file
    condition = db.Column(db.Integer, nullable=False)

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
def create_user_id():
    data, params = parse_message(flask.request)
    username = data['username']

    user = User.query.filter_by(username=username).first()
    if user is None:
        # create a new random user id
        user = User(id=str(uuid.uuid4()), username=username)
        db.session.add(user)
        db.session.commit()

    return flask.jsonify(user_id=user.id)

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
    db.create_all()

