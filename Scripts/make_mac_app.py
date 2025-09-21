#!/usr/bin/env python3
from os import walk, strerror
from errno import ENOENT
from zipfile import ZipInfo, ZIP_DEFLATED, ZipFile
from time import localtime
from pathlib import Path
import argparse


parser = argparse.ArgumentParser()
parser.add_argument("app_dir", help="The path of the Lumafly.app directory")
parser.add_argument("publish", help="The directory of the published mac executable")
parser.add_argument("out", help="The output directory for the zip file")

args = parser.parse_args()

app_dir = Path(args.app_dir)
publish = Path(args.publish)
out = Path(args.out)
exe = publish / "LumaflyV2"

if app_dir.suffix != ".app":
    print("Error: " + app_dir + " is not an .app folder.")
    exit(-1)

if not app_dir.exists():
    raise FileNotFoundError(ENOENT, strerror(ENOENT), app_dir)

if not exe.exists():
    raise FileNotFoundError(ENOENT, strerror(ENOENT), exe)

# Basic sanity checks to avoid packaging a Windows binary into a mac .app
def looks_like_windows_pe(path: Path) -> bool:
    try:
        with open(path, 'rb') as f:
            sig = f.read(2)
            return sig == b'MZ'
    except Exception:
        return False

def looks_like_mach_o(path: Path) -> bool:
    try:
        with open(path, 'rb') as f:
            header = f.read(4)
            # Mach-O magic numbers (32/64-bit, both endians) and fat/universal headers
            return header in [
                b'\xfe\xed\xfa\xce',  # MH_MAGIC
                b'\xce\xfa\xed\xfe',  # MH_CIGAM
                b'\xfe\xed\xfa\xcf',  # MH_MAGIC_64
                b'\xcf\xfa\ed\xfe',  # MH_CIGAM_64
                b'\xca\xfe\xba\xbe',  # FAT_MAGIC
                b'\xbe\xba\xfe\xca'   # FAT_CIGAM
            ]
    except Exception:
        return False

if looks_like_windows_pe(exe):
    print(f"Error: The publish executable '{exe}' appears to be a Windows PE file (starts with 'MZ').\n"
          "Packaging a Windows executable inside a mac .app will result in 'damaged' or 'incomplete' app errors on macOS.\n"
          "Please publish a macOS build (Mach-O) into the publish folder and rerun this script.")
    exit(-2)

if not looks_like_mach_o(exe):
    print(f"Warning: The publish executable '{exe}' does not appear to be a Mach-O binary.\n"
          "If you intended to package a macOS app, ensure you published for macOS (dotnet publish -r osx-x64/osx-arm64) and placed the native binary named 'LumaflyV2' in the publish folder.\n"
          "This script will continue, but the resulting .app may not open correctly on macOS.")

def write_executable(zfile, path, zip_path=None):
    if zip_path is None:
        zip_path = path

    with open(path, 'rb') as f:
        fbytes = f.read()

    info = ZipInfo(str(zip_path))
    info.date_time = localtime()
    # -rwx-r---r--
    info.external_attr = 0o100755 << 16
    # UNIX host
    info.create_system = 3

    zfile.writestr(info, fbytes, ZIP_DEFLATED)

if not Path(out).exists():
    Path(out).mkdir()

with ZipFile(out / "LumaflyV2-mac.zip", 'w', ZIP_DEFLATED) as zip_f:
    for root, dirs, files in walk(app_dir):
        root = Path(root)

        for fname in files:
            # Skip any existing executable found inside the source app_dir; we'll add
            # the published executable into Contents/MacOS explicitly below so it
            # ends up in the correct location and with correct permissions.
            if fname == "LumaflyV2":
                continue

            path = Path(root, fname)
            zip_f.write(path)

        if root.name != "Contents":
            continue

        for publish_root, _, files in walk(publish):
            publish_root = Path(publish_root)
            for fname in files:
                if fname == "LumaflyV2":
                    continue

                # keep original filenames for publish artifacts; place them under
                # Contents/MacOS. We don't rename the executable here.
                path = publish_root / fname
                zip_path = root / "MacOS" / fname
                zip_f.write(path, zip_path)

        # Add the published executable into Contents/MacOS with the expected name
        # and executable bits. This ensures the app bundle opens correctly on macOS.
        write_executable(zip_f, publish_root / "LumaflyV2", root / "MacOS" / "run")


print("Created LumaflyV2-mac.zip")
