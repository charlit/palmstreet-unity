#!/usr/bin/env python3
"""Masques de parties du skateur PixelLab (pour repeindre selon la tenue) : <Nom>_Mask.png à côté de chaque planche.

Le skateur est dessiné avec une couleur repère par partie (Tools/skaterpix.py, image de départ sk4_key) : on reconnaît
chaque pixel à sa teinte seule, donc de la même façon sur toutes les images. Canal rouge = partie : 0 rien (contour,
roues, yeux…), 1 haut (bleu 215°), 2 rayures du haut (cyan 170°), 3 bas (vert 125°), 4 chaussures (magenta 305°),
5 planche (rouge 350°), 6 peau, 7 cheveux, 8 bandeau (vert clair 80°), 9 chaussettes (violet 260°).
Canal vert = luminosité de référence de la partie × 255 (pour garder les ombres en repeignant).
Usage : skatermask.py <Assets/Resources/SkaterPix> [aperçu.png]
"""
import colorsys, sys
from PIL import Image

NAMES = ('Ride', 'Push', 'Ollie', 'Flip', 'Shove', 'Grind5050', 'GrindNose', 'GrindTail', 'GrindBoard', 'Crash')
KEYS = ((1, 215), (2, 170), (3, 125), (4, 305), (5, 350), (8, 80), (9, 260))


def part(p):
    r, g, b, a = p
    if a < 128:
        return 0
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255); h *= 360
    if v < 0.16:
        return 0                                   # contour
    if s >= 0.3:
        best = min(KEYS, key=lambda k: min(abs(h - k[1]), 360 - abs(h - k[1])))
        d = min(abs(h - best[1]), 360 - abs(h - best[1]))
        if d <= 22:
            return best[0]
    if 34 <= h <= 55 and s > 0.35 and v > 0.7:
        return 7                                   # cheveux
    if 5 <= h < 34 and 0.1 < s < 0.55 and v > 0.5:
        return 6                                   # peau
    return 0


def main(d, preview=None):
    PREVIEW = {1: (40, 80, 255), 2: (120, 255, 255), 3: (40, 220, 40), 4: (255, 220, 0), 5: (255, 0, 255), 6: (250, 210, 180), 7: (255, 240, 120), 8: (255, 120, 0), 9: (255, 255, 255)}
    rows = []
    for n in NAMES:
        sheet = Image.open(f'{d}/{n}.png').convert('RGBA'); sp = sheet.load()
        mask = Image.new('RGBA', sheet.size); mp = mask.load()
        for y in range(sheet.height):
            for x in range(sheet.width):
                k = part(sp[x, y])
                if k:
                    mp[x, y] = (k, 0, 0, 255)
        mask.save(f'{d}/{n}_Mask.png')
        # contrôle : pixels bien colorés rangés dans aucune partie (ils garderaient leur couleur repère et clignoteraient)
        for i in range(sheet.width // 128):
            lost = 0
            for y in range(sheet.height):
                for x in range(i * 128, i * 128 + 128):
                    r, g, b, a = sp[x, y]
                    if a >= 128 and not mp[x, y][3]:
                        _, s_, v_ = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
                        lost += s_ > 0.35 and v_ > 0.35
            if lost > 25:
                print(f'attention : {n} image {i} : {lost} pixels colorés sans partie')
        if preview:
            v = Image.new('RGBA', sheet.size, (30, 30, 40, 255)); vp = v.load()
            for y in range(sheet.height):
                for x in range(sheet.width):
                    if mp[x, y][3]: vp[x, y] = PREVIEW[mp[x, y][0]] + (255,)
            rows.append(v)
    if preview:
        o = Image.new('RGBA', (rows[0].width, 128 * len(rows)))
        for i, r in enumerate(rows): o.paste(r, (0, i * 128))
        o.save(preview)


if __name__ == '__main__':
    main(*sys.argv[1:])
