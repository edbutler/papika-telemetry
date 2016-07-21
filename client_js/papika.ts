/**!
 * Papika telemetry client library.
 * Copyright 2015-2016 Eric Butler.
 * Revision Id: UNKNOWN_REVISION_ID
 */
namespace papika {
    export type Uuid = string;

    export interface EventArgs {
        category: number;
        type: number;
        detail: any;
    }
    export interface TaskStartArgs extends EventArgs {
        group: Uuid;
    }

    // internally we stick a few more data fields on events, this is to make the typechecker happy
    interface EventArgsInternal extends EventArgs {
        task_event?: any;
        task_start?: any;
    }

    export interface TelemetryClient {
        log_session(args: { user: Uuid, detail: any }): Promise<Uuid>;
        query_user_id(args: { username: string }): Promise<Uuid>;
        query_experimental_condition(args: { user: Uuid, experiment: Uuid }): Promise<number>;
        query_user_data(args: { user: Uuid }): Promise<any>;
        save_user_data(args: { user: Uuid, savedata: any }): Promise<boolean>;
        log_event(args: EventArgs, do_create_promise?: boolean): Promise<any> | void;
        start_task(args: TaskStartArgs): TaskLogger;
    }

    export interface TaskLogger {
        log_event(args: EventArgs): void;
    }

    const PROTOCOL_VESRION = 2;
    const REVISION_ID = 'UNKNOWN_REVISION_ID';

    const uuid_regex = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
    export function is_uuid(str) {
        return uuid_regex.test(str);
    }

    function is_short(x) {
        return x === +x && x === (x|0) && x >= 0 && x <= 32767;
    }

    function send_post_request(url:string, params) {
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

    function send_nonsession_request(url:string, data, release_id:Uuid, release_key) {
        let sdata = JSON.stringify(data);
        return send_post_request(url, {
            version: PROTOCOL_VESRION,
            data: sdata,
            release: release_id,
            checksum: ''
        });
    }

    function send_session_request(url:string, data, session_id:Uuid, session_key) {
        let sdata = JSON.stringify(data);
        return send_post_request(url, {
            version: PROTOCOL_VESRION,
            data: sdata,
            session: session_id,
            checksum: ''
        });
    }

    function query_user_id(baseUri, username, release_id, release_key) {
        let data = {
            username: username
        };
        return send_nonsession_request(baseUri + '/api/user', data, release_id, release_key);
    }

    function query_experimental_condition(baseUri, args, release_id, release_key) {
        let data = {
            user_id: args.user,
            experiment_id: args.experiment
        };
        return send_nonsession_request(baseUri + '/api/experiment', data, release_id, release_key);
    }

    function query_user_data(baseUri, args, release_id, release_key) {
        let data = {
            id: args.user,
        };
        return send_nonsession_request(baseUri + '/api/user/get_data', data, release_id, release_key);
    };

    function save_user_data(baseUri, args, release_id, release_key) {
        let data = {
            id: args.user,
            savedata: JSON.stringify(args.savedata)
        };
        return send_nonsession_request(baseUri + '/api/user/set_data', data, release_id, release_key);
    };

    function log_session(baseUri, args, release_id, release_key) {
        let data = {
            user_id: args.user,
            release_id: release_id,
            client_time: new Date().toISOString(),
            detail: JSON.stringify(args.detail),
            library_revid: REVISION_ID,
        };
        return send_nonsession_request(baseUri + '/api/session', data, release_id, release_key);
    }

    function log_events(baseUri, to_log, session_id, session_key) {
        let events = to_log.map(function(e) { return e.event; });
        return send_session_request(baseUri + '/api/event', events, session_id, session_key);
    }

    export function TelemetryClient(baseUri:string, release_id:Uuid, release_key:string): TelemetryClient {
        if (!is_uuid(release_id)) throw Error('release id is not a uuid!');
        if (typeof release_key !== 'string') throw Error('release key is not a string!');
        if (typeof baseUri !== 'string') throw Error('baseUri is not a string!');

        let self = {} as TelemetryClient;

        let session_sequence_counter = 1;
        let task_id_counter = 1;
        let p_session_id = undefined;
        let p_event_log = new Promise(function(resolve){resolve();});
        let event_log_lock = false;
        let events_to_log = [];

        function flush_event_log() {
            // block until the current operation has finished
            p_event_log.then(function() {
                // if something else was already waiting to send events (and beat us) then give up
                if (event_log_lock || events_to_log.length === 0) return;
                // else stop anything else waiting to send events
                event_log_lock = true;

                p_event_log = p_session_id.then(function(session) {
                    let log_to = events_to_log.length;

                    return log_events(baseUri, events_to_log, session.session_id, session.session_key).then(function() {
                        // success! throw out the events we successfully logged, after resolving any promises
                        events_to_log.splice(0, log_to).forEach(function(e) {
                            if (e.resolve) { e.resolve(); }
                        });
                        event_log_lock = false;
                    }, function() {
                        // error! end the promise anyway, but keep the failed events around
                        event_log_lock = false;
                    });
                });
            });
        }

        self.log_session = function(args) {
            if (p_session_id) throw Error('session already logged!');
            if (!args) throw Error("no session data!");
            if (!is_uuid(args.user)) throw Error("bad/missing session user id!");
            if (typeof args.detail === 'undefined') throw Error("bad/missing session detail object!");

            p_session_id = log_session(baseUri, args, release_id, release_key);
            return p_session_id;
        };

        self.query_user_id = function(args) {
            if (typeof args.username !== 'string') throw Error('bad/missing username!');
            return query_user_id(baseUri, args.username, release_id, release_key).then(function(result) {
                return result.user_id;
            });
        };

        self.query_experimental_condition = function(args) {
            if (!is_uuid(args.user)) throw Error('bad/missing user id!');
            if (!is_uuid(args.experiment)) throw Error('bad/missing experiment id!');
            return query_experimental_condition(baseUri, args, release_id, release_key).then(function(result) {
                return result.condition;
            });
        };

        self.query_user_data = function(args) {
            if (!is_uuid(args.user)) throw Error('bad/missing user id!');
            return query_user_data(baseUri, args, release_id, release_key).then(function(result) {
                return result.savedata;
            });
        };

        self.save_user_data = function(args) {
            if (!is_uuid(args.user)) throw Error('bad/missing user id!');
            if (typeof args.savedata === 'undefined') throw Error('bad/missing savedata');
            return save_user_data(baseUri, args, release_id, release_key).then(function(result) {
                return true;
            });
        };

        self.log_event = function(args:EventArgsInternal, do_create_promise:boolean) {
            // TODO add some argument checking and error handling
            if (!p_session_id) throw Error('session not yet logged!');
            if (!is_short(args.category)) throw Error('bad/missing category!');
            if (!is_short(args.type)) throw Error('bad/missing type!');
            if (typeof args.detail === 'undefined') throw Error("bad/missing session detail object!");
            let detail = JSON.stringify(args.detail);

            let data:any = {
                category_id: args.category,
                type_id: args.type,
                session_sequence_index: session_sequence_counter,
                client_time: new Date().toISOString(),
                detail: detail,
            };
            if (args.task_start) data.task_start = args.task_start;
            if (args.task_event) data.task_event = args.task_event;

            let to_log = {event:data};

            let promise;
            if (do_create_promise) {
                promise = new Promise(function(resolve, reject) {
                    (to_log as any).resolve = resolve;
                });
            }

            events_to_log.push(to_log);
            session_sequence_counter += 1;

            // TODO maybe wait and batch flushes (with a timeout if it doesn't fill up)
            flush_event_log();
            return promise;
        };

        self.start_task = function(args) {
            if (!is_uuid(args.group)) throw Error('bad/missing group!');

            let task_id = task_id_counter;
            task_id_counter += 1;
            let task_sequence_counter = 1;

            (args as EventArgsInternal).task_start = {
                task_id: task_id,
                group_id: args.group
            };
            self.log_event(args);

            return {
                log_event: function(args) {
                    (args as any).task_event = {
                        task_id: task_id,
                        task_sequence_index: task_sequence_counter
                    };
                    task_sequence_counter += 1;
                    self.log_event(args);
                }
            };
        };

        return self;
    }
}
