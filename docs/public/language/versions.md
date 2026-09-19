# Version story and project profile

The repository anchors its user-facing language documentation on LOLCODE 1.2.
Earlier 1.0 and 1.1 documents are preserved under `reference/archive` as
historical source material, not as competing rules for this compiler.

The upstream `lci/future` corpus has later 1.3/1.4-oriented behavior. This
repository pins selected behavior from that corpus for conformance work; it
does **not** automatically mean every proposed feature is stable LOLCODE 1.2.
The precise support matrix, including BUKKIT, SRS, libraries, and deferred
TYPE behavior, lives in the authoritative
[implementation profile](../reference/implementation-profile.md). The proposal
deltas are available as
[1.3 changes](../reference/language-spec-1.3-changes.md) and
[1.4 changes](../reference/language-spec-1.4-changes.md).

When writing examples for learners, label which profile they need and prefer
the 1.2 core. That keeps a first program portable and lets advanced readers
make a deliberate choice. The [sample gallery](samples.md) identifies examples
that exercise future-reference features.
