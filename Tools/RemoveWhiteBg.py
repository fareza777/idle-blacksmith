# RemoveWhiteBg.py — flood-fills the white/near-white background of icon art from the
# image borders inward and replaces it with transparency, keeping enclosed white gaps.
# Usage: py -3 RemoveWhiteBg.py <file1> [file2 ...]
import sys
from PIL import Image, ImageDraw

def process(path, thresh=44):
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    px = im.load()
    # Seed points: dense scan along all four borders, only where the pixel is near-white.
    seeds = []
    def near_white(p):
        return p[0] >= 255 - thresh and p[1] >= 255 - thresh and p[2] >= 255 - thresh
    for x in range(0, w, 6):
        for y in (0, 1, h - 2, h - 1):
            if near_white(px[x, y]): seeds.append((x, y))
    for y in range(0, h, 6):
        for x in (0, 1, w - 2, w - 1):
            if near_white(px[x, y]): seeds.append((x, y))
    for s in seeds:
        ImageDraw.floodfill(im, s, (0, 0, 0, 0), thresh=thresh)
    im.save(path)
    print(f"bg removed: {path} ({w}x{h}, {len(seeds)} seeds)")

if __name__ == "__main__":
    for p in sys.argv[1:]:
        try:
            process(p)
        except Exception as e:
            print(f"FAILED {p}: {e}")
