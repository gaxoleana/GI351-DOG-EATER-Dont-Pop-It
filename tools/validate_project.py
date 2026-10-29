"""Read-only Unity repository checks. Requires Python 3.10+ and Git, not Unity."""

import argparse
import json
from pathlib import Path
import re
import subprocess
import sys


def validate(project):
    errors = []
    warnings = []

    def read(relative):
        return (project / relative).read_text(encoding="utf-8-sig")

    manifest = json.loads(read("Packages/manifest.json"))["dependencies"]
    locked = json.loads(read("Packages/packages-lock.json"))["dependencies"]
    for name, requested in manifest.items():
        resolved = locked.get(name)
        if resolved is None:
            errors.append(f"Package missing from lockfile: {name}")
        elif resolved["source"] in {"registry", "builtin"} and requested != resolved["version"]:
            errors.append(f"Package mismatch: {name}: manifest={requested}, lock={resolved['version']}")
    for name, package in locked.items():
        for dependency in package.get("dependencies", {}):
            if dependency not in locked:
                errors.append(f"Unresolved dependency: {name} -> {dependency}")

    version = re.search(r"^m_EditorVersion: (.+)$", read("ProjectSettings/ProjectVersion.txt"), re.M)
    if version is None:
        errors.append("ProjectVersion.txt has no editor version")

    result = subprocess.run(
        ["git", "-C", str(project), "ls-files", "--cached", "--others", "--exclude-standard", "-z", "--", "Assets"],
        check=True, capture_output=True,
    )
    # Include new files; omit worktree deletions. Never modify the index or Assets.
    assets = sorted({p for p in result.stdout.decode("utf-8").split("\0") if p and (project / p).is_file()})
    guid_paths = {}
    archive_count = 0
    generated_count = 0
    folder_paths = set()
    for relative in assets:
        path = Path(relative)
        if "__MACOSX" in path.parts or path.name.startswith(".") or path.name == "__MACOSX.meta":
            archive_count += 1
            continue
        if path.name.startswith(("PerformanceTestRunInfo.json", "PerformanceTestRunSettings.json")):
            generated_count += 1
            continue
        folder_paths.update(parent for parent in path.parents if parent.as_posix() not in {".", "Assets"})
        if path.suffix != ".meta":
            if not (project / (relative + ".meta")).is_file():
                errors.append(f"Missing metadata: {relative}.meta")
            continue
        if not (project / relative[:-5]).exists():
            errors.append(f"Orphan metadata: {relative}")
        match = re.search(r"^guid: ([0-9a-f]{32})$", read(relative), re.M)
        if match is None:
            errors.append(f"Invalid metadata GUID: {relative}")
            continue
        guid = match[1]
        if guid in guid_paths:
            errors.append(f"Duplicate GUID: {guid_paths[guid]} and {relative}")
        guid_paths[guid] = relative
    for folder in sorted(folder_paths):
        if not (project / (str(folder) + ".meta")).is_file():
            errors.append(f"Missing folder metadata: {folder.as_posix()}.meta")

    scene_settings = read("ProjectSettings/EditorBuildSettings.asset")
    scenes = re.findall(r"- enabled: 1\s+path: (.+)\s+guid: ([0-9a-f]{32})", scene_settings)
    if not scenes:
        errors.append("No enabled build scenes")
    elif scenes[0][0] != "Assets/V.1/Scene/MainMenuV.1.unity":
        errors.append("The first enabled build scene must be MainMenuV.1")
    if not any(path == "Assets/V.1/Scene/GameSceneV.1.unity" for path, _ in scenes):
        errors.append("GameSceneV.1 is missing from enabled build scenes")
    for path, guid in scenes:
        if not (project / path).is_file():
            errors.append(f"Build scene missing: {path}")
        if guid_paths.get(guid) != path + ".meta":
            errors.append(f"Build scene GUID does not match its metadata: {path}")

    if "m_SerializationMode: 2" not in read("ProjectSettings/EditorSettings.asset"):
        errors.append("Unity asset serialization must use Force Text")
    if "m_Mode: Visible Meta Files" not in read("ProjectSettings/VersionControlSettings.asset"):
        errors.append("Unity version control must use Visible Meta Files")
    if archive_count:
        warnings.append(f"Preserved {archive_count} archive/OS metadata files; they are excluded from asset checks")
    if generated_count:
        warnings.append(f"Preserved {generated_count} performance-test resource files; Unity may regenerate them")
    return version[1] if version else "unknown", len(assets), errors, warnings


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    try:
        version, count, errors, warnings = validate(args.project.resolve())
    except (OSError, ValueError, KeyError, subprocess.CalledProcessError) as error:
        print(f"ERROR: Cannot validate project: {error}", file=sys.stderr)
        return 1
    print(f"Unity {version}; checked {count} asset and metadata files")
    for warning in warnings:
        print(f"WARNING: {warning}")
    for error in errors:
        print(f"ERROR: {error}")
    print(f"Project validation {'FAILED' if errors else 'PASSED'} ({len(errors)} errors)")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
