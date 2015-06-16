import sys
import os
import json
import hashlib
import flask
import flask.ext.sqlalchemy
from sqlalchemy.dialects.postgresql import UUID
import datetime
import uuid

app = flask.Flask(__name__)
app.config.from_object('papika.defaultconfig')
app.config.from_envvar('PAPIKA_SETTINGS', silent=True)
db = flask.ext.sqlalchemy.SQLAlchemy(app)

releases = app.config['RELEASES']

def set_from_dict(self, items):
    for c in self.__table__.columns:
        n = c.name
        if n in items:
            setattr(self, n, items[n])
db.Model.set_from_dict = set_from_dict

class Session(db.Model):
    id = db.Column(UUID, primary_key=True)
    player_id = db.Column(UUID, nullable=False)
    release_id = db.Column(UUID, nullable=False)
    server_time = db.Column(db.DateTime(timezone=True), nullable=False)
    client_time = db.Column(db.DateTime(timezone=True), nullable=False)
    detail = db.Column(db.Unicode)

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
    print(request.headers)
    content = request.json
    data = content['data']
    return json.loads(data), content

def create_object(obj, server_time, data, required_fields):
    if required_fields != frozenset(data.keys()):
        raise ValueError('missing data: ' + str(list(required_fields.difference(data.keys()))))

    # FIXME maybe like use the actual client time
    #data['client_time'] = server_time
    data['server_time'] = server_time

    obj.set_from_dict(data)

    return obj

@app.after_request
def add_cors_header(response):
    response.headers['Access-Control-Allow-Origin'] = '*'
    response.headers['Access-Control-Allow-Methods'] = 'HEAD, POST, OPTIONS'
    response.headers['Access-Control-Allow-Headers'] = 'Origin, X-Requested-With, Content-Type, Accept'
    response.headers['Access-Control-Allow-Credentials'] = 'true'
    return response

@app.route('/api/session', methods=['POST'])
def log_session():
    server_time = datetime.datetime.utcnow()
    data, params = parse_message(flask.request)
    required = frozenset(['player_id', 'release_id', 'client_time', 'detail'])
    obj = create_object(Session(), server_time, data, required)
    obj.id = str(uuid.uuid4())
    db.session.add(obj)
    db.session.commit()
    return flask.jsonify(session_id=obj.id)

@app.route('/api/event', methods=['POST'])
def log_events():
    server_time = datetime.datetime.utcnow()
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

