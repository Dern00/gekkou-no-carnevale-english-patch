"""Render Project 5's opening captions as transparent frames for the installer."""
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path


# Frame boundaries measured in the December 2020 Extras clip, at 30000/1001 fps.
# Each entry contains its half-open frame range, romanization, and English text.
CUES = [
    (541, 661, "Mawaridashita\nMonogatari", "It's beginning to turn\nthis our story"),
    (661, 778, "Kaketa tamashii", "With an imperfect soul"),
    (778, 1019, "Ayatsurarete\nHonoka ni hikaru ito wa\nYami ni mau",
     "Manipulated,\nthe faint string of light\ndances in the darkness"),
    (1019, 1212, "Yume wo miru ningyou wa", "The doll that has had dreams"),
    (1264, 1481, "Ikudotonaku neji o makare", "Had their screw wound one too many times"),
    (1496, 1633, "Kishimi\nNageku", "She creaks\nshe sighs"),
    (1633, 1715, "Dum gemo tacet luna", "As I sigh the moon is silent"),
    (1715, 1895, "Odore\nAoki tsukikage", "Dance\no pale moonlight"),
    (1895, 2194, "Tsumi ni yakarete\nMienu jubaku ni torawarete", "Burned by a sin\nPrisoner of invisible restraints"),
    (2194, 2514, "Sora ni nobasu\nYubi ga tsukamu no wa\nMaboroshi...?",
     "The fingers that stretch to the sky\nis all they grasp\nan illusion...?"),
    (2514, 2713, "Ahhh\nImawashiki kioku", "Ahhh, what hideous memories"),
    (2713, 3020, "Horobi no kotoba\nAgeyou", "I will give you\nthe words of destruction"),
]


def render_opening(directory):
    font = ImageFont.truetype("arial.ttf", 18)
    timeline = ["ffconcat version 1.0"]

    def save(image, name):
        header = b"P7\nWIDTH 800\nHEIGHT 608\nDEPTH 4\nMAXVAL 255\nTUPLTYPE RGB_ALPHA\nENDHDR\n"
        (directory / name).write_bytes(header + image.tobytes())

    def append(name, frames):
        timeline.extend((f"file {name}", "option framerate 30000/1001",
                         f"duration {frames * 1001 / 30000:.9f}"))

    frame = Image.new("RGBA", (800, 608))
    save(frame, "blank.pam")
    previous = 0
    for index, (first, end, romanization, english) in enumerate(CUES):
        if first > previous:
            append("blank.pam", first - previous)
        frame = Image.new("RGBA", (800, 608))
        draw = ImageDraw.Draw(frame)
        for lines, top in ((romanization, 19), (english, 566 - 21 * english.count("\n"))):
            for line_number, line in enumerate(lines.splitlines()):
                draw.text((400, top + line_number * 21), line, font=font, anchor="mt",
                          fill="white", stroke_width=2, stroke_fill="black")
        name = f"cue{index:02d}.pam"
        save(frame, name)
        append(name, end - first)
        previous = end
    append("blank.pam", 3163 - previous)
    timeline.extend(("file blank.pam", "option framerate 30000/1001"))
    (directory / "cues.ffconcat").write_text("\n".join(timeline) + "\n", encoding="ascii")
