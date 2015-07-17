
from flask.ext.sqlalchemy import SQLAlchemy
from sqlalchemy.dialects.postgresql import UUID

db = SQLAlchemy()

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

    event = db.relationship("Event", uselist=False, backref=db.backref("task_start", uselist=False))

class TaskEvent(db.Model):
    __tablename__ = 'task_event'
    id = db.Column(Event.id.type, db.ForeignKey('event.id'), primary_key=True)
    # client-selected id for this task. all events within the task will share this id.
    task_id = db.Column(TaskStart.id.type, nullable=False)
    # index of this event in all events this task (e.g., 1st event is 1, 2nd is 2...)
    task_sequence_index = db.Column(db.Integer, nullable=False)

    event = db.relationship("Event", uselist=False, backref=db.backref("task_event", uselist=False))

# mapping from indenifiable usernames to (presumably anonymous) user ids.
class User(db.Model):
    # the user id, will be generated
    id = db.Column(UUID, primary_key=True)
    # the username
    username = db.Column(db.Unicode, unique=True, nullable=False)
    # json blob of arbitrary queryable "save data"
    savedata = db.Column(db.Unicode)

# many-to-many mapping of users to experimental condition
class UserExperiment(db.Model):
    __tablename__ = 'user_experiment'
    # user id, might not be in the user table if the client is generating its own user ids
    user_id = db.Column(UUID, primary_key=True)
    # experiement id, specified in the config file
    experiment_id = db.Column(UUID, primary_key=True)
    # condition to which they are assigned, chocies are specified in the config file
    condition = db.Column(db.Integer, nullable=False)

