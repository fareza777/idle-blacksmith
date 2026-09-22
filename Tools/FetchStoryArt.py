#!/usr/bin/env python3
"""FetchStoryArt.py — generates portraits, cinematic-intro panels and the app icon
via Replicate (flux-schnell). Mirrors Tools/FetchMenuArt.ps1.
Usage: REPLICATE_API_TOKEN=... python3 Tools/FetchStoryArt.py [--force] [--only name ...]
"""
import json
import os
import sys
import time
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MENU_DIR = os.path.join(ROOT, "Assets/_IdleBlacksmith/Art/Menu")
APP_DIR = os.path.join(ROOT, "Assets/_IdleBlacksmith/Art/App")
MODEL_URL = "https://api.replicate.com/v1/models/black-forest-labs/flux-schnell/predictions"
NO_TEXT = "absolutely no text, no letters, no words, no watermark, no logo text"
PORTRAIT_STYLE = (
    "flat chunky stylized mobile game character portrait, head and shoulders bust, "
    "front facing, three-quarter view, rich warm colors, soft painterly shading, "
    "single character only, plain deep warm brown gradient background, "
)

ART = [
    # --- app icon (opaque, square, reads at 48px) ---
    {"name": "app_icon", "dir": APP_DIR, "ratio": "1:1",
     "prompt": "Mobile game app icon, a glowing forge anvil with a crossed blacksmith hammer "
               "and rising orange ember flame, chunky rounded shapes, warm gold and deep brown "
               "palette, dark vignette edges, centered single emblem, flat stylized vector "
               "illustration, " + NO_TEXT},

    # --- character portraits (distinct faces, same style) ---
    {"name": "portrait_bram", "dir": MENU_DIR, "ratio": "1:1",
     "prompt": PORTRAIT_STYLE + "a burly middle-aged male master blacksmith named Bram, "
               "broad warm smile, thick chestnut beard, bald head with short side hair, "
               "leather apron and rolled sleeves, kind amber eyes, " + NO_TEXT},
    {"name": "portrait_petra", "dir": MENU_DIR, "ratio": "1:1",
     "prompt": PORTRAIT_STYLE + "a young cheerful female miner named Petra, freckles, "
               "messy copper ponytail, brass goggles pushed up on forehead, cheeky grin, "
               "tiny smudge of coal on cheek, " + NO_TEXT},
    {"name": "portrait_sable", "dir": MENU_DIR, "ratio": "1:1",
     "prompt": PORTRAIT_STYLE + "an elegant middle-aged female merchant named Sable, "
               "dark skin, gold silk headscarf and hoop earrings, sly confident smile, "
               "jeweled rings, rich purple clothing, " + NO_TEXT},
    {"name": "portrait_aldric", "dir": MENU_DIR, "ratio": "1:1",
     "prompt": PORTRAIT_STYLE + "a weathered elderly male knight named Sir Aldric, "
               "grey mustache and short beard, thin scar across one brow, stern noble "
               "expression with soft eyes, steel armor collar, " + NO_TEXT},
    {"name": "portrait_nyx", "dir": MENU_DIR, "ratio": "1:1",
     "prompt": PORTRAIT_STYLE + "a mysterious hooded enchanter named Nyx, pale sharp face, "
               "glowing violet eyes, deep purple hood with faint rune glyphs, subtle smirk, "
               "androgynous features, " + NO_TEXT},

    # --- cinematic intro panels (3:2, dark moody) ---
    {"name": "intro_village", "dir": MENU_DIR, "ratio": "3:2",
     "prompt": "A tiny mountain village at dusk seen from above, one cold dark forge "
               "chimney with no smoke, muted blue-grey palette, lonely quiet mood, "
               "flat stylized illustration for a game cinematic, " + NO_TEXT},
    {"name": "intro_ember", "dir": MENU_DIR, "ratio": "3:2",
     "prompt": "A single glowing orange ember spark drifting down through darkness toward "
               "a cold grey anvil, dramatic chiaroscuro, the ember is the only warm light "
               "in a dark forge, flat stylized illustration, " + NO_TEXT},
    {"name": "intro_oath", "dir": MENU_DIR, "ratio": "3:2",
     "prompt": "A young blacksmith raising a hammer overhead, silhouetted against roaring "
               "orange forge flames, sparks flying upward, dramatic backlight, oath pose, "
               "flat stylized illustration for a game cinematic, " + NO_TEXT},
    {"name": "intro_rise", "dir": MENU_DIR, "ratio": "3:2",
     "prompt": "A grand blacksmith workshop blazing with warm golden light, racks of "
               "legendary glowing swords, banners, wide triumphant shot, feeling of a "
               "forge empire rising, flat stylized illustration, " + NO_TEXT},
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

def fetch(name, out_dir, ratio, prompt, token, force):
    out = os.path.join(out_dir, name + ".png")
    if not force and os.path.exists(out) and os.path.getsize(out) > 100000:
        print(f"{name}: present", flush=True)
        return True
    for attempt in (1, 2):
        try:
            resp = post(MODEL_URL, token, {
                "input": {"prompt": prompt, "aspect_ratio": ratio,
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
            if len(data) > 100000 and data[:2] == b"\x89P":
                os.makedirs(out_dir, exist_ok=True)
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
        fetch(a["name"], a["dir"], a["ratio"], a["prompt"], token, force)
    print("DONE", flush=True)

if __name__ == "__main__":
    main()
