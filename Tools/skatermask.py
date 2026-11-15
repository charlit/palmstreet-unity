#!/usr/bin/env python3
"""Masques de parties du skateur PixelLab (pour repeindre selon la tenue) : <Nom>_Mask.png à côté de chaque planche.

Canal rouge = partie : 0 rien (contour, roues, yeux…), 1 haut, 2 rayures du haut, 3 bas, 4 chaussures, 5 planche, 6 peau,
7 cheveux, 8 bandeau, 9 chaussettes. Le même rouge sert au maillot, aux chaussettes, au bandeau et à la planche : on les
sépare par morceaux reliés — le grand morceau rouge le plus proche des cheveux est le maillot, les autres grands morceaux
la planche, les petits les chaussettes ; un petit morceau rouge qui touche les cheveux est le bandeau (son blanc est gardé).
Usage : skatermask.py <Assets/Resources/SkaterPix> [aperçu.png]
"""
import colorsys, os, sys
from PIL import Image

NAMES = ('Ride', 'Push', 'Ollie', 'Flip', 'Shove', 'Grind5050', 'GrindNose', 'GrindTail', 'GrindBoard', 'Crash')


def family(p):
    r, g, b, a = p
    if a < 128:
        return None
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255); h *= 360
    if v < 0.2:
        return 'ink'
    if r > 110 and r - g > 60 and r - b > 40 and (h > 330 or h < 12):
        return 'red'
    if s < 0.13 and v > 0.6:
        return 'white'
    if 34 <= h <= 52 and s > 0.38 and v > 0.75:
        return 'hair'
    if 10 <= h < 36 and 0.12 < s < 0.5 and v > 0.55:
        return 'skin'
    if s < 0.35 and v <= 0.5:
        return 'gray'
    return None


def comps(cells, fam, kind):
    seen, out = set(), []
    for start in cells:
        if start in seen or fam[start] != kind:
            continue
        comp, stack = [], [start]; seen.add(start)
        while stack:
            x, y = stack.pop(); comp.append((x, y))
            for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if q in fam and q not in seen and fam[q] == kind:
                    seen.add(q); stack.append(q)
        out.append(comp)
    return out


def near(comp, pts, d=2):
    return any((x + dx, y + dy) in pts for x, y in comp for dx in range(-d, d + 1) for dy in range(-d, d + 1))


def frame_mask(im):
    px = im.load(); w, h = im.size
    fam = {(x, y): family(px[x, y]) for y in range(h) for x in range(w) if px[x, y][3] >= 128}
    mask = {}
    hair = {q for q, f in fam.items() if f == 'hair'}
    for q, f in fam.items():
        if f == 'hair': mask[q] = 7
        elif f == 'skin': mask[q] = 6
        elif f == 'gray': mask[q] = 4
    hc = (sum(x for x, _ in hair) / len(hair), sum(y for _, y in hair) / len(hair)) if hair else (w / 2, 0)
    cen = lambda c: (sum(x for x, _ in c) / len(c), sum(y for _, y in c) / len(c))
    dist = lambda c: ((cen(c)[0] - hc[0]) ** 2 + (cen(c)[1] - hc[1]) ** 2) ** 0.5
    reds = comps(list(fam), fam, 'red')
    big = [c for c in reds if len(c) >= 40]
    jersey = min(big, key=dist) if big else []
    jx0 = min((x for x, _ in jersey), default=0); jx1 = max((x for x, _ in jersey), default=-1)
    jy0 = min((y for _, y in jersey), default=0); jy1 = max((y for _, y in jersey), default=-1)
    inj = lambda c: jx0 - 2 <= cen(c)[0] <= jx1 + 2 and jy0 - 2 <= cen(c)[1] <= jy1 + 2
    # maillot : le morceau de départ, puis tout morceau rouge qui le touche presque (les rayures blanches le coupent)
    jset, members, grew = set(jersey), {id(jersey)}, True
    while grew:
        grew = False
        for c in reds:
            if id(c) not in members and len(c) >= 6 and not (near(c, hair) and len(c) < 40) and near(c, jset, 3):
                members.add(id(c)); jset.update(c); grew = True
    deck, red1 = set(), set()
    for c in reds:
        if id(c) in members: part = 1
        elif near(c, hair) and len(c) < 40: part = 8
        elif inj(c): part = 1
        elif len(c) >= 40 and dist(c) > dist(jersey) + 8: part = 5
        else: part = 9
        for q in c: mask[q] = part
        (deck if part == 5 else red1 if part == 1 else set()).update(c)
    def edge_ratio(c, pts):   # part des voisins du morceau qui sont dans pts
        cs, n, k = set(c), 0, 0
        for x, y in c:
            for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if q not in cs: n += 1; k += q in pts
        return k / max(1, n)
    for c in comps(list(fam), fam, 'white'):
        if near(c, hair) and len(c) < 60: part = 0   # blanc du bandeau : gardé
        elif edge_ratio(c, red1) >= 0.35: part = 2   # rayure : bordée de maillot
        elif len(c) >= 25 and not near(c, deck, 1): part = 3
        else: part = 0
        for q in c: mask[q] = part
    out = Image.new('RGBA', im.size, (0, 0, 0, 0)); op = out.load()
    for (x, y), part in mask.items():
        op[x, y] = (part, 0, 0, 255)
    return out


PREVIEW = {1: (255, 60, 60), 2: (255, 200, 200), 3: (60, 120, 255), 4: (40, 40, 40), 5: (255, 160, 0), 6: (250, 210, 180), 7: (255, 230, 0), 8: (200, 0, 255), 9: (0, 200, 120)}


def main(d, preview=None):
    rows = []
    for n in NAMES:
        sheet = Image.open(f'{d}/{n}.png').convert('RGBA')
        mask = Image.new('RGBA', sheet.size)
        for i in range(sheet.width // 128):
            mask.paste(frame_mask(sheet.crop((i * 128, 0, i * 128 + 128, 128))), (i * 128, 0))
        mask.save(f'{d}/{n}_Mask.png')
        if preview:
            v = Image.new('RGBA', sheet.size, (30, 30, 40, 255)); vp = v.load(); mp = mask.load()
            for y in range(sheet.height):
                for x in range(sheet.width):
                    if mp[x, y][3] and mp[x, y][0] in PREVIEW: vp[x, y] = PREVIEW[mp[x, y][0]] + (255,)
            rows.append(v)
    if preview:
        o = Image.new('RGBA', (rows[0].width, 128 * len(rows)))
        for i, r in enumerate(rows): o.paste(r, (0, i * 128))
        o.save(preview)


if __name__ == '__main__':
    main(*sys.argv[1:])
