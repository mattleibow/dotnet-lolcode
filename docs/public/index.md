---
title: dotnet-lolcode | The Trail Map
description: Learn LOLCODE, explore its reference, and build a .NET compiler.
---

<div class="trail-hero">
<h1>The Trail Map <span class="brand-inline">:3</span></h1>
<p class="trail-lede">A friendly route from your first program to a real .NET compiler. Pick an outcome, follow the trail, and keep the reference shelf close.</p>
</div>

## Make your first program

Open the <a href="https://mattleibow.github.io/dotnet-lolcode/playground/">browser playground</a> for a no-install experiment,
or save this as `hello.lol` and follow the
[file-based workflow](getting-started/file-based.md).

```lolcode
HAI 1.2
  VISIBLE "HAI, WORLD!"
KTHXBYE
```

`HAI` starts a program, `KTHXBYE` ends it, and `VISIBLE` writes a line. That is
already a program: instructions run in order and produce an observable result.

<div class="trail-grid">
<a class="trail-card" href="learn/index.md"><strong>1. Learn and build</strong><p>Start with values and output; finish with a small app, FizzBuzz, and a terminal adventure.</p></a>
<a class="trail-card" href="language/index.md"><strong>2. Know the language</strong><p>Find LOLCODE's history and community context, a language tour, samples, diagnostics, and the authoritative reference.</p></a>
<a class="trail-card" href="compiler-course/index.md"><strong>3. Build a compiler</strong><p>Trace this repository from text through tokens, trees, binding, IL, assemblies, tooling, and tests.</p></a>
</div>

<div class="trail-shelf">
<strong>Reference shelf.</strong> Need a spelling or rule? Use the <a href="language/reference.md">syntax hub</a>, the normative <a href="reference/language-spec.md">LOLCODE 1.2 specification</a>, the <a href="reference/implementation-profile.md">implementation profile</a>, or generated <a href="api/index.md">.NET API reference</a>. The playground is an experiment bench, not a security sandbox.
</div>

## Choose the next step

- New to programming? Begin at [Program structure and output](learn/foundations.md).
- Want to run a local file now? Read [Getting started](getting-started/index.md).
- Curious how `VISIBLE` becomes an assembly? Start [the compiler course](compiler-course/index.md).
