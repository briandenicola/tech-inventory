#!/usr/bin/env python3
"""Compare two OpenAPI documents by structure rather than by bytes.

The committed `openapi.yaml` is JSON (it was produced by scraping the running
API's `/openapi/v1.json`), while `OpenApiDocumentExporter` writes YAML via
`OpenApiYamlWriter`. A textual `git diff` between them reports the entire file
as changed every time, which is serializer noise, not drift. Both formats parse
to the same object model, so compare that instead and report the paths that
actually differ.

Usage: compare-openapi.py <committed> <freshly-generated>
Exit 0 when the documents describe the same API, 1 otherwise.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path


def load(path: Path):
    text = path.read_text(encoding="utf-8")
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        pass
    try:
        import yaml
    except ImportError:
        sys.exit(
            f"{path} is not JSON and PyYAML is unavailable to parse it as YAML.\n"
            "Install it with: python3 -m pip install pyyaml"
        )
    return yaml.safe_load(text)


def walk(committed, generated, path="", out=None):
    """Collect human-readable differences between two parsed documents."""
    if out is None:
        out = []

    if type(committed) is not type(generated):
        out.append(f"{path or '<root>'}: type changed "
                   f"({type(committed).__name__} -> {type(generated).__name__})")
        return out

    if isinstance(committed, dict):
        for key in sorted(set(committed) | set(generated)):
            child = f"{path}.{key}" if path else key
            if key not in committed:
                out.append(f"{child}: missing from the committed document (added to the API)")
            elif key not in generated:
                out.append(f"{child}: present only in the committed document (removed from the API)")
            else:
                walk(committed[key], generated[key], child, out)
    elif isinstance(committed, list):
        if len(committed) != len(generated):
            out.append(f"{path}: list length {len(committed)} -> {len(generated)}")
        else:
            for index, (left, right) in enumerate(zip(committed, generated)):
                walk(left, right, f"{path}[{index}]", out)
    elif committed != generated:
        out.append(f"{path}: {committed!r} -> {generated!r}")

    return out


def main() -> int:
    if len(sys.argv) != 3:
        sys.exit(__doc__)

    committed_path, generated_path = Path(sys.argv[1]), Path(sys.argv[2])
    differences = walk(load(committed_path), load(generated_path))

    if not differences:
        print(f"OpenAPI document is current ({committed_path}).")
        return 0

    print(f"{committed_path} is out of sync with the API project.", file=sys.stderr)
    print("Run 'task openapi:export' and commit the result.\n", file=sys.stderr)
    shown = differences[:40]
    for difference in shown:
        print(f"  {difference}", file=sys.stderr)
    if len(differences) > len(shown):
        print(f"  ... and {len(differences) - len(shown)} more", file=sys.stderr)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
