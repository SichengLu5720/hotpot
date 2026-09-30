using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    public sealed partial class GameplayView
    {
        Texture2D orderSignTexture,orderClipClosed,orderClipOpen;
        RectTransform orderDepartureClip,orderDepartureLayer;
        readonly RectTransform[] currentOrderPapers=new RectTransform[4];
        readonly RawImage[] currentOrderClips=new RawImage[4];
        readonly float[] orderPaperAges={1,1,1,1};
        static readonly Rect PaperUv=new Rect(197f/1983,159f/793,1589f/1983,475f/793);
        static readonly Rect ClipUv=new Rect(338f/1254,228f/1254,656f/1254,628f/1254);
        void BuildOrderSign(Transform parent,ViewOrder order)
        {
            var sign=Node(parent,"PaperOrderSign_"+order.slot,new Rect(0,0,100,54));
            currentOrderPapers[order.slot]=sign;
            BuildOrderSignContents(sign,order.foodId,order.count,order.required);
            SetOrderTransition(order.slot,orderPaperAges[order.slot]);
        }
        void BuildOrderClip(Transform parent,int slot)
        {
            if(!orderClipClosed)orderClipClosed=Resources.Load<Texture2D>("Hotpot/TASK049/copper-clip-closed");
            if(!orderClipOpen)orderClipOpen=Resources.Load<Texture2D>("Hotpot/TASK049/copper-clip-open");
            Picture(parent,orderClipClosed,new Rect(38,32,24,23),"CopperClip");
            var clip=parent.GetChild(parent.childCount-1).GetComponent<RawImage>();clip.uvRect=ClipUv;clip.raycastTarget=false;
            currentOrderClips[slot]=clip;SetOrderTransition(slot,orderPaperAges[slot]);
        }
        public void SetOrderTransition(int slot,float age)
        {
            orderPaperAges[slot]=age;
            float offset=age<.30f?28*(1-Mathf.SmoothStep(0,1,age/.30f)):age<.42f?-1.2f*Mathf.Sin((age-.30f)/.12f*Mathf.PI):0;
            if(currentOrderPapers[slot])currentOrderPapers[slot].anchoredPosition=new Vector2(0,offset);
            if(currentOrderClips[slot])currentOrderClips[slot].texture=age<.28f?orderClipOpen:orderClipClosed;
        }
        void BuildOrderSignContents(RectTransform sign,int foodId,int count,int required)
        {
            if(!orderSignTexture)orderSignTexture=Resources.Load<Texture2D>("Hotpot/TASK049/blank-paper");
            Picture(sign,orderSignTexture,new Rect(3,0,94,40),"OrderPaper");
            sign.Find("OrderPaper").GetComponent<RawImage>().uvRect=PaperUv;
            Food(sign,foodId,new Rect(7,1,34,34));
            Label(sign,count+"/"+required,new Rect(43,0,48,35),20);
            foreach(var graphic in sign.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
        }
        public RectTransform CreateDepartingOrderSign(int slot,int completedFoodId)
        {
            if(!orderDepartureClip||orderDepartureClip.parent!=board)
            {
                orderDepartureClip=Node(board,"OrderDepartureClip",new Rect(0,54,420,173));
                orderDepartureClip.gameObject.AddComponent<RectMask2D>();
                orderDepartureClip.SetSiblingIndex(Mathf.Min(hud.GetSiblingIndex(),orderNodes[0].GetSiblingIndex()));
                orderDepartureLayer=Node(orderDepartureClip,"OrderDepartureLayer",new Rect(0,-54,420,900));
            }
            var node=Node(orderDepartureLayer,"DepartingOrderSign_"+slot,new Rect(6+slot*103,72,100,54));
            BuildOrderSignContents(node,completedFoodId,3,3);
            return node;
        }
    }
}

