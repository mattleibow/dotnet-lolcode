# 12 Build and share an application

**Prerequisite:** planning, functions, tests, and either a file or project
workflow.

**Outcome:** deliver a complete application with a written requirement, test
table, clean source, repeatable run instructions, and an honest support note.

## Core capstone: stable 1.2 habit tracker

Build a terminal program that:

1. asks for a habit name;
2. asks for seven daily `y`/`n` results;
3. counts completed days;
4. reports the count and one of three messages: start, steady, or strong;
5. rejects no input silently—unexpected answers must be reported and not
   counted.

Keep this version under `HAI 1.2`. Suggested state:

- `habit` YARN;
- `completed` NUMBR;
- seven loop iterations;
- one answer per iteration.

Suggested functions:

- `isYes(answer)` returns TROOF;
- `messageFor(count)` returns a YARN.

A reduced, runnable core looks like this:

```lolcode
HAI 1.2
  HOW IZ I messageFor YR count
    BOTH SAEM BIGGR OF count AN 6 AN count
    O RLY?
      YA RLY, FOUND YR "STRONG WEEK"
    OIC
    BOTH SAEM BIGGR OF count AN 3 AN count
    O RLY?
      YA RLY, FOUND YR "KEEP GOIN"
    OIC
    FOUND YR "START SMALL TOMORROW"
  IF U SAY SO

  I HAS A completed ITZ 5
  VISIBLE "DONE " completed "/7"
  VISIBLE I IZ messageFor YR completed MKAY
KTHXBYE
```

Expected output:

```text
DONE 5/7
KEEP GOIN
```

Your capstone replaces the fixed `5` with seven inputs.

## Test before polishing

Write expected results first:

| Completed | Expected message |
| ---: | --- |
| 0 | START SMALL TOMORROW |
| 3 | START SMALL TOMORROW |
| 4 | KEEP GOIN |
| 6 | KEEP GOIN |
| 7 | STRONG WEEK |

Also test all `n`, all `y`, mixed letter case if you choose to support it, an
unexpected answer, and exactly seven prompts. Check for off-by-one output.

## Package the result

Your project should include:

- source with comments only where intent is not obvious;
- a `.lolproj` or file directive pinned to the published
  `Lolcode.NET.Sdk/0.2.0` package for the stable 1.2 capstone;
- a short README containing prerequisites, `dotnet run`, example interaction,
  and the language/profile used;
- no generated `bin` or `obj` directories in shared source;
- the final test table and results.

Run a release build:

```bash
dotnet build --configuration Release
dotnet run --configuration Release
```

For distribution, use [publishing](../projects/publishing.md) and share the
complete output, not a lone DLL.

## Optional development capstone

Create a 1.4 project that imports `STRING` to report UTF-8 byte counts or
`STDIO` to save a summary. This intentionally moves beyond stable 1.2. State
that requirement in the README, include normal runtime assets, and document
resource closure. Library discovery with trimming remains deferred.

## Practice: predict, run, explain, modify, create

1. **Predict:** Which boundary counts select each message?
2. **Run:** Execute every table row.
3. **Explain:** Describe state ownership and each function's contract.
4. **Modify:** Add a percentage without changing the testable message function.
5. **Create:** Complete, package, and give the application to another person
   with no verbal instructions.

<details>
<summary>Final review checklist</summary>

- Can a new user run it from the README?
- Does every source file have a matching complete header and footer?
- Do functions receive everything they need as parameters?
- Does every loop have an obvious termination condition?
- Are input conversion and unexpected-input behavior deliberate?
- Do tests include boundaries and failure paths?
- Does documentation distinguish stable 1.2 from development features?
- Does published output include normal runtime and library assets?

If another person cannot run it, repair the instructions before adding another
feature. A shared program includes its operational knowledge.
</details>

## Common mistakes

- Expanding the feature list before the core tests pass.
- Claiming malformed input is supported without a test.
- Publishing only the application DLL.
- Depending on source-checkout paths in consumer instructions.
- Mixing 1.4 libraries into a project described as stable 1.2.

## Checkpoint

You have moved from one output statement to a planned, tested, repeatable
application. Continue with the [tutorials](../tutorials/index.md), browse
[samples](../language/samples.md), or learn how the implementation works in
[Build a compiler](../compiler-course/index.md).
