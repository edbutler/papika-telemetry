import sys
import argparse
from tornado.wsgi import WSGIContainer
from tornado.httpserver import HTTPServer
from tornado.ioloop import IOLoop
from . import app

DEV_PORT=5000
PRD_PORT=5000

def _go(args):
    app.setup()

    if args.mode == 'dev':
        print("Starting development server on port %d..." % DEV_PORT)
        app.app.config['DEBUG'] = True
        app.app.run(port=DEV_PORT)
    elif args.mode == 'prd':
        http_server = HTTPServer(WSGIContainer(app.app))
        http_server.listen(PRD_PORT)
        IOLoop.instance().start()
    else:
        sys.stderr.write("Invalid mode %s, aborting.\n" % args.mode)
        sys.exit(1)

def main():

    _desc = '''
    Run the papika telemetry web server.
    '''
    parser = argparse.ArgumentParser(description=_desc)

    mut = parser.add_mutually_exclusive_group()
    mut.add_argument('-d', action='store_const', dest='mode', const='dev', default='dev',
        help="Run in development mode. Will use flask's built-in auto-reloading web server"
    )
    mut.add_argument('-p', action='store_const', dest='mode', const='prd',
        help="Run in production mode. Will use an actual web server (gevent)."
    )

    _go(parser.parse_args())

