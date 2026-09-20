"""Create stable .meta identities for source/assets. Does not require Unity."""
from pathlib import Path
import hashlib
ROOT=Path(__file__).resolve().parents[1]/'Unity'
for path in sorted((ROOT/'Assets').rglob('*')):
    if path.suffix=='.meta':continue
    relative=path.relative_to(ROOT).as_posix()
    guid=hashlib.md5(relative.encode()).hexdigest()
    header=f'fileFormatVersion: 2\nguid: {guid}\n'
    if path.is_dir():text=header+'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    elif path.suffix=='.cs':text=header+'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    elif path.suffix=='.json':text=header+'TextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    elif path.suffix=='.png':text=header+'''TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureType: 0
  alphaSource: 1
  alphaIsTransparency: 1
  maxTextureSize: 256
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  platformSettings: []
  userData:
  assetBundleName:
  assetBundleVariant:
'''
    elif path.suffix=='.jslib':text=header+'''PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any:
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 0
      settings:
        DefaultValueInitialized: true
  - first:
      WebGL: WebGL
    second:
      enabled: 1
      settings: {}
  userData:
  assetBundleName:
  assetBundleVariant:
'''
    else:text=header+'DefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    Path(str(path)+'.meta').write_text(text,encoding='utf-8')
print('Stable Unity .meta files generated.')
