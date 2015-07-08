DEBUG = True
SECRET_KEY = b'\xf067\x1a\xcb{z\xea\xca&\x88\x1b_=\xfa\x00fx\xe2\xcf\xf7\xd80\xc76\xaa\x07\xa7\xa5e\xb3]'
MAX_CONTENT_LENGTH = 50 << 20
SQLALCHEMY_DATABASE_URI = 'postgresql:///logging_dev'
PORT = 5000
SERVER = 'development'
PAPIKA_EXPERIMENTS = {
    '00000000-0000-0000-0000-000000000000': {
        'conditions': [
            {'id':1, 'name': 'im a condition'},
            {'id':2, 'name': 'im another condition'},
        ],
    },
}
