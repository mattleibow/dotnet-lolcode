#!/usr/bin/env python3
"""Validate canonical compatibility fixtures and their historic test mappings."""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
COMPATIBILITY = ROOT / "tests" / "Compatibility"
CLASSIFICATIONS = ROOT / "tools" / "compatibility-classifications.json"
TEST_ROOTS = [
    ROOT / "tests" / "Lolcode.EndToEnd.Tests",
    ROOT / "tests" / "Lolcode.CodeAnalysis.Tests",
    ROOT / "tests" / "Lolcode.Web.Tests",
]
PROGRAM = re.compile(
    r"\bHAI\s+(?:[0-9.]+|\{\{version\}\})\b[\s\S]*?\bKTHXBYE\b",
    re.MULTILINE,
)
ROOTS = {"Shared", "KnownLciDivergence", "DotNet"}
ASSERTIONS = {
    "Output": "test.out",
    "RuntimeError": "test.err",
    "CompileError": "test.diag",
}
ADD_LOL_TEST = re.compile(r"^[ \t]*ADD_LOL_TEST\s*\((?P<args>[^)]*)\)", re.I | re.M)


def strip_comments(cmake: str) -> str:
    """Match the intentionally small CMake parser used by LciRegistrationParser."""
    return "\n".join(line.split("#", 1)[0] for line in cmake.splitlines())


def registrations(cmake: Path) -> list[dict[str, object]]:
    """Parse active ADD_LOL_TEST registrations and resolve all declared paths."""
    result: list[dict[str, object]] = []
    for match in ADD_LOL_TEST.finditer(strip_comments(cmake.read_text(encoding="utf-8"))):
        args = match.group("args").split()
        if not args:
            raise ValueError(f"ADD_LOL_TEST has no test name in {cmake}")
        registration: dict[str, object] = {
            "name": args[0],
            "LOLCODE": cmake.parent / "test.lol",
            "ERROR": False,
            "CWD": False,
        }
        index = 1
        while index < len(args):
            argument = args[index]
            if argument in {"LOLCODE", "OUTPUT", "INPUT"}:
                index += 1
                if index == len(args):
                    raise ValueError(f"ADD_LOL_TEST {argument} lacks a path in {cmake}")
                registration[argument] = cmake.parent / args[index]
            elif argument == "ERROR":
                registration["ERROR"] = True
            elif argument == "CWD":
                registration["CWD"] = True
            else:
                raise ValueError(f"unknown ADD_LOL_TEST argument {argument!r} in {cmake}")
            index += 1
        result.append(registration)
    return result


def registration_exists(fixture: Path) -> bool:
    cmake = fixture / "CMakeLists.txt"
    return cmake.is_file() and bool(registrations(cmake))


def test_sources() -> list[str]:
    return [
        path.read_text(encoding="utf-8")
        for root in TEST_ROOTS
        for path in root.rglob("*.cs")
    ]


def method_body(source: str, consumer: str) -> str | None:
    """Find one fully-qualified xUnit consumer without global suffix matching."""
    parts = consumer.rsplit(".", 2)
    if len(parts) != 3:
        return None
    namespace, class_name, method_name = parts
    if not re.search(rf"namespace\s+{re.escape(namespace)}\s*;", source):
        return None
    class_match = re.search(rf"\bclass\s+{re.escape(class_name)}\b", source)
    if class_match is None:
        return None
    class_end = source.find("\nclass ", class_match.end())
    class_source = source[class_match.end():] if class_end < 0 else source[class_match.end():class_end]
    method_match = re.search(rf"\b{re.escape(method_name)}\s*\(", class_source)
    if method_match is None:
        return None
    method_start = method_match.start()
    attributes = class_source[max(0, method_start - 1000):method_start]
    if not re.search(r"\[(?:Fact|Theory)(?:Attribute)?(?:\([^]]*\))?\]", attributes):
        return None
    open_brace = source.find("{", class_match.end() + method_match.end())
    if open_brace < 0:
        return None
    depth = 0
    for index in range(open_brace, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}" and (depth := depth - 1) == 0:
            return source[open_brace:index + 1]
    return None


def main() -> int:
    failures: list[str] = []
    inventory_path = COMPATIBILITY / "inventory.json"
    inventory = json.loads(inventory_path.read_text(encoding="utf-8"))
    if inventory.get("schemaVersion") != 2:
        failures.append("inventory schemaVersion must be 2")
    expected_mappings = inventory.get("expectedMappings", {})
    identities: set[str] = set()
    actual_mappings: dict[str, str] = {}
    sources = test_sources()

    actual_fixtures = {
        fixture.relative_to(COMPATIBILITY).as_posix()
        for fixture in COMPATIBILITY.rglob("test.lol")
    }
    for child in COMPATIBILITY.iterdir():
        if child.is_dir() and child.name not in ROOTS:
            failures.append(f"unclassified compatibility root: {child.name}")
    for fixture in actual_fixtures:
        if fixture.split("/", 1)[0] not in ROOTS:
            failures.append(f"fixture is outside a canonical root: {fixture}")

    for item in inventory.get("inlinePrograms", []):
        identity = item.get("historicIdentity", item.get("test", ""))
        fixture_name = item.get("fixture", "")
        classification = item.get("classification")
        assertion = item.get("assertion")
        if not identity or not fixture_name:
            failures.append("inventory rows require historic identity and fixture")
            continue
        if identity in identities:
            failures.append(f"duplicate historic identity: {identity}")
        identities.add(identity)
        actual_mappings[identity] = fixture_name
        fixture = COMPATIBILITY / fixture_name
        root = fixture_name.split("/", 1)[0]
        if classification not in ROOTS or root != classification:
            failures.append(f"classification/path mismatch for {identity}: {fixture_name}")
        if not (fixture / "test.lol").is_file():
            failures.append(f"missing fixture for {identity}: {fixture_name}")
            continue
        if classification in {"Shared", "KnownLciDivergence"} and not registration_exists(fixture):
            failures.append(f"fixture lacks active CMake registration: {fixture_name}")
        sidecar = ASSERTIONS.get(assertion)
        if sidecar is not None and not (fixture / sidecar).is_file():
            failures.append(f"{assertion} fixture lacks {sidecar}: {fixture_name}")
        if assertion == "Specialized":
            consumer = item.get("consumer")
            source_unit = item.get("sourceUnit")
            if not isinstance(consumer, str) or not consumer or not isinstance(source_unit, str) or not source_unit:
                failures.append(f"specialized mapping lacks consumer/source-unit identity: {identity}")
            else:
                body = next(
                    (body for source in sources if (body := method_body(source, consumer)) is not None),
                    None)
                if body is None:
                    failures.append(f"specialized consumer is not xUnit-discoverable: {consumer}")
                elif fixture_name not in body:
                    failures.append(f"specialized consumer does not consume fixture: {consumer} -> {fixture_name}")
        elif assertion not in ASSERTIONS:
            failures.append(f"unknown assertion kind for {identity}: {assertion}")
    if expected_mappings != actual_mappings:
        failures.append("historic inventory mappings were deleted or retargeted")

    catalog = json.loads(CLASSIFICATIONS.read_text(encoding="utf-8"))["knownLciDivergences"]
    divergence_root = COMPATIBILITY / "KnownLciDivergence"
    registered_divergences = {
        cmake.parent.relative_to(COMPATIBILITY).as_posix()
        for cmake in divergence_root.rglob("CMakeLists.txt")
        if registrations(cmake)
    }
    if set(catalog) != registered_divergences:
        failures.append("known-lci-divergence catalog must exactly classify every registered fixture")
    for fixture, metadata in catalog.items():
        if not metadata.get("category") or not metadata.get("evidence"):
            failures.append(f"missing category or evidence for {fixture}")

    for cmake in COMPATIBILITY.rglob("CMakeLists.txt"):
        try:
            active = registrations(cmake)
        except ValueError as error:
            failures.append(str(error))
            continue
        for registration in active:
            for keyword in ("LOLCODE", "OUTPUT", "INPUT"):
                path = registration.get(keyword)
                if path is not None and not Path(path).is_file():
                    failures.append(f"registered {keyword} path is missing: {path}")
            if registration["ERROR"]:
                err = cmake.parent / "test.err"
                diag = cmake.parent / "test.diag"
                if err.is_file() and diag.is_file():
                    failures.append(f"ERROR registration has both sidecars: {cmake.parent.relative_to(COMPATIBILITY)}")

    # Exceptions are scoped to the narrow construction rather than a whole file.
    for item in inventory.get("inlineExceptions", []):
        if not item.get("file") or not item.get("method") or not item.get("reason"):
            failures.append("inline exception requires file, method, and reason")

    if failures:
        print("Compatibility fixture validation failed:", file=sys.stderr)
        print("\n".join(f"  {failure}" for failure in failures), file=sys.stderr)
        return 1
    print(f"Validated {len(identities)} historic mappings and {len(actual_fixtures)} classified fixtures.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
