/**
 * Login-password pre-submit encryption helper for the legacy Portal login flow.
 *
 * @module portal-login-password-encryption
 * @lang zh-CN 登录密码提交前加密助手；保留 IE6 级语法兼容，并只向页面暴露最小的加密入口。
 * @lang en Login-password pre-submit encryption helper; it keeps IE6-level syntax compatibility and exposes only the minimal encryption entry points to the page.
 */
(function (window, document) {
    // <lang>
    //   <zh-CN>旧浏览器可能没有 console；此兼容对象只吸收诊断调用，不改变加密或提交结果。</zh-CN>
    //   <en>Legacy browsers may not provide console; this compatibility object only absorbs diagnostics and does not change encryption or submission results.</en>
    // </lang>
    if (!window.console) {
        window.console = {
            error: function () { },
            log: function () { }
        };
    }

    /**
     * Resolves a page element by its DOM id.
     *
     * @function getById
     * @param {string} id DOM id supplied by the page integration.
     * @returns {HTMLElement|null} The matching element, or null when the page does not contain it.
     * @lang zh-CN 按页面集成提供的 DOM id 查找元素；找不到时返回 null，不创建或修改元素。
     * @lang en Resolves the element identified by the page integration's DOM id; returns null when it is absent and never creates or mutates an element.
     */
    function getById(id) {
        // <lang>
        //   <zh-CN>查询保持在调用方文档上下文内，缺失元素由上层流程决定是否报告为准备失败。</zh-CN>
        //   <en>Keep the lookup within the caller's document context; the higher-level flow decides whether a missing element is a preparation failure.</en>
        // </lang>
        return document.getElementById(id);
    }

    /**
     * Writes a fixed login-preparation message into the page message element.
     *
     * @function setMessage
     * @param {string} messageElementId DOM id of the page-owned message element.
     * @param {string} text Message text selected by this helper.
     * @returns {void}
     * @lang zh-CN 将本助手选择的固定提示写入页面消息元素；不承担通用 HTML 净化或错误详情展示。
     * @lang en Writes the fixed message selected by this helper into the page message element; it is not a general HTML sanitizer or error-detail renderer.
     */
    function setMessage(messageElementId, text) {
        // <lang>
        //   <zh-CN>消息元素是可选的；登录页缺少该元素时仍保持原有布尔失败语义，不因提示展示失败而抛出异常。</zh-CN>
        //   <en>The message element is optional; when the login page omits it, the existing Boolean failure semantics remain and message rendering does not throw.</en>
        // </lang>
        // <lang>
        //   <zh-CN>保存页面消息节点，用于避免重复查找并限定后续副作用范围。</zh-CN>
        //   <en>Resolve the page message node once to avoid repeated lookup and limit the scope of the following side effect.</en>
        // </lang>
        var messageElement = getById(messageElementId);
        if (messageElement) {
            // <lang>
            //   <zh-CN>提示文本由本文件的固定失败消息调用；这里只更新可见提示，不改变隐藏字段或密码值。</zh-CN>
            //   <en>The caller supplies this file's fixed failure message; update only the visible hint without changing hidden fields or password values.</en>
            // </lang>
            messageElement.innerHTML = text;
        }
    }

    /**
     * Creates an XMLHttpRequest-compatible object for the current browser.
     *
     * @function createRequest
     * @returns {XMLHttpRequest|null} A native or ActiveX request object, or null when the browser exposes neither API.
     * @lang zh-CN 按现代接口、Msxml2.XMLHTTP、Microsoft.XMLHTTP 顺序创建请求对象；均不可用时返回 null。
     * @lang en Creates a request object in the order native API, Msxml2.XMLHTTP, then Microsoft.XMLHTTP; returns null when none is available.
     */
    function createRequest() {
        // <lang>
        //   <zh-CN>优先使用标准 XMLHttpRequest，避免在支持标准接口的浏览器中触发旧版 ActiveX 分支。</zh-CN>
        //   <en>Prefer the standard XMLHttpRequest so browsers with the native API never enter the legacy ActiveX branches.</en>
        // </lang>
        if (window.XMLHttpRequest) {
            return new window.XMLHttpRequest();
        }

        // <lang>
        //   <zh-CN>仅在旧 IE 缺少标准接口时尝试 ActiveX；失败不向页面泄露 COM 细节，而是继续走受控的 null 回退。</zh-CN>
        //   <en>Try ActiveX only for old IE without the standard API; do not expose COM details to the page and fall back to the controlled null result.</en>
        // </lang>
        if (window.ActiveXObject) {
            try {
                return new window.ActiveXObject("Msxml2.XMLHTTP");
            } catch (ignoreMsxml) {
                // <lang>
                //   <zh-CN>Msxml2 不可用时保留 Microsoft.XMLHTTP 兼容回退，异常本身不改变调用方的失败判定。</zh-CN>
                //   <en>Keep the Microsoft.XMLHTTP compatibility fallback when Msxml2 is unavailable; the exception itself does not alter the caller's failure decision.</en>
                // </lang>
                return new window.ActiveXObject("Microsoft.XMLHTTP");
            }
        }

        // <lang>
        //   <zh-CN>浏览器没有可用请求实现时返回 null，由公钥读取流程统一转为空响应。</zh-CN>
        //   <en>Return null when the browser exposes no request implementation; the public-key flow converts it into an empty response.</en>
        // </lang>
        return null;
    }

    /**
     * Fetches the public key text used for one synchronous encryption pass.
     *
     * @function requestPublicKey
     * @param {string} keyUrl Public-key endpoint selected by the server-rendered page.
     * @returns {string} Response text for a 2xx response, otherwise an empty string.
     * @lang zh-CN 以同步 GET 读取页面指定的公钥端点并加入时间戳避免缓存；本函数不验证 URL 来源，来源信任由页面集成负责。
     * @lang en Reads the page-selected public-key endpoint with a synchronous GET and cache-busting timestamp; URL-origin validation remains the responsibility of the page integration.
     */
    function requestPublicKey(keyUrl) {
        // <lang>
        //   <zh-CN>请求对象只在本次读取中使用；创建失败时不发起网络调用并立即返回空响应。</zh-CN>
        //   <en>Use the request object only for this read; when creation fails, make no network call and return an empty response immediately.</en>
        // </lang>
        var request = createRequest();
        if (!request) {
            return "";
        }

        // <lang>
        //   <zh-CN>根据现有查询串选择连接符，时间戳仅用于抑制缓存，不改变服务端公钥参数。</zh-CN>
        //   <en>Choose the separator from the existing query string; the timestamp only suppresses caching and does not change server-side key parameters.</en>
        // </lang>
        var separator = keyUrl.indexOf("?") >= 0 ? "&" : "?";
        // <lang>
        //   <zh-CN>同步 GET 保持旧登录提交链的顺序：调用方在加密前必须拿到同一轮公钥响应。</zh-CN>
        //   <en>The synchronous GET preserves the legacy login sequence: the caller must receive this public-key response before encryption.</en>
        // </lang>
        request.open("GET", keyUrl + separator + "t=" + new Date().getTime(), false);
        // <lang>
        //   <zh-CN>禁止中间缓存返回过期公钥；请求头不携带密码或其他凭据。</zh-CN>
        //   <en>Prevent intermediary caches from returning an expired public key; no password or other credential is sent in this request header.</en>
        // </lang>
        request.setRequestHeader("Cache-Control", "no-cache");
        request.send(null);

        // <lang>
        //   <zh-CN>仅接受 2xx 响应正文；非成功状态或空正文返回空响应，底层 send 异常保持现有调用方异常路径。</zh-CN>
        //   <en>Accept only the response body from a 2xx status; non-success statuses or empty bodies return an empty response, while send exceptions remain on the existing caller exception path.</en>
        // </lang>
        if (request.status >= 200 && request.status < 300) {
            return request.responseText || "";
        }

        return "";
    }

    /**
     * Encrypts one or more page password fields and commits ciphertext only after the whole batch succeeds.
     *
     * @function encryptPasswordFields
     * @param {Array<{passwordElementId:string, encryptedElementId:string}>} fieldPairs Password/plaintext and ciphertext field id pairs.
     * @param {string} keyUrl Public-key endpoint selected by the server-rendered page.
     * @param {string} messageElementId DOM id for the optional preparation-failure message.
     * @returns {boolean} True when no plaintext is present or every plaintext value is encrypted and committed; false otherwise.
     * @lang zh-CN 校验成对字段、按需读取公钥并先完成整批加密，再一次性写入密文并清空明文；任一准备或加密失败都保留未提交的明文值供页面重试。
     * @lang en Validates field pairs, fetches the public key only when needed, encrypts the complete batch first, then commits ciphertext and clears plaintext; any preparation or encryption failure leaves uncommitted plaintext available for a page retry.
     */
    function encryptPasswordFields(fieldPairs, keyUrl, messageElementId) {
        // <lang>
        //   <zh-CN>失败提示使用固定文案，避免把请求、密钥或加密库细节写回页面。</zh-CN>
        //   <en>Use a fixed failure message so request, key, or library details never flow back into the page.</en>
        // </lang>
        var failureMessage = "密码加密准备失败，请刷新页面后重试。";
        // <lang>
        //   <zh-CN>保存已验证的元素和值快照；只有整批密文成功后才写回 DOM。</zh-CN>
        //   <en>Store validated element references and value snapshots; write back to the DOM only after the complete ciphertext batch succeeds.</en>
        // </lang>
        var pairs = [];
        // <lang>
        //   <zh-CN>循环索引保持 ES3/IE6 兼容，并在两个阶段循环中复用。</zh-CN>
        //   <en>Reuse this loop index across both processing passes to retain ES3/IE6 compatibility.</en>
        // </lang>
        var index;
        // <lang>
        //   <zh-CN>记录是否至少有一个非空明文，从而避免无必要的公钥请求。</zh-CN>
        //   <en>Track whether any non-empty plaintext exists so an unnecessary public-key request can be avoided.</en>
        // </lang>
        var hasPlainValue = false;

        // <lang>
        //   <zh-CN>没有字段对时立即失败；不发起网络请求，也不触碰页面密码值。</zh-CN>
        //   <en>Fail immediately when no field pairs are supplied; make no network request and do not touch page password values.</en>
        // </lang>
        if (!fieldPairs || !fieldPairs.length) {
            setMessage(messageElementId, failureMessage);
            return false;
        }

        // <lang>
        //   <zh-CN>解析每一对页面元素并清空旧密文，防止提交链继续使用上一轮残留值。</zh-CN>
        //   <en>Resolve each page-element pair and clear stale ciphertext before proceeding, preventing the submit flow from reusing a previous value.</en>
        // </lang>
        for (index = 0; index < fieldPairs.length; index++) {
            // <lang>
            //   <zh-CN>当前配置项只描述两个 DOM id；元素引用和快照随后进入本轮短生命周期集合。</zh-CN>
            //   <en>The current configuration item contains only two DOM ids; element references and snapshots enter this pass's short-lived collection.</en>
            // </lang>
            var pair = fieldPairs[index];
            // <lang>
            //   <zh-CN>读取明文与密文控件，缺失任一控件都不能安全完成提交转换。</zh-CN>
            //   <en>Resolve the plaintext and ciphertext controls; either missing control prevents a safe submit conversion.</en>
            // </lang>
            var passwordElement = getById(pair.passwordElementId);
            var encryptedElement = getById(pair.encryptedElementId);

            if (!passwordElement || !encryptedElement) {
                setMessage(messageElementId, failureMessage);
                return false;
            }

            // <lang>
            //   <zh-CN>先移除旧密文，再保存当前明文快照；此时尚未清空用户输入，失败可由页面重试。</zh-CN>
            //   <en>Remove stale ciphertext before snapshotting the current plaintext; the user's input remains available for retry at this point.</en>
            // </lang>
            encryptedElement.value = "";
            // <lang>
        //   <zh-CN>保存本轮需要更新的两个控件和明文副本，避免部分加密成功时提前清空明文或写入新密文。</zh-CN>
        //   <en>Save both controls and the plaintext copy needed for this pass so a partial encryption cannot clear plaintext or write new ciphertext early.</en>
            // </lang>
            pairs[pairs.length] = {
                passwordElement: passwordElement,
                encryptedElement: encryptedElement,
                value: passwordElement.value || ""
            };

            // <lang>
            //   <zh-CN>只要存在一个明文就需要同一公钥完成后续整批转换。</zh-CN>
            //   <en>Any plaintext value means the same public key is required for the complete batch conversion.</en>
            // </lang>
            if (passwordElement.value) {
                hasPlainValue = true;
            }
        }

        // <lang>
        //   <zh-CN>全部字段为空时保持成功，表示没有需要转换的凭据，也不触发公钥网络请求。</zh-CN>
        //   <en>Return success when every field is empty: there is no credential to convert and no public-key request is needed.</en>
        // </lang>
        if (!hasPlainValue) {
            return true;
        }

        // <lang>
        //   <zh-CN>缺少加密库时停止提交转换，并保留明文值供页面重试。</zh-CN>
        //   <en>Stop submit conversion when the encryption library is absent and leave plaintext available for a page retry.</en>
        // </lang>
        if (!window.JSEncrypt) {
            setMessage(messageElementId, failureMessage);
            return false;
        }

        // <lang>
        //   <zh-CN>读取并粗略确认 PEM 公钥响应；详细密钥格式和密码学运算交由 JSEncrypt 处理。</zh-CN>
        //   <en>Read and minimally identify the PEM public-key response; JSEncrypt remains responsible for key parsing and cryptographic operations.</en>
        // </lang>
        var publicKey = requestPublicKey(keyUrl);
        if (publicKey.indexOf("BEGIN PUBLIC KEY") < 0) {
            setMessage(messageElementId, failureMessage);
            return false;
        }

        // <lang>
        //   <zh-CN>为本轮字段集合创建独立加密器并加载服务端公钥，不把密钥或明文写入全局状态。</zh-CN>
        //   <en>Create a pass-local encryptor and load the server public key without placing the key or plaintext in global state.</en>
        // </lang>
        var encryptor = new window.JSEncrypt();
        encryptor.setPublicKey(publicKey);

        // <lang>
        //   <zh-CN>先为每个快照生成密文但不写回控件；这样任何一个失败都不会清空已成功项的明文。</zh-CN>
        //   <en>Generate ciphertext for every snapshot without writing controls yet, so one failure cannot clear plaintext for already successful items.</en>
        // </lang>
        for (index = 0; index < pairs.length; index++) {
            if (pairs[index].value) {
                pairs[index].encryptedValue = encryptor.encrypt(pairs[index].value);
                if (!pairs[index].encryptedValue) {
                    setMessage(messageElementId, failureMessage);
                    return false;
                }
            } else {
                pairs[index].encryptedValue = "";
            }
        }

        // <lang>
        //   <zh-CN>整批密文准备完成后才提交 DOM 变更：写入密文并立即清空对应明文输入。</zh-CN>
        //   <en>Commit DOM changes only after the complete ciphertext batch is ready: write ciphertext and immediately clear each matching plaintext input.</en>
        // </lang>
        for (index = 0; index < pairs.length; index++) {
            pairs[index].encryptedElement.value = pairs[index].encryptedValue;
            pairs[index].passwordElement.value = "";
        }

        return true;
    }

    /**
     * Adapts one password pair to the batch encryption routine.
     *
     * @function encryptPassword
     * @param {string} passwordElementId DOM id of the plaintext password input.
     * @param {string} encryptedElementId DOM id of the hidden ciphertext input.
     * @param {string} keyUrl Public-key endpoint selected by the server-rendered page.
     * @param {string} messageElementId DOM id for the optional preparation-failure message.
     * @returns {boolean} The same success/failure result returned by encryptPasswordFields.
     * @lang zh-CN 将单个密码控件对适配到批量流程，保持单字段入口与多字段入口使用同一失败和明文清理语义。
     * @lang en Adapts one password-control pair to the batch flow so single-field and multi-field entry points share the same failure and plaintext-clearing semantics.
     */
    function encryptPassword(passwordElementId, encryptedElementId, keyUrl, messageElementId) {
        // <lang>
        //   <zh-CN>构造单项配置并复用批量实现，避免两个入口产生不同的安全边界。</zh-CN>
        //   <en>Build a single-item configuration and reuse the batch implementation so the two entry points cannot diverge in security behavior.</en>
        // </lang>
        return encryptPasswordFields(
            [
                {
                    passwordElementId: passwordElementId,
                    encryptedElementId: encryptedElementId
                }
            ],
            keyUrl,
            messageElementId);
    }

    // <lang>
    //   <zh-CN>仅公开单字段和批量字段两个稳定入口；内部请求、DOM 查找和消息处理保持模块私有。</zh-CN>
    //   <en>Expose only the stable single-field and batch-field entry points; keep request, DOM lookup, and message handling private to the module.</en>
    // </lang>
    window.PortalLoginPasswordEncryption = {
        encryptPassword: encryptPassword,
        encryptPasswordFields: encryptPasswordFields
    };
})(window, document);
