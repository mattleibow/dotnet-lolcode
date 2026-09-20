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
PROGRAM = re.compile(r"\bHAI\s+(?:[0-9.]+|\{\{version\}\})\b[^\n]*\n.*?\bKTHXBYE\b", re.DOTALL)
ROOTS = {"Shared", "KnownLciDivergence", "DotNet"}
ASSERTIONS = {
    "Output": "test.out",
    "RuntimeError": "test.err",
    "CompileError": "test.diag",
}


def registration_exists(fixture: Path) -> bool:
    cmake = fixture / "CMakeLists.txt"
    return cmake.is_file() and "ADD_LOL_TEST" in cmake.read_text(encoding="utf-8")


def test_sources() -> str:
    return "\n".join(
        path.read_text(encoding="utf-8")
        for root in TEST_ROOTS
        for path in root.rglob("*.cs")
    )


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
    for item in inventory.get("inlinePrograms", []):
        identity = item.get("test", "")
        fixture_name = item.get("fixture", "")
        classification = item.get("classification")
        assertion = item.get("assertion")
        if not identity or not fixture_name:
            failures.append("inventory rows require test and fixture")
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
        if classification == "Shared" and not registration_exists(fixture):
            failures.append(f"shared fixture lacks CMake registration: {fixture_name}")
        if classification == "KnownLciDivergence" and not registration_exists(fixture):
            failures.append(f"divergence fixture lacks CMake registration: {fixture_name}")
        sidecar = ASSERTIONS.get(assertion)
        if sidecar is not None and not (fixture / sidecar).is_file():
            failures.append(f"{assertion} fixture lacks {sidecar}: {fixture_name}")
        if assertion == "Specialized":
            method = identity.rsplit(".", 1)[-1]
            if not re.search(rf"\b{re.escape(method)}\s*\(", sources):
                failures.append(f"specialized mapping has no consuming C# test: {identity}")
            if fixture_name not in sources:
                failures.append(f"specialized mapping is stale or retargeted: {identity} -> {fixture_name}")
        elif assertion not in ASSERTIONS:
            failures.append(f"unknown assertion kind for {identity}: {assertion}")
    if expected_mappings != actual_mappings:
        failures.append("historic inventory mappings were deleted or retargeted")

    catalog = json.loads(CLASSIFICATIONS.read_text(encoding="utf-8"))["knownLciDivergences"]
    registered_divergences = {
        str(cmake.parent.relative_to(COMPATIBILITY)).replace("\\", "/")
        for cmake in (COMPATIBILITY / "KnownLciDivergence").glob("**/CMakeLists.txt")
    }
    if set(catalog) != registered_divergences:
        failures.append("known-lci-divergence catalog must exactly classify every registered fixture")
    for fixture, metadata in catalog.items():
        if not metadata.get("category") or not metadata.get("evidence"):
            failures.append(f"missing category or evidence for {fixture}")

    exception_paths = {ROOT / item["file"] for item in inventory.get("inlineExceptions", [])}
    for item in inventory.get("inlineExceptions", []):
        if not item.get("reason"):
            failures.append(f"inline exception lacks reason: {item.get('file')}")
    for root in TEST_ROOTS:
        for source in root.rglob("*.cs"):
            if PROGRAM.search(source.read_text(encoding="utf-8")) and source not in exception_paths:
                failures.append(f"complete inline LOLCODE program is not inventoried: {source.relative_to(ROOT)}")

    if failures:
        print("Compatibility fixture validation failed:", file=sys.stderr)
        print("\n".join(f"  {failure}" for failure in failures), file=sys.stderr)
        return 1
    print(f"Validated {len(identities)} historic mappings and {len(exception_paths)} narrow inline exceptions.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
