---
layout: home
title: Home
parent: Product overview
nav_order: 1
lang: en
has_toc: false
---

<!-- markdownlint-disable MD033 -->
<div class="oc-page">
<section class="oc-hero">
  <span class="oc-hero-tag">Intranet portal</span>
  <h1 class="oc-hero-title">A Web Forms portal platform for the enterprise intranet</h1>
  <p class="oc-hero-lead">Accounts and role authorisation, org-subtree data scope, state machines with explicit transition tables, operational auditing, module and theme governance, and layered automated gates — a portal foundation for the intranet, ASP.NET Web Forms / .NET Framework 4.8, MIT licensed.</p>
  <div class="oc-code">
    <button class="oc-code-copy" type="button" data-oc-copy>Copy</button>
    <pre><span class="oc-code-comment"># Restore NuGet packages (classic packages.config)</span>
powershell -NoProfile -ExecutionPolicy Bypass -File dev\scripts\Restore-NuGetPackages.ps1

<span class="oc-code-comment"># Build the solution: Debug | Any CPU</span>
powershell -NoProfile -ExecutionPolicy Bypass -File dev\scripts\Build-Solution.ps1 -Configuration Debug -Platform "Any CPU"</pre>
  </div>
  <p class="oc-cta-row">
    <a class="oc-btn oc-btn-primary" href="{{ site.baseurl }}/en/user-guide/">Get started</a>
    <a class="oc-btn oc-btn-ghost" href="{{ site.baseurl }}/en/architecture/">Architecture</a>
  </p>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">Why it</p>
  <h2 class="oc-h2">For teams that already have .NET assets</h2>
  <p class="oc-lead">If you want to keep building intranet portals on your existing .NET Framework 4.8 / Web Forms stack and operations model, this project supplies the parts that tend to be missing: fine-grained authorisation, workflow state machines, auditing, module and theme governance, and quality constraints run as automated commands.</p>
  <div class="oc-stats">
    <div><div class="oc-stat-val">v1.3.0</div><div class="oc-stat-label">Current version</div></div>
    <div><div class="oc-stat-val">MIT</div><div class="oc-stat-label">Licence</div></div>
    <div><div class="oc-stat-val">7</div><div class="oc-stat-label">Official skins</div></div>
    <div><div class="oc-stat-val">2</div><div class="oc-stat-label">UI languages</div></div>
    <div><div class="oc-stat-val">50</div><div class="oc-stat-label">Documents</div></div>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">Core capability</p>
  <h2 class="oc-h2">Ready out of the box, and yours to extend</h2>
  <p class="oc-lead">These five areas are the platform's core capabilities. Each is independent: enable what you need, or take a single one as a reference.</p>
  <div class="oc-grid-2">
    <a class="oc-card" href="{{ site.baseurl }}/en/business-application-state-machine/">
      <h3 class="oc-card-title">Permissions and data scope</h3>
      <p>Account registration, role-based entity permissions, and data scope that narrows along the org subtree, so the same pages show different visible ranges to different people.</p>
      <span class="oc-card-more">Business application state machine →</span>
    </a>
    <a class="oc-card" href="{{ site.baseurl }}/en/collaboration-item-state-machine/">
      <h3 class="oc-card-title">Workflow state machines</h3>
      <p>Business applications and collaboration items both use explicit transition tables: fail-closed before writes, and every transition records an event.</p>
      <span class="oc-card-more">Collaboration item state machine →</span>
    </a>
    <a class="oc-card" href="{{ site.baseurl }}/en/audit-retention-policy/">
      <h3 class="oc-card-title">Auditing and compliance</h3>
      <p>An operational audit table with retention policy, log maintenance and archiving keeps "who changed what, when" inside a controlled window.</p>
      <span class="oc-card-more">Audit retention policy →</span>
    </a>
    <a class="oc-card" href="{{ site.baseurl }}/en/theme-module-contract/">
      <h3 class="oc-card-title">Module and theme governance</h3>
      <p>Trusted module packages can be registered, enabled, disabled, removed and boundary-checked; theme packages have contract validation and fallback, with 7 official skins sharing one semantic variable matrix.</p>
      <span class="oc-card-more">Theme-module appearance contract →</span>
      </a>
      <a class="oc-card" href="{{ site.baseurl }}/en/code-documentation/">
      <h3 class="oc-card-title">Code documentation</h3>
      <p>API-level documentation generated from source comments by HIA-Documentation-Sys: the JSDoc half is published as bilingual pages, while the DotNetDoc half produces intermediate output awaiting a rendering component.</p>
      <span class="oc-card-more">Code documentation guide →</span>
      </a>
      </div>
      </section>

<section class="oc-section">
  <p class="oc-eyebrow">Feature tour</p>
  <h2 class="oc-h2">What ships in the box</h2>
  <p class="oc-lead">From identity to automated gates — none of it needs add-ons.</p>
  <div class="oc-grid">
    <div class="oc-card"><h3 class="oc-card-title">Identity and authorisation</h3><p>Account registration, role-based entity permissions, org-subtree data scope.</p></div>
    <div class="oc-card"><h3 class="oc-card-title">Workflow state machines</h3><p>Explicit transition tables, fail-closed before writes, events always recorded.</p></div>
    <div class="oc-card"><h3 class="oc-card-title">Auditing</h3><p>Operational audit table, retention policy, log maintenance and archiving.</p></div>
    <div class="oc-card"><h3 class="oc-card-title">Module package governance</h3><p>Registration, enable/disable, removal and boundary checks for trusted packages.</p></div>
    <div class="oc-card"><h3 class="oc-card-title">Theme governance</h3><p>Contract validation and fallback; 7 skins share one semantic variable matrix.</p></div>
    <div class="oc-card"><h3 class="oc-card-title">Internationalisation</h3><p>Chinese / English resources with strongly typed resource keys.</p></div>
    <div class="oc-card"><h3 class="oc-card-title">Automated gates</h3><p>Layered orchestration across L0 (static) / L1 (post-build) / L2 (runtime).</p></div>
    <div class="oc-card"><h3 class="oc-card-title">Default credential governance</h3><p>A documented path for initial passwords and legacy password migration.</p></div>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">Documentation</p>
  <h2 class="oc-h2">Read what solves your problem</h2>
  <p class="oc-lead">Documentation is grouped into eight sections, each available in Chinese and English.</p>
  <div class="oc-grid">
    <a class="oc-card" href="{{ site.baseurl }}/en/sections/product-overview/"><h3 class="oc-card-title">Product overview</h3><p>Positioning, boundaries and the full document index.</p><span class="oc-card-more">Open section →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/en/sections/evaluate/"><h3 class="oc-card-title">Evaluate</h3><p>System structure, data access and current architecture risks.</p><span class="oc-card-more">Open section →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/en/sections/quick-start/"><h3 class="oc-card-title">Quick start</h3><p>Run locally, external configuration and common modules.</p><span class="oc-card-more">Open section →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/en/sections/learn/"><h3 class="oc-card-title">Learn</h3><p>A complete business path showing how the capabilities work together.</p><span class="oc-card-more">Open section →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/en/sections/reference/"><h3 class="oc-card-title">Reference</h3><p>Test tiers, theme contracts, state machines and compliance policy.</p><span class="oc-card-more">Open section →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/en/sections/develop/"><h3 class="oc-card-title">Develop</h3><p>Module and theme development, frontend assets, development environment.</p><span class="oc-card-more">Open section →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/en/sections/operate/"><h3 class="oc-card-title">Operate</h3><p>Deployment, operations, default credential governance and rollback.</p><span class="oc-card-more">Open section →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/en/sections/changes/"><h3 class="oc-card-title">Changes</h3><p>Version rules and the release notes template.</p><span class="oc-card-more">Open section →</span></a>
  </div>
  <div class="oc-band">
    <p class="oc-eyebrow">Not sure where to start</p>
    <h2 class="oc-h2">The documentation map gives role-based reading paths</h2>
    <p class="oc-lead">Three entries — user, developer, auditor — each listing what to read.</p>
    <p class="oc-cta-row"><a class="oc-btn oc-btn-primary" href="{{ site.baseurl }}/en/documentation-map/">Open the documentation map</a></p>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">Getting started</p>
  <h2 class="oc-h2">Three common first steps</h2>
  <div class="oc-grid">
    <a class="oc-card" href="{{ site.baseurl }}/en/user-guide/"><h3 class="oc-card-title">Run it in Visual Studio</h3><p>Open <code>src/master.sln</code>, restore NuGet packages, pick Debug | Any CPU, then build and run.</p><span class="oc-card-more">User guide →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/en/dev-guide/"><h3 class="oc-card-title">Build from the command line</h3><p>In-repo scripts locate MSBuild and the NuGet CLI without depending on an IDE.</p><span class="oc-card-more">Developer guide →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/en/operations-runbook/"><h3 class="oc-card-title">Deploy to IIS</h3><p>External connection strings, application pools and site configuration.</p><span class="oc-card-more">Operations runbook →</span></a>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">FAQ</p>
  <h2 class="oc-h2">Asked before choosing it</h2>
  <div class="oc-faq">
    <details>
      <summary>What kind of project is it?</summary>
      <p>An ASP.NET Web Forms portal running on .NET Framework 4.8: Windows + IIS (or IIS Express) + SQL Server / LocalDB. It is not a single-page application and has no client-side routing; pages and modules render as Web Forms control trees.</p>
    </details>
    <details>
      <summary>Who is it a good fit for?</summary>
      <p>Teams that want to stay on their existing .NET Framework 4.8 / Web Forms stack and operations model — particularly organisations that need permissions, workflows and auditing unified inside one portal.</p>
    </details>
    <details>
      <summary>Can I put it straight into production?</summary>
      <p>The current version suits development, testing, demonstration and reference use. No production-environment validation evidence exists yet (real IIS, TLS, application pool ACLs, target SQL Server, enterprise scanning). Complete your own security assessment, backups and an operations rehearsal first — see the <a href="{{ site.baseurl }}/en/versioning/">versioning policy</a>.</p>
    </details>
    <details>
      <summary>Is there an external API?</summary>
      <p>No external REST / GraphQL API: this is a Web Forms portal. Integration boundaries are described in the <a href="{{ site.baseurl }}/en/product-overview/">product overview</a>.</p>
    </details>
    <details>
      <summary>What are the licence and version rules?</summary>
      <p>MIT licensed. Version numbers and semantics (including how a module manifest's <code>minimumPortalVersion</code> is resolved) are in the <a href="{{ site.baseurl }}/en/versioning/">versioning policy</a>.</p>
    </details>
  </div>
</section>
</div>

<script>
  document.querySelectorAll('[data-oc-copy]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var code = btn.parentElement.querySelector('pre').innerText;
      if (navigator.clipboard) { navigator.clipboard.writeText(code); }
      var old = btn.textContent; btn.textContent = 'Copied';
      setTimeout(function () { btn.textContent = old; }, 1600);
    });
  });
</script>
<!-- markdownlint-enable MD033 -->
