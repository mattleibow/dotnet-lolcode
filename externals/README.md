# External Dependencies

## `lci`

[`justinmeza/lci`](https://github.com/justinmeza/lci) is tracked as a Git
submodule at `externals/lci`. The gitlink pins an exact commit from its
`future` branch so local and CI conformance results are reproducible.

The compiler does not build or link against `lci`. This repository uses its
source and test fixtures as:

- the executable reference for informal LOLCODE 1.4 behavior;
- the upstream conformance corpus consumed by the end-to-end tests; and
- evidence for resolving ambiguities in the archived language drafts.

Initialize the submodule after a non-recursive clone:

```bash
git submodule update --init --recursive
```

To inspect the pinned revision:

```bash
git submodule status externals/lci
```

The `Update lci conformance baseline` workflow checks the tip of `future`
nightly, resets its automation branch to the current `main`, updates the
gitlink, and opens or refreshes a pull request. Normal pull request CI then
runs every registered upstream test against the new revision.

`lci` and its test corpus are licensed under GPLv3; see
[`lci/COPYING`](lci/COPYING). The submodule preserves upstream content and
history rather than copying those files into this repository.
