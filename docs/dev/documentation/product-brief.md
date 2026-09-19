# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

The primary audience is newcomers learning LOLCODE from scratch, including
people using LOLCODE as an approachable way to learn programming concepts.
Secondary audiences are existing LOLCODE users adopting this .NET
implementation and .NET developers who want to understand, embed, contribute
to, or build a compiler and language system of their own.

## Product Purpose

dotnet-lolcode is an official-feeling home for learning LOLCODE and using a
production-quality compiler implementation. It should help a first-time visitor
understand what LOLCODE is, run a useful program quickly, learn the language in
progressive steps, find authoritative syntax and behavior details, and continue
into larger programs or compiler development. The complete experience has three
coherent pillars:

1. Learn LOLCODE and programming by writing applications.
2. Understand the language, its history, community, creator, and specification.
3. Learn how to build a .NET language and compiler from the implementation.

Success means a newcomer can move from curiosity to a working program without
having to reconstruct the language from a specification, while experienced
readers can still reach normative and implementation-specific details quickly.

## Positioning

Unlike a language-history page or interpreter-only tutorial, dotnet-lolcode
connects a complete learning path to a Roslyn-inspired compiler, standard .NET
build workflows, real IL assemblies, a browser playground, an MSBuild SDK, and
the repository's pinned LOLCODE reference behavior.

## Operating Context

Readers may try the language in the browser, run a file-based app with
`dotnet run --file`, create a `.lolproj` project, browse complete sample
programs, consult the language reference, or explore the compiler API and
architecture.

The documentation is built with DocFX and is expected to work as a local site
and as a static deployment. The browser playground remains a distinct runnable
experience linked from the documentation.

## Capabilities and Constraints

- The compiler targets .NET 10 and is written in C# 14.
- It implements LOLCODE 1.2 plus the supported behavior documented in the
  repository's pinned future implementation profile.
- File-based apps, `.lolproj` projects, NuGet packages, templates, sample
  programs, compiler APIs, diagnostics, and the browser playground must all be
  documented from repository evidence.
- Normative language rules and implementation-specific behavior must be clearly
  distinguished.
- Compiler documentation must teach the full lexer-to-IL pipeline as a guided
  course for readers building their own .NET language, not merely expose API
  signatures.
- Unsupported or deferred features must be described honestly and never implied
  to work.
- Existing source documents and samples are authoritative inputs; the learning
  experience may reorganize and explain them but must not contradict them.

## Brand Commitments

The product name is `dotnet-lolcode`. The established playground identity uses
the `:3` cat mark, dark terminal-inspired surfaces, a warm amber accent, direct
technical language, and restrained LOLCODE-flavored personality. Documentation
should feel official and complete without sanding away the language's humor.

## Evidence on Hand

- `README.md` describes the product, supported workflows, examples, and package
  surface.
- `docs/public/reference/language-spec.md` is the normative LOLCODE 1.2
  language specification.
- `docs/public/reference/implementation-profile.md` records .NET mappings and adopted future
  behavior.
- `docs/public/reference/language-spec-1.3-changes.md` and
  `docs/public/reference/language-spec-1.4-changes.md` preserve later reference
  deltas.
- `docs/dev/compiler-architecture.md` and `docs/dev/roadmap.md` document
  compiler architecture and
  implementation history.
- `samples/` contains 23 runnable `.lol` programs spanning fundamentals,
  algorithms, file I/O, objects, and games.
- `src/Lolcode.Web` provides the established visual identity and browser
  playground experience.
- No testimonials, adoption metrics, customer claims, or official relationship
  with the original LOLCODE creators are present and none should be fabricated.

## Product Principles

- Teach progressively before presenting exhaustive reference material.
- Keep every claim traceable to implemented behavior or a cited source.
- Make runnable code the shortest path from curiosity to understanding.
- Preserve the delightful personality of LOLCODE while maintaining technical
  precision.
- Let beginners and advanced readers share one coherent information
  architecture instead of splitting into disconnected sites.

## Accessibility & Inclusion

The documentation must support keyboard navigation, visible focus, readable
contrast, reduced motion, responsive layouts, semantic headings and landmarks,
and code examples that remain usable without color alone.
