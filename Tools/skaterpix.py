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


def main(src, dst):
    for name, out, small in (('sk_roule', 'Ride', None), ('sk_pousse', 'Push', None), ('sk_ollie', 'Ollie', None), ('sk_flip', 'Flip', 250), ('sk_shove', 'Shove', 250),
                              ('sk_g5050', 'Grind5050', None), ('sk_gnose', 'GrindNose', None), ('sk_gtail', 'GrindTail', None), ('sk_gboard', 'GrindBoard', None), ('sk_chute', 'Crash', 150)):
        sheet = Image.new('RGBA', (128 * 8, 128))
        for i, k in enumerate(range(1, 9)):
            sheet.paste(keep_main(Image.open(f'{src}/{name}_{k}.png').convert('RGBA'), small), (i * 128, 0))
        sheet.save(f'{dst}/{out}.png')
    f = Image.open(f'{dst}/Ride.png').crop((0, 0, 128, 128)); print('bbox', f.getbbox())


if __name__ == '__main__':
    main(*sys.argv[1:])
