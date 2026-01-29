from __future__ import annotations
import json
import urllib.request

def send_scene_completed(url: str, payload: dict) -> None:
    """
    Optional n8n webhook integration.
    Keep disabled by default so the game runs without network requirements.
    """
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(url, data=data, headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=5) as resp:
        _ = resp.read()
