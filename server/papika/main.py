import os, sys
import argparse
from tornado.wsgi import WSGIContainer
from tornado.httpserver import HTTPServer
from tornado.ioloop import IOLoop

def create_app(args):
    if args.config is not None:
        path = os.path.abspath(args.config.name)
        os.environ['PAPIKA_CONFIG'] = path
        args.config.close()
    else:
        try:
            with open(os.environ['PAPIKA_CONFIG'], 'r'): pass
        except KeyError:
            sys.stderr.write("WARNING: no config file specified via CLI and PAPIKA_CONFIG envvar not set! You should set the config via one of those methods.\n")
        except:
            sys.stderr.write("WARNING: PAPIKA_CONFIG envvar does not point to a valid file, not loading config!\n")

    from . import app
    return app

def _go(args):
    app = create_app(args)
    port = app.app.config['PORT']
    app.setup()

    mode = app.app.config['SERVER']
    if args.mode == 'dev':
        mode = 'development'
        app.app.config['DEBUG'] = True
    elif args.mode == 'prd':
        mode = 'production'

    if mode == 'development':
        print("Starting development server on port %d..." % port)
        app.app.run(port=port)
    elif mode == 'production':
        appl = WSGIContainer(app.app)
        if 'SSL_KEY' in app.app.config:
            http_server = HTTPServer(appl, ssl_options={
                "certfile": app.app.config['SSL_CRT'],
                "keyfile": app.app.config['SSL_KEY'],
            })
        else:
            http_server = HTTPServer(appl)
        http_server.listen(port)
        print("Starting production server on port %d..." % port)
        IOLoop.instance().start()
    else:
        sys.stderr.write("Invalid SERVER setting '%s', aborting.\n" % args.mode)
        sys.exit(1)

def main():

    _desc = '''
    Run the papika telemetry web server.
    '''
    parser = argparse.ArgumentParser(description=_desc)

    mut = parser.add_mutually_exclusive_group()
    mut.add_argument('-d', '--dev', action='store_const', dest='mode', const='dev', default=None,
        help="Force run in development mode. Overrides SERVER config setting. Will use flask's built-in auto-reloading web server"
    )
    mut.add_argument('-p', '--prd', action='store_const', dest='mode', const='prd',
        help="Force run in production mode. Overrides SERVER config setting. Will use an actual web server (tornado)."
    )

    parser.add_argument('config', nargs='?', default=None, type=argparse.FileType('r'),
        help="Config file to use for the server. An alternative is to set the PAPIKA_CONFIG environment variable."
    )

    _go(parser.parse_args())

