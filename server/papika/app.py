import os, sys
import json
import uuid
import flask, flask.ext.sqlalchemy, flask_cors
from sqlalchemy.dialects.postgresql import UUID
import datetime, dateutil.parser

app = flask.Flask(__name__)
app.config.from_object('papika.defaultconfig')
app.config.from_envvar('PAPIKA_SETTINGS', silent=True)
db = flask.ext.sqlalchemy.SQLAlchemy(app)
cors = flask_cors.CORS(app)

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
    detail = db.Column(db.Unicode)
    # the VCS revision id of the telemetry library (uniquely identifying which version is running)
    library_revid = db.Column(db.Unicode)

class Event(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    session_id = db.Column(UUID, db.ForeignKey('session.id'), nullable=False)
    # index of this event in all events this session (e.g., 1st event is 1, 2nd is 2...)
    session_sequence_index = db.Column(db.Integer, nullable=False)
    # wall time the server received the request to log this event
    server_time = db.Column(db.DateTime(timezone=True), nullable=False)
    # wall time of the client computer when the event was generated
    client_time = db.Column(db.DateTime(timezone=True), nullable=False)
    # the "type" of the event. This is entirely game specific, and is used
    # by games to partition events into different kinds.
    type_id = db.Column(db.SmallInteger, nullable=False)
    # blob of json data
    detail = db.Column(db.Unicode)

def parse_message(request):
    content = request.json
    version = content['version']
    if version != PROTOCOL_VERSION:
        raise RuntimeError('invalid protocol version!')
    data = content['data']
    return data, content

def create_object(obj, server_time, data, required_fields):
    if required_fields != frozenset(data.keys()):
        raise ValueError('missing data: ' + str(list(required_fields.difference(data.keys()))))

    data['client_time'] = dateutil.parser.parse(data['client_time'])
    data['server_time'] = server_time

    obj.set_from_dict(data)

    return obj

# we want a custom 500 error handler so CORS headers are set correctly, even on exceptions.
# TODO this apparently actually doesn't work at all, hmmmm
@app.errorhandler(500)
def internal_error(e):
    return "internal error", 500

@app.route('/api/session', methods=['POST'])
def log_session():
    server_time = datetime.datetime.utcnow()
    data, params = parse_message(flask.request)
    required = frozenset(['user_id', 'release_id', 'client_time', 'library_revid', 'detail'])
    obj = create_object(Session(), server_time, data, required)
    obj.id = str(uuid.uuid4())
    db.session.add(obj)
    db.session.commit()
    return flask.jsonify(session_id=obj.id)

@app.route('/api/event', methods=['POST'])
def log_events():
    server_time = datetime.datetime.now()
    data, params = parse_message(flask.request)
    session_id = params['session']
    required = frozenset(['session_sequence_index', 'client_time', 'type_id', 'detail'])
    for e in data:
        obj = create_object(Event(), server_time, e, required)
        obj.session_id = session_id
        db.session.add(obj)
    db.session.commit()
    return flask.jsonify(is_success=True)

def setup():
    db.create_all()

