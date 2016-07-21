
var gulp = require('gulp');
var ts = require('gulp-typescript');
var grs = require('git-rev-sync');
var replace = require('gulp-replace');
// override typescript version, we need to use 2.0+
var tsProject = ts.createProject("tsconfig.json", {typescript: require('typescript')});

var version = grs.long();

gulp.task('default', [], function() {
    return gulp.src(['papika.ts'])
        .pipe(replace('UNKNOWN_REVISION_ID', version))
        .pipe(ts(tsProject))
        .js.pipe(gulp.dest('.'));
});

