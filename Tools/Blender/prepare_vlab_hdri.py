"""Create a mobile-conscious runtime EXR while preserving the supplied 4K source."""

import sys
from pathlib import Path

import bpy


def argument_value(name: str) -> Path:
    arguments = sys.argv[sys.argv.index("--") + 1 :]
    index = arguments.index(name)
    return Path(arguments[index + 1]).resolve()


source_path = argument_value("--source")
output_path = argument_value("--output")
output_path.parent.mkdir(parents=True, exist_ok=True)

image = bpy.data.images.load(str(source_path), check_existing=False)
source_width, source_height = image.size
target_width = min(2048, source_width)
target_height = max(1, round(source_height * target_width / source_width))
image.scale(target_width, target_height)
image.filepath_raw = str(output_path)
image.file_format = "OPEN_EXR"
settings = bpy.context.scene.render.image_settings
settings.file_format = "OPEN_EXR"
settings.color_mode = "RGB"
settings.color_depth = "16"
settings.exr_codec = "ZIP"
image.save_render(str(output_path), scene=bpy.context.scene)

print(f"VLAB_HDRI_RUNTIME={output_path} {target_width}x{target_height}")
