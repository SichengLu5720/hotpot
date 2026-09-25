#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotpotSort.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    public static class Task030CollectionVisualDiagnostic
    {
        sealed class Store:ICollectionStore
        {
            public CollectionDocument doc=new CollectionDocument();
            public event Action CollectionChanged;
            public Store(){doc.entryUnlocked=true;for(int i=0;i<16;i++){doc.unlocked.Add(CollectionCatalog.Id(i));doc.selected.Add(CollectionCatalog.Id(i));}}
            public CollectionDocument ReadCollection()=>doc;
            public List<string> BeginSelectionDraft()=>new List<string>(doc.selected);
            public bool TrySaveSelection(IEnumerable<string> ids){var next=ids.ToList();if(next.Count<16)return false;doc.selected=next;return true;}
            public string[] CreateSessionSelection()=>doc.selected.ToArray();
            public CollectionReward RecordWin(string sessionId,DateTimeOffset utc,bool grantLocally)=>null;
            public bool TryConsumeTool(RewardKind kind,string operationId)=>false;
            public bool TryExchangeDuplicate(string operationId,string offeredId,string receivedId)=>false;
            public void ApplyAuthoritativeSnapshot(CollectionDocument value){doc=value;CollectionChanged?.Invoke();}
        }
        static void Check(bool pass,string message){if(!pass)throw new Exception("TASK030: "+message);Debug.Log("TASK030_VIS_PASS "+message);}
        [MenuItem("Hotpot/TASK030/Check Collection Visual Binding")]
        public static void Run()
        {
            // Use Play mode: production UI deliberately uses deferred Destroy.
            if(!Application.isPlaying)throw new InvalidOperationException("Run this visual diagnostic in Play mode.");
            var fixture=new GameObject("Task030VisualFixture",typeof(RectTransform),typeof(Canvas));
            var view=fixture.AddComponent<GameplayView>();
            try
            {
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                var rect=fixture.GetComponent<RectTransform>();
                foreach(string field in new[]{"content","canvasRoot"})typeof(GameplayView).GetField(field,flags).SetValue(view,rect);
                typeof(GameplayView).GetField("viewport",flags).SetValue(view,new Rect(0,0,420,900));
                view.playerFont=TaskAssetValidation.LoadModernFont();
                var store=new Store();view.ConfigureCollection(store);view.AttachCollectionEntry(rect);
                Check(fixture.GetComponentsInChildren<Button>().Any(b=>b.name=="CollectionEntry"&&b.interactable),"unlocked entry interactive");
                store.doc.entryUnlocked=false;view.RefreshCollectionEntry();
                Check(!fixture.GetComponentsInChildren<Button>().First(b=>b.name=="CollectionEntry").interactable,"locked entry noninteractive");
                store.doc.entryUnlocked=true;view.OpenCollection();
                var grid=fixture.GetComponentsInChildren<RectTransform>().First(t=>t.name=="CollectionGrid");
                Check(grid.childCount==32,"32 collection tiles");
                Check(grid.GetChild(0).GetComponent<RectTransform>().anchoredPosition.y==grid.GetChild(3).GetComponent<RectTransform>().anchoredPosition.y,"four tiles per row");
                Check(grid.GetChild(4).GetComponent<RectTransform>().anchoredPosition.y<grid.GetChild(0).GetComponent<RectTransform>().anchoredPosition.y,"next row below");
                var draft=(HashSet<string>)typeof(GameplayView).GetField("collectionDraft",flags).GetValue(view);draft.Remove("food_00");
                Check(!view.TryCloseCollection(),"insufficient draft cannot close");
                Check(fixture.GetComponentsInChildren<Text>().Any(t=>t.text==CollectionCatalog.TooFewMessage),"exact insufficient-food message");
                draft.Add("food_00");Check(view.TryCloseCollection(),"valid draft saves and closes");
                Check(view.ShowCollectionReward(new CollectionReward{day="diagnostic",sessionId="one",ingredientId="food_24",firstUnlock=true},()=>{}),"reward mounts");
                Check(!view.ShowCollectionReward(new CollectionReward{day="diagnostic",sessionId="one"},()=>{}),"duplicate reward blocked");
            }
            finally{view.DisposeCollectionPresentation();UnityEngine.Object.Destroy(fixture);}
        }
    }
}
#endif
