#!/usr/bin/env python3
"""Negative regression tests for tools/validate-e2e-fixtures.py."""
from __future__ import annotations
import contextlib
import importlib.util
import io
import json
import shutil
import unittest
from pathlib import Path

SCRIPT = Path(__file__).with_name("validate-e2e-fixtures.py")
spec = importlib.util.spec_from_file_location("fixture_validator", SCRIPT)
validator = importlib.util.module_from_spec(spec)
assert spec.loader is not None
spec.loader.exec_module(validator)


class FixtureValidatorTests(unittest.TestCase):
    work = Path(__file__).parent / ".fixture-validator-tests"

    def setUp(self) -> None:
        shutil.rmtree(self.work, ignore_errors=True)
        (self.work / "tests" / "Compatibility" / "Shared" / "1.2" / "Basic" / "case").mkdir(parents=True)
        fixture = self.work / "tests" / "Compatibility" / "Shared" / "1.2" / "Basic" / "case"
        (fixture / "test.lol").write_text("HAI 1.2\nKTHXBYE\n", encoding="utf-8")
        (fixture / "test.out").write_text("", encoding="utf-8")
        (fixture / "CMakeLists.txt").write_text("ADD_LOL_TEST(case OUTPUT test.out)\n", encoding="utf-8")
        classifications = self.work / "tools"
        classifications.mkdir()
        (classifications / "compatibility-classifications.json").write_text(
            json.dumps({"knownLciDivergences": {}}), encoding="utf-8")
        for name in ("Lolcode.EndToEnd.Tests", "Lolcode.CodeAnalysis.Tests", "Lolcode.Web.Tests"):
            (self.work / "tests" / name).mkdir(parents=True)
        self.inventory = {
            "schemaVersion": 2,
            "expectedMappings": {"Case.Output": "Shared/1.2/Basic/case"},
            "inlinePrograms": [{
                "test": "Case.Output",
                "assertion": "Output",
                "fixture": "Shared/1.2/Basic/case",
                "classification": "Shared",
            }],
            "inlineExceptions": [],
        }
        self.write_inventory()
        self.old = (validator.ROOT, validator.COMPATIBILITY, validator.CLASSIFICATIONS, validator.TEST_ROOTS)
        validator.ROOT = self.work
        validator.COMPATIBILITY = self.work / "tests" / "Compatibility"
        validator.CLASSIFICATIONS = self.work / "tools" / "compatibility-classifications.json"
        validator.TEST_ROOTS = [
            self.work / "tests" / "Lolcode.EndToEnd.Tests",
            self.work / "tests" / "Lolcode.CodeAnalysis.Tests",
            self.work / "tests" / "Lolcode.Web.Tests",
        ]

    def tearDown(self) -> None:
        validator.ROOT, validator.COMPATIBILITY, validator.CLASSIFICATIONS, validator.TEST_ROOTS = self.old
        shutil.rmtree(self.work, ignore_errors=True)

    def write_inventory(self) -> None:
        path = self.work / "tests" / "Compatibility" / "inventory.json"
        path.write_text(json.dumps(self.inventory), encoding="utf-8")

    def validate(self) -> str:
        error = io.StringIO()
        with contextlib.redirect_stderr(error):
            self.assertEqual(validator.main(), 1)
        return error.getvalue()

    def test_missing_registration_is_rejected(self) -> None:
        (self.work / "tests" / "Compatibility" / "Shared" / "1.2" / "Basic" / "case" / "CMakeLists.txt").unlink()
        self.assertIn("lacks CMake registration", self.validate())

    def test_missing_assertion_file_is_rejected(self) -> None:
        (self.work / "tests" / "Compatibility" / "Shared" / "1.2" / "Basic" / "case" / "test.out").unlink()
        self.assertIn("lacks test.out", self.validate())

    def test_stale_mapping_is_rejected(self) -> None:
        self.inventory["inlinePrograms"][0]["fixture"] = "Shared/1.2/Basic/other"
        self.write_inventory()
        self.assertIn("missing fixture", self.validate())

    def test_identity_mismatch_is_rejected(self) -> None:
        self.inventory["inlinePrograms"][0]["test"] = "Case.Renamed"
        self.write_inventory()
        self.assertIn("historic inventory mappings", self.validate())


if __name__ == "__main__":
    unittest.main()
