/**!
 * Papika telemetry client library.
 * Copyright 2015 Eric Butler.
 * Revision Id: UNKNOWN_REVISION_ID
 */

if (typeof module !== 'undefined' && module.exports) {
    fetch = require('node-fetch');
}

var papika = function(){
    "use strict";
    var mdl = {};

    var PROTOCOL_VESRION = 1;
    var REVISION_ID = 'UNKNOWN_REVISION_ID';

    function send_post_request(url, params) {
        return fetch(url, {
            method: 'post',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(params)
        }).then(function(response) {
            if (response.status === 200) {
                return response.json();
            } else {
                throw new Error(response.status + ' ' + response.statusText);
            }
        });
    }

    function send_nonsession_request(url, data, release_id, release_key) {
        var sdata = JSON.stringify(data);
        return send_post_request(url, {
            version: PROTOCOL_VESRION,
            data: sdata,
            release: release_id,
            checksum: ''
        });
    }

    function send_session_request(url, data, session_id, session_key) {
        var sdata = JSON.stringify(data);
        return send_post_request(url, {
            version: PROTOCOL_VESRION,
            data: sdata,
            session: session_id,
            checksum: ''
        });
    }

    function query_user_id(baseUri, username, release_id, release_key) {
        var data = {
            username: username
        };
        return send_nonsession_request(baseUri + '/api/user', data, session_id, session_key)
            .then(function(result) {
                return result.user_id;
            });
    }

    function query_experimental_condition(baseUri, args) {
        var data = {
            user_id: args.user_id,
            experiment_id: args.experiment_id
        };


        return send_post_request(baseUri + '/api/experiment', params).then(function(result) {
            return result.user_id;
        });
    }

    function log_session(baseUri, args, release_id, release_key) {
        var data = {
            user_id: args.user,
            release_id: args.release,
            client_time: new Date().toISOString(),
            detail: JSON.stringify(args.detail),
            library_revid: REVISION_ID,
        };
        return send_nonsession_request(baseUri + '/api/session', data, release_id, release_key)
            .then(function(result) {
                return result.session_id;
            });
    }

    function log_events(baseUri, events, session_id, session_key) {
        return send_session_request(baseUri + '/api/event', events, session_id, session_key);
    }

    mdl.TelemetryClient = function(baseUri) {
        if (typeof baseUri !== 'string') throw Error('baseUri is not a string!');

        var self = {}; 

        var session_sequence_counter = 1;
        var p_session_id = undefined;
        var p_event_log = new Promise(function(resolve){resolve();});
        var event_log_lock = false;
        var events_to_log = [];

        self.log_session = function(args) {
            if (p_session_id) throw Error('session alread logged!');
            if (!args) throw Error("no session data!");
            if (typeof args.user !== 'string') throw Error("bad/missing session user id!");
            if (typeof args.release !== 'string') throw Error("bad/missing session release id!");
            if (typeof args.detail === 'undefined') throw Error("bad/missing session detail object!");

            p_session_id = log_session(baseUri, args, args.release, null);
            return p_session_id;
        };

        function flush_event_log() {
            // block until the current operation has finished
            p_event_log.then(function() {
                // if something else was already waiting to send events (and beat us) then give up
                if (event_log_lock || events_to_log.length === 0) return;
                // else stop anything else waiting to send events
                event_log_lock = true;

                p_event_log = p_session_id.then(function(session_id) {
                    var log_to = events_to_log.length;

                    return log_events(baseUri, events_to_log, session_id).then(function() {
                        // success! throw out the events we successfully logged
                        events_to_log.splice(0, log_to);
                        event_log_lock = false;
                    }, function() {
                        // error! end the promise anyway, but keep the failed events around
                        event_log_lock = false;
                    });
                });
            });
        }

        self.log_event = function(args) {
            // TODO add some argument checking and error handling
            if (!p_session_id) throw Error('session not yet logged!');
            if (typeof args.type !== 'number') throw Error('bad/missing type!');
            if (typeof args.detail === 'undefined') throw Error("bad/missing session detail object!");
            var detail = JSON.stringify(args.detail);

            events_to_log.push({
                type_id: args.type,
                session_sequence_index: session_sequence_counter,
                client_time: new Date().toISOString(),
                detail: detail,
            });
            session_sequence_counter += 1;

            // TODO maybe wait and batch flushes (with a timeout if it doesn't fill up)
            flush_event_log();
        }

        return self;
    };

    return mdl;
}();

if (typeof module !== 'undefined' && module.exports) {
    module.exports = papika;
}


