"""
Composite the Guybrush pixel-art sprite into the RimWorld menu screenshot,
replacing the key-art figure, and produce branding PNG variants.

Pipeline:
  1. knock out the sprite's white background (border flood-fill, 8-connected,
     treating light *desaturated* pixels as background so JPEG ringing goes too
     but skin / blue coat stay opaque).
  2. paint out the original key-art figure with a per-row nebula fill (only the
     part beside Guybrush's head is ever visible; his body hides the rest).
  3. composite Guybrush, then draw the GTI WEAPON WEAR banner at full res,
     positioned clear of Guybrush.

Re-runnable: tweak CONFIG and `python _compose_guybrush.py`.
"""
from PIL import Image, ImageDraw, ImageFont
from collections import deque

SHOT = "20250908173744_1.jpg"   # 2560x1440 full-res menu screenshot
SPRITE = "gt_front2.jpg"        # 808x1450 front-facing Guybrush, white bg

# ---- Guybrush placement (fractions of canvas) ---------------------------
SPRITE_HEIGHT_FRAC = 1.00
CENTER_X_FRAC      = 0.305
FEET_Y_FRAC        = 1.00

# ---- original-figure paint-out box (full-res px) ------------------------
FIG_BOX = (250, 0, 1465, 1440)    # x0, y0, x1, y1 covering figure + raven wings
DONOR_X = 1360                    # clean nebula column just right of the box
FEATHER = 70                      # px edge blend so the fill has no hard seam

FONT_BOLD = r"C:\Windows\Fonts\segoeuib.ttf"
FONT_SEMI = r"C:\Windows\Fonts\segoeui.ttf"
# -------------------------------------------------------------------------


def knockout_white(img, tol=28):
    img = img.convert("RGBA")
    w, h = img.size
    px = img.load()

    def is_white(x, y):
        r, g, b, a = px[x, y]
        mn, mx = min(r, g, b), max(r, g, b)
        return mn >= 200 and (mx - mn) <= tol

    visited = bytearray(w * h)
    q = deque()
    border = [(x, y) for x in range(w) for y in (0, h - 1)]
    border += [(x, y) for y in range(h) for x in (0, w - 1)]
    for x, y in border:
        if is_white(x, y) and not visited[y * w + x]:
            visited[y * w + x] = 1
            q.append((x, y))
    nb = ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1))
    while q:
        x, y = q.popleft()
        r, g, b, _ = px[x, y]
        px[x, y] = (r, g, b, 0)
        for dx, dy in nb:
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not visited[ny * w + nx] and is_white(nx, ny):
                visited[ny * w + nx] = 1
                q.append((nx, ny))
    return img


def paint_out_figure(shot):
    """Build a per-row nebula fill (sampling only green nebula pixels, so stars,
    ships and asteroids in the donor column are ignored) and blend it over the
    figure box with feathered edges so there is no hard seam."""
    im = shot.convert("RGB")
    src = im.load()
    x0, y0, x1, y1 = FIG_BOX
    bw, bh = x1 - x0, y1 - y0

    fill = Image.new("RGB", (bw, bh))
    fp = fill.load()
    prev = (20, 60, 55)
    for y in range(y0, y1):
        greens = []
        for i in range(0, 120, 3):
            r, g, b = src[DONOR_X + i, y]
            if g >= r and g >= b and (g - min(r, b)) > 6:   # nebula, not debris
                greens.append((r, g, b))
        if greens:
            greens.sort(key=lambda c: c[1])
            prev = greens[len(greens) // 2]
        for x in range(bw):
            fp[x, y - y0] = prev

    # feather mask: opaque interior, ramp to 0 over FEATHER px on L/R/top edges
    # (bottom stays hard — it is off-canvas / hidden behind Guybrush)
    f = max(1, FEATHER)
    mask = Image.new("L", (bw, bh), 255)
    mp = mask.load()
    for x in range(bw):
        ax = min(255, int(255 * min(x, bw - 1 - x) / f))   # left/right ramp
        for y in range(bh):
            ay = min(255, int(255 * y / f))                # top ramp
            mp[x, y] = min(ax, ay)

    out = im.copy()
    out.paste(fill, (x0, y0), mask)
    return out


def place(canvas, sprite, hfrac, cxfrac, feetfrac):
    cw, ch = canvas.size
    target_h = int(ch * hfrac)
    scale = target_h / sprite.height
    s = sprite.resize((int(sprite.width * scale), target_h), Image.NEAREST)
    x = int(cw * cxfrac) - s.width // 2
    y = int(ch * feetfrac) - target_h
    out = canvas.convert("RGBA")
    out.alpha_composite(s, (x, y))
    return out


def draw_banner(img):
    img = img.convert("RGBA")
    d = ImageDraw.Draw(img)
    # panel, shifted right to clear Guybrush (right edge ~1182px)
    x0, y0, x1, y1 = 1245, 470, 2230, 1000
    panel = Image.new("RGBA", img.size, (0, 0, 0, 0))
    pd = ImageDraw.Draw(panel)
    pd.rounded_rectangle([x0, y0, x1, y1], radius=46, fill=(9, 26, 33, 214))
    img = Image.alpha_composite(img, panel)
    d = ImageDraw.Draw(img)

    cx = (x0 + x1) // 2
    f_gti = ImageFont.truetype(FONT_BOLD, 62)
    f_title = ImageFont.truetype(FONT_BOLD, 150)
    f_sub = ImageFont.truetype(FONT_SEMI, 50)

    def centered(text, font, y, fill):
        w = d.textlength(text, font=font)
        d.text((cx - w / 2, y), text, font=font, fill=fill)

    centered("GTI", f_gti, y0 + 36, (240, 192, 74, 255))
    centered("WEAPON WEAR", f_title, y0 + 96, (255, 255, 255, 255))
    centered("Weapons wear with use   •   repair & auto-upkeep",
             f_sub, y0 + 286, (208, 220, 224, 255))

    # wear progress bar
    bx0, bx1 = x0 + 90, x1 - 90
    by0, by1 = y1 - 92, y1 - 52
    d.rounded_rectangle([bx0, by0, bx1, by1], radius=20, fill=(28, 40, 46, 255))
    fill_x = bx0 + int((bx1 - bx0) * 0.62)
    for i in range(bx0, fill_x):                 # orange->amber gradient
        t = (i - bx0) / max(1, fill_x - bx0)
        r = int(214 + t * (245 - 214))
        g = int(120 + t * (176 - 120))
        d.line([(i, by0 + 4), (i, by1 - 4)], fill=(r, g, 40, 255))
    d.rounded_rectangle([bx0, by0, fill_x, by1], radius=20, outline=None)
    return img


def main():
    sprite = knockout_white(Image.open(SPRITE))
    sprite.save("_gt_front2_cutout.png")

    shot = paint_out_figure(Image.open(SHOT))

    scene = place(shot, sprite, SPRITE_HEIGHT_FRAC, CENTER_X_FRAC, FEET_Y_FRAC)
    scene.convert("RGB").save("gt_preview_scene.png")

    branded = draw_banner(scene)
    branded.convert("RGB").save("gt_preview_branded.png")
    print("done")


if __name__ == "__main__":
    main()
