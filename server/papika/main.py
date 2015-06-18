import os, sys
import argparse
from tornado.wsgi import WSGIContainer
from tornado.httpserver import HTTPServer
from tornado.ioloop import IOLoop

def _go(args):
    if args.config is not None:
        path = os.path.abspath(args.config.name)
        os.environ['PAPIKA_CONFIG'] = path
        args.config.close()

    from . import app
    port = app.app.config['PORT']
    app.setup()

    if args.mode == 'dev':
        print("Starting development server on port %d..." % port)
        app.app.run(port=port)
    elif args.mode == 'prd':
        http_server = HTTPServer(WSGIContainer(app.app))
        http_server.listen(port)
        print("Starting production server on port %d..." % port)
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
    mut.add_argument('-d', '--dev', action='store_const', dest='mode', const='dev', default='dev',
        help="Run in development mode. Will use flask's built-in auto-reloading web server"
    )
    mut.add_argument('-p', '--prd', action='store_const', dest='mode', const='prd',
        help="Run in production mode. Will use an actual web server (gevent)."
    )

    parser.add_argument('config', nargs='?', default=None, type=argparse.FileType('r'),
        help="Config file to use for the server. An alternative is to set the PAPIKA_CONFIG environment variable."
    )

    _go(parser.parse_args())

