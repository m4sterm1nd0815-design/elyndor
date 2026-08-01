import importlib.util
import json
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

MODULE_PATH = Path(__file__).parents[1] / "asset_fetcher.py"
SPEC = importlib.util.spec_from_file_location("asset_fetcher", MODULE_PATH)
fetcher = importlib.util.module_from_spec(SPEC)
assert SPEC.loader
sys.modules[SPEC.name] = fetcher
SPEC.loader.exec_module(fetcher)


class AssetFetcherTests(unittest.TestCase):
    def setUp(self):
        self.config = fetcher.load_config()

    def test_license_filter_accepts_only_configured_cc0(self):
        self.assertEqual(fetcher.ensure_cc0("CC0", self.config), "CC0")
        self.assertEqual(fetcher.ensure_cc0("CC0-1.0", self.config), "CC0-1.0")
        for value in (None, "", "CC-BY", "Royalty Free", "unknown"):
            with self.assertRaises(fetcher.LicenseRejected):
                fetcher.ensure_cc0(value, self.config)

    def test_zip_slip_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            archive = Path(temp) / "unsafe.zip"
            with zipfile.ZipFile(archive, "w") as zf:
                zf.writestr("../escape.png", b"x")
            with self.assertRaises(fetcher.UnsafeArchive):
                fetcher.validate_zip(archive, self.config)

    def test_executable_in_archive_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            archive = Path(temp) / "unsafe.zip"
            with zipfile.ZipFile(archive, "w") as zf:
                zf.writestr("setup.exe", b"MZ")
            with self.assertRaises(fetcher.UnsafeArchive):
                fetcher.validate_zip(archive, self.config)

    def test_safe_archive_extracts_only_to_temp(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            archive = root / "safe.zip"
            target = root / "extract"
            target.mkdir()
            with zipfile.ZipFile(archive, "w") as zf:
                zf.writestr("textures/rock_Color.png", b"png")
            paths = fetcher.safe_extract_zip(archive, target, self.config)
            self.assertEqual(paths, [target / "textures" / "rock_Color.png"])

    def test_manifest_record_has_required_provenance(self):
        result = {"name": "Test Rock", "source": "Poly Haven", "source_id": "test_rock",
                  "authors": {"Artist": "All"}, "license": "CC0", "type": "model",
                  "category": "Rocks", "source_url": "https://polyhaven.com/a/test_rock"}
        record = fetcher.make_record(result, self.config, "2k")
        with tempfile.TemporaryDirectory(dir=fetcher.PROJECT_ROOT) as temp:
            manifest = Path(temp) / "manifest.json"
            manifest.write_text('{"schema_version": 1, "assets": []}\n', encoding="utf-8")
            fetcher.write_manifest_record(record, manifest)
            saved = json.loads(manifest.read_text(encoding="utf-8"))["assets"][0]
        for key in ("name", "source", "source_id", "author", "license", "download_date",
                    "original_files", "hash", "unity_target_path", "changes", "import_report"):
            self.assertIn(key, saved)
        self.assertFalse(saved["import_report"]["materials_overwritten"])


if __name__ == "__main__":
    unittest.main()
