#!/usr/bin/env python3
"""Extract portable AssertOutput tests into lci-format compatibility fixtures.

This deliberately inventories every complete inline HAI/KTHXBYE test in the
selected EndToEnd categories, then writes only the portable subset.  The
allowlist and explicit retained reasons make migration reviewable rather than
silently treating .NET-specific tests as lci contracts.
"""

from __future__ import annotations

import argparse
import re
import sys
import textwrap
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
TEST_ROOT = ROOT / "tests" / "Lolcode.EndToEnd.Tests"
CORPUS_ROOT = ROOT / "tests" / "Compatibility" / "Extracted"

FILES = {
    "Basic": "BasicProgramTests.cs",
    "Variables": "VariableTests.cs",
    "Math": "MathTests.cs",
    "Boolean": "BooleanTests.cs",
    "Comparison": "ComparisonTests.cs",
    "Casting": "CastingTests.cs",
    "Conditional": "ConditionalTests.cs",
    "Switch": "SwitchTests.cs",
    "Loop": "LoopTests.cs",
    "Function": "FunctionTests.cs",
    "String": "StringTests.cs",
    "Comment": "CommentTests.cs",
    "Formatting": "FormattingTests.cs",
    "IO": "IoTests.cs",
    "Gtfo": "GtfoTests.cs",
    "Type": "TypeTests.cs",
    "Expression": "ExpressionTests.cs",
    "EdgeCase": "EdgeCaseTests.cs",
}

RETAINED = {
    "Loop": {
        "LoopScopeItStartsNoobAndParentItIsRestored",
        "LoopVariableShadowsParentForGuardBodyAndUpdate",
        "CustomUnaryOperationUsesReturnedValue",
        "CustomOperationAcceptsSrsScopeAndName",
        "CustomOperationAcceptsObjectQualifiedScopeAndName",
        "GtfoSkipsCustomLoopOperation",
    },
    "Function": {
        "NestedFunctionIsEmittedAndCallable",
        "ReplacedFunctionUsesCurrentRuntimeArity",
        "FunctionItIndependent",
        "DynamicParameterNamesResolveInCallerBeforeEachArgument",
    },
    "String": {
        "InterpolationResolvesSrsCreatedBindingAtRuntime",
        "UnicodeHexEscapeAscii",
        "UnicodeHexEscapeNonAscii",
    },
}


@dataclass(frozen=True)
class Case:
    category: str
    method: str
    source: str
    expected: str
    stdin: str | None


def kebab(value: str) -> str:
    return re.sub(r"(?<!^)(?=[A-Z])", "-", value).replace("_", "-").lower()


def decode_csharp_string(value: str) -> str:
    result: list[str] = []
    index = 0
    escapes = {"a": "\a", "n": "\n", "r": "\r", "t": "\t", '"': '"', "\\": "\\"}
    while index < len(value):
        if value[index] != "\\":
            result.append(value[index])
            index += 1
            continue
        index += 1
        if index == len(value):
            raise ValueError("dangling C# string escape")
        result.append(escapes.get(value[index], value[index]))
        index += 1
    return "".join(result)


def extract(category: str, path: Path) -> tuple[list[Case], int]:
    value = path.read_text(encoding="utf-8")
    complete_programs = value.count("HAI ")
    cases: list[Case] = []
    pattern = re.compile(
        r"public void (?P<method>\w+)\(\)\s*\{\s*"
        r"AssertOutput\(\s*\"\"\"(?P<source>.*?)\"\"\"\s*,\s*"
        r'"(?P<expected>(?:\\.|[^"\\])*)"'
        r"(?P<tail>.*?)\);",
        re.DOTALL,
    )
    for match in pattern.finditer(value):
        method = match.group("method")
        if method in RETAINED.get(category, set()):
            continue
        source = textwrap.dedent(match.group("source")).strip("\n") + "\n"
        if "HAI " not in source or "KTHXBYE" not in source:
            continue
        stdin_match = re.search(
            r'stdin:\s*"((?:\\.|[^"\\])*)"', match.group("tail"), re.DOTALL
        )
        cases.append(
            Case(
                category,
                method,
                source,
                decode_csharp_string(match.group("expected")),
                decode_csharp_string(stdin_match.group(1)) if stdin_match else None,
            )
        )
    return cases, complete_programs


def expected_bytes(case: Case) -> bytes:
    if not case.expected:
        return b""
    lines = [line.strip() for line in case.source.splitlines() if line.strip()]
    writes_newline = not any(line.startswith("VISIBLE") and line.endswith("!") for line in lines[-2:])
    suffix = "\n" if writes_newline else ""
    return (case.expected + suffix).encode("utf-8")


def write_case(case: Case, check: bool) -> bool:
    directory = CORPUS_ROOT / kebab(case.category) / kebab(case.method)
    files = {
        "CMakeLists.txt": (
            "INCLUDE(AddLolTest)\n"
            f"ADD_LOL_TEST({kebab(case.method)} OUTPUT test.out"
            + (" INPUT test.in" if case.stdin is not None else "")
            + ")\n"
        ).encode(),
        "test.lol": case.source.encode(),
        "test.out": expected_bytes(case),
    }
    if case.stdin is not None:
        files["test.in"] = case.stdin.encode()

    stale = False
    for name, content in files.items():
        target = directory / name
        if target.exists() and target.read_bytes() == content:
            continue
        stale = True
        if not check:
            directory.mkdir(parents=True, exist_ok=True)
            target.write_bytes(content)
    return stale


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    cases: list[Case] = []
    inventory = 0
    for category, filename in FILES.items():
        extracted, total = extract(category, TEST_ROOT / filename)
        cases.extend(extracted)
        inventory += total

    stale = [case for case in cases if write_case(case, args.check)]
    retained = inventory - len(cases)
    print(
        f"Inline HAI/KTHXBYE inventory: {inventory}; "
        f"portable fixtures: {len(cases)}; retained: {retained}."
    )
    if args.check and stale:
        print("Stale generated compatibility fixtures:", file=sys.stderr)
        for case in stale:
            print(f"  {case.category}/{case.method}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
