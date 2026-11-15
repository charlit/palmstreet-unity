#!/usr/bin/env python3
"""Skateur PixelLab (essai) : planches Ride, Push, Ollie de 8 images 128 px dans Assets/Resources/SkaterPix.

On efface les petits bouts détachés (traînées blanches de PixelLab) mais on garde la planche, détachée des pieds pendant les flips.
Usage : skaterpix.py <Art/PixelLab> <Assets/Resources/SkaterPix>
"""
import sys
from PIL import Image


def keep_main(im, small=None):
    """small=None : seulement le plus grand bloc ; sinon tous les blocs d'au moins small pixels."""
    px = im.load(); w, h = im.size; seen = set(); best = []
    for y in range(h):
        for x in range(w):
            if (x, y) in seen or px[x, y][3] < 30:
                continue
            comp, stack = [], [(x, y)]; seen.add((x, y))
            while stack:
                cx, cy = stack.pop(); comp.append((cx, cy))
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen and px[nx, ny][3] >= 30:
                        seen.add((nx, ny)); stack.append((nx, ny))
            if small is None:
                if len(comp) > len(best):
                    best = comp
            elif len(comp) >= small:
                best += comp
    out = Image.new('RGBA', im.size); op = out.load()
    for x, y in best:
        op[x, y] = px[x, y]
    return out


def no_trails(im):
    """Efface les traînées blanches de PixelLab : sur le skateur aux couleurs repères, le blanc n'est que les roues (près de
    la planche rouge) et le blanc des yeux (petit, contre la peau) ; tout autre morceau blanc ou gris très clair part."""
    import colorsys
    px = im.load(); w, h = im.size
    def hsv(p): return colorsys.rgb_to_hsv(p[0] / 255, p[1] / 255, p[2] / 255)
    def pale(p): s_, v_ = hsv(p)[1:]; return p[3] >= 60 and s_ < 0.2 and v_ > 0.62
    def deck(p): h_, s_, v_ = hsv(p); return p[3] >= 128 and s_ > 0.35 and v_ > 0.3 and (h_ * 360 > 335 or h_ * 360 < 10)
    def skin(p): h_, s_, v_ = hsv(p); return p[3] >= 128 and 5 <= h_ * 360 < 34 and 0.1 < s_ < 0.55 and v_ > 0.5
    seen = set()
    for y in range(h):
        for x in range(w):
            if (x, y) in seen or not pale(px[x, y]):
                continue
            comp, stack = [], [(x, y)]; seen.add((x, y))
            while stack:
                cx, cy = stack.pop(); comp.append((cx, cy))
                for q in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= q[0] < w and 0 <= q[1] < h and q not in seen and pale(px[q]):
                        seen.add(q); stack.append(q)
            def near(test, d):
                return any(0 <= cx + dx < w and 0 <= cy + dy < h and test(px[cx + dx, cy + dy]) for cx, cy in comp for dx in range(-d, d + 1) for dy in range(-d, d + 1))
            xs = [q[0] for q in comp]; ys = [q[1] for q in comp]
            compact = max(xs) - min(xs) <= 14 and max(ys) - min(ys) <= 14   # une roue ; une traînée est longue et fine
            if (compact and near(deck, 3)) or (len(comp) <= 12 and near(skin, 2)):
                continue
            for q in comp:
                px[q] = (0, 0, 0, 0)
    return im


def main(src, dst):
    for name, out, small in (('sk_roule', 'Ride', None), ('sk_pousse', 'Push', None), ('sk_ollie', 'Ollie', None), ('sk_flip', 'Flip', 250), ('sk_shove', 'Shove', 250),
                              ('sk_g5050', 'Grind5050', None), ('sk_gnose', 'GrindNose', None), ('sk_gtail', 'GrindTail', None), ('sk_gboard', 'GrindBoard', None), ('sk_chute', 'Crash', 150)):
        sheet = Image.new('RGBA', (128 * 8, 128))
        for i, k in enumerate(range(1, 9)):
            sheet.paste(keep_main(no_trails(Image.open(f'{src}/{name}_{k}.png').convert('RGBA')), small), (i * 128, 0))
        sheet.save(f'{dst}/{out}.png')
    f = Image.open(f'{dst}/Ride.png').crop((0, 0, 128, 128)); print('bbox', f.getbbox())




def hats(src, dst):
    """Casquette et bonnet (PixelLab) en gris clair : le jeu leur donne la couleur de l'objet (SpriteRenderer.color)."""
    for name, out in (('casquette', 'Hat_Cap'), ('bonnet', 'Hat_Beanie')):
        im = Image.open(f'{src}/{name}.png').convert('RGBA'); im = im.crop(im.getbbox()); px = im.load()
        ref = 0.2126 * 200 + 0.7152 * 35 + 0.0722 * 55   # rouge principal du dessin
        for y in range(im.height):
            for x in range(im.width):
                r, g, b, a = px[x, y]
                if a:
                    v = min(255, int(255 * (0.2126 * r + 0.7152 * g + 0.0722 * b) / ref * 0.92))
                    px[x, y] = (v, v, v, a)
        im.save(f'{dst}/{out}.png')


if __name__ == '__main__':
    main(*sys.argv[1:])
    hats(*sys.argv[1:])
