/// <binding ProjectOpened='startWatch' />

/* eslint-disable no-undef */

/**
 * Portal front-end build tasks.
 *
 * @module portal-build
 * @lang zh-CN 门户前端资源构建任务。
 * @lang en Portal front-end build tasks.
 */

// <lang>
//   <zh-CN>这些导入共同组成旧 Web Forms 门户的前端资产管线；保持显式依赖，避免 Visual Studio Task Runner 与 VSCode 自动化走不同实现。</zh-CN>
//   <en>These imports form the legacy Web Forms portal asset pipeline; keeping dependencies explicit prevents Visual Studio Task Runner and VSCode automation from using divergent implementations.</en>
// </lang>
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

// <lang>
//   <zh-CN>当前文件使用 ESM，Node 不再提供 CommonJS 的 `__filename`；这里从模块 URL 还原物理脚本路径。</zh-CN>
//   <en>This file uses ESM, so Node no longer provides CommonJS `__filename`; restore the physical script path from the module URL.</en>
// </lang>
const __filename = fileURLToPath(import.meta.url);

// <lang>
//   <zh-CN>`__dirname` 是 watcher 信号文件和相对资产目录的稳定根，生命周期限定在当前 Gulp 进程。</zh-CN>
//   <en>`__dirname` is the stable root for the watcher signal file and relative asset folders, scoped to the current Gulp process.</en>
// </lang>
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
    // <lang>
    //   <zh-CN>先补浏览器前缀再压缩，避免 cssnano 在最终输出阶段前移除仍需参与兼容转换的结构。</zh-CN>
    //   <en>Apply browser prefixes before minification so cssnano does not remove structures still needed for compatibility transforms.</en>
    // </lang>
    autoprefixer(),

    // <lang>
    //   <zh-CN>最后压缩 CSS，使 Visual Studio 和自动化任务得到相同的发布形态资源。</zh-CN>
    //   <en>Minify CSS last so Visual Studio and automation tasks produce the same release-shaped assets.</en>
    // </lang>
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
    // <lang>
    //   <zh-CN>保留低成本控制台标记，方便 Task Runner 与命令行区分 ES 模块构建是否真正触发。</zh-CN>
    //   <en>Keep a low-cost console marker so Task Runner and command-line runs can tell whether the ES module build actually started.</en>
    // </lang>
    console.log('esjs begin');

    // <lang>
    //   <zh-CN>以 `*.src.js` 作为唯一输入契约，输出同目录 `.js` 与 source map，避免自动化任务扫描或创建额外目录。</zh-CN>
    //   <en>Use `*.src.js` as the only input contract and emit adjacent `.js` files plus source maps without scanning or creating extra directories.</en>
    // </lang>
    return gulp.src('js/**/*.src.js', { sourcemaps: true })
        // <lang>
        //   <zh-CN>按目标 `.js` 文件判断是否变化，避免源文件后缀与输出后缀不同导致每次都重建。</zh-CN>
        //   <en>Compare against the target `.js` file so the source/output suffix difference does not force a rebuild every time.</en>
        // </lang>
        .pipe(changed('js/', {
            extension: '.js',
            transformPath: (newPath) => {
                // <lang>
                //   <zh-CN>`targetPath` 是 gulp-changed 用来定位已生成文件的相对输出路径，只在本次路径比较回调中有效。</zh-CN>
                //   <en>`targetPath` is the relative output path used by gulp-changed to locate generated files and is valid only for this path-comparison callback.</en>
                // </lang>
                const targetPath = path.join(
                    path.dirname(newPath),
                    path.basename(newPath.replace(/\.src\.js$/, '.js'))
                );

                // <lang>
                //   <zh-CN>输出受限路径诊断，不包含文件内容或凭据，便于定位增量构建命中情况。</zh-CN>
                //   <en>Emit restricted path diagnostics without file contents or credentials so incremental-build hits remain traceable.</en>
                // </lang>
                console.log('changed newPath', targetPath);
                return targetPath;
            }
        }))
        // <lang>
        //   <zh-CN>在 Babel 与压缩前初始化 source map，确保旧浏览器转译后的定位仍可回到源码。</zh-CN>
        //   <en>Initialize source maps before Babel and minification so transpiled legacy-browser output can still map back to sources.</en>
        // </lang>
        .pipe(sourcemaps.init())
        .pipe(babel({
            presets: ['@babel/env']
        }))
        .pipe(gulpUglify())
        .pipe(gulpRename((newPath) => {
            // <lang>
            //   <zh-CN>`newPath` 是 gulp-rename 提供的可变输出描述，仅在当前文件重命名回调中修改。</zh-CN>
            //   <en>`newPath` is the mutable output descriptor supplied by gulp-rename and is changed only within this per-file rename callback.</en>
            // </lang>
            console.log('rename newPath', newPath);

            // <lang>
            //   <zh-CN>去掉人工源码后缀 `.src`，保持运行时脚本名与历史页面引用兼容。</zh-CN>
            //   <en>Remove the authoring suffix `.src` so runtime script names remain compatible with legacy page references.</en>
            // </lang>
            newPath.basename = newPath.basename.replace(/\.src$/, '');

            // <lang>
            //   <zh-CN>强制输出扩展名为 `.js`，避免源后缀或中间管线泄露到发布资源名。</zh-CN>
            //   <en>Force the output extension to `.js` so source suffixes or intermediate pipeline state do not leak into release asset names.</en>
            // </lang>
            newPath.extname = '.js';
        }))
        // <lang>
        //   <zh-CN>source map 与压缩脚本写回同一资产树，保持旧门户静态文件部署模型不变。</zh-CN>
        //   <en>Write source maps beside the minified scripts in the same asset tree, preserving the legacy portal static-file deployment model.</en>
        // </lang>
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
    // <lang>
    //   <zh-CN>CoffeeScript 管线只处理现有 `.coffee` 源文件，并以相邻 `.js` 作为增量输出目标。</zh-CN>
    //   <en>The CoffeeScript pipeline processes only existing `.coffee` sources and uses adjacent `.js` files as incremental output targets.</en>
    // </lang>
    return gulp.src('js/**/*.coffee', { sourcemaps: true })
        .pipe(changed('js/', { extension: '.js' }))
        .pipe(sourcemaps.init())
        // <lang>
        //   <zh-CN>`bare: true` 保持旧脚本的全局/模块包装行为，不额外套 CoffeeScript 生成闭包。</zh-CN>
        //   <en>`bare: true` preserves legacy script global/module wrapping behavior instead of adding a CoffeeScript-generated closure.</en>
        // </lang>
        .pipe(gulpCoffee({ bare: true }))
        .pipe(gulpUglify())
        .pipe(gulpRename((newPath) => {
            // <lang>
            //   <zh-CN>无论输入后缀如何，运行时产物固定为 `.js`，匹配 Web Forms 页面和静态资源约定。</zh-CN>
            //   <en>Whatever the input suffix, runtime artifacts are fixed to `.js` to match Web Forms page and static-asset conventions.</en>
            // </lang>
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
    // <lang>
    //   <zh-CN>`sassDealer` 绑定当前 Dart Sass 实现，并提供同一个错误记录器给本次 Sass 管线。</zh-CN>
    //   <en>`sassDealer` binds the current Dart Sass implementation and supplies the same error logger for this Sass pipeline.</en>
    // </lang>
    const sassDealer = gulpSass(sass);

    // <lang>
    //   <zh-CN>Sass/SCSS 共用同一输出目录与 source map 策略，避免主题 CSS 构建在两种语法间分叉。</zh-CN>
    //   <en>Sass and SCSS share the same output folder and source-map strategy so theme CSS builds do not diverge by syntax.</en>
    // </lang>
    return gulp.src(['css/**/*.scss', 'css/**/*.sass'], { sourcemaps: true })
        .pipe(changed('css/', { extension: '.css' }))
        .pipe(sourcemaps.init())
        // <lang>
        //   <zh-CN>Sass 编译错误交给插件日志器，保持 watcher 进程可诊断而不是在第一处样式错误后静默退出。</zh-CN>
        //   <en>Sass compilation errors go through the plugin logger so the watcher remains diagnosable instead of silently exiting on the first style error.</en>
        // </lang>
        .pipe(sassDealer().on('error', sassDealer.logError))
        // <lang>
        //   <zh-CN>PostCSS 统一执行兼容前缀和压缩前处理，然后再交给 cleanCSS 做最终压缩。</zh-CN>
        //   <en>PostCSS performs shared compatibility prefixing and pre-minification processing before cleanCSS applies final compression.</en>
        // </lang>
        .pipe(postcss(postcssProcessors()))
        .pipe(cleanCSS())
        .pipe(sourcemaps.write(''))
        .pipe(gulp.dest('css/'));
};

// <lang>
//   <zh-CN>`esWatcher` 保存 ES 模块 watcher 的可关闭句柄；null 表示当前进程尚未启动该 watcher。</zh-CN>
//   <en>`esWatcher` stores the closable ES module watcher handle; null means this process has not started that watcher.</en>
// </lang>
let esWatcher = null;

// <lang>
//   <zh-CN>`coffeeWatcher` 保存 CoffeeScript watcher 句柄，避免重复注册同一输入 glob。</zh-CN>
//   <en>`coffeeWatcher` stores the CoffeeScript watcher handle so the same input glob is not registered repeatedly.</en>
// </lang>
let coffeeWatcher = null;

// <lang>
//   <zh-CN>`sassWatcher` 保存 Sass/SCSS watcher 句柄，生命周期与当前 Visual Studio Task Runner 进程一致。</zh-CN>
//   <en>`sassWatcher` stores the Sass/SCSS watcher handle with the same lifetime as the current Visual Studio Task Runner process.</en>
// </lang>
let sassWatcher = null;

// <lang>
//   <zh-CN>`watcherSignWatcher` 监听退出信号文件，用于让外部任务请求 watcher 进程自愿退出。</zh-CN>
//   <en>`watcherSignWatcher` watches the exit-signal file so external tasks can ask the watcher process to exit voluntarily.</en>
// </lang>
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
    // <lang>
    //   <zh-CN>关闭 ES watcher 后立即清空句柄，确保后续 `startWatch` 能重新建立干净监听。</zh-CN>
    //   <en>Clear the ES watcher handle immediately after closing it so a later `startWatch` can create a clean listener.</en>
    // </lang>
    if (esWatcher) {
        esWatcher.close();
        esWatcher = null;
    }

    // <lang>
    //   <zh-CN>关闭 CoffeeScript watcher 时保持同样的幂等模式，避免重复 close 已释放资源。</zh-CN>
    //   <en>Use the same idempotent pattern for the CoffeeScript watcher to avoid closing an already released resource.</en>
    // </lang>
    if (coffeeWatcher) {
        coffeeWatcher.close();
        coffeeWatcher = null;
    }

    // <lang>
    //   <zh-CN>关闭 Sass watcher 后释放模块级引用，防止长驻 Task Runner 进程继续持有文件系统监听。</zh-CN>
    //   <en>Release the module-level reference after closing the Sass watcher so the long-lived Task Runner process stops holding file-system listeners.</en>
    // </lang>
    if (sassWatcher) {
        sassWatcher.close();
        sassWatcher = null;
    }

    // <lang>
    //   <zh-CN>退出信号 watcher 也属于同一资源生命周期；重启 watcher 前必须先解除旧信号监听。</zh-CN>
    //   <en>The exit-signal watcher shares the same resource lifetime and must be removed before the watcher set is restarted.</en>
    // </lang>
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
    // <lang>
    //   <zh-CN>仅在句柄为空时注册 ES watcher，避免 ProjectOpened 重入造成重复构建回调。</zh-CN>
    //   <en>Register the ES watcher only when its handle is empty so ProjectOpened re-entry does not duplicate build callbacks.</en>
    // </lang>
    if (!esWatcher) {
        esWatcher = gulp.watch('js/**/*.src.js', { ignoreInitial: true, delay: 500 });
        esWatcher.on('all', esjs);
    }

    // <lang>
    //   <zh-CN>CoffeeScript watcher 同样跳过初始构建，只响应后续文件变化以保护 Visual Studio 打开项目的速度。</zh-CN>
    //   <en>The CoffeeScript watcher also skips the initial build and reacts only to later changes to preserve Visual Studio project-open speed.</en>
    // </lang>
    if (!coffeeWatcher) {
        coffeeWatcher = gulp.watch('js/**/*.coffee', { ignoreInitial: true, delay: 500 });
        coffeeWatcher.on('all', coffeejs);
    }

    // <lang>
    //   <zh-CN>Sass watcher 同时覆盖 `.scss` 与缩进式 `.sass`，保持两类历史样式源的热构建入口一致。</zh-CN>
    //   <en>The Sass watcher covers both `.scss` and indented `.sass` sources so both legacy style formats share one hot-build entry.</en>
    // </lang>
    if (!sassWatcher) {
        sassWatcher = gulp.watch(['css/**/*.scss', 'css/**/*.sass'], { ignoreInitial: true, delay: 500 });
        sassWatcher.on('all', sasscss);
    }

    // <lang>
    //   <zh-CN>信号 watcher 只在缺失时创建，避免多个退出监听同时响应同一次 stopWatch 写入。</zh-CN>
    //   <en>Create the signal watcher only when absent so multiple exit listeners do not respond to one stopWatch write.</en>
    // </lang>
    if (!watcherSignWatcher) {
        watcherSignWatcher = gulp.watch('Gulp/gulp-watcher-sign.cfg', { ignoreInitial: true });
        watcherSignWatcher.on('change', () => {
            // <lang>
            //   <zh-CN>收到信号后先释放所有 watcher，确保退出前不再接收新的资产变更事件。</zh-CN>
            //   <en>After receiving the signal, release all watchers first so no new asset-change events are accepted before exit.</en>
            // </lang>
            closeWatchers();

            // <lang>
            //   <zh-CN>延迟退出给文件系统 close 事件留出短窗口，降低 Task Runner 看到异常退出的概率。</zh-CN>
            //   <en>Delay process exit briefly so file-system close events can settle and Task Runner is less likely to observe an abrupt termination.</en>
            // </lang>
            setTimeout(() => {
                process.exit();
            }, 1000);
        });
    }

    // <lang>
    //   <zh-CN>所有 watcher 注册完成后通知 Gulp；此回调不表示任何资产已经被重新构建。</zh-CN>
    //   <en>Notify Gulp after all watchers are registered; this callback does not mean any asset has been rebuilt.</en>
    // </lang>
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
    // <lang>
    //   <zh-CN>`filePath` 指向受控信号文件，不从用户输入或网络输入派生。</zh-CN>
    //   <en>`filePath` points to the controlled signal file and is not derived from user or network input.</en>
    // </lang>
    const filePath = path.join(__dirname, 'Gulp/gulp-watcher-sign.cfg');

    // <lang>
    //   <zh-CN>`dataToWrite` 只包含低敏 UTC 秒值，用作变化触发器而非审计记录。</zh-CN>
    //   <en>`dataToWrite` contains only a low-sensitivity UTC seconds value used as a change trigger rather than an audit record.</en>
    // </lang>
    const dataToWrite = { Date: new Date().getUTCSeconds() };

    // <lang>
    //   <zh-CN>同步写入确保 stop task 返回前信号已落盘；文件内容不包含凭据或本机路径。</zh-CN>
    //   <en>Synchronous writing ensures the signal is on disk before the stop task returns; the file content contains no credentials or local paths.</en>
    // </lang>
    fs.writeFileSync(filePath, JSON.stringify(dataToWrite), 'utf8');

    // <lang>
    //   <zh-CN>通知 Gulp stop task 已完成；实际 watcher 退出由另一个进程收到文件变化后完成。</zh-CN>
    //   <en>Notify Gulp that the stop task is complete; the actual watcher exits after the other process receives the file change.</en>
    // </lang>
    cb();
};

/**
 * One-time asset build task for VSCode and AI automation. It processes only existing source globs, writes outputs beside
 * them, and does not create input directories or change the Visual Studio `startWatch` binding.
 *
 * @type {Function}
 * @lang zh-CN 供 VSCode 与 AI 自动化使用的一次性资源构建任务；仅处理已有输入并在相邻目录写入输出，
 * 不创建输入目录，也不改变 Visual Studio 的 `startWatch` 绑定。
 * @lang en One-time asset build task for VSCode and AI automation; it processes existing inputs and writes adjacent outputs
 * without creating input directories or changing the Visual Studio `startWatch` binding.
 */
// <lang>
//   <zh-CN>`assetsBuild` 聚合三个一次性构建管线，供自动化显式调用而不启动长驻 watcher。</zh-CN>
//   <en>`assetsBuild` aggregates the three one-time build pipelines for automation to call explicitly without starting long-lived watchers.</en>
// </lang>
const assetsBuild = gulp.parallel(esjs, coffeejs, sasscss);

// <lang>
//   <zh-CN>注册稳定任务名供 VSCode/AI 脚本使用，并与 Visual Studio 的 `ProjectOpened='startWatch'` 绑定分离。</zh-CN>
//   <en>Register the stable task name for VSCode/AI scripts while keeping it separate from Visual Studio's `ProjectOpened='startWatch'` binding.</en>
// </lang>
gulp.task('assets:build', assetsBuild);

// <lang>
//   <zh-CN>只导出 watcher 控制入口，保持外部脚本不能绕过本文件定义的资产构建契约。</zh-CN>
//   <en>Export only the watcher control entries so external scripts cannot bypass the asset-build contract defined in this file.</en>
// </lang>
export { startWatch, stopWatch };
