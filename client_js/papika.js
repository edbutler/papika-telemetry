if (typeof module !== 'undefined' && module.exports) {
    fetch = require('node-fetch');
}

var papika = function(){
    "use strict";
    var mdl = {};

    var PROTOCOL_VESRION = 1;

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

    function log_session(baseUri, args) {
        if (!args) throw Error("no session data!");
        if (typeof args.user !== 'string') throw Error("bad/missing session user id!");
        if (typeof args.release !== 'string') throw Error("bad/missing session release id!");
        if (typeof args.detail !== 'object') throw Error("bad/missing session detail object!");

        var data = {
            player_id: args.user,
            release_id: args.release,
            client_time: new Date().toISOString(),
            detail: JSON.stringify(args.detail),
        };

        var params = {
            version: PROTOCOL_VESRION,
            data: data,
            release: args.release,
        };

        return send_post_request(baseUri + '/api/session', params).then(function(result) {
            return result.session_id;
        });
    }

    function log_events(baseUri, session_id, events) {
        var params = {
            version: PROTOCOL_VESRION,
            data: events,
            session: session_id,
        };

        return send_post_request(baseUri + '/api/event', params);
    }

    mdl.TelemetryClient = function(baseUri) {
        if (typeof baseUri !== 'string') throw Error('baseUri is not a string!');

        var self = {}; 

        var session_sequence_counter = 1;
        var p_session_id = undefined;
        var events_to_log = [];

        self.log_session = function(args) {
            if (p_session_id) throw Error('session alread logged!');
            p_session_id = log_session(baseUri, args);
            p_session_id.then(function (sid) {
                console.log('Success! Session id is ' + sid);
            }, function (err) {
                console.log('Error logging session: ' + err);
            });
        };

        function flush_event_log() {
            // FIXME need to not do this until the current operation has finished
            p_session_id.then(function(session_id) {
                log_events(baseUri, session_id, events_to_log);
            });
        }

        self.log_event = function(args) {
            // TODO add some argument checking and error handling

            if (!p_session_id) throw Error('session not yet logged!');

            events_to_log.push({
                type_id: args.type_id,
                session_sequence_index: session_sequence_counter,
                client_time: new Date().toISOString(),
                detail: JSON.stringify(args.detail),
            });
            session_sequence_counter += 1;

            // TODO wait until it's bigger
            flush_event_log();
        }

        return self;
    };

    return mdl;
}();

if (typeof module !== 'undefined' && module.exports) {
    module.exports = papika;
}


