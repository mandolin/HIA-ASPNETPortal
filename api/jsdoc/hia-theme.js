"use strict";

(() => {
  const input = document.getElementById("hia-symbol-search");
  const dataElement = document.getElementById("hia-search-data");
  const i18nElement = document.getElementById("hia-i18n-data");
  const themeElement = document.getElementById("hia-theme-data");

  if (!input || !dataElement) {
    return;
  }

  let searchData = [];

  try {
    searchData = JSON.parse(dataElement.textContent || "[]");
  } catch (_error) {
    searchData = [];
  }

  const records = searchData.map((entry) => ({
    id: entry.id,
    text: [
      entry.kind,
      entry.name,
      entry.longname,
      entry.memberof,
      entry.summary,
      ...Object.values(entry.localizedSummaries || {})
    ].join(" ").toLowerCase()
  }));

  const links = Array.from(document.querySelectorAll(".nav-group a"));
  const groups = Array.from(document.querySelectorAll(".nav-group"));
  const articles = Array.from(document.querySelectorAll(".doclet"));

  function setVisible(element, visible) {
    element.hidden = !visible;
  }

  function update() {
    const query = input.value.trim().toLowerCase();
    const visibleIds = new Set(
      records
        .filter((record) => !query || record.text.includes(query))
        .map((record) => record.id)
    );

    for (const article of articles) {
      setVisible(article, visibleIds.has(article.id));
    }

    for (const link of links) {
      const targetId = decodeURIComponent((link.getAttribute("href") || "").replace(/^#/, ""));
      setVisible(link, visibleIds.has(targetId));
    }

    for (const group of groups) {
      const hasVisibleLink = Array.from(group.querySelectorAll("a")).some((link) => !link.hidden);
      setVisible(group, hasVisibleLink);
    }
  }

  input.addEventListener("input", update);

  let themeData = {};

  if (themeElement) {
    try {
      themeData = JSON.parse(themeElement.textContent || "{}");
    } catch (_error) {
      themeData = {};
    }
  }

  const codeFontFamilies = {
    cascadia: "\"Cascadia Code\", \"Cascadia Mono\", Consolas, monospace",
    consolas: "Consolas, \"Courier New\", monospace",
    mono: "ui-monospace, \"SFMono-Regular\", Menlo, Monaco, Consolas, monospace",
    system: "monospace"
  };
  const codeStorageKey = "hia-docs-code-display";
  const codeControls = document.querySelector("[data-hia-code-controls]");

  function clampNumber(value, fallback, min, max) {
    const number = Number(value);

    if (!Number.isFinite(number)) {
      return fallback;
    }

    return Math.min(max, Math.max(min, number));
  }

  function normalizeCodeSettings(value) {
    const source = value && typeof value === "object" ? value : {};
    const fontFamily = codeFontFamilies[source.fontFamily] ? source.fontFamily : "cascadia";

    return {
      fontFamily,
      fontSize: clampNumber(source.fontSize, 12, 10, 20),
      lineHeight: clampNumber(source.lineHeight, 1.55, 1.2, 2.2),
      tabSize: Math.round(clampNumber(source.tabSize, 2, 2, 8)),
      wrap: Boolean(source.wrap)
    };
  }

  function readStoredCodeSettings() {
    try {
      if (!window.localStorage) {
        return null;
      }

      const raw = window.localStorage.getItem(codeStorageKey);
      return raw ? JSON.parse(raw) : null;
    } catch (_error) {
      return null;
    }
  }

  function writeStoredCodeSettings(settings) {
    try {
      if (window.localStorage) {
        window.localStorage.setItem(codeStorageKey, JSON.stringify(settings));
      }
    } catch (_error) {
      // Ignore storage failures; code display controls still work for the page.
    }
  }

  function clearStoredCodeSettings() {
    try {
      if (window.localStorage) {
        window.localStorage.removeItem(codeStorageKey);
      }
    } catch (_error) {
      // Ignore storage failures.
    }
  }

  function applyCodeSettings(settings) {
    const normalized = normalizeCodeSettings(settings);
    const body = document.body;

    body.style.setProperty("--code-font-family", codeFontFamilies[normalized.fontFamily]);
    body.style.setProperty("--code-font-size", `${normalized.fontSize}px`);
    body.style.setProperty("--code-line-height", String(normalized.lineHeight));
    body.style.setProperty("--code-tab-size", String(normalized.tabSize));
    body.classList.toggle("hia-code-wrap", normalized.wrap);

    if (codeControls) {
      const fontFamilyInput = codeControls.querySelector("[data-hia-code-font-family]");
      const fontSizeInput = codeControls.querySelector("[data-hia-code-font-size]");
      const lineHeightInput = codeControls.querySelector("[data-hia-code-line-height]");
      const tabSizeInput = codeControls.querySelector("[data-hia-code-tab-size]");
      const wrapInput = codeControls.querySelector("[data-hia-code-wrap]");

      if (fontFamilyInput) {
        fontFamilyInput.value = normalized.fontFamily;
      }

      if (fontSizeInput) {
        fontSizeInput.value = String(normalized.fontSize);
      }

      if (lineHeightInput) {
        lineHeightInput.value = String(normalized.lineHeight);
      }

      if (tabSizeInput) {
        tabSizeInput.value = String(normalized.tabSize);
      }

      if (wrapInput) {
        wrapInput.checked = normalized.wrap;
      }
    }

    return normalized;
  }

  function initCodeControls() {
    if (!codeControls) {
      return;
    }

    const defaults = normalizeCodeSettings(themeData.code || {});
    const stored = readStoredCodeSettings();
    let current = applyCodeSettings(stored ? { ...defaults, ...stored } : defaults);

    function updateCodeSettings(next) {
      current = applyCodeSettings({ ...current, ...next });
      writeStoredCodeSettings(current);
    }

    const fontFamilyInput = codeControls.querySelector("[data-hia-code-font-family]");
    const fontSizeInput = codeControls.querySelector("[data-hia-code-font-size]");
    const lineHeightInput = codeControls.querySelector("[data-hia-code-line-height]");
    const tabSizeInput = codeControls.querySelector("[data-hia-code-tab-size]");
    const wrapInput = codeControls.querySelector("[data-hia-code-wrap]");
    const resetButton = codeControls.querySelector("[data-hia-code-reset]");

    if (fontFamilyInput) {
      fontFamilyInput.addEventListener("change", () => {
        updateCodeSettings({ fontFamily: fontFamilyInput.value });
      });
    }

    if (fontSizeInput) {
      fontSizeInput.addEventListener("input", () => {
        updateCodeSettings({ fontSize: fontSizeInput.value });
      });
    }

    if (lineHeightInput) {
      lineHeightInput.addEventListener("input", () => {
        updateCodeSettings({ lineHeight: lineHeightInput.value });
      });
    }

    if (tabSizeInput) {
      tabSizeInput.addEventListener("input", () => {
        updateCodeSettings({ tabSize: tabSizeInput.value });
      });
    }

    if (wrapInput) {
      wrapInput.addEventListener("change", () => {
        updateCodeSettings({ wrap: wrapInput.checked });
      });
    }

    if (resetButton) {
      resetButton.addEventListener("click", () => {
        clearStoredCodeSettings();
        current = applyCodeSettings(defaults);
      });
    }
  }

  initCodeControls();

  let i18nData = {};

  if (i18nElement) {
    try {
      i18nData = JSON.parse(i18nElement.textContent || "{}");
    } catch (_error) {
      i18nData = {};
    }
  }

  if (!i18nData.enabled || !Array.isArray(i18nData.locales) || !i18nData.locales.length) {
    return;
  }

  const localePageSelects = Array.from(document.querySelectorAll("[data-hia-locale-page-select]"));

  for (const select of localePageSelects) {
    select.addEventListener("change", () => {
      if (select.value) {
        window.location.href = select.value;
      }
    });
  }

  if (i18nData.runtimeSwitch === false) {
    return;
  }

  const localeControls = Array.from(document.querySelectorAll("[data-hia-locale-control]"));
  const localeSelects = Array.from(document.querySelectorAll("[data-hia-locale-select]"));
  const localizedBlocks = Array.from(document.querySelectorAll("[data-hia-locale]"));
  const storageKey = "hia-docs-locale";

  function readStoredLocale() {
    try {
      return window.localStorage ? window.localStorage.getItem(storageKey) : "";
    } catch (_error) {
      return "";
    }
  }

  function writeStoredLocale(locale) {
    try {
      if (window.localStorage) {
        window.localStorage.setItem(storageKey, locale);
      }
    } catch (_error) {
      // Ignore storage failures; language switching still works for the page.
    }
  }

  function getInitialLocale() {
    const stored = readStoredLocale();

    if (stored && i18nData.locales.includes(stored)) {
      return stored;
    }

    if (i18nData.locales.includes(i18nData.defaultLocale)) {
      return i18nData.defaultLocale;
    }

    return i18nData.locales[0];
  }

  function getLabels(locale) {
    const labels = i18nData.labels || {};
    const normalized = String(locale || "");
    const baseLocale = normalized.split("-")[0];

    return Object.assign(
      {},
      labels.en || {},
      labels[baseLocale] || {},
      labels[normalized] || {}
    );
  }

  function formatLabel(template, element) {
    return String(template || "").replace(/\{([a-zA-Z0-9_]+)\}/g, (_match, name) => {
      const value = element.getAttribute(`data-hia-label-${name}`);
      return value === null ? "" : value;
    });
  }

  function applyLabels(locale) {
    const labels = getLabels(locale);

    for (const element of document.querySelectorAll("[data-hia-label]")) {
      const key = element.getAttribute("data-hia-label");

      if (labels[key]) {
        element.textContent = formatLabel(labels[key], element);
      }
    }

    for (const element of document.querySelectorAll("[data-hia-label-aria]")) {
      const key = element.getAttribute("data-hia-label-aria");

      if (labels[key]) {
        element.setAttribute("aria-label", formatLabel(labels[key], element));
      }
    }

    for (const element of document.querySelectorAll("[data-hia-label-placeholder]")) {
      const key = element.getAttribute("data-hia-label-placeholder");

      if (labels[key]) {
        element.setAttribute("placeholder", formatLabel(labels[key], element));
      }
    }
  }

  function setLocale(locale) {
    if (!i18nData.locales.includes(locale)) {
      return;
    }

    document.documentElement.lang = locale;
    applyLabels(locale);

    writeStoredLocale(locale);

    for (const block of localizedBlocks) {
      setVisible(block, block.getAttribute("data-hia-locale") === locale);
    }

    for (const control of localeControls) {
      const active = control.getAttribute("data-hia-locale-control") === locale;
      control.classList.toggle("active", active);
      control.setAttribute("aria-pressed", active ? "true" : "false");
    }

    for (const select of localeSelects) {
      select.value = locale;
    }
  }

  for (const control of localeControls) {
    control.addEventListener("click", () => {
      setLocale(control.getAttribute("data-hia-locale-control"));
    });
  }

  for (const select of localeSelects) {
    select.addEventListener("change", () => {
      setLocale(select.value);
    });
  }

  setLocale(getInitialLocale());
})();
