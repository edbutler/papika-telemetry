if (typeof module !== 'undefined' && module.exports) {
    fetch = require('node-fetch');
}

var papika = function(){
    "use strict";
    var mdl = {};

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
            data: JSON.stringify(data),
            release: args.release,
        };

        return send_post_request(baseUri + '/api/session', params).then(function(result) {
            return result.session_id;
        });
    }

    mdl.TelemetryClient = function(baseUri) {
        var self = {}; 
        self.log_session = function(args) {
            return log_session(baseUri, args);
        };

        return self;
    };

    return mdl;
}();

if (typeof module !== 'undefined' && module.exports) {
    module.exports = papika;
}


