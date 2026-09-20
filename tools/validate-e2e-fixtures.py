#!/usr/bin/env python3
"""Validates the canonical EndToEnd fixture migration.

The inventory records every historic complete inline program and its durable fixture.
C# EndToEnd sources may not reintroduce HAI...KTHXBYE programs: specialized tests
must load their sources from the same fixture tree.
"""
from __future__ import annotations
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
COMPATIBILITY = ROOT / "tests" / "Compatibility"
END_TO_END = ROOT / "tests" / "Lolcode.EndToEnd.Tests"
PROGRAM = re.compile(r"\bHAI\s+[0-9.]+\b.*?\bKTHXBYE\b", re.DOTALL)
ROOTS = {"Shared", "KnownLciDivergence", "DotNet"}

def main() -> int:
    failures: list[str] = []
    inventory_path = COMPATIBILITY / "inventory.json"
    inventory = json.loads(inventory_path.read_text(encoding="utf-8"))
    identities: set[str] = set()
    for item in inventory["inlinePrograms"]:
        identity = item["test"]
        if identity in identities:
            failures.append(f"duplicate historic identity: {identity}")
        identities.add(identity)
        fixture = COMPATIBILITY / item["fixture"]
        if not (fixture / "test.lol").is_file():
            failures.append(f"missing fixture for {identity}: {item['fixture']}")
        if item["classification"] not in ROOTS:
            failures.append(f"unknown classification for {identity}: {item['classification']}")
        if item["fixture"].split("/", 1)[0] != item["classification"]:
            failures.append(f"classification/path mismatch for {identity}: {item['fixture']}")

    for source in END_TO_END.rglob("*.cs"):
        if PROGRAM.search(source.read_text(encoding="utf-8")):
            failures.append(f"complete inline LOLCODE program is forbidden: {source.relative_to(ROOT)}")

    if failures:
        print("EndToEnd fixture validation failed:", file=sys.stderr)
        print("\n".join(f"  {failure}" for failure in failures), file=sys.stderr)
        return 1
    print(f"Validated {len(identities)} historic inline programs; no inline EndToEnd programs remain.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
