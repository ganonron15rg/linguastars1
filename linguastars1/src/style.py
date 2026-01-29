import json
from pathlib import Path

def load_tokens(path: Path) -> dict:
    data = json.loads(path.read_text(encoding="utf-8"))
    # minimal sanity checks
    for k in ["screen", "safe_margins", "palette", "outline", "object", "guide", "anim"]:
        if k not in data:
            raise ValueError(f"Missing token key: {k}")
    return data
