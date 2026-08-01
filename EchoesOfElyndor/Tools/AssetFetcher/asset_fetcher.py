#!/usr/bin/env python3
"""Safe metadata discovery and dry-run planning for approved CC0 asset sources."""

from __future__ import annotations

import argparse
import hashlib
import json
import logging
import re
import shutil
import stat
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from dataclasses import asdict, dataclass, field
from pathlib import Path, PurePosixPath
from typing import Any, Iterable

TOOL_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = TOOL_DIR.parents[1]
CONFIG_PATH = TOOL_DIR / "approved_sources.json"
MANIFEST_PATH = TOOL_DIR / "asset_manifest.json"
UNITY_ROOT = PROJECT_ROOT / "Assets" / "_Elyndor" / "ThirdParty"
TYPE_NAMES = {0: "hdri", 1: "texture", 2: "model"}
PBR_MARKERS = {
    "albedo": ("color", "colour", "albedo", "diff", "basecolor"),
    "normal": ("normal", "nor_gl", "nor_dx"),
    "roughness": ("rough", "roughness"),
    "displacement": ("disp", "displacement", "height"),
    "ambient_occlusion": ("ao", "ambientocclusion"),
    "metallic": ("metal", "metallic"),
    "opacity": ("opacity", "alpha"),
}
LOG = logging.getLogger("asset_fetcher")


class AssetFetcherError(RuntimeError):
    pass


class LicenseRejected(AssetFetcherError):
    pass


class UnsafeArchive(AssetFetcherError):
    pass


class NoRedirects(urllib.request.HTTPRedirectHandler):
    """Reject redirects so an approved API host cannot bounce to an unapproved host."""

    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise AssetFetcherError(f"API-Redirect aus Sicherheitsgründen abgelehnt: {code}")


@dataclass
class AssetRecord:
    name: str
    source: str
    source_id: str
    author: str
    license: str
    download_date: str | None
    original_files: list[dict[str, Any]]
    hash: str | None
    unity_target_path: str
    changes: list[str] = field(default_factory=list)
    status: str = "planned"
    source_url: str | None = None
    asset_type: str | None = None
    category: str | None = None
    resolution: str | None = None
    pbr_maps: list[str] = field(default_factory=list)
    import_report: dict[str, Any] = field(default_factory=dict)


def load_config(path: Path = CONFIG_PATH) -> dict[str, Any]:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise AssetFetcherError(f"Konfiguration nicht lesbar: {path}: {exc}") from exc


def ensure_cc0(license_name: str | None, config: dict[str, Any]) -> str:
    normalized = (license_name or "").strip().upper().replace(" ", "-")
    allowed = {item.upper() for item in config["allowed_licenses"]}
    if normalized not in allowed:
        raise LicenseRejected(f"Lizenz abgelehnt: {license_name or 'nicht angegeben'}")
    return "CC0" if normalized == "CC0" else "CC0-1.0"


def safe_slug(value: str) -> str:
    slug = re.sub(r"[^a-zA-Z0-9_-]+", "_", value.strip()).strip("_")
    if not slug or slug in {".", ".."}:
        raise AssetFetcherError("Assetname ergibt keinen sicheren Zielordner")
    return slug[:120]


def repository_path(path: Path, *, must_exist: bool = False) -> Path:
    resolved = path.resolve(strict=must_exist)
    try:
        resolved.relative_to(PROJECT_ROOT.resolve())
    except ValueError as exc:
        raise AssetFetcherError(f"Pfad liegt außerhalb des Unity-Projekts: {path}") from exc
    return resolved


def unity_target(source: str, name: str) -> str:
    path = UNITY_ROOT / safe_slug(source) / safe_slug(name)
    repository_path(path)
    return path.relative_to(PROJECT_ROOT).as_posix() + "/"


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return f"sha256:{digest.hexdigest()}"


def validate_file(path: Path, config: dict[str, Any]) -> dict[str, Any]:
    path = path.resolve(strict=True)
    if not path.is_file():
        raise AssetFetcherError(f"Keine Datei: {path}")
    extension = path.suffix.lower()
    if extension in config["blocked_extensions"]:
        raise AssetFetcherError(f"Ausführbarer/aktiver Dateityp abgelehnt: {extension}")
    allowed = set(config["unity_extensions"] + config["archive_extensions"])
    if extension not in allowed:
        raise AssetFetcherError(f"Nicht zugelassener Dateityp: {extension or '(ohne Endung)'}")
    size = path.stat().st_size
    if size > int(config["network"]["max_asset_bytes"]):
        raise AssetFetcherError(f"Datei überschreitet Größenlimit: {size} Bytes")
    return {"name": path.name, "size": size, "sha256": sha256_file(path)}


def validate_zip(archive: Path, config: dict[str, Any]) -> list[dict[str, Any]]:
    validate_file(archive, config)
    total = 0
    members: list[dict[str, Any]] = []
    blocked = set(config["blocked_extensions"])
    allowed = set(config["unity_extensions"])
    try:
        with zipfile.ZipFile(archive) as zf:
            for info in zf.infolist():
                name = info.filename.replace("\\", "/")
                pure = PurePosixPath(name)
                if pure.is_absolute() or ".." in pure.parts or re.match(r"^[A-Za-z]:", name):
                    raise UnsafeArchive(f"ZIP-Slip/Pfad-Traversal abgelehnt: {info.filename}")
                mode = info.external_attr >> 16
                if mode and stat.S_ISLNK(mode):
                    raise UnsafeArchive(f"Symbolischer Link im Archiv abgelehnt: {info.filename}")
                if info.is_dir():
                    continue
                ext = pure.suffix.lower()
                if ext in blocked or ext not in allowed:
                    raise UnsafeArchive(f"Nicht zugelassene Archivdatei: {info.filename}")
                total += info.file_size
                if total > int(config["network"]["max_asset_bytes"]):
                    raise UnsafeArchive("Entpackte Archivgröße überschreitet das Limit")
                members.append({"name": name, "size": info.file_size, "crc32": f"{info.CRC:08x}"})
    except zipfile.BadZipFile as exc:
        raise UnsafeArchive(f"Ungültiges ZIP-Archiv: {archive}") from exc
    return members


def safe_extract_zip(archive: Path, destination: Path, config: dict[str, Any]) -> list[Path]:
    """Extract only into an existing temp directory; never into the repository."""
    destination = destination.resolve(strict=True)
    temp_root = Path(tempfile.gettempdir()).resolve()
    try:
        destination.relative_to(temp_root)
    except ValueError as exc:
        raise UnsafeArchive("Archive dürfen nur in einen temporären Ordner entpackt werden") from exc
    members = validate_zip(archive, config)
    extracted: list[Path] = []
    with zipfile.ZipFile(archive) as zf:
        for member in members:
            target = (destination / member["name"]).resolve()
            try:
                target.relative_to(destination)
            except ValueError as exc:
                raise UnsafeArchive(f"Unsicherer Zielpfad: {member['name']}") from exc
            target.parent.mkdir(parents=True, exist_ok=True)
            if target.exists():
                raise UnsafeArchive(f"Bestehende Datei würde überschrieben: {target}")
            with zf.open(member["name"]) as source, target.open("xb") as output:
                shutil.copyfileobj(source, output)
            extracted.append(target)
    return extracted


def request_json(url: str, config: dict[str, Any], allowed_hosts: Iterable[str]) -> Any:
    parsed = urllib.parse.urlparse(url)
    if parsed.scheme != "https" or parsed.hostname not in set(allowed_hosts):
        raise AssetFetcherError(f"Nicht zugelassene API-URL: {url}")
    request = urllib.request.Request(url, headers={
        "User-Agent": config["user_agent"], "Accept": "application/json"
    })
    limit = int(config["network"]["max_metadata_bytes"])
    try:
        opener = urllib.request.build_opener(NoRedirects())
        with opener.open(request, timeout=config["network"]["timeout_seconds"]) as response:
            content_type = response.headers.get_content_type()
            length = response.headers.get("Content-Length")
            if content_type not in {"application/json", "text/json"}:
                raise AssetFetcherError(f"Unerwarteter API-Inhaltstyp: {content_type}")
            if length and int(length) > limit:
                raise AssetFetcherError("API-Antwort überschreitet Größenlimit")
            payload = response.read(limit + 1)
            if len(payload) > limit:
                raise AssetFetcherError("API-Antwort überschreitet Größenlimit")
            return json.loads(payload)
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError) as exc:
        raise AssetFetcherError(f"API-Anfrage fehlgeschlagen: {exc}") from exc


def resolution_matches(meta: dict[str, Any], requested: str | None) -> bool:
    if not requested:
        return True
    match = re.fullmatch(r"(\d+)[kK]", requested)
    if not match:
        raise AssetFetcherError("Auflösung muss wie 1k, 2k, 4k, 8k oder 16k angegeben werden")
    required = int(match.group(1)) * 1024
    maximum = meta.get("max_resolution") or []
    return bool(maximum) and max(maximum) >= required


def search_poly_haven(args: argparse.Namespace, config: dict[str, Any]) -> list[dict[str, Any]]:
    source = config["sources"]["poly_haven"]
    LOG.info("Poly-Haven-API wird verwendet (Herkunft: https://polyhaven.com)")
    data = request_json(f"{source['api_base']}/assets", config, source["allowed_hosts"])
    requested_type = args.type.lower() if args.type else None
    keyword = (args.keyword or "").casefold()
    category = (args.category or "").casefold()
    results = []
    for source_id, meta in data.items():
        asset_type = TYPE_NAMES.get(meta.get("type"), "unknown")
        haystack = " ".join([source_id, meta.get("name", ""), meta.get("description", ""),
                             meta.get("category", ""), *meta.get("tags", [])]).casefold()
        if requested_type and asset_type != requested_type:
            continue
        if keyword and keyword not in haystack:
            continue
        if category and category not in meta.get("category", "").casefold():
            continue
        if not resolution_matches(meta, args.resolution):
            continue
        results.append({
            "name": meta.get("name", source_id), "source": "Poly Haven", "source_id": source_id,
            "type": asset_type, "category": meta.get("category"), "tags": meta.get("tags", []),
            "authors": meta.get("authors", {}), "license": "CC0",
            "max_resolution": meta.get("max_resolution"), "thumbnail_url": meta.get("thumbnail_url"),
            "source_url": f"https://polyhaven.com/a/{source_id}", "api_provenance": "Poly Haven public API",
        })
    return results[: args.limit]


def search_ambientcg(args: argparse.Namespace, config: dict[str, Any]) -> list[dict[str, Any]]:
    source = config["sources"]["ambientcg"]
    query = {"limit": min(max(args.limit, 1), 100), "include": "tagData,downloadData"}
    if args.keyword:
        query["q"] = args.keyword
    if args.type:
        query["type"] = {"texture": "Material", "model": "3DModel", "surface": "Material"}.get(args.type, args.type)
    url = f"{source['api_base']}/full_json?{urllib.parse.urlencode(query)}"
    data = request_json(url, config, source["allowed_hosts"])
    assets = data.get("foundAssets") or data.get("assets") or []
    results = []
    for meta in assets:
        categories = meta.get("category") or meta.get("categories") or []
        category_text = categories if isinstance(categories, str) else " ".join(str(x) for x in categories)
        if args.category and args.category.casefold() not in category_text.casefold():
            continue
        downloads = meta.get("downloadData") or meta.get("downloads") or []
        download_names = json.dumps(downloads).casefold()
        if args.resolution and args.resolution.casefold() not in download_names:
            continue
        pbr_maps = [name for name, markers in PBR_MARKERS.items() if any(x in download_names for x in markers)]
        results.append({"name": meta.get("name") or meta.get("displayName") or meta.get("id"),
                        "source": "ambientCG", "source_id": meta.get("id"),
                        "type": meta.get("type"), "category": meta.get("category") or meta.get("categories"),
                        "tags": meta.get("tags") or meta.get("tagData") or [], "license": "CC0",
                        "pbr_maps": pbr_maps, "source_url": f"https://ambientcg.com/view?id={meta.get('id')}",
                        "download_variants": downloads})
    return results[: args.limit]


def search_kenney(args: argparse.Namespace, config: dict[str, Any]) -> list[dict[str, Any]]:
    return [{"source": "Kenney", "query": args.keyword, "type": args.type, "category": args.category,
             "license_requirement": "Nur explizit CC0", "catalog_url": config["sources"]["kenney"]["catalog_url"],
             "status": "manual_search_and_download_required",
             "reason": "Kein stabiler offizieller API-/Downloadweg konfiguriert; Website wird nicht automatisiert gescrapt."}]


def make_record(result: dict[str, Any], config: dict[str, Any], resolution: str | None) -> AssetRecord:
    license_name = ensure_cc0(result.get("license"), config)
    authors = result.get("authors") or result.get("author") or "Unbekannt"
    if isinstance(authors, dict):
        authors = ", ".join(authors.keys())
    source = result["source"]
    name = result.get("name") or result["source_id"]
    target = unity_target(source, name)
    if (PROJECT_ROOT / target).exists():
        raise AssetFetcherError(f"Bestehendes Assetziel wird nicht überschrieben: {target}")
    return AssetRecord(
        name=name, source=source, source_id=str(result["source_id"]), author=str(authors),
        license=license_name, download_date=None, original_files=[], hash=None,
        unity_target_path=target, changes=["Keine (Dry-Run)"],
        source_url=result.get("source_url"), asset_type=result.get("type"),
        category=result.get("category"), resolution=resolution,
        pbr_maps=result.get("pbr_maps", []),
        import_report={"mode": "dry-run", "files_imported": 0, "materials_overwritten": False,
                       "prefab_created": False, "colliders_added": False, "lods_added": False,
                       "notes": ["Technische Prüfung vor jedem Import erforderlich"]},
    )


def write_manifest_record(record: AssetRecord, manifest_path: Path = MANIFEST_PATH) -> None:
    repository_path(manifest_path)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    key = (record.source.casefold(), record.source_id)
    if any((item["source"].casefold(), item["source_id"]) == key for item in manifest["assets"]):
        raise AssetFetcherError(f"Manifest enthält das Asset bereits: {record.source}/{record.source_id}")
    manifest["assets"].append(asdict(record))
    manifest_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Sichere CC0-Asset-Suche und Unity-Importplanung (keine Downloads in Phase 1).")
    parser.add_argument("--verbose", action="store_true", help="Ausführlichere, secret-freie Logs")
    sub = parser.add_subparsers(dest="command", required=True)
    search = sub.add_parser("search", help="Freigegebene Quelle durchsuchen; gibt nur Metadaten aus")
    search.add_argument("--source", required=True, choices=["poly-haven", "ambientcg", "kenney"])
    search.add_argument("--type", choices=["hdri", "texture", "surface", "model"])
    search.add_argument("--category", help="Kategorie (Teiltreffer)")
    search.add_argument("--keyword", help="Stichwort (Teiltreffer)")
    search.add_argument("--resolution", help="Mindest-/gewünschte Auflösung, z. B. 2k oder 4k")
    search.add_argument("--limit", type=int, default=10, help="Maximale Trefferzahl (1-100, Standard: 10)")
    preview = sub.add_parser("preview", help="Gespeicherte Suchergebnis-JSON sicher als Importvorschau anzeigen")
    preview.add_argument("result_file", type=Path)
    preview.add_argument("--index", type=int, default=0)
    dry = sub.add_parser("dry-run", help="Importbericht erzeugen, optional geplanten Manifest-Eintrag speichern")
    dry.add_argument("result_file", type=Path)
    dry.add_argument("--index", type=int, default=0)
    dry.add_argument("--resolution")
    dry.add_argument("--write-manifest", action="store_true")
    archive = sub.add_parser("validate-archive", help="Lokales ZIP ohne Entpacken auf Sicherheit prüfen")
    archive.add_argument("archive", type=Path)
    return parser


def load_results(path: Path) -> list[dict[str, Any]]:
    data = json.loads(path.read_text(encoding="utf-8"))
    return data if isinstance(data, list) else data.get("results", [])


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    logging.basicConfig(level=logging.DEBUG if args.verbose else logging.INFO,
                        format="%(levelname)s %(name)s: %(message)s")
    try:
        config = load_config()
        if args.command == "search":
            if not 1 <= args.limit <= 100:
                raise AssetFetcherError("--limit muss zwischen 1 und 100 liegen")
            handlers = {"poly-haven": search_poly_haven, "ambientcg": search_ambientcg, "kenney": search_kenney}
            print(json.dumps({"mode": "metadata-only", "downloads_performed": 0,
                              "results": handlers[args.source](args, config)}, indent=2, ensure_ascii=False))
        elif args.command in {"preview", "dry-run"}:
            results = load_results(args.result_file)
            if not results or args.index < 0 or args.index >= len(results):
                raise AssetFetcherError("Ungültiger Ergebnisindex oder leere Ergebnisdatei")
            if args.command == "preview":
                print(json.dumps(results[args.index], indent=2, ensure_ascii=False))
            else:
                record = make_record(results[args.index], config, args.resolution)
                if args.write_manifest:
                    write_manifest_record(record)
                print(json.dumps(asdict(record), indent=2, ensure_ascii=False))
        else:
            members = validate_zip(args.archive, config)
            print(json.dumps({"safe": True, "members": members}, indent=2, ensure_ascii=False))
        return 0
    except (AssetFetcherError, OSError, json.JSONDecodeError, IndexError) as exc:
        LOG.error("%s", exc)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
