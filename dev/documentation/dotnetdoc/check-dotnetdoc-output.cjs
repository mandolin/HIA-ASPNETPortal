#!/usr/bin/env node

const fs = require("node:fs");
const path = require("node:path");

const repositoryRoot = path.resolve(__dirname, "..", "..", "..");
const outputDirectory = path.join(repositoryRoot, "temp", "documentation", "dotnetdoc");
const resultPath = path.join(outputDirectory, "dotnetdoc.producer-result.json");

// <lang>
//   <zh-CN>本检查器只验证本机生成产物边界，不把 DotNetDoc 输出提升为公开发布物。</zh-CN>
//   <en>This checker validates local generated artifact boundaries only; it does not promote DotNetDoc output to public release artifacts.</en>
// </lang>
if (!fs.existsSync(resultPath)) {
  fail(`DotNetDoc result manifest was not found: ${toRelative(resultPath)}`);
}

const result = readJson(resultPath);
if (result.contract !== "documentation-producer-result") {
  fail("DotNetDoc result manifest contract is not documentation-producer-result.");
}

if (!["success", "partial"].includes(result.status)) {
  fail(`DotNetDoc result status is not acceptable: ${result.status}`);
}

const artifacts = Array.isArray(result.artifacts) ? result.artifacts : [];
if (artifacts.length === 0) {
  fail("DotNetDoc result does not contain any artifacts.");
}

const artifactKinds = new Set(artifacts.map((artifact) => artifact.kind));
if (!artifactKinds.has("dotnetdoc-extraction")) {
  fail("DotNetDoc result does not contain dotnetdoc-extraction artifacts.");
}

if (!artifactKinds.has("hia-document")) {
  fail("DotNetDoc result does not contain HIA document artifacts.");
}

// <lang>
//   <zh-CN>首轮 source relation 可以有 unresolved warning，但不能泄漏源码正文或本机绝对路径。</zh-CN>
//   <en>The first source-relation pass may contain unresolved warnings, but it must not leak source text or local absolute paths.</en>
// </lang>
const jsonFiles = listJsonFiles(outputDirectory);
for (const filePath of jsonFiles) {
  const text = fs.readFileSync(filePath, "utf8");
  if (text.includes("\"sourcesContent\"")) {
    fail(`Generated artifact contains forbidden sourcesContent: ${toRelative(filePath)}`);
  }

  if (/(^|["'\s])[A-Za-z]:[\\/][^"'\r\n]*/.test(text)) {
    fail(`Generated artifact appears to contain a Windows absolute path: ${toRelative(filePath)}`);
  }

  const suspiciousLine = text
    .split(/\r?\n/)
    .find((line) => /"(password|pwd|token|cookie|connectionString|connectionStrings|certificate|privateKey)"\s*:|(?:password|pwd|token|connectionString|privateKey)\s*=/i.test(line));
  if (suspiciousLine) {
    fail(`Generated artifact appears to contain a sensitive assignment pattern: ${toRelative(filePath)} :: ${suspiciousLine.trim()}`);
  }
}

const errors = (Array.isArray(result.diagnostics) ? result.diagnostics : [])
  .filter((diagnostic) => diagnostic && diagnostic.severity === "error");
if (errors.length > 0) {
  fail(`DotNetDoc reported ${errors.length} error diagnostic(s).`);
}

const summary = {
  status: result.status,
  artifactCount: artifacts.length,
  jsonFileCount: jsonFiles.length,
  artifactKinds: Array.from(artifactKinds).sort()
};

process.stdout.write(`${JSON.stringify(summary, null, 2)}\n`);

function listJsonFiles(directory) {
  if (!fs.existsSync(directory)) {
    return [];
  }

  const results = [];
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      results.push(...listJsonFiles(entryPath));
    } else if (entry.isFile() && entry.name.toLowerCase().endsWith(".json")) {
      results.push(entryPath);
    }
  }

  return results;
}

function readJson(filePath) {
  try {
    return JSON.parse(fs.readFileSync(filePath, "utf8"));
  } catch (error) {
    fail(`Unable to parse JSON ${toRelative(filePath)}: ${error.message}`);
  }
}

function toRelative(filePath) {
  return path.relative(repositoryRoot, filePath).replaceAll(path.sep, "/");
}

function fail(message) {
  process.stderr.write(`${message}\n`);
  process.exit(1);
}
