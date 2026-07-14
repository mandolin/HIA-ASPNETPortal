"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

const repositoryRoot = path.resolve(__dirname, "../../..");
const outputDirectory = path.join(repositoryRoot, "temp", "documentation", "jsdoc");
const expectedFiles = [
  "index.html",
  "hia-metadata.json",
  "hia-integration.json"
].map((fileName) => path.join(outputDirectory, fileName));

for (const filePath of expectedFiles) {
  assert.equal(fs.existsSync(filePath), true, `${path.basename(filePath)} should exist`);
}

const html = fs.readFileSync(path.join(outputDirectory, "index.html"), "utf8");
const metadataText = fs.readFileSync(path.join(outputDirectory, "hia-metadata.json"), "utf8");
const integrationText = fs.readFileSync(path.join(outputDirectory, "hia-integration.json"), "utf8");
const integration = JSON.parse(integrationText);

assert.match(html, /Portal front-end build tasks|门户前端资源构建任务/);
assert.match(html, /供 VSCode 与 AI 自动化使用的一次性资源构建任务。|One-time asset build task for VSCode and AI automation/);
assert.match(html, /https:\/\/github\.com\/mandolin\/HIA-ASPNETPortal\/blob\/main\/src\/Portal\/gulpfile\.js/);
assert.equal(integration.contract, "hia-jsdoc-integration");
assert.equal(integration.contractVersion, "0.1.0");
assert.equal(integration.artifactKind, "hia-integration");
assert.equal(Array.isArray(integration.ir.nodes), true);
assert.equal(
  integration.ir.nodes.some((node) => node.kind === "module" && node.longname === "module:portal-build"),
  true
);

// 文档输出不得泄漏本机物理路径；源码片段中的变量名 `filePath` 不是路径元数据。
// Generated output must not expose local physical paths; a `filePath` variable in source snippets is not path metadata.
for (const [name, content] of [
  ["metadata", metadataText],
  ["integration", integrationText]
]) {
  assert.equal(content.includes(repositoryRoot), false, `${name} must not contain the repository path`);
  assert.equal(
    content.includes(repositoryRoot.split(path.sep).join("/")),
    false,
    `${name} must not contain the normalized repository path`
  );
}

console.log("HIA ASP.NET Portal JSDoc pilot verification passed.");
