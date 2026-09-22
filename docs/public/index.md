---
title: dotnet-lolcode documentation
description: Learn LOLCODE, build .NET applications, and understand the compiler.
---

<div class="trail-hero">
<h1>dotnet-lolcode <span class="brand-inline">:3</span></h1>
<p class="trail-lede">A LOLCODE compiler for .NET 10, with a beginner course, real project tooling, a browser playground, and an implementation you can study.</p>
</div>

## Choose your route

<div class="audience-routes">
<a class="audience-route audience-route-primary" href="learn/course.md"><strong>New to programming</strong><span>Start with a source file and learn values, decisions, loops, functions, testing, projects, and a complete application.</span></a>
<a class="audience-route" href="learn/dotnet/index.md"><strong>C#, Visual Basic, or F# developer</strong><span>Map familiar .NET ideas to LOLCODE, then learn the semantic differences that matter when porting or interoperating.</span></a>
<a class="audience-route" href="language/index.md"><strong>Need a language or SDK answer</strong><span>Go directly to syntax, runtime behavior, projects, libraries, publishing, compatibility, and specifications.</span></a>
</div>

## Run this now

<div class="hello-panel">
<div>

```lolcode
HAI 1.2
  VISIBLE "HAI, WORLD!"
KTHXBYE
```

</div>
<div class="hello-copy">
<p><code>HAI</code> begins the program, <code>VISIBLE</code> writes a line, and <code>KTHXBYE</code> ends it.</p>
<a class="docs-action" href="https://mattleibow.github.io/dotnet-lolcode/playground/">Open in the Playground</a>
</div>
</div>

## Go deeper

- [Build a compiler](compiler-course/index.md) — trace source text through syntax, binding, lowering, runtime operations, IL, testing, and tooling.
- [API reference](api.md) — orient yourself before browsing the generated CodeAnalysis, Runtime, and Build contracts.

> [!NOTE]
> The repository version is `0.3.0`. Package publication can lag the source
> branch, so check [versions, support, and availability](language/versions.md)
> before pinning an SDK package.
