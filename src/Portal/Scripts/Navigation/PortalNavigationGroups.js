/*
 * <lang>
 *   <zh-CN>主导航分组的渐进增强脚本：为 disclosure 分组按钮绑定展开/折叠、箭头符号、aria-expanded 同步，
 *   以及 Escape 关闭并把焦点交还分组按钮。服务端已把全部分组渲染为「默认展开」，
 *   因此脚本缺失、被禁用或加载失败时页签仍全部可达；本文件只做增强，不承担导航可达性。</zh-CN>
 *   <en>Progressive enhancement for main-navigation groups: binds expand/collapse, the caret symbol, aria-expanded
 *   synchronization, and Escape-to-close with focus returned to the group button. The server renders every group
 *   expanded by default, so all tabs remain reachable when this script is missing, disabled, or fails to load;
 *   this file is enhancement only and never owns navigation reachability.</en>
 * </lang>
 * <lang>
 *   <zh-CN>兼容性：仅使用 ES5 语法与 IE9 支持的 DOM API。不使用 classList（IE9 无）、
 *   querySelectorAll 的现代选择器、CSS 变量或任何框架；className 以空格包围的整词比较，
 *   避免子串误判。站点导航按 W3C WAI-ARIA APG disclosure 模式实现，不用 menu / menubar role。</zh-CN>
 *   <en>Compatibility: ES5 syntax and IE9-supported DOM APIs only. No classList (absent in IE9), modern
 *   querySelectorAll selectors, CSS variables, or frameworks; className is compared as a space-delimited whole word
 *   to avoid substring false positives. Site navigation follows the W3C WAI-ARIA APG disclosure pattern with
 *   no menu / menubar role.</en>
 * </lang>
 */
(function () {
    'use strict';

    var EXPANDED_CARET = '\u25be';
    var COLLAPSED_CARET = '\u25b8';
    var HIDDEN_CLASS = 'portal-nav-group-hidden';

    /**
     * <lang>
     *   <zh-CN>按整词判断元素是否带某个 class；IE9 没有 classList，故用空格包围后做 indexOf。</zh-CN>
     *   <en>Check for a whole-word class name; IE9 has no classList, so pad with spaces and use indexOf.</en>
     * </lang>
     */
    function hasClass(element, className) {
        if (!element || !element.className) {
            return false;
        }
        return (' ' + element.className + ' ').indexOf(' ' + className + ' ') >= 0;
    }

    /**
     * <lang>
     *   <zh-CN>按整词添加 class；重复添加不产生重复片段，避免 className 无限增长。</zh-CN>
     *   <en>Add a whole-word class; repeated calls never duplicate the token, so className cannot grow unbounded.</en>
     * </lang>
     */
    function addClass(element, className) {
        if (!element || hasClass(element, className)) {
            return;
        }
        element.className = element.className ? (element.className + ' ' + className) : className;
    }

    /**
     * <lang>
     *   <zh-CN>按整词移除 class，并把多余空白收敛为单空格；不改动其它 class。</zh-CN>
     *   <en>Remove a whole-word class and collapse extra whitespace to single spaces, leaving other classes untouched.</en>
     * </lang>
     */
    function removeClass(element, className) {
        if (!element || !element.className) {
            return;
        }
        var tokens = element.className.split(/\s+/);
        var kept = [];
        for (var i = 0; i < tokens.length; i++) {
            if (tokens[i] && tokens[i] !== className) {
                kept.push(tokens[i]);
            }
        }
        element.className = kept.join(' ');
    }

    /**
     * <lang>
     *   <zh-CN>在一个祖先下按标签名与整词 class 找子元素；不使用 querySelectorAll 的现代选择器。</zh-CN>
     *   <en>Find descendants by tag name and whole-word class without modern querySelectorAll selectors.</en>
     * </lang>
     */
    function findByClass(root, tagName, className) {
        var result = [];
        if (!root || !root.getElementsByTagName) {
            return result;
        }
        var candidates = root.getElementsByTagName(tagName);
        for (var i = 0; i < candidates.length; i++) {
            if (hasClass(candidates[i], className)) {
                result.push(candidates[i]);
            }
        }
        return result;
    }

    /**
     * <lang>
     *   <zh-CN>把分组设为展开或折叠：同步 aria-expanded、箭头符号与容器隐藏类。
     *   display:none 由主题层提供，折叠时组内链接自然移出键盘 Tab 序列，无需逐个改 tabindex。</zh-CN>
     *   <en>Set a group expanded or collapsed, synchronizing aria-expanded, the caret symbol and the container hidden class.
     *   The theme supplies display:none, so collapsed links leave the keyboard Tab sequence naturally without per-link tabindex edits.</en>
     * </lang>
     */
    function setExpanded(toggle, panel, expanded) {
        if (!toggle || !panel) {
            return;
        }
        toggle.setAttribute('aria-expanded', expanded ? 'true' : 'false');
        if (expanded) {
            removeClass(panel, HIDDEN_CLASS);
        } else {
            addClass(panel, HIDDEN_CLASS);
        }
        var carets = findByClass(toggle, 'span', 'portal-nav-group-caret');
        for (var i = 0; i < carets.length; i++) {
            carets[i].innerHTML = expanded ? EXPANDED_CARET : COLLAPSED_CARET;
        }
    }

    /**
     * <lang>
     *   <zh-CN>从 aria-controls 解析容器 id 并只接受同一文档里的元素；不拼接 DOM、不 eval 配置文本。</zh-CN>
     *   <en>Resolve the container id from aria-controls and accept only an element in the same document; no DOM
     *   concatenation and no eval of configuration text.</en>
     * </lang>
     */
    function resolvePanel(toggle) {
        var id = toggle.getAttribute('aria-controls');
        if (!id) {
            return null;
        }
        return document.getElementById(id);
    }

    /**
     * <lang>
     *   <zh-CN>向上找最近的分组容器；ES5 不用 closest（IE9 无），逐层比对整词 class。
     *   找不到时返回 null，调用方回退到组内容器。</zh-CN>
     *   <en>Walk upwards for the nearest group container; ES5 avoids closest (absent in IE9) and compares whole-word
     *   classes per level. A null result makes the caller fall back to the in-group container.</en>
     * </lang>
     */
    function findGroupContainer(toggle) {
        var node = toggle.parentNode;
        while (node && node !== document.body) {
            if (hasClass(node, 'portal-nav-group')) {
                return node;
            }
            node = node.parentNode;
        }
        return null;
    }

    /**
     * <lang>
     *   <zh-CN>绑定单个分组：点击切换状态；**整个分组容器**内按 Escape 折叠并把焦点交还按钮。
     *   键盘监听必须挂在同时包含按钮与组内链接的祖先上：若只挂在组内容器，焦点停在按钮时
     *   按下的 Escape 不会冒泡到它（二者是兄弟），键盘契约会出现「只有点进链接才生效」的缺口。
     *   事件用 attachEvent / addEventListener 双通道，覆盖 IE9 与现代浏览器；不使用内联 onclick。</zh-CN>
     *   <en>Bind one group: click toggles the state; Escape anywhere inside the **whole group container** collapses it
     *   and returns focus to the button. The key listener must sit on an ancestor containing both the button and the
     *   in-group links: attaching it only to the inner container would miss Escape pressed while focus stays on the
     *   button (they are siblings), leaving a keyboard-contract gap where Escape works only after entering a link.
     *   Events use both attachEvent and addEventListener to cover IE9 and modern browsers, with no inline onclick.</en>
     * </lang>
     */
    function bindGroup(toggle) {
        var panel = resolvePanel(toggle);
        if (!panel) {
            return;
        }

        // <lang>
        //   <zh-CN>分组容器是按钮与组内容器的共同祖先；标记结构意外变化时回退到组内容器，
        //   保证「至少能绑上」而不是静默丢失键盘能力。</zh-CN>
        //   <en>The group container is the common ancestor of the button and the inner container; if the markup shape
        //   changes unexpectedly, fall back to the inner container so keyboard support is never silently lost.</en>
        // </lang>
        var keydownHost = findGroupContainer(toggle) || panel;

        function toggleExpanded() {
            var expanded = toggle.getAttribute('aria-expanded') === 'true';
            setExpanded(toggle, panel, !expanded);
        }

        function onKeyDown(event) {
            var evt = event || window.event;
            if (evt.keyCode === 27 && toggle.getAttribute('aria-expanded') === 'true') {
                setExpanded(toggle, panel, false);
                if (toggle.focus) {
                    toggle.focus();
                }
            }
        }

        if (toggle.addEventListener) {
            toggle.addEventListener('click', toggleExpanded, false);
            keydownHost.addEventListener('keydown', onKeyDown, false);
        } else if (toggle.attachEvent) {
            toggle.attachEvent('onclick', toggleExpanded);
            keydownHost.attachEvent('onkeydown', onKeyDown);
        }
    }

    /**
     * <lang>
     *   <zh-CN>初始化时保持服务端输出的默认展开态，不主动折叠任何分组；只绑定交互。
     *   多次执行（部分刷新）用自定义标记去重，避免重复绑定导致点击一次切换两次。</zh-CN>
     *   <en>Initialization keeps the server-rendered default-expanded state and collapses nothing; it only binds
     *   interactions. A custom marker de-duplicates repeated initialization (partial refresh), preventing one click
     *   from toggling twice.</en>
     * </lang>
     */
    function init() {
        var toggles = findByClass(document, 'button', 'portal-nav-group-toggle');
        for (var i = 0; i < toggles.length; i++) {
            if (toggles[i].getAttribute('data-nav-group-bound') === 'true') {
                continue;
            }
            toggles[i].setAttribute('data-nav-group-bound', 'true');
            bindGroup(toggles[i]);
        }
    }

    // <lang>
    //   <zh-CN>脚本位于导航容器之后同步加载，此时目标 DOM 已解析；仍兼容 DOMContentLoaded 与 load 双通道，
    //   避免异步加载顺序变化时漏绑定。绑定失败也不改变任何分组的可见性（渐进增强的兜底）。</zh-CN>
    //   <en>The script loads synchronously after the navigation container, so target DOM is already parsed; DOMContentLoaded
    //   and load are both handled to cover changed async load orders. A binding failure never changes any group's
    //   visibility, which is the progressive-enhancement fallback.</en>
    // </lang>
    if (document.readyState === 'complete' || document.readyState === 'interactive') {
        init();
    } else if (document.addEventListener) {
        document.addEventListener('DOMContentLoaded', init, false);
        window.addEventListener('load', init, false);
    } else if (window.attachEvent) {
        window.attachEvent('onload', init);
    }
}());
