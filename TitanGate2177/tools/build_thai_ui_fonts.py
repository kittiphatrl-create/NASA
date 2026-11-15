"""Build the two UI faces with pre-positioned Thai combining glyphs.

Run from the project root:
    uv run --no-project --with fonttools --with uharfbuzz python tools/build_thai_ui_fonts.py
"""

from pathlib import Path

import uharfbuzz as hb
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont


FONTS = Path("Assets/Resources/Fonts")
# Match the display encoding in GameUI.Thai. This block is unused in Chakra Petch.
FIRST_GLYPH = 0xE000
FIRST_CONSONANT, LAST_CONSONANT = 0x0E01, 0x0E2E
FIRST_TONE, LAST_TONE = 0x0E48, 0x0E4B
UPPER_VOWELS = "\u0e31\u0e34\u0e35\u0e36\u0e37\u0e4d"


def build(weight):
    source = FONTS / f"ChakraPetch-{weight}.ttf"
    font = TTFont(source, recalcTimestamp=False)
    shaper = hb.Font(hb.Face(source.read_bytes()))
    glyphs = font["glyf"]
    original_names = font.getGlyphOrder()
    names = original_names.copy()
    unicode_tables = [
        table for table in font["cmap"].tables
        if table.isUnicode() and table.format in (4, 12)
    ]
    index = FIRST_GLYPH

    for base in range(FIRST_CONSONANT, LAST_CONSONANT + 1):
        for vowel in UPPER_VOWELS:
            for tone in range(FIRST_TONE, LAST_TONE + 1):
                buffer = hb.Buffer()
                buffer.add_str(chr(base) + vowel + chr(tone))
                buffer.guess_segment_properties()
                hb.shape(shaper, buffer)
                pen = TTGlyphPen(glyphs.glyphs)
                x = 0
                for info, pos in zip(buffer.glyph_infos, buffer.glyph_positions):
                    if info.codepoint == 0:
                        raise ValueError(f"Missing Thai glyph for U+{base:04X}")
                    pen.addComponent(original_names[info.codepoint], (1, 0, 0, 1, x + pos.x_offset, pos.y_offset))
                    x += pos.x_advance
                name = f"uni{index:04X}"
                glyph = pen.glyph()
                glyph.recalcBounds(glyphs)
                glyphs[name] = glyph
                font["hmtx"][name] = (x, glyph.xMin)
                names.append(name)
                if any(index in table.cmap for table in unicode_tables):
                    raise ValueError(f"Font already contains U+{index:04X}")
                for table in unicode_tables:
                    table.cmap[index] = name
                index += 1

    font.setGlyphOrder(names)
    renamed = {
        1: "Titan Gate UI", 2: weight, 3: f"TitanGateUI-{weight}-1.0",
        4: f"Titan Gate UI {weight}", 6: f"TitanGateUI-{weight}",
        16: "Titan Gate UI", 17: weight,
    }
    for record in font["name"].names:
        if record.nameID in renamed:
            record.string = renamed[record.nameID]
    font.save(FONTS / f"TitanGateUI-{weight}.ttf")


if __name__ == "__main__":
    for style in ("Regular", "Bold"):
        build(style)
