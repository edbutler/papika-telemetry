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
    else:
        try:
            with open(os.environ['PAPIKA_CONFIG'], 'r'): pass
        except KeyError:
            sys.stderr.write("WARNING: no config file specified via CLI and PAPIKA_CONFIG envvar not set! You should set the config via one of those methods.\n")
        except:
            sys.stderr.write("WARNING: PAPIKA_CONFIG envvar does not point to a valid file, not loading config!\n")


    from . import app
    port = app.app.config['PORT']
    app.setup()

    if args.mode == 'dev':
        print("Starting development server on port %d..." % port)
        app.app.run(port=port)
    elif args.mode == 'prd':
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

