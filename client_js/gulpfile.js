
var gulp = require('gulp');
var ts = require("gulp-typescript");
// override typescript version, we need to use 2.0+
var tsProject = ts.createProject("tsconfig.json", {typescript: require('typescript')});

gulp.task('default', [], function() {
    return gulp.src(['papika.ts'])
        .pipe(ts(tsProject))
        .js.pipe(gulp.dest('.'));
});

