import os
from PIL import Image

def make_black_alpha_transparent(path):
    img = Image.open(path).convert("RGBA")
    pixels = img.load()

    width, height = img.size
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if r == 0 and g == 0 and b == 0:
                pixels[x, y] = (r, g, b, 0)  # same color, alpha = 0

    img.save(path)
    print(f"Processed: {path}")

def main():
    folder = os.path.dirname(os.path.abspath(__file__))
    for filename in os.listdir(folder):
        if filename.lower().endswith(".png"):
            make_black_alpha_transparent(os.path.join(folder, filename))

if __name__ == "__main__":
    main()