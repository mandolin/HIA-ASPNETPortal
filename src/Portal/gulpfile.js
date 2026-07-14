/// <binding ProjectOpened='startWatch' />

/* eslint-disable no-undef */

/**
 * Portal front-end build tasks.
 *
 * @module portal-build
 * @lang zh-CN 门户前端资源构建任务。
 * @lang en Portal front-end build tasks.
 */

import gulp from 'gulp';
import changed from 'gulp-changed';
import gulpCoffee from 'gulp-coffee';
import gulpUglify from 'gulp-uglify';
import gulpRename from 'gulp-rename';
import sourcemaps from 'gulp-sourcemaps';
import cleanCSS from 'gulp-clean-css';
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

/**
 * Creates the PostCSS processor chain shared by Sass asset builds.
 *
 * @function postcssProcessors
 * @returns {Function[]} PostCSS processors used to add browser prefixes and minimize CSS.
 * @lang zh-CN 创建 Sass 资源构建共用的 PostCSS 处理器链，负责补全浏览器前缀并压缩 CSS。
 * @lang en Creates the PostCSS processor chain shared by Sass asset builds.
 */
const postcssProcessors = () => [
    autoprefixer(),
    cssnano()
];

/**
 * Builds ES module source files from `*.src.js` to minimized JavaScript with source maps.
 *
 * @function esjs
 * @returns {NodeJS.ReadWriteStream} Gulp stream for the ES module build.
 * @lang zh-CN 构建 `*.src.js` ES 模块源码，输出压缩后的 JavaScript 及 source map。
 * @lang en Builds ES module source files from `*.src.js` to minimized JavaScript with source maps.
 */
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

/**
 * Builds CoffeeScript source files to minimized JavaScript with source maps.
 *
 * @function coffeejs
 * @returns {NodeJS.ReadWriteStream} Gulp stream for the CoffeeScript build.
 * @lang zh-CN 构建 CoffeeScript 源文件，输出压缩后的 JavaScript 及 source map。
 * @lang en Builds CoffeeScript source files to minimized JavaScript with source maps.
 */
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

/**
 * Builds Sass and SCSS source files to prefixed and minimized CSS with source maps.
 *
 * @function sasscss
 * @returns {NodeJS.ReadWriteStream} Gulp stream for the Sass and SCSS build.
 * @lang zh-CN 构建 Sass 与 SCSS 源文件，输出补全前缀、压缩后的 CSS 及 source map。
 * @lang en Builds Sass and SCSS source files to prefixed and minimized CSS with source maps.
 */
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
let watcherSignWatcher = null;

/**
 * Stops every active Gulp watcher and clears its in-memory handle.
 *
 * @function closeWatchers
 * @returns {void}
 * @lang zh-CN 停止全部活动的 Gulp watcher，并清空对应的内存句柄。
 * @lang en Stops every active Gulp watcher and clears its in-memory handle.
 */
const closeWatchers = () => {
    if (esWatcher) {
        esWatcher.close();
        esWatcher = null;
    }

    if (coffeeWatcher) {
        coffeeWatcher.close();
        coffeeWatcher = null;
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

/**
 * Starts the Visual Studio Task Runner watchers without performing an initial asset build.
 *
 * @function startWatch
 * @param {Function} cb Gulp completion callback.
 * @returns {void}
 * @lang zh-CN 启动 Visual Studio Task Runner 使用的 watcher，不执行首次资源构建。
 * @lang en Starts the Visual Studio Task Runner watchers without performing an initial asset build.
 */
const startWatch = (cb) => {
    if (!esWatcher) {
        esWatcher = gulp.watch('js/**/*.src.js', { ignoreInitial: true, delay: 500 });
        esWatcher.on('all', esjs);
    }

    if (!coffeeWatcher) {
        coffeeWatcher = gulp.watch('js/**/*.coffee', { ignoreInitial: true, delay: 500 });
        coffeeWatcher.on('all', coffeejs);
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

/**
 * Writes the watcher signal file so the active Visual Studio watcher process exits gracefully.
 *
 * @function stopWatch
 * @param {Function} cb Gulp completion callback.
 * @returns {void}
 * @lang zh-CN 写入 watcher 信号文件，使活动的 Visual Studio watcher 进程正常退出。
 * @lang en Writes the watcher signal file so the active Visual Studio watcher process exits gracefully.
 */
const stopWatch = (cb) => {
    const filePath = path.join(__dirname, 'Gulp/gulp-watcher-sign.cfg');
    const dataToWrite = { Date: new Date().getUTCSeconds() };

    fs.writeFileSync(filePath, JSON.stringify(dataToWrite), 'utf8');
    cb();
};

/**
 * One-time asset build task for VSCode and AI automation; it does not change the Visual Studio `startWatch` binding.
 *
 * @type {Function}
 * @lang zh-CN 供 VSCode 与 AI 自动化使用的一次性资源构建任务，不改变 Visual Studio 的 `startWatch` 绑定。
 * @lang en One-time asset build task for VSCode and AI automation; it does not change the Visual Studio `startWatch` binding.
 */
const assetsBuild = gulp.parallel(esjs, coffeejs, sasscss);
gulp.task('assets:build', assetsBuild);

export { startWatch, stopWatch };
