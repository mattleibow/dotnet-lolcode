---
title: dotnet-lolcode documentation
description: Write LOLCODE, build .NET projects, and explore the compiler.
---

<div class="trail-hero">
<h1>dotnet-lolcode <span class="brand-inline">:3</span></h1>
<p class="trail-lede">The practical guide to writing LOLCODE, building it with .NET, and understanding the compiler behind it.</p>
</div>

## Start writing

Start in the browser or with a file. A complete program is wonderfully small:

```lolcode
HAI 1.2
  VISIBLE "HAI, WORLD!"
KTHXBYE
```

`HAI` starts a program, `KTHXBYE` ends it, and `VISIBLE` writes a line.

<div class="trail-grid">
<a class="trail-card" href="getting-started/index.md"><strong>Get started</strong><p>Choose the playground, a file-based app, or a project and run your first program.</p></a>
<a class="trail-card" href="learn/index.md"><strong>Learn LOLCODE</strong><p>Build confidence with values, decisions, text, loops, functions, and a small application.</p></a>
<a class="trail-card" href="tutorials/index.md"><strong>Tutorials</strong><p>Practice with FizzBuzz, a calculator, and a terminal adventure.</p></a>
</div>

## Find answers and build projects

<div class="task-card-grid">
<a class="task-card" href="language/reference.md"><strong>Language reference</strong><span class="badge badge-core">Stable core</span><p>Look up syntax, types, diagnostics, and the 1.2 specification.</p></a>
<a class="task-card" href="language/versions.md"><strong>Versions and support</strong><span class="badge">Compatibility</span><p>Separate language provenance, implementation support, and package availability.</p></a>
<a class="task-card" href="projects/index.md"><strong>.NET projects</strong><span class="badge badge-dotnet">.NET</span><p>Build multiple files, libraries, managed imports, providers, and published apps.</p></a>
<a class="task-card" href="language/samples.md"><strong>Samples</strong><span class="badge">Runnable</span><p>Browse every small file-based program and the project-based integration scenarios.</p></a>
</div>

## Explore the implementation

<div class="task-card-grid">
<a class="task-card" href="compiler-course/index.md"><strong>Compiler course</strong><p>Follow source text through parsing, binding, runtime operations, IL, assemblies, and tooling.</p></a>
<a class="task-card" href="xref:Lolcode.CodeAnalysis"><strong>Generated API</strong><p>Browse the public compiler, runtime, provider, and MSBuild surfaces.</p></a>
<a class="task-card" href="https://mattleibow.github.io/dotnet-lolcode/playground/"><strong>Playground</strong><span class="badge badge-play">Utility</span><p>Try a short program without installing anything. It is an experiment bench, not a security sandbox.</p></a>
</div>

<div class="trail-shelf">
<strong>Need an exact rule?</strong> The <a href="reference/implementation-profile.md">implementation profile</a> is the canonical record of compiler behavior, including advanced runtime features.
</div>
