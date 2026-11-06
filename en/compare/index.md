---
layout: home
title: Compare
parent: Product overview
lang: en
has_toc: false
---

<!-- markdownlint-disable MD033 -->
<div class="oc-page">
<section class="oc-hero">
  <span class="oc-hero-tag">Compare</span>
  <h1 class="oc-hero-title">Adopt it, or build from scratch?</h1>
  <p class="oc-lead" style="max-width:46rem">For teams building an intranet portal there are usually two realistic options: adopt this project, or develop your own from the original Starter Kit / a blank project. Compared fact by fact below.</p>
</section>

<section class="oc-section">
  <div class="oc-grid-2">
    <div class="oc-card">
      <h3 class="oc-card-title">Adopt this project</h3>
      <p>Permissions and data scope, workflow state machines, auditing, module and theme package governance, externalised configuration and automated gates are built in, documented and checked; the source is open (MIT) and business features plug in as module packages rather than being locked into the core.</p>
    </div>
    <div class="oc-card">
      <h3 class="oc-card-title">Build on the Starter Kit</h3>
      <p>The original Starter Kit only provides a basic page framework and sample modules: authorisation granularity, state machines, auditing, governance and automated checks all have to be designed and implemented by your team.</p>
    </div>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">Item by item</p>
  <h2 class="oc-h2">Capability checklist</h2>
  <table>
    <thead>
      <tr><th>Capability</th><th>This project</th><th>DIY from the Starter Kit</th></tr>
    </thead>
    <tbody>
      <tr><td>Authorisation</td><td>Role × entity permissions + org-subtree data scope</td><td>Design it yourself</td></tr>
      <tr><td>Workflows</td><td>Explicit transition tables, fail-closed, events recorded</td><td>Design it yourself</td></tr>
      <tr><td>Auditing</td><td>Operational audit table + retention policy + archiving</td><td>Design it yourself</td></tr>
      <tr><td>Modules</td><td>Trusted packages: register / toggle / boundary checks</td><td>Edit the source directly</td></tr>
      <tr><td>Themes</td><td>7 skins + semantic variable matrix + contract fallback</td><td>Hand-rolled styles</td></tr>
      <tr><td>Configuration</td><td>Externalised + contract verification</td><td>Usually committed to the repo</td></tr>
      <tr><td>Quality gates</td><td>Layered L0 / L1 / L2 automated checks</td><td>Up to team discipline</td></tr>
    </tbody>
  </table>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">When it is not a fit</p>
  <h2 class="oc-h2">Honest boundaries</h2>
  <p class="oc-lead">Teams that need an external REST / GraphQL API, a single-page front end, or to run outside Windows / IIS should look elsewhere — this is a Web Forms portal; integration boundaries are in the <a href="{{ site.baseurl }}/en/product-overview/">product overview</a>. Also, the current version has no production-environment validation evidence yet; complete your own security assessment and operations rehearsal before production.</p>
  <div class="oc-band">
    <p class="oc-eyebrow">Next step</p>
    <h2 class="oc-h2">Read the boundaries, then run it</h2>
    <p class="oc-cta-row"><a class="oc-btn oc-btn-primary" href="{{ site.baseurl }}/en/user-guide/">Get started</a><a class="oc-btn oc-btn-ghost" href="{{ site.baseurl }}/en/product-overview/">Product overview</a></p>
  </div>
</section>
</div>
<!-- markdownlint-enable MD033 -->
