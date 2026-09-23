using System.Collections.Generic;
using UnityEngine;

namespace HotpotSort.Presentation
{
    public sealed partial class GameplayView
    {
        public const float ShuffleFeedbackSeconds = .54f;
        readonly Dictionary<string, Vector2> shuffleOrigins = new Dictionary<string, Vector2>();
        readonly Dictionary<string, Vector2> shufflePositions = new Dictionary<string, Vector2>();
        readonly Dictionary<string, float> shuffleRotations = new Dictionary<string, float>();
        float shuffleAge, shuffleScale = 1;
        string shuffleSession;
        long shuffleGeneration;
        public bool ShuffleFeedbackActive => shuffleOrigins.Count != 0;

        // The original atomic physical shuffle remains authoritative. Only its visual
        // transition is interpolated; the brief settling interval cannot accept a tap
        // at a decorative intermediate position or consume another reward.
        public bool TryShuffleWithFeedback()
        {
            if (ShuffleFeedbackActive || World == null || LastSnapshot == null) return false;
            var starts = new Dictionary<string, Vector2>();
            foreach (var body in World.Bodies) starts[body.data.plateId] = World.Position(body);
            if (!World.TryShuffle(true)) return false;
            shuffleOrigins.Clear();
            foreach (var pair in starts) shuffleOrigins.Add(pair.Key, pair.Value);
            shuffleAge = 0; shuffleScale = 1;
            shuffleSession = LastSnapshot.sessionId; shuffleGeneration = LastSnapshot.sessionGeneration;
            TickShuffleFeedback(0);
            return true;
        }

        void CancelShuffleFeedback()
        {
            shuffleOrigins.Clear(); shufflePositions.Clear(); shuffleRotations.Clear(); shuffleAge = 0; shuffleScale = 1;
            foreach (var pair in plateVisuals) if (pair.Value) { pair.Value.localScale = Vector3.one; pair.Value.localRotation = Quaternion.identity; }
        }

        void TickShuffleFeedback(float delta)
        {
            if (!ShuffleFeedbackActive) return;
            if (LastSnapshot == null || shuffleSession != LastSnapshot.sessionId || shuffleGeneration != LastSnapshot.sessionGeneration ||
                LastSnapshot.phase == ViewPhase.Entry || LastSnapshot.phase == ViewPhase.Aborted || LastSnapshot.phase == ViewPhase.Overflow || LastSnapshot.phase == ViewPhase.Won)
            { CancelShuffleFeedback(); return; }
            if (PresentationForeground && LastSnapshot.phase == ViewPhase.Running && LastSnapshot.pauseReasons == ViewPauseReasons.None)
                shuffleAge += Mathf.Clamp(delta, 0, .1f);
            if (shuffleAge >= ShuffleFeedbackSeconds) { CancelShuffleFeedback(); return; }
            float t = Mathf.Clamp01(shuffleAge / ShuffleFeedbackSeconds);
            // Ease in quickly, travel cleanly, then spend the last beat settling.
            float motion = t * t * (3 - 2 * t);
            float settle = Mathf.Clamp01((t - .72f) / .28f);
            shuffleScale = 1 - .16f * Mathf.Sin(t * Mathf.PI) + .025f * Mathf.Sin(settle * Mathf.PI);
            shufflePositions.Clear();
            shuffleRotations.Clear();
            foreach (var body in World.Bodies)
            {
                string id = body.data.plateId;
                var end = World.Position(body);
                if (!shuffleOrigins.TryGetValue(id, out var start)) { shufflePositions[id] = end; shuffleRotations[id] = 0; continue; }
                var travel = end - start;
                var normal = travel.sqrMagnitude > .01f ? new Vector2(-travel.y, travel.x).normalized : Vector2.zero;
                float arc = Mathf.Sin(motion * Mathf.PI);
                shufflePositions[id] = Vector2.Lerp(start, end, motion) + normal * (arc * Mathf.Min(22, travel.magnitude * .10f));
                float direction = Mathf.Abs(travel.x) > .01f ? Mathf.Sign(travel.x) : Mathf.Sign(travel.y);
                shuffleRotations[id] = direction * arc * Mathf.Min(4.5f, 1.5f + travel.magnitude * .012f);
            }
            // Shrink the complete plate+food group, never colliders. A conservative
            // shared scale keeps even crossing trajectories visually non-overlapping.
            foreach (var a in World.Bodies)
            {
                var at = shufflePositions[a.data.plateId];
                float margin = Mathf.Min(at.x, 420-at.x);
                shuffleScale = Mathf.Min(shuffleScale, Mathf.Max(0, margin) / (a.data.radius * 1.15f));
                foreach (var b in World.Bodies)
                {
                    if (string.CompareOrdinal(a.data.plateId,b.data.plateId) >= 0) continue;
                    float distance = Vector2.Distance(at,shufflePositions[b.data.plateId]);
                    shuffleScale = Mathf.Min(shuffleScale, Mathf.Max(0,distance-1) / ((a.data.radius+b.data.radius)*1.15f));
                }
            }
            shuffleScale = Mathf.Clamp01(shuffleScale);
        }

        void PositionPlateVisual(HotpotSort.UnityPhysics.PlatePresentationWorld.PlateBody body, RectTransform node)
        {
            var at = World.Position(body);
            if (ShuffleFeedbackActive && shufflePositions.TryGetValue(body.data.plateId, out var animated)) at = animated;
            var position=new Vector2(at.x,-at.y);
            if((node.anchoredPosition-position).sqrMagnitude>.000001f)node.anchoredPosition=position;
            float scale=ShuffleFeedbackActive?shuffleScale:1;
            if(Mathf.Abs(node.localScale.x-scale)>.00001f)node.localScale=Vector3.one*scale;
            float rotation=ShuffleFeedbackActive&&shuffleRotations.TryGetValue(body.data.plateId,out var angle)?angle:0;
            if(Mathf.Abs(Mathf.DeltaAngle(node.localEulerAngles.z,rotation))>.0001f)node.localRotation=Quaternion.Euler(0,0,rotation);
        }
    }
}
