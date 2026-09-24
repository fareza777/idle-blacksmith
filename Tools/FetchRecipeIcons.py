#!/usr/bin/env python3
"""FetchRecipeIcons.py — generates weapon icons for recipes via Replicate (flux-schnell).
Writes Art/Icons/Raw/<name>.png so the existing sprite importer picks them up.
Usage: REPLICATE_API_TOKEN=... python3 Tools/FetchRecipeIcons.py [--force] [--only name ...]
"""
import json
import os
import sys
import time
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ICON_DIR = os.path.join(ROOT, "Assets/_IdleBlacksmith/Art/Icons/Raw")
MODEL_URL = "https://api.replicate.com/v1/models/black-forest-labs/flux-schnell/predictions"
NO_TEXT = "absolutely no text, no letters, no words, no watermark, no logo text"
STYLE = (
    "flat chunky stylized mobile game weapon icon, single weapon centered and slightly "
    "diagonal, rich warm colors, soft painterly shading with a subtle rim of light, "
    "plain warm ivory background, no border, " + NO_TEXT
)

ART = [
    {"name": "copper",
     "prompt": STYLE + "a sturdy copper dagger, warm orange-brown blade with a soft metallic "
               "sheen, wrapped dark leather grip, simple round pommel"},
    {"name": "iron",
     "prompt": STYLE + "a classic iron longsword, honest grey blade, simple straight "
               "crossguard, brown leather wrapped grip"},
    {"name": "steel",
     "prompt": STYLE + "an elegant curved steel sabre, bright polished silver blade with a "
               "subtle sheen, small brass knuckle guard, dark grip"},
    {"name": "silver",
     "prompt": STYLE + "a slender silver rapier, pale gleaming narrow blade, ornate swept "
               "hilt of interlaced silver wire, elegant and precise"},
    {"name": "mithril",
     "prompt": STYLE + "a mystical mint-teal mithril katana, softly glowing pale edge, "
               "dark wrapped handle, slim elegant curve"},
    {"name": "dragonsteel",
     "prompt": STYLE + "a massive dragonsteel greatsword, deep red blade veined with "
               "glowing ember cracks, heavy black crossguard, imposing silhouette"},
]


def post(url, token, payload, timeout=90):
    req = urllib.request.Request(
        url,
        data=json.dumps(payload).encode(),
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json",
            "Prefer": "wait=55",
        },
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return json.loads(r.read())


def get(url, token, timeout=60):
    req = urllib.request.Request(url, headers={"Authorization": f"Bearer {token}"})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return json.loads(r.read())


def fetch(name, prompt, token, force):
    out = os.path.join(ICON_DIR, name + ".png")
    if not force and os.path.exists(out) and os.path.getsize(out) > 300000:
        print(f"{name}: present", flush=True)
        return True
    for attempt in (1, 2):
        try:
            resp = post(MODEL_URL, token, {
                "input": {"prompt": prompt, "aspect_ratio": "1:1",
                          "output_format": "png", "num_outputs": 1}
            })
            deadline = time.time() + 150
            while resp.get("status") not in ("succeeded", "failed", "canceled") and time.time() < deadline:
                time.sleep(3)
                resp = get(resp["urls"]["get"], token, 30)
            if resp.get("status") != "succeeded":
                print(f"{name}: try {attempt} status={resp.get('status')}", flush=True)
                continue
            urls = resp["output"]
            url = urls[0] if isinstance(urls, list) else urls
            with urllib.request.urlopen(url, timeout=120) as r:
                data = r.read()
            if len(data) > 300000 and data[:2] == b"\x89P":
                os.makedirs(ICON_DIR, exist_ok=True)
                with open(out, "wb") as f:
                    f.write(data)
                print(f"{name}: OK {len(data)}B", flush=True)
                return True
            print(f"{name}: try {attempt} bad payload", flush=True)
        except Exception as e:
            print(f"{name}: try {attempt} error {e}", flush=True)
    print(f"{name}: FAILED", flush=True)
    return False


def main():
    token = (os.environ.get("REPLICATE_API_TOKEN") or "").strip()
    if not token:
        sys.exit("REPLICATE_API_TOKEN missing")
    force = "--force" in sys.argv
    only = sys.argv[sys.argv.index("--only") + 1:] if "--only" in sys.argv else []
    for a in ART:
        if only and a["name"] not in only:
            continue
        fetch(a["name"], a["prompt"], token, force)
    print("DONE", flush=True)


if __name__ == "__main__":
    main()
