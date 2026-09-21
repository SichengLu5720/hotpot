#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using HotpotSort.Determinism;
using UnityEditor;
using UnityEngine;

namespace HotpotSort.ContentImport
{
    public static class Task001AssetImporter
    {
        public const string ResourceRoot="Hotpot/TASK001/v3/r001";
        public static void ImportApprovedVisualContract()
        {
            string project=Directory.GetParent(Application.dataPath).Parent.FullName;
            string sourceRoot=Path.Combine(project,".harness/artifacts/TASK-001");
            string manifestPath=Path.Combine(sourceRoot,"asset-manifest.json");
            var manifest=CanonicalJson.Map(CanonicalJson.Parse(File.ReadAllText(manifestPath)));
            if(CanonicalJson.Int(manifest["taskVersion"])!=4 || (string)manifest["acceptedInputCheckpoint"]!="HC-02-r2" || (string)manifest["revision"]!="r001")throw new InvalidOperationException("Unexpected asset checkpoint/version");
            string destination="Assets/HotpotSort/Resources/"+ResourceRoot;
            var assets=CanonicalJson.Array(manifest["assets"]).Select(CanonicalJson.Map).ToArray();
            if(assets.Select(a=>(string)a["assetId"]).Distinct().Count()!=assets.Length)throw new InvalidOperationException("Duplicate asset IDs");
            foreach(var asset in assets)
            {
                string relative=(string)asset["relativePath"],id=(string)asset["assetId"];
                if(id.Contains("..")||Path.IsPathRooted(relative))throw new InvalidOperationException("Unsafe asset path");
                string source=Path.GetFullPath(Path.Combine(sourceRoot,relative));if(!source.StartsWith(Path.GetFullPath(sourceRoot)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Asset outside batch");
                if(CanonicalJson.Hash(File.ReadAllBytes(source))!=(string)asset["sha256"])throw new InvalidOperationException("Asset hash mismatch: "+id);
                string target=destination+"/"+id+Path.GetExtension(source).ToLowerInvariant();
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                if(File.Exists(target)&&CanonicalJson.Hash(File.ReadAllBytes(target))!=(string)asset["sha256"])throw new InvalidOperationException("Refusing to overwrite a different asset: "+target);
                if(!File.Exists(target))File.Copy(source,target);
            }
            foreach(var entry in CanonicalJson.Array(manifest["licenseFiles"]).Select(CanonicalJson.Map))
            {
                string source=Path.Combine(sourceRoot,(string)entry["relativePath"]);if(CanonicalJson.Hash(File.ReadAllBytes(source))!=(string)entry["sha256"])throw new InvalidOperationException("Font licence hash mismatch");
                string target=destination+"/fonts/"+Path.GetFileName(source);if(!File.Exists(target))File.Copy(source,target);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var asset in assets.Where(a=>(string)a["format"]=="PNG"))
            {
                string path=destination+"/"+(string)asset["assetId"]+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePivot=new Vector2(.5f,.5f);
                importer.mipmapEnabled=false;importer.alphaIsTransparency=(bool)asset["alpha"];importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.isReadable=((string)asset["assetId"]).StartsWith("food/");importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Bilinear;
                var border=CanonicalJson.Array(asset["nineSliceBorder"]).Select(Convert.ToSingle).ToArray();importer.spriteBorder=new Vector4(border[0],border[1],border[2],border[3]);
                importer.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if(texture.width!=CanonicalJson.Int(asset["width"])||texture.height!=CanonicalJson.Int(asset["height"]))throw new InvalidOperationException("Imported dimensions mismatch "+path);
            }
            Debug.Log("TASK001_VISUAL_IMPORT_OK: "+assets.Length+" assets, hashes/dimensions/import settings; human visual approval pending; audio excluded");
        }
    }
}
#endif
