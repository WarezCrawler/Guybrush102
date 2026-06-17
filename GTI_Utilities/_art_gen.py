"""
Generate the animal-augmentation-bench textures by DERIVING from EPOE-Forked's
TableBionics art (personal/unpublished use only):

  1. Hue-shift the bright-blue body -> veterinary green, leaving the neutral
     grey panels (monitor, cogs) and any reds untouched.
  2. Composite a glowing paw "decal" onto the dark monitor so the bench reads
     as a veterinary console.

Keeps RimWorld's native shading / perspective / damage overlays. Re-run to tweak.
Requires Pillow.
"""
import colorsys
import os
import tempfile
from PIL import Image, ImageDraw, ImageFilter

SRC = r"T:\SteamLibrary\steamapps\workshop\content\294100\1949064302\Textures\Things\Building\Production"
# Pillow cannot flush directly onto the O: drive here, so emit to a local temp
# dir; a follow-up copy step moves the PNGs into the mod's Textures folder.
DST = os.path.join(tempfile.gettempdir(), "gti_bench_art")
os.makedirs(DST, exist_ok=True)

# blue body -> green: shift hues in the blue band by this many degrees
HUE_SHIFT = -105
BLUE_LO, BLUE_HI = 150, 300   # only remap hues in this band (the blue body)
SAT_MIN = 0.22                # leave near-grey steel alone

# Palette tuned to harmonise with the bench's recoloured body (true green, H=120):
# a light GREEN-white core (no cyan: B<=R), a true-green glow, dark-green rim.
PAW_CORE = (182, 248, 178, 255)   # light green-white fill (reads green, not cyan)
PAW_GLOW = (58, 200, 88)          # true vet-green screen-glow (~H125)
PAW_RIM  = (12, 52, 26, 255)      # dark green rim so toes read against the screen


def recolor(im):
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            hh, ss, vv = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            hd = hh * 360
            if ss > SAT_MIN and BLUE_LO <= hd <= BLUE_HI:
                nh = (hd + HUE_SHIFT) % 360
                # gently warm the green so it is not neon
                ns = min(1.0, ss * 0.95)
                nr, ng, nb = colorsys.hsv_to_rgb(nh / 360, ns, vv)
                px[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), a)
    return im


def paw_overlay(size):
    """A glowing paw print (pad + four clearly separated toes) on a
    transparent square of side `size`. A dark rim plus a TIGHT glow keep the
    gaps between toes and pad open, so it reads as a paw and not a blob."""
    S = size
    SS = S * 4  # supersample for smooth edges
    img = Image.new("RGBA", (SS, SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # (cx, cy, rx, ry) normalised: main pad + four toes in an arc
    shapes = [
        (0.500, 0.730, 0.235, 0.190),
        (0.215, 0.460, 0.100, 0.125),
        (0.400, 0.305, 0.118, 0.150),
        (0.600, 0.305, 0.118, 0.150),
        (0.785, 0.460, 0.100, 0.125),
    ]

    def draw(col, grow):
        for cx, cy, rx, ry in shapes:
            d.ellipse([(cx - rx - grow) * SS, (cy - ry - grow) * SS,
                       (cx + rx + grow) * SS, (cy + ry + grow) * SS], fill=col)

    draw(PAW_RIM, 0.016)   # rim first, slightly larger
    draw(PAW_CORE, 0.0)    # bright core on top

    img = img.resize((S, S), Image.LANCZOS)

    # tight outer glow (small radius) so it does NOT close the gaps
    glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    glow.paste(PAW_GLOW + (255,), (0, 0), img.split()[3])
    glow = glow.filter(ImageFilter.GaussianBlur(max(1.0, S * 0.045)))
    glow.putalpha(glow.split()[3].point(lambda v: int(v * 0.5)))

    out = Image.alpha_composite(Image.new("RGBA", (S, S), (0, 0, 0, 0)), glow)
    out = Image.alpha_composite(out, img)
    return out


def stamp(base, overlay, cx, cy):
    x = int(round(cx - overlay.width / 2))
    y = int(round(cy - overlay.height / 2))
    base.alpha_composite(overlay, (x, y))


def oriented_paw(size, quarter_turns_cw):
    """Upright paw rotated by a whole number of 90-deg CW quarter-turns
    (lossless transpose), so it tracks how the bench is rotated per facing."""
    p = paw_overlay(size)
    q = quarter_turns_cw % 4
    if q == 1:
        p = p.transpose(Image.ROTATE_270)   # 90 CW
    elif q == 2:
        p = p.transpose(Image.ROTATE_180)
    elif q == 3:
        p = p.transpose(Image.ROTATE_90)     # 90 CCW
    return p


# Per facing: paw-box centre on the console screen + 90-deg CW quarter-turns.
# The paw is painted on the bench surface, so it ROTATES WITH THE BENCH, exactly
# like EPOE's own wrench/gear decals (ground truth): south & north stay upright
# (EPOE draws the back-view decals upright too), east = south rotated 90 deg CW
# (toes point right, matching the wrench head); west auto-mirrors east (toes
# left). One constant size keeps the in-game scale identical across facings.
PAW_SIZE = 30
JOBS = {
    "south": ((114, 32), 0),    # upright, centred on the upper monitor
    "north": ((112, 60), 0),    # upright (matches south per EPOE back-view), centred on the tray
    "east":  ((36, 106), 1),    # rotated 90 deg CW (toes right), centred on the monitor
}

if __name__ == "__main__":
    for name, (center, qcw) in JOBS.items():
        im = recolor(Image.open(f"{SRC}\\TableBionics_{name}.png"))
        stamp(im, oriented_paw(PAW_SIZE, qcw), *center)
        im.save(f"{DST}\\GTI_AnimalBionicsTable_{name}.png")
        print(f"{name}: centre={center} cw90={qcw} size={PAW_SIZE}")
    print("DONE")
