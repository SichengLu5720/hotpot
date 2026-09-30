using System;
using HotpotSort.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    public sealed class ToolDemoLoop : MonoBehaviour
    {
        const float HoldSeconds=1f,MotionSeconds=3f,CycleSeconds=HoldSeconds+MotionSeconds;
        RectTransform stage,cycleRoot;
        CanvasGroup group;
        RewardKind kind;
        Texture2D plate,dish,hint,potBody,potBroth,potRim;
        Texture2D[] foods;
        Rect[] foodUvs;
        RectTransform[] moving=new RectTransform[0];
        Vector2[] from=new Vector2[0],to=new Vector2[0];
        Vector2[] cluster=new Vector2[0];
        float[] clearAngles=new float[0];
        RectTransform clearTransport;
        RectTransform swapDim,swapTarget;
        RawImage swapTargetFood;
        Text swapCount;
        int swapFoodId;
        float age;
        public int Generation { get; private set; }
        public float CycleDuration=>CycleSeconds;
        public RewardKind Kind=>kind;

        public void Initialize(RewardKind value,Texture2D plateTexture,Texture2D dishTexture,Texture2D hintTexture,Texture2D[] foodTextures,Rect[] uvs,Texture2D body=null,Texture2D broth=null,Texture2D rim=null)
        {
            kind=value;plate=plateTexture;dish=dishTexture;hint=hintTexture;foods=foodTextures;foodUvs=uvs;
            potBody=body;potBroth=broth;potRim=rim;
            stage=(RectTransform)transform;if(!GetComponent<RectMask2D>())gameObject.AddComponent<RectMask2D>();age=0;Generation=0;Rebuild();
        }
        void Update(){Advance(Time.unscaledDeltaTime);}
        public void Advance(float delta)
        {
            if(!stage||delta<=0)return;
            age+=Mathf.Min(delta,.1f);
            while(age>=CycleSeconds){age-=CycleSeconds;Rebuild();}
            float t=Mathf.Clamp01((age-HoldSeconds)/MotionSeconds);
            if(group)
            {
                float fadeIn=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.20f));
                float fadeOut=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((age-3.52f)/.48f));
                group.alpha=fadeIn*fadeOut;
            }
            if(kind==RewardKind.SwapOrder)AnimateSwap(t);
            else if(kind==RewardKind.ClearBuffer)AnimateClear(t);
            else AnimateShuffle(t);
        }
        void Rebuild()
        {
            if(cycleRoot){cycleRoot.gameObject.SetActive(false);Destroy(cycleRoot.gameObject);}
            cycleRoot=Node(stage,"DemoCycle_"+Generation,new Rect(0,0,stage.rect.width,stage.rect.height));
            group=cycleRoot.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
            moving=new RectTransform[0];from=new Vector2[0];to=new Vector2[0];cluster=new Vector2[0];clearAngles=new float[0];clearTransport=null;
            int variant=Generation++%3;
            if(kind==RewardKind.SwapOrder)BuildSwap(variant);
            else if(kind==RewardKind.ClearBuffer)BuildClear(variant);
            else BuildShuffle(variant);
        }
        void BuildSwap(int variant)
        {
            swapFoodId=(variant*3)%16;
            for(int i=0;i<5;i++)Raw(cycleRoot,"BufferDish_"+i,dish,new Rect(13+i*58,182,50,50));
            var other=Node(cycleRoot,"SwapOtherOrder",new Rect(177,28,108,53));
            SwapPot(other);
            Raw(other,"OrderCard",hint,new Rect(0,0,108,53));Raw(other,"Target",Food((swapFoodId+2)%16),new Rect(7,7,38,38),FoodUv((swapFoodId+2)%16));DemoText(other,"0/3",new Rect(49,8,55,36));
            swapDim=Raw(cycleRoot,"SwapDemoDim",Texture2D.whiteTexture,new Rect(0,0,310,270));swapDim.GetComponent<RawImage>().color=new Color(0,0,0,.65f);swapDim.gameObject.SetActive(false);
            swapTarget=Node(cycleRoot,"SwapSelectedOrder",new Rect(25,28,108,53));
            SwapPot(swapTarget);
            Raw(swapTarget,"OrderCard",hint,new Rect(0,0,108,53));
            swapTargetFood=Raw(swapTarget,"Target",Food(swapFoodId),new Rect(7,7,38,38),FoodUv(swapFoodId)).GetComponent<RawImage>();
            swapCount=DemoText(swapTarget,"2/3",new Rect(49,8,55,36));
            moving=new RectTransform[2];from=new[]{new Vector2(48,103),new Vector2(77,108)};to=new[]{new Vector2(21,190),new Vector2(79,190)};
            for(int i=0;i<2;i++)moving[i]=Raw(cycleRoot,"SwapReturnFood_"+i,Food(swapFoodId),new Rect(from[i].x,from[i].y,34,34),FoodUv(swapFoodId));
        }
        void SwapPot(Transform parent)
        {
            var rect=new Rect(-10,32,128,128);
            Raw(parent,"PotBody",potBody,rect);
            Raw(parent,"Broth",potBroth,rect);
            Raw(parent,"PotRim",potRim,rect);
        }
        Text DemoText(Transform parent,string value,Rect rect)
        {
            var node=Node(parent,"OrderCount",rect);node.sizeDelta=rect.size*4;node.localScale=Vector3.one*.25f;
            var label=node.gameObject.AddComponent<Text>();label.font=TaskAssetValidation.LoadModernFont();label.fontSize=84;label.text=value;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.22f,.10f,.04f);label.raycastTarget=false;return label;
        }
        void AnimateSwap(float t)
        {
            swapDim.gameObject.SetActive(t>0&&t<.70f);
            float travel=Mathf.Clamp01((t-.26f)/(.34f/3f));
            for(int i=0;i<moving.Length;i++)SetTopLeft(moving[i],Vector2.Lerp(from[i],to[i],travel));
            bool changed=t>=.70f;int id=changed?(swapFoodId+1)%16:swapFoodId;
            swapTargetFood.texture=Food(id);swapTargetFood.uvRect=FoodUv(id);swapCount.text=changed?"0/3":"2/3";
        }
        void BuildClear(int variant)
        {
            clearTransport=Node(cycleRoot,"GatherTransport",new Rect(0,0,stage.rect.width,stage.rect.height));
            var gather=Raw(clearTransport,"GatherPlate",plate,new Rect(102,52,106,106));
            var dishPositions=new[]{new Vector2(14,190),new Vector2(72,188),new Vector2(130,194),new Vector2(188,188),new Vector2(246,190)};
            moving=new RectTransform[5];from=new Vector2[5];to=new Vector2[5];
            Vector2[][] mixedLayouts={
                new[]{new Vector2(114,70),new Vector2(143,65),new Vector2(159,87),new Vector2(124,101),new Vector2(149,108)},
                new[]{new Vector2(121,64),new Vector2(151,72),new Vector2(111,91),new Vector2(140,96),new Vector2(159,109)},
                new[]{new Vector2(112,76),new Vector2(140,63),new Vector2(160,80),new Vector2(129,99),new Vector2(151,108)}
            };
            float[][] mixedAngles={new[]{-11f,8f,-6f,12f,-8f},new[]{7f,-12f,9f,-5f,11f},new[]{-7f,11f,-10f,6f,-4f}};
            clearAngles=mixedAngles[variant];
            for(int i=0;i<5;i++)
            {
                Raw(cycleRoot,"BufferDish_"+i,dish,new Rect(dishPositions[i].x,dishPositions[i].y,50,50));
                var food=Raw(clearTransport,"GatherFood_"+i,Food((i+variant*3)%16),new Rect(dishPositions[i].x+8,dishPositions[i].y+8,34,34),FoodUv((i+variant*3)%16));
                moving[i]=food;from[i]=new Vector2(dishPositions[i].x+8,dishPositions[i].y+8);
                to[i]=mixedLayouts[variant][i];
            }
            clearTransport.SetAsLastSibling();
        }
        void AnimateClear(float t)
        {
            if(moving.Length<5||!clearTransport)return;
            for(int i=0;i<5;i++)
            {
                float local=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-(.09f+i*.065f))/.25f));
                SetTopLeft(moving[i],Vector2.Lerp(from[i],to[i],local));
                moving[i].localScale=Vector3.one*Mathf.Lerp(1,.72f,local);
                moving[i].localRotation=Quaternion.Euler(0,0,Mathf.Lerp(0,clearAngles[i],local));
            }
            // Once gathered, plate and food share one parent so departure cannot drift apart.
            float depart=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.66f)/.18f));
            SetTopLeft(clearTransport,new Vector2(0,Mathf.Lerp(0,-182,depart)));
            clearTransport.localScale=Vector3.one*Mathf.Lerp(1,.9f,depart);
        }
        void BuildShuffle(int variant)
        {
            from=new[]{new Vector2(25,31),new Vector2(200,43),new Vector2(38,155),new Vector2(191,146)};
            Vector2[][] layouts={
                new[]{new Vector2(19,159),new Vector2(192,145),new Vector2(102,116),new Vector2(66,29)},
                new[]{new Vector2(34,157),new Vector2(198,168),new Vector2(119,112),new Vector2(169,33)},
                new[]{new Vector2(18,149),new Vector2(186,164),new Vector2(99,125),new Vector2(49,41)}
            };
            to=layouts[variant];moving=new RectTransform[4];
            for(int i=0;i<4;i++)
                moving[i]=PlateToken("ShufflePlate_"+i,from[i],84,(i*3+variant)%16,(i*3+variant+6)%16);
        }
        void AnimateShuffle(float t)
        {
            for(int i=0;i<moving.Length;i++)if(moving[i])
            {
                // A short empty beat separates clearing from illustrative top-down drops.
                bool oldBoard=t<.16f;
                float drop=Mathf.Clamp01((t-.29f-i*.065f)/.29f);
                moving[i].gameObject.SetActive(oldBoard||drop>0);
                Vector2 point=oldBoard?from[i]:Vector2.Lerp(new Vector2(to[i].x,-90),to[i],drop*drop);
                float scale=oldBoard?1-Mathf.SmoothStep(0,1,t/.16f):1;
                SetTopLeft(moving[i],point);moving[i].localScale=Vector3.one*scale;
            }
        }
        RectTransform PlateToken(string name,Vector2 at,float size,int foodA,int foodB)
        {
            var root=Node(cycleRoot,name,new Rect(at.x,at.y,size,size));
            Raw(root,"Plate",plate,new Rect(0,0,size,size));
            Raw(root,"FoodA",Food(foodA),new Rect(size*.16f,size*.20f,size*.43f,size*.43f),FoodUv(foodA));
            Raw(root,"FoodB",Food(foodB),new Rect(size*.44f,size*.39f,size*.39f,size*.39f),FoodUv(foodB));
            return root;
        }
        Texture2D Food(int id)=>foods!=null&&id>=0&&id<foods.Length?foods[id]:null;
        Rect FoodUv(int id)=>foodUvs!=null&&id>=0&&id<foodUvs.Length?foodUvs[id]:new Rect(0,0,1,1);
        static RectTransform Node(Transform parent,string name,Rect rect)
        {
            var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);
            rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);SetTopLeft(rt,rect.position);rt.sizeDelta=rect.size;return rt;
        }
        static RectTransform Raw(Transform parent,string name,Texture texture,Rect rect){return Raw(parent,name,texture,rect,new Rect(0,0,1,1));}
        static RectTransform Raw(Transform parent,string name,Texture texture,Rect rect,Rect uv)
        {
            var rt=Node(parent,name,rect);var image=rt.gameObject.AddComponent<RawImage>();image.texture=texture;image.uvRect=uv;image.raycastTarget=false;return rt;
        }
        static void SetTopLeft(RectTransform node,Vector2 point){if(node)node.anchoredPosition=new Vector2(point.x,-point.y);}
    }

    public sealed partial class GameplayView
    {
        void ToolRewardOfferV7(RewardKind kind,string title,string note,string action,RewardRoute route)
        {
            if(IsToolUsed(kind))return;
            var session=LastSnapshot?.sessionId;var generation=LastSnapshot?.sessionGeneration;
            var card=ModalV7("ToolDemo_"+kind,650);
            Label(card,title,new Rect(25,20,310,49),29);
            var stage=Skin(card,"ToolDemoStage",new Rect(25,82,310,270),"ui.panel");
            var stageImage=stage.GetComponent<Image>();if(stageImage)stageImage.raycastTarget=false;
            var uvs=new Rect[foods.Length];for(int i=0;i<uvs.Length;i++)uvs[i]=art.FoodUv(i);
            var demo=stage.gameObject.AddComponent<ToolDemoLoop>();demo.Initialize(kind,plate,dish,art.Texture("ui.order_card"),foods,uvs,potBody,CurrentBrothSurface(),potRim);
            Label(card,note,new Rect(28,370,304,52),17).color=ModernPalette.Muted;
            VButton(card,action,new Rect(40,484,280,52),()=>{CloseModal();if(IsToolUsed(kind)||LastSnapshot?.sessionId!=session||LastSnapshot?.sessionGeneration!=generation)return;if(kind==RewardKind.SwapOrder)RequestSwap(route);else RewardRequested?.Invoke(kind,route);},true,21);
            VButton(card,"取消",new Rect(40,552,280,48),()=>{CloseModal();if(kind==RewardKind.SwapOrder)CancelSwapPresentation();},false,20);
        }
    }
}
