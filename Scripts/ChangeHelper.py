#!/usr/bin/python

import os
import sys

HEAD = b'#if FAIRYGUI_DRAGONBONES\n\n'
ENDING = b'\n#endif'


def read_file(filename):
    with open(filename, "rb") as f:
        return f.read()


def save_file(filename, data):
    with open(filename, "wb") as f:
        f.write(data)


def walk_dir(path, func):
    for root, dirs, files in os.walk(path):
        for f in files:
            func(os.path.join(root, f))

        for d in dirs:
            walk_dir(os.path.join(root, d), func)


def change_content(file):
    if file.lower().endswith(".cs"):
        content = read_file(file)
        if not content.startswith(HEAD):
            content = HEAD + content + ENDING
            save_file(file, content)


def main(path):
    walk_dir(path, change_content)


if __name__ == "__main__":
    path = "."
    if len(sys.argv) >= 2:
        path = sys.argv[1]
    main(path)
