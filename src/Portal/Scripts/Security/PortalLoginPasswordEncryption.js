// 中文：登录密码提交前加密助手，保持 IE6 级语法兼容。
// English: Login-password pre-submit encryption helper with IE6-level syntax compatibility.
(function (window, document) {
    if (!window.console) {
        window.console = {
            error: function () { },
            log: function () { }
        };
    }

    function getById(id) {
        return document.getElementById(id);
    }

    function setMessage(messageElementId, text) {
        var messageElement = getById(messageElementId);
        if (messageElement) {
            messageElement.innerHTML = text;
        }
    }

    function createRequest() {
        if (window.XMLHttpRequest) {
            return new window.XMLHttpRequest();
        }

        if (window.ActiveXObject) {
            try {
                return new window.ActiveXObject("Msxml2.XMLHTTP");
            } catch (ignoreMsxml) {
                return new window.ActiveXObject("Microsoft.XMLHTTP");
            }
        }

        return null;
    }

    function requestPublicKey(keyUrl) {
        var request = createRequest();
        if (!request) {
            return "";
        }

        var separator = keyUrl.indexOf("?") >= 0 ? "&" : "?";
        request.open("GET", keyUrl + separator + "t=" + new Date().getTime(), false);
        request.setRequestHeader("Cache-Control", "no-cache");
        request.send(null);

        if (request.status >= 200 && request.status < 300) {
            return request.responseText || "";
        }

        return "";
    }

    function encryptPassword(passwordElementId, encryptedElementId, keyUrl, messageElementId) {
        var passwordElement = getById(passwordElementId);
        var encryptedElement = getById(encryptedElementId);
        var failureMessage = "密码加密准备失败，请刷新页面后重试。";

        if (!passwordElement || !encryptedElement) {
            setMessage(messageElementId, failureMessage);
            return false;
        }

        encryptedElement.value = "";
        if (!passwordElement.value) {
            return true;
        }

        if (!window.JSEncrypt) {
            setMessage(messageElementId, failureMessage);
            return false;
        }

        var publicKey = requestPublicKey(keyUrl);
        if (publicKey.indexOf("BEGIN PUBLIC KEY") < 0) {
            setMessage(messageElementId, failureMessage);
            return false;
        }

        var encryptor = new window.JSEncrypt();
        encryptor.setPublicKey(publicKey);

        var encryptedPassword = encryptor.encrypt(passwordElement.value);
        if (!encryptedPassword) {
            setMessage(messageElementId, failureMessage);
            return false;
        }

        encryptedElement.value = encryptedPassword;
        passwordElement.value = "";
        return true;
    }

    window.PortalLoginPasswordEncryption = {
        encryptPassword: encryptPassword
    };
})(window, document);
