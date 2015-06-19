DEBUG = True
MAX_CONTENT_LENGTH = 50 << 20
SQLALCHEMY_DATABASE_URI = 'postgresql:///logging_dev'
PORT = 5000
PAPIKA_EXPERIMENTS = {
    '00000000-0000-0000-0000-000000000000': {
        'conditions': [
            {'id':1, 'name': 'im a condition'},
            {'id':2, 'name': 'im another condition'},
        ],
    },
}
