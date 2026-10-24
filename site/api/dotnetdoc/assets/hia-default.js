(() => {
  document.documentElement.dataset.hiaTheme = "default";

  const skinIds = ["portal.classic","portal.graphite","portal.lumen"];
  const schemes = ["dark","light","system"];
  const skinControl = document.querySelector("[data-hia-skin-control]");
  const schemeControl = document.querySelector("[data-hia-scheme-control]");
  const skinStorageKey = "hia-doc.portal.skin";
  const schemeStorageKey = "hia-doc.portal.scheme";

  function readPreference(key) {
    try { return localStorage.getItem(key); } catch { return null; }
  }

  function writePreference(key, value) {
    try { localStorage.setItem(key, value); } catch { /* <lang><zh-CN>偏好持久化是可选增强。</zh-CN><en>Preference persistence is optional.</en></lang> */ }
  }

  function applyThemeSelection(skin, scheme, persist) {
    // <lang><zh-CN>未知存储值 fail closed 到构建期默认，不允许注入任意 data attribute。</zh-CN><en>Unknown stored values fail closed to build defaults and cannot inject arbitrary data attributes.</en></lang>
    const selectedSkin = skinIds.includes(skin) ? skin : "portal.classic";
    const selectedScheme = schemes.includes(scheme) ? scheme : "system";
    document.documentElement.dataset.hiaSkin = selectedSkin;
    document.documentElement.dataset.hiaScheme = selectedScheme;
    if (skinControl) skinControl.value = selectedSkin;
    if (schemeControl) schemeControl.value = selectedScheme;
    if (persist) {
      writePreference(skinStorageKey, selectedSkin);
      writePreference(schemeStorageKey, selectedScheme);
    }
  }

  // <lang><zh-CN>本地阅读偏好覆盖构建默认，但永不进入 profile、manifest 或 search index。</zh-CN><en>Local reading preferences override build defaults but never enter profiles, manifests, or search indexes.</en></lang>
  const initialSkin = readPreference(skinStorageKey) || document.documentElement.dataset.hiaSkin || "portal.classic";
  const initialScheme = readPreference(schemeStorageKey) || document.documentElement.dataset.hiaScheme || "system";
  applyThemeSelection(initialSkin, initialScheme, false);
  skinControl?.addEventListener("change", () => applyThemeSelection(skinControl.value, schemeControl?.value || initialScheme, true));
  schemeControl?.addEventListener("change", () => applyThemeSelection(skinControl?.value || initialSkin, schemeControl.value, true));

  const control = document.querySelector("[data-hia-locale-control]");
  const localizedBlocks = Array.from(document.querySelectorAll("[data-hia-locale]"));

  function applyLocale(locale) {
    document.documentElement.lang = locale;

    for (const block of localizedBlocks) {
      block.hidden = block.getAttribute("data-hia-locale") !== locale;
    }
  }

  if (control) {
    applyLocale(control.value);
    control.addEventListener("change", () => applyLocale(control.value));
  }
})();