const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const { join } = require("node:path");
const test = require("node:test");

const root = join(__dirname, "..");
const readJson = (path) => JSON.parse(readFileSync(join(root, path), "utf8"));

test("contributes the LOLCODE language and grammar", () => {
  const manifest = readJson("package.json");
  const language = manifest.contributes.languages[0];

  assert.equal(language.id, "lolcode");
  assert.deepEqual(language.aliases, ["LOLCODE", "lolcode"]);
  assert.deepEqual(language.extensions, [".lol"]);
  assert.equal(language.firstLine, "^#!.*\\bdotnet\\s+run\\s+--file\\b");
  assert.equal(language.configuration, "./language-configuration.json");
  assert.deepEqual(manifest.contributes.grammars, [{
    language: "lolcode",
    scopeName: "source.lolcode",
    path: "./syntaxes/lolcode.tmLanguage.json",
  }]);
});

test("configures LOLCODE comments and conservative editor pairs", () => {
  const configuration = readJson("language-configuration.json");

  assert.deepEqual(configuration.comments, {
    lineComment: "BTW",
    blockComment: ["OBTW", "TLDR"],
  });
  assert.deepEqual(configuration.autoClosingPairs, [{
    open: "\"",
    close: "\"",
    notIn: ["string", "comment"],
  }]);
  assert.equal(configuration.surroundingPairs.length, 1);
});

test("grammar patterns recognize representative LOLCODE source", () => {
  const grammar = readJson("syntaxes/lolcode.tmLanguage.json");
  const repo = grammar.repository;

  assert.equal(grammar.scopeName, "source.lolcode");

  const cases = [
    [repo.directives.patterns[0].match, "#!/usr/bin/env -S dotnet run --file"],
    [repo.comments.patterns[0].begin, "OBTW a comment"],
    [repo.comments.patterns[1].match, "VISIBLE \"HAI\" BTW a comment"],
    [repo.strings.patterns[0].match, "\"OH HAI :{name}!\""],
    [repo.strings.patterns[1].match, "\"colon :: and hex :(0041)\""],
    [repo.numbers.patterns[0].match, "-3.14"],
    [repo.numbers.patterns[1].match, "42"],
    [repo.types.match, "NUMBAR"],
    [repo.booleans.match, "WIN"],
    [repo["function-declarations"].match, "HOW IZ I greet YR name"],
    [repo["function-calls"].match, "I IZ greet YR \"CAT\" MKAY"],
    [repo.declarations.patterns[0].match, "I HAS A cat ITZ \"CEILING CAT\""],
    [repo["member-access"].match, "bukkit'Z slot"],
    [repo.operators.match, "SUM OF 1 AN 2"],
    [repo.keywords.match, "O RLY?"],
  ];

  for (const [pattern, source] of cases)
    assert.match(source, new RegExp(pattern));
});
