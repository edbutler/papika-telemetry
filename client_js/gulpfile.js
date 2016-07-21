
var gulp = require('gulp');
var ts = require('gulp-typescript');
var grs = require('git-rev-sync');
var merge = require('merge-stream');
var replace = require('gulp-replace');
// override typescript version, we need to use 2.0+
var tsProject = ts.createProject("tsconfig.json", {typescript: require('typescript')});

var version = grs.long();

gulp.task('default', [], function() {
    var out = gulp.src(['papika.ts', 'typings/index.d.ts'])
        .pipe(replace('UNKNOWN_REVISION_ID', version))
        .pipe(ts(tsProject));
    var js = out.js.pipe(gulp.dest('.'));
    var dts = out.dts.pipe(gulp.dest('.'));
    return merge(js,dts);
});

