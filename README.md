
Papika Telemetry System
=======================

Papika is server and set of client libraries that allow applications to log event data in a server-side database.
Intended applications include research experiments and usability testing.
The server is written in Python with a Python reference implementation of a client.
Currently, the only client is for Javascript applications.


Server Installation
-------------------

The server runs in Python 3.4+ using Flask and SQLAlchmey, backed by a PostgreSQL database.
It uses Postgres's native UUID type and thus other DBMS are **not** supported.
It has been run and tested on Linux and Windows.
The suggested installation method for Python packages is to use virtualenv and pip.

Prequisites:

* Python 3.4+ (with development headers if using pip to install `psycopg2`)
* PostgreSQL 9.4+ (with development headers if using pip to install `psycopg2`)
* virtualenv and pip
* A C/C++ compiler if using pip to install `psycopg2`
* All the Python packages listed in `server/requirements.txt`, which can be installed through pip.

In the following instructions,
&lt;user&gt; is the name of the user that will be running the server,
&lt;server\_dir&gt; is the directory in which the server code lives (should contain a `requirements.txt`)
and &lt;db\_name&gt; is the name of the database in which data will be stored.
We typically use `logging` for the database name.

### Ubuntu (14.04+) Linux Instructions:

1. Install required packages:

        apt install build-essential g++ python3-dev python3-virtualenv postgresql-server postgresql-server-dev-all
2. Create the postgres database (run as root, these instructions assume you have never configured postgres so may need to be changed):

        service postgresql start
        sudo -u postgres createuser -d <user>
        sudo su <user>:
        createdb <user> # this is only so there's a default database when running psql with no args
        createdb <db_name>
3. Install Python virtual environment and requirements (run as &lt;user&gt;):

        cd <server_dir>
        virtualenv -p /usr/bin/python3.4 venv
        source ./venv/bin/activate
        pip install -r ./requirements.txt

To later run the server from a different shell:

        cd <server_dir>
        source ./venv/bin/activate # only if this is a different shell than the one you installed with.
        ./bin/runserver.py [args...]

### Red Hat Enterprise Linux Instructions:

1. Install required packages:

        yum install rh-python34 postgresql-server postgresql-devel gcc
2. Create the postgres database (run as root, these instructions assume you have never configured postgres so may need to be changed):

        postgresql-setup initdb
        service postgresql start
        sudo -u postgres createuser -d <user>
        # then, running as <user>:
        createdb <user> # this is only so there's a default database when running psql with no args
        createdb <db_name>

3. Install Python virtual environment and requirements (run as &lt;user&gt;):

        cd <server_dir>
        scl enable rh-python34 bash
        virtualenv venv
        source ./venv/bin/activate
        pip install -r ./requirements.txt

To later run the server from a different shell:

    cd <server_dir>
    scl enable rh-python34 bash
    source ./venv/bin/activate
    ./bin/runserver.py [args...]

Windows Installation:

Detailed instructions forthcoming, but here's a few pointers:

- `psycopg2` should be installed with the system Python using a binary installer because it's half-impossible to build from source.
  `virtualenv` can be instructed to use site packages with the `--system-site-packages` flag.
  The rest of the Python requirements can still be installed with `virtualenv` and `pip`.

Running the Server
------------------

The server is executed by simply running `server/bin/runserver.py` (with virtualenv active).
The task of turning this into a daemon is not (yet) covered here but is recommended for real-world deployments.
An easier but more fragile method (but useful for testing/development) is to run it inside of tmux/screen.

The server is configured through a Python file in a manner similar to Flask (it is, in fact, the Flask config file with some extra fields).
It can be passed to `runserver.py` either as a CLI argument or through the `PAPIKA_CONFIG` environment variable.
There is a sample configuration file in `server/sample_config.py`.

Configuration Values (any built-in Flask values also work):

Name  | Description
------------- | -------------
DEBUG | boolean, enable/disable debug mode. Should be `False` for production.
SQLALCHEMY\_DATABASE\_URI | The URI for the postgres database, e.g., 'postgresql:///logging'
PORT | the port number on which to serve.
SERVER | Either `'production'` or `'deveopment'`. Production uses the Tornado web server; development uses the built-in Flask web server.
SSL\_KEY | Path to the SSL key. Will use HTTP is not set, HTTPS if set. Only works for production server.
SSL\_CRT | Path tot he SSL certificate. Must be set if SSL\_KEY is set.

