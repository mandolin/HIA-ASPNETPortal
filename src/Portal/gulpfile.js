/// <binding ProjectOpened='startWatch' />

/* eslint-disable no-undef */

import gulp from 'gulp';
import changed from 'gulp-changed';
import gulpCoffee from 'gulp-coffee';
import gulpUglify from 'gulp-uglify';
import gulpRename from 'gulp-rename';
import sourcemaps from 'gulp-sourcemaps';
import cleanCSS from 'gulp-clean-css';
import stylus from 'gulp-stylus';
import poststylus from 'poststylus';
import postcss from 'gulp-postcss';
import gulpSass from 'gulp-sass';
import * as sass from 'sass';
import autoprefixer from 'autoprefixer';
import cssnano from 'cssnano';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import babel from 'gulp-babel';

// 当前文件使用 ESM，Node 中需要自行得到 __dirname。
const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const postcssProcessors = () => [
    autoprefixer(),
    cssnano()
];

// 编译 ES6 源文件：*.src.js -> *.js。
const esjs = () => {
    console.log('esjs begin');

    return gulp.src('js/**/*.src.js', { sourcemaps: true })
        .pipe(changed('js/', {
            extension: '.js',
            transformPath: (newPath) => {
                const targetPath = path.join(
                    path.dirname(newPath),
                    path.basename(newPath.replace(/\.src\.js$/, '.js'))
                );

                console.log('changed newPath', targetPath);
                return targetPath;
            }
        }))
        .pipe(sourcemaps.init())
        .pipe(babel({
            presets: ['@babel/env']
        }))
        .pipe(gulpUglify())
        .pipe(gulpRename((newPath) => {
            console.log('rename newPath', newPath);
            newPath.basename = newPath.basename.replace(/\.src$/, '');
            newPath.extname = '.js';
        }))
        .pipe(sourcemaps.write(''))
        .pipe(gulp.dest('js/'));
};

// 编译 CoffeeScript 源文件：*.coffee -> *.js。
const coffeejs = () => {
    return gulp.src('js/**/*.coffee', { sourcemaps: true })
        .pipe(changed('js/', { extension: '.js' }))
        .pipe(sourcemaps.init())
        .pipe(gulpCoffee({ bare: true }))
        .pipe(gulpUglify())
        .pipe(gulpRename((newPath) => {
            newPath.extname = '.js';
        }))
        .pipe(sourcemaps.write(''))
        .pipe(gulp.dest('js/'));
};

// 编译 Stylus 源文件：*.styl -> *.css。
const styluscss = () => {
    return gulp.src('css/**/*.styl', { sourcemaps: true })
        .pipe(changed('css/', { extension: '.css' }))
        .pipe(sourcemaps.init())
        .pipe(stylus({
            use: [
                poststylus(postcssProcessors())
            ]
        }))
        .pipe(cleanCSS())
        .pipe(sourcemaps.write(''))
        .pipe(gulp.dest('css/'));
};

// 编译 Sass/SCSS 源文件：*.sass、*.scss -> *.css。
const sasscss = () => {
    const sassDealer = gulpSass(sass);

    return gulp.src(['css/**/*.scss', 'css/**/*.sass'], { sourcemaps: true })
        .pipe(changed('css/', { extension: '.css' }))
        .pipe(sourcemaps.init())
        .pipe(sassDealer().on('error', sassDealer.logError))
        .pipe(postcss(postcssProcessors()))
        .pipe(cleanCSS())
        .pipe(sourcemaps.write(''))
        .pipe(gulp.dest('css/'));
};

let esWatcher = null;
let coffeeWatcher = null;
let sassWatcher = null;
let stylusWatcher = null;
let watcherSignWatcher = null;

const closeWatchers = () => {
    if (esWatcher) {
        esWatcher.close();
        esWatcher = null;
    }

    if (coffeeWatcher) {
        coffeeWatcher.close();
        coffeeWatcher = null;
    }

    if (stylusWatcher) {
        stylusWatcher.close();
        stylusWatcher = null;
    }

    if (sassWatcher) {
        sassWatcher.close();
        sassWatcher = null;
    }

    if (watcherSignWatcher) {
        watcherSignWatcher.close();
        watcherSignWatcher = null;
    }
};

// Visual Studio Task Runner 在打开项目时调用该任务。
const startWatch = (cb) => {
    if (!esWatcher) {
        esWatcher = gulp.watch('js/**/*.src.js', { ignoreInitial: true, delay: 500 });
        esWatcher.on('all', esjs);
    }

    if (!coffeeWatcher) {
        coffeeWatcher = gulp.watch('js/**/*.coffee', { ignoreInitial: true, delay: 500 });
        coffeeWatcher.on('all', coffeejs);
    }

    if (!stylusWatcher) {
        stylusWatcher = gulp.watch('css/**/*.styl', { ignoreInitial: true, delay: 500 });
        stylusWatcher.on('all', styluscss);
    }

    if (!sassWatcher) {
        sassWatcher = gulp.watch(['css/**/*.scss', 'css/**/*.sass'], { ignoreInitial: true, delay: 500 });
        sassWatcher.on('all', sasscss);
    }

    if (!watcherSignWatcher) {
        watcherSignWatcher = gulp.watch('Gulp/gulp-watcher-sign.cfg', { ignoreInitial: true });
        watcherSignWatcher.on('change', () => {
            closeWatchers();

            setTimeout(() => {
                process.exit();
            }, 1000);
        });
    }

    cb();
};

// 写入停止信号文件，让 startWatch 中的 watcher 收到通知后退出。
const stopWatch = (cb) => {
    const filePath = path.join(__dirname, 'Gulp/gulp-watcher-sign.cfg');
    const dataToWrite = { Date: new Date().getUTCSeconds() };

    fs.writeFileSync(filePath, JSON.stringify(dataToWrite), 'utf8');
    cb();
};

// VSCode / AI 自动化使用的一次性资源构建任务，不改变 VS 原 startWatch 绑定。
const assetsBuild = gulp.parallel(esjs, coffeejs, styluscss, sasscss);
gulp.task('assets:build', assetsBuild);

export { startWatch, stopWatch };
