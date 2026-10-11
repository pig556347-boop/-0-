"""Create stable Unity asset GUIDs without overwriting existing metadata."""
from pathlib import Path
import uuid

root = Path(__file__).resolve().parents[1]
namespace = uuid.UUID('842b446f-af5e-4f39-9583-e44be5f9bf43')
for path in sorted((root / 'Assets').rglob('*')):
    if path.suffix == '.meta':
        continue
    meta = Path(str(path) + '.meta')
    if not meta.exists():
        guid = uuid.uuid5(namespace, path.relative_to(root).as_posix()).hex
        text = f'fileFormatVersion: 2\nguid: {guid}\n'
        if path.is_dir():
            text += 'folderAsset: yes\n'
        meta.write_text(text)
scene_meta = root / 'Assets/Scenes/Prototype.unity.meta'
guid = next(line.split(': ', 1)[1] for line in scene_meta.read_text().splitlines()
            if line.startswith('guid: '))
settings = root / 'ProjectSettings/EditorBuildSettings.asset'
if not settings.exists():
    settings.write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1045 &1
EditorBuildSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Scenes:
  - enabled: 1
    path: Assets/Scenes/Prototype.unity
    guid: ''' + guid + '\n  m_configObjects: {}\n')
print('Unity asset GUIDs and scene build registration prepared')
