from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

source = Path(r"C:\Github\SimpleGame\tmp\difficulty-work-20260801\previews-before")
output = source.parent / "contact-before.png"
paths = sorted(source.glob("*-0.png")) + [source / "StageSpawn-1.png"]
thumb_width = 480
label_height = 34
margin = 18
columns = 3
tiles = []
for path in paths:
    image = Image.open(path).convert("RGB")
    ratio = min(1.0, thumb_width / image.width, 360 / image.height)
    thumb = image.resize(
        (max(1, int(image.width * ratio)), max(1, int(image.height * ratio)))
    )
    tile = Image.new("RGB", (thumb_width, 410), "white")
    draw = ImageDraw.Draw(tile)
    draw.text((8, 8), path.name, fill="black", font=ImageFont.load_default())
    tile.paste(thumb, ((thumb_width - thumb.width) // 2, label_height))
    tiles.append(tile)

rows = (len(tiles) + columns - 1) // columns
sheet = Image.new(
    "RGB",
    (
        columns * thumb_width + (columns + 1) * margin,
        rows * 410 + (rows + 1) * margin,
    ),
    "#d7dde3",
)
for index, tile in enumerate(tiles):
    x = margin + (index % columns) * (thumb_width + margin)
    y = margin + (index // columns) * (410 + margin)
    sheet.paste(tile, (x, y))
sheet.save(output)
