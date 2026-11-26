"""Package a Unity Windows x64 player. Run after Drowned Bell > Build Windows."""
import argparse
from pathlib import Path
from tempfile import NamedTemporaryFile
from zipfile import ZIP_DEFLATED, ZipFile

PROJECT = Path(__file__).resolve().parents[1]


def package(player, output):
    required = ("DrownedBell.exe", "UnityPlayer.dll", "DrownedBell_Data/globalgamemanagers")
    for name in required:
        if not (player / name).is_file():
            raise SystemExit(f"Incomplete Windows player: missing {player / name}")
    if output.resolve().is_relative_to(player.resolve()):
        raise SystemExit("Select a ZIP path outside the player folder.")
    output.parent.mkdir(parents=True, exist_ok=True)
    with NamedTemporaryFile(dir=output.parent, delete=False) as tmp:
        staged = Path(tmp.name)
    try:
        with ZipFile(staged, "w", ZIP_DEFLATED, compresslevel=6) as archive:
            for path in sorted(player.rglob("*")):
                relative = path.relative_to(player)
                if path.is_file() and not any("DoNotShip" in part for part in relative.parts):
                    archive.write(path, "Drowned Bell/" + relative.as_posix())
            archive.write(PROJECT / "docs/PLAY.txt", "Drowned Bell/PLAY.txt")
        with ZipFile(staged) as archive:
            bad = archive.testzip()
            if bad:
                raise SystemExit(f"Could not verify {bad}")
        staged.replace(output)
    finally:
        staged.unlink(missing_ok=True)
    print(output)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--player", type=Path, default=PROJECT / "Builds/Windows")
    parser.add_argument("--output", type=Path, default=PROJECT / "Builds/Drowned-Bell-Windows-x64.zip")
    args = parser.parse_args()
    package(args.player, args.output)
