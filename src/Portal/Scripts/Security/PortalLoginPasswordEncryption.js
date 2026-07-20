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

    function encryptPasswordFields(fieldPairs, keyUrl, messageElementId) {
        var failureMessage = "密码加密准备失败，请刷新页面后重试。";
        var pairs = [];
        var index;
        var hasPlainValue = false;

        if (!fieldPairs || !fieldPairs.length) {
            setMessage(messageElementId, failureMessage);
            return false;
        }

        for (index = 0; index < fieldPairs.length; index++) {
            var pair = fieldPairs[index];
            var passwordElement = getById(pair.passwordElementId);
            var encryptedElement = getById(pair.encryptedElementId);

            if (!passwordElement || !encryptedElement) {
                setMessage(messageElementId, failureMessage);
                return false;
            }

            encryptedElement.value = "";
            pairs[pairs.length] = {
                passwordElement: passwordElement,
                encryptedElement: encryptedElement,
                value: passwordElement.value || ""
            };

            if (passwordElement.value) {
                hasPlainValue = true;
            }
        }

        if (!hasPlainValue) {
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

        for (index = 0; index < pairs.length; index++) {
            pairs[index].encryptedElement.value = pairs[index].encryptedValue;
            pairs[index].passwordElement.value = "";
        }

        return true;
    }

    function encryptPassword(passwordElementId, encryptedElementId, keyUrl, messageElementId) {
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

    window.PortalLoginPasswordEncryption = {
        encryptPassword: encryptPassword,
        encryptPasswordFields: encryptPasswordFields
    };
})(window, document);
