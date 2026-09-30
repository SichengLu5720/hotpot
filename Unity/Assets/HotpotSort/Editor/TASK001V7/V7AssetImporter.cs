#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HotpotSort.Bootstrap;
using HotpotSort.Presentation;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HotpotSort.Task001V7
{
    public static class V7AssetImporter
    {
        [Serializable] public sealed class Manifest {public Asset[] assets;}
        [Serializable] public sealed class Asset {public string assetId,relativePath,sha256;public int width,height;public bool alpha;public float[] pivot,nineSliceBorder;}
        [Serializable] public sealed class Result {public int textureCount,readableFoodCount;public string resourceRoot,status;}
        public static void Run()
        {
            int exit=0;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-visualEvidence");string evidence=i>=0?args[i+1]:null;
            try
            {
                string repo=Directory.GetParent(Application.dataPath).Parent.FullName;
                string path=Path.Combine(repo,".harness/art-production/TASK-001/v7/r001/director-final/asset-manifest.json");
                var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
                if(manifest.assets.Length!=65)throw new Exception("Expected 65 selected bitmap assets");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach(var asset in manifest.assets)
                {
                    string full=Path.Combine(repo,asset.relativePath);
                    using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(full))).Replace("-","").ToLowerInvariant()!=asset.sha256)throw new Exception("Hash mismatch: "+asset.assetId);
                    string unityPath=asset.relativePath.Substring("Unity/".Length);
                    var importer=AssetImporter.GetAtPath(unityPath) as TextureImporter;
                    if(!importer)throw new Exception("No TextureImporter "+unityPath);
                    importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                    importer.spritePivot=new Vector2(asset.pivot[0],asset.pivot[1]);importer.spritePixelsPerUnit=100;
                    importer.spriteBorder=new Vector4(asset.nineSliceBorder[0],asset.nineSliceBorder[1],asset.nineSliceBorder[2],asset.nineSliceBorder[3]);
                    var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;importer.SetTextureSettings(settings);
                    importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=asset.alpha;
                    importer.isReadable=asset.assetId.StartsWith("food.");importer.mipmapEnabled=false;importer.sRGBTexture=true;
                    importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
                    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(unityPath);
                    if(texture.width!=asset.width||texture.height!=asset.height)throw new Exception("Imported dimension mismatch "+asset.assetId);
                }
                var errors=TaskAssetValidation.Validate(V7Art.Root);if(errors.Length!=0)throw new Exception(string.Join("; ",errors));
                var scene=EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");
                var composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();
                var serialized=new SerializedObject(composition);serialized.FindProperty("approvedAssetRoot").stringValue=V7Art.Root;serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                var result=new Result{textureCount=manifest.assets.Length,readableFoodCount=16,resourceRoot=V7Art.Root,status="IMPORT_AND_BOOT_BINDING_VALID"};
                if(evidence!=null)File.WriteAllText(Path.Combine(evidence,"import-result.json"),JsonUtility.ToJson(result,true));
                Debug.Log("V7_IMPORT_OK "+JsonUtility.ToJson(result));
            }
            catch(Exception error){exit=1;Debug.LogException(error);}
            EditorApplication.Exit(exit);
        }
    }
}
#endif

