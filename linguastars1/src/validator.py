from __future__ import annotations
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Any


@dataclass(frozen=True)
class Curriculum:
    meta: Dict[str, Any]
    colors: Dict[str, Dict[str, Any]]        # id -> {hex,name}
    shapes: List[str]
    voice_lines: Dict[str, Any]
    hint_defaults: Dict[str, Any]


def load_curriculum(path: Path) -> Curriculum:
    raw = json.loads(path.read_text(encoding="utf-8"))

    # Strict validation (anti-hallucination)
    required_top = ["meta", "colors", "shapes", "voice_lines", "hint_policy_defaults"]
    for k in required_top:
        if k not in raw:
            raise ValueError(f"Curriculum missing key: {k}")

    colors = {}
    for c in raw["colors"]:
        for k in ["id", "hex", "name"]:
            if k not in c:
                raise ValueError(f"Color missing '{k}'")
        colors[c["id"]] = {"hex": c["hex"], "name": c["name"]}

    shapes = [s["id"] for s in raw["shapes"]]
    if len(shapes) == 0:
        raise ValueError("No shapes provided")

    voice_lines = raw["voice_lines"]
    # Ensure templates exist
    for k in ["intro", "prompt", "label", "oops", "praise_short", "reward", "hint_repeat"]:
        if k not in voice_lines:
            raise ValueError(f"voice_lines missing '{k}'")

    return Curriculum(
        meta=raw["meta"],
        colors=colors,
        shapes=shapes,
        voice_lines=voice_lines,
        hint_defaults=raw["hint_policy_defaults"],
    )


def assert_color(curr: Curriculum, color_id: str) -> None:
    if color_id not in curr.colors:
        raise ValueError(f"Unknown color id: {color_id}")


def assert_shape(curr: Curriculum, shape_id: str) -> None:
    if shape_id not in curr.shapes:
        raise ValueError(f"Unknown shape id: {shape_id}")


def format_line(template: str, *, color: str | None = None, c1: str | None = None, c2: str | None = None, c3: str | None = None) -> str:
    # Only allow known placeholders. No free-form generation.
    out = template
    if "{COLOR}" in out:
        if color is None:
            raise ValueError("COLOR placeholder requires 'color'")
        out = out.replace("{COLOR}", color.upper())
    if "{C1}" in out:
        if c1 is None or c2 is None or c3 is None:
            raise ValueError("C1/C2/C3 placeholders require c1,c2,c3")
        out = out.replace("{C1}", c1.capitalize()).replace("{C2}", c2.capitalize()).replace("{C3}", c3.capitalize())
    # Disallow any other braces (simple guard)
    if "{" in out or "}" in out:
        raise ValueError("Unexpected placeholder in template")
    return out
