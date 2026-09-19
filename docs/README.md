# Documentation

The documentation tree has two explicit audiences:

- `docs/public/` contains everything shipped to GitHub Pages: the root landing
  page, DocFX source, language reference, tutorials, compiler course, assets,
  and custom template.
- `docs/dev/` contains repository-only architecture, roadmap, maintainer
  briefs, and superseded implementation notes.

## Build the public site

DocFX 2.80.1 is pinned in `.config/dotnet-tools.json`. From the repository
root, build the complete Pages artifact:

```bash
docs/build-pages.sh
```

The complete Pages payload is generated under `docs/_site/`: the chooser is at
the root, the DocFX site is under `docs/_site/docs/`, and the playground is
under `docs/_site/playground/`. Run the browser smoke tests and capture desktop
and mobile screenshots with:

```bash
docs/test-pages.sh
```

The tests select an available localhost port and host the site beneath
`/dotnet-lolcode/`, matching the GitHub Pages path base. Select a specific port
with `docs/test-pages.sh docs/_site 8090`.

Every pull request that changes the site publishes a downloadable
`dotnet-lolcode-pages` workflow artifact. Download it, run the same Playwright
checks, and keep a local preview server open with:

```bash
docs/preview-pages-pr.sh <pull-request-number>
```

The script prints its selected preview URL. Pass a second argument to use a
specific port.

## Authoring map

The DocFX home is `docs/public/index.md`; navigation is defined by
`docs/public/toc.yml` and nested `toc.yml` files. The custom template layer is
`docs/public/templates/lolcode/public/main.css` and `main.js`; it loads after
DocFX's `default` and `modern` templates so built-in search and navigation
remain intact.

Use relative Markdown links within the public site, keep
`docs/public/reference/language-spec.md` and
`docs/public/reference/implementation-profile.md` authoritative, and do not
edit generated `docs/public/_api/` output.

Repository-only documentation includes:

- `docs/dev/compiler-architecture.md` and `docs/dev/roadmap.md`;
- `docs/dev/browser-playground.md`;
- `docs/dev/documentation/product-brief.md` and
  `docs/dev/documentation/documentation-design.md`; and
- `docs/dev/archive/pdb-implementation-notes/`, the superseded planning packet
  used while Portable PDB support was being implemented.

Historical LOLCODE specifications remain public under
`docs/public/reference/archive/` because the language history pages link to
them.
