using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HotpotSort.Presentation
{
    public static class PlateSupplyGeometry
    {
        public const float Gate=140,ClipTop=292,Floor=828,Top=-134;
        public static float SpawnY(float radius)=>-radius-8;
    }
    // Shared board coordinates are Y-down. Positive angles turn clockwise on screen.
    // UI RectTransforms therefore use -rotationDegrees; physics uses +rotationDegrees.
    public static class PlateItemTransform
    {
        public static PresentationPoint Rotate(float x,float y,float degrees)
        {double a=degrees*Math.PI/180,c=Math.Cos(a),s=Math.Sin(a);return new PresentationPoint((float)(x*c-y*s),(float)(x*s+y*c));}
        public static PresentationPoint ToLocalOffset(ViewItem item,float boardOffsetX,float boardOffsetY)=>Rotate(boardOffsetX,boardOffsetY,-item.rotationDegrees);
        public static PresentationPoint ToBoard(ViewItem item,float plateX,float plateY,float localX,float localY)
        {var p=Rotate(localX,localY,item.rotationDegrees);return new PresentationPoint(plateX+item.x+p.x,plateY+item.y+p.y);}
        public static PresentationPoint ToTextureUv(ViewItem item,float plateX,float plateY,float boardX,float boardY)
        {var p=ToLocalOffset(item,boardX-plateX-item.x,boardY-plateY-item.y);return new PresentationPoint(item.uvX+item.uvWidth*(.5f+p.x/(2*item.radius)),item.uvY+item.uvHeight*(.5f-p.y/(2*item.radius)));}
        public static PresentationPoint HalfExtents(ViewItem item)
        {double a=item.rotationDegrees*Math.PI/180;float e=(float)(item.radius*(Math.Abs(Math.Cos(a))+Math.Abs(Math.Sin(a))));return new PresentationPoint(e,e);}
    }
    // Alpha mask rows are top-to-bottom in the chosen square UV region. The hull uses
    // normalized item-local coordinates [-.5,.5]. Alpha determines contour, not occupancy area.
    public sealed class PlateFoodShape
    {
        public readonly PresentationPoint[] Contour;
        public readonly byte[] Alpha;
        public readonly int Resolution;
        public readonly float UvX,UvY,UvWidth,UvHeight;
        public readonly bool IsProxy;
        public PlateFoodShape(byte[] alpha,int resolution,float uvX=0,float uvY=0,float uvWidth=1,float uvHeight=1,bool isProxy=false)
        {
            if(alpha==null||resolution<2||alpha.Length!=resolution*resolution)throw new ArgumentException("Square alpha mask required");
            Alpha=(byte[])alpha.Clone();Resolution=resolution;UvX=uvX;UvY=uvY;UvWidth=uvWidth;UvHeight=uvHeight;IsProxy=isProxy;
            var points=new List<PresentationPoint>();
            for(int y=0;y<resolution;y++)for(int x=0;x<resolution;x++)if(Alpha[y*resolution+x]>25)
            {
                if(x>0&&y>0&&x<resolution-1&&y<resolution-1&&Alpha[y*resolution+x-1]>25&&Alpha[y*resolution+x+1]>25&&Alpha[(y-1)*resolution+x]>25&&Alpha[(y+1)*resolution+x]>25)continue;
                float left=(float)x/resolution-.5f,top=(float)y/resolution-.5f,step=1f/resolution;
                points.Add(new PresentationPoint(left,top));points.Add(new PresentationPoint(left+step,top));points.Add(new PresentationPoint(left,top+step));points.Add(new PresentationPoint(left+step,top+step));
            }
            if(points.Count==0)throw new ArgumentException("Food alpha contour is empty");
            Contour=PlateItemLayout.ConvexHull(points).ToArray();
        }
        public bool Opaque(float x,float y)
        {int ix=(int)Math.Floor((x+.5)*Resolution),iy=(int)Math.Floor((y+.5)*Resolution);return ix>=0&&iy>=0&&ix<Resolution&&iy<Resolution&&Alpha[iy*Resolution+ix]>25;}
        public static PlateFoodShape ProxySquare()=>new PlateFoodShape(Enumerable.Repeat((byte)255,64).ToArray(),8,isProxy:true);
    }
    public sealed class PlateLayoutResult
    {
        public ViewItem[] Items;
        public float EnvelopeRatio,MinimumVisibleFraction;
        public bool UsesProxyContour;
    }
    public static class PlateItemLayout
    {
        public const string Version="plate_layout_v8_r013_1";
        public const float TargetRatio=.8f;
        // Directly transcribed from accepted r013/PreviewV8.cs, not new art decisions.
        static readonly float[][] Templates={
            new float[]{0,0},new float[]{-.30f,-.03f,.30f,.03f},new float[]{0,-.32f,-.31f,.24f,.33f,.25f},
            new float[]{-.30f,-.30f,.30f,-.29f,-.30f,.30f,.30f,.31f},new float[]{0,-.41f,-.41f,-.12f,.41f,-.10f,-.25f,.36f,.27f,.38f}
        };
        public static float Angle(ulong seed,string plateId,string itemId,int sourceIndex)
        {
            string key=Version+"|"+seed.ToString(CultureInfo.InvariantCulture)+"|"+plateId+"|"+itemId+"|"+sourceIndex.ToString(CultureInfo.InvariantCulture);
            ulong hash=14695981039346656037UL;unchecked{foreach(char c in key){hash^=c;hash*=1099511628211UL;}}
            return (hash%30001)/1000f-15f;
        }
        static double Cross(PresentationPoint a,PresentationPoint b,PresentationPoint c)=>(double)(b.x-a.x)*(c.y-a.y)-(double)(b.y-a.y)*(c.x-a.x);
        internal static List<PresentationPoint> ConvexHull(IEnumerable<PresentationPoint> input)
        {
            var points=input.OrderBy(p=>p.x).ThenBy(p=>p.y).ToArray();var hull=new List<PresentationPoint>();
            foreach(var p in points){while(hull.Count>=2&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            int lower=hull.Count;for(int i=points.Length-2;i>=0;i--){var p=points[i];while(hull.Count>lower&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            if(hull.Count>1)hull.RemoveAt(hull.Count-1);return hull;
        }
        struct Circle {public double x,y,r;public Circle(double x,double y,double r){this.x=x;this.y=y;this.r=r;}}
        static bool Outside(Circle c,PresentationPoint p)=>(p.x-c.x)*(p.x-c.x)+(p.y-c.y)*(p.y-c.y)>c.r*c.r+1e-9;
        static Circle Pair(PresentationPoint a,PresentationPoint b){double x=(a.x+b.x)/2d,y=(a.y+b.y)/2d;return new Circle(x,y,Math.Sqrt((a.x-x)*(a.x-x)+(a.y-y)*(a.y-y)));}
        static Circle Three(PresentationPoint a,PresentationPoint b,PresentationPoint c)
        {
            double d=2*((double)a.x*(b.y-c.y)+(double)b.x*(c.y-a.y)+(double)c.x*(a.y-b.y));
            if(Math.Abs(d)<1e-10){var ab=Pair(a,b);var ac=Pair(a,c);var bc=Pair(b,c);return ab.r>ac.r?(ab.r>bc.r?ab:bc):(ac.r>bc.r?ac:bc);}
            double aa=(double)a.x*a.x+(double)a.y*a.y,bb=(double)b.x*b.x+(double)b.y*b.y,cc=(double)c.x*c.x+(double)c.y*c.y;
            double x=(aa*(b.y-c.y)+bb*(c.y-a.y)+cc*(a.y-b.y))/d,y=(aa*(c.x-b.x)+bb*(a.x-c.x)+cc*(b.x-a.x))/d;
            return new Circle(x,y,Math.Sqrt((a.x-x)*(a.x-x)+(a.y-y)*(a.y-y)));
        }
        static Circle Enclose(IEnumerable<PresentationPoint> input)
        {
            var p=ConvexHull(input);var circle=new Circle(0,0,-1);
            for(int i=0;i<p.Count;i++)if(circle.r<0||Outside(circle,p[i]))
            {circle=new Circle(p[i].x,p[i].y,0);for(int j=0;j<i;j++)if(Outside(circle,p[j])){circle=Pair(p[i],p[j]);for(int k=0;k<j;k++)if(Outside(circle,p[k]))circle=Three(p[i],p[j],p[k]);}}
            return circle;
        }
        public static float MeasureEnvelope(ViewItem[] items,Func<int,PlateFoodShape> shapes,float plateRadius)
        {
            var points=new List<PresentationPoint>();foreach(var item in items)foreach(var p in shapes(item.foodId).Contour)points.Add(PlateItemTransform.ToBoard(item,0,0,p.x*item.radius*2,p.y*item.radius*2));
            return (float)(Enclose(points).r/plateRadius);
        }
        public static float Visibility(ViewItem[] items,PlateFoodShape[] shapes)
        {
            float minimum=1;
            for(int i=0;i<items.Length;i++)
            {
                int total=0,visible=0,n=shapes[i].Resolution;var item=items[i];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)if(shapes[i].Alpha[y*n+x]>25)
                {
                    total++;var at=PlateItemTransform.ToBoard(item,0,0,((x+.5f)/n-.5f)*item.radius*2,((y+.5f)/n-.5f)*item.radius*2);bool covered=false;
                    for(int j=i+1;j<items.Length;j++){var local=PlateItemTransform.ToLocalOffset(items[j],at.x-items[j].x,at.y-items[j].y);if(shapes[j].Opaque(local.x/(items[j].radius*2),local.y/(items[j].radius*2))){covered=true;break;}}
                    if(!covered)visible++;
                }
                minimum=Math.Min(minimum,(float)visible/total);
            }
            return minimum;
        }
        public static PlateLayoutResult Create(ulong seed,string plateId,float plateRadius,IEnumerable<ViewItem> initialItems,Func<int,PlateFoodShape> shapeProvider)
        {
            var items=initialItems.OrderBy(i=>i.sourceIndex).ToArray();int count=items.Length;
            if(count<1||count>5||plateRadius<=0)throw new ArgumentException("Expected initial plate of one to five items");
            var shapes=items.Select(i=>shapeProvider(i.foodId)).ToArray();var positions=Templates[count-1];
            var result=items.Select((item,index)=>new ViewItem{itemId=item.itemId,foodId=item.foodId,sourceIndex=item.sourceIndex,drawOrder=index,layoutVersion=Version,
                rotationDegrees=Angle(seed,plateId,item.itemId,item.sourceIndex),uvX=shapes[index].UvX,uvY=shapes[index].UvY,uvWidth=shapes[index].UvWidth,uvHeight=shapes[index].UvHeight}).ToArray();
            for(int step=0;step<=20;step++)
            {
                float diameter=1.06f-.025f*step;var points=new List<PresentationPoint>();
                for(int i=0;i<count;i++)foreach(var p in shapes[i].Contour){var v=PlateItemTransform.Rotate(p.x*diameter,p.y*diameter,result[i].rotationDegrees);points.Add(new PresentationPoint(v.x+positions[i*2],v.y+positions[i*2+1]));}
                var circle=Enclose(points);float factor=(float)(plateRadius*TargetRatio/circle.r);
                for(int i=0;i<count;i++){result[i].x=(positions[i*2]-(float)circle.x)*factor;result[i].y=(positions[i*2+1]-(float)circle.y)*factor;result[i].radius=diameter*factor*.5f;}
                float visible=Visibility(result,shapes);
                // A geometry-only diagnostic proxy cannot certify real food visibility.
                // Production always supplies readable asset alpha and never takes this fallback.
                if(visible>=.775f||(step==20&&shapes.Any(s=>s.IsProxy)))return new PlateLayoutResult{Items=result,EnvelopeRatio=TargetRatio,MinimumVisibleFraction=visible,UsesProxyContour=shapes.Any(s=>s.IsProxy)};
            }
            throw new InvalidOperationException("Approved r013 layout cannot preserve visibility for plate "+plateId+"; return to Visual for parameter review");
        }
    }
    // Own one cache per gameplay session. Build the layout on the first active snapshot;
    // retain all initial members even after they move to Buffer/Order/a returned plate.
    public sealed class PlateLayoutCache
    {
        readonly Dictionary<string,PlateLayoutResult> entries=new Dictionary<string,PlateLayoutResult>();
        readonly Func<int,PlateFoodShape> shapes;
        readonly PlateFoodShape proxy;
        public PlateLayoutCache(Func<int,PlateFoodShape> shapes=null){if(shapes==null){proxy=PlateFoodShape.ProxySquare();this.shapes=id=>proxy;}else this.shapes=shapes;}
        public void Clear()=>entries.Clear();
        public PlateLayoutResult GetOrCreate(ulong seed,string plateId,float radius,IEnumerable<ViewItem> initialItems)
        {if(!entries.TryGetValue(plateId,out var value)){value=PlateItemLayout.Create(seed,plateId,radius,initialItems,shapes);entries.Add(plateId,value);}return value;}
    }
}
