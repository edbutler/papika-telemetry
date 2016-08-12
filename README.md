
Papika Telemetry System
=======================

Overview
--------

Papika is a server and a set of client libraries that allow applications to log event data in a server-side database.
Intended applications include research experiments and usability testing.
The server is written in Python with a Python reference implementation of a client.
Currently, the only production-usable client is for JavaScript applications.

This is still an early work in progress and breaking changes are expected, as well as clients in more languages.

### Motivation and Goals

What this is designed to do:

It should support lightweight yet robust remote logging of event data for small/medium application.
The library should handle all the details of getting the data to a server while prescribing few restrictions on the type of data being logged.
To that end, the model is as simple as possible, and allows users to specify arbitrary JSON data blobs for each event.
It's intended for applications that are small enough that dumping the entire database to a file and analyzing with e.g., a Python script is feasible.

What this is **not** designed to do:

- *Handle extremely large databases or very complex logging models.*
  If you're planning on logging billions of rows, this library is probably not going to give you the performance your need.
  You'll need a specialized database model.
  Likewise for very complex event data that you want to analyze directly through database queries.

- *Come bundled with preset event types*.
  The basic, user-specified events can be associated with arbitrary JSON data.
  Papika prescribes no built-in event types, leaving the choice entirely up to the application.
  This also implies that there is no built-in event-specific analysis code.

- *Behave as an application server*.
  This system is designed an intended only for data analysis done after the application is run.
  Though it supports very limited storing of user data to support experiments, it is designed only for telemetry.
  Your application should not depend on Papika for correct application behavior.

### Server

The server requires Python3.5+ with Flask and SQLAlchemy, and PostgreSQL.
It runs on several Linux distros and Windows (and probably Mac, though this is untested).
See [server/README.md](server/README.md) for more detailed instructions.

### Clients

See the respective readmes in the client folders for detailed instructions on how to use each client.

