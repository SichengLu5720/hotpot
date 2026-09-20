using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    /// <summary>Presentation-only VFX/SFX. It consumes authoritative view updates and never mutates gameplay state.</summary>
    public sealed class GameplayFeedback : MonoBehaviour
    {
        private readonly HashSet<string> consumed = new HashSet<string>();
        private readonly List<GameObject> effects = new List<GameObject>();
        private AudioSource audioSource;
        private RectTransform layer;
        private Canvas canvas;
        private ViewPhase phase = ViewPhase.Entry;
        private string sessionId;
        private AudioClip tapClip, dropClip, serveClip, winClip, failClip, buttonClip;
        private static readonly Color Accent = new Color32(232, 82, 55, 220);
        private static readonly Color Gold = new Color32(246, 177, 54, 230);

        public void Initialize(Transform parent, Canvas ownerCanvas)
        {
            canvas = ownerCanvas;
            var node = new GameObject("FeedbackLayer", typeof(RectTransform));
            node.transform.SetParent(parent, false);
            layer = (RectTransform)node.transform;
            layer.anchorMin = Vector2.zero; layer.anchorMax = Vector2.one;
            layer.offsetMin = layer.offsetMax = Vector2.zero;
            audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            tapClip = Tone("tap", 660, .055f); dropClip = Tone("drop", 440, .08f);
            serveClip = Tone("serve", 880, .13f); winClip = Tone("win", 988, .24f);
            failClip = Tone("fail", 180, .18f); buttonClip = Tone("button", 520, .045f);
        }

        public void ResetFeedback()
        {
            consumed.Clear(); sessionId = null; phase = ViewPhase.Entry; ClearEffects();
            if (audioSource) audioSource.Stop();
        }

        public void Apply(ViewUpdate update, ViewSnapshot previous)
        {
            if (update == null || update.snapshot == null) return;
            var current = update.snapshot;
            if (sessionId != current.sessionId) { consumed.Clear(); sessionId = current.sessionId; }
            foreach (var evt in update.events ?? new ViewEvent[0])
            {
                string key = current.sessionId + ":" + evt.sequence + ":" + evt.transactionId + ":" + evt.itemId + ":" + evt.kind;
                if (!consumed.Add(key)) continue;
                var point = ResolveItem(current, evt.itemId);
                switch (evt.kind ?? string.Empty)
                {
                    case "TapAccepted": Pulse(point, Accent, .12f); Play(tapClip); break;
                    case "ItemRoutedToOrder": Pulse(point, Gold, .18f); Play(dropClip); break;
                    case "ItemRoutedToBuffer": Pulse(BufferPoint(current, evt.itemId), Accent, .15f); Play(dropClip); break;
                    case "BufferAutoAbsorbed": Pulse(point, Gold, .14f); break;
                    case "OrderCompleted": case "ServeCompleted": case "PlateServed": Pulse(point, Gold, .24f); Play(serveClip); break;
                    case "ChallengeWon": Pulse(Vector2.zero, Gold, .4f); Play(winClip); break;
                    case "ChallengeFailed": case "BufferOverflowAttempted": Pulse(Vector2.zero, Accent, .3f); Play(failClip); break;
                }
            }
            if (current.phase != phase)
            {
                if (current.phase == ViewPhase.Won) { Pulse(Vector2.zero, Gold, .4f); Play(winClip); }
                else if (current.phase == ViewPhase.Overflow || current.phase == ViewPhase.Aborted) { Pulse(Vector2.zero, Accent, .3f); Play(failClip); }
                phase = current.phase;
                if (phase != ViewPhase.Running) ClearEffects();
            }
        }

        public void Button() { Play(buttonClip); }

        private Vector2 ResolveItem(ViewSnapshot snapshot, string itemId)
        {
            if (!string.IsNullOrEmpty(itemId))
                foreach (var plate in snapshot.plates ?? new ViewPlate[0])
                    foreach (var item in plate.items ?? new ViewItem[0])
                        if (item.itemId == itemId) return new Vector2(plate.x + item.x, plate.y + item.y);
            return Vector2.zero;
        }

        private Vector2 BufferPoint(ViewSnapshot snapshot, string itemId)
        {
            for (int i = 0; i < (snapshot.buffer ?? new ViewItem[0]).Length; i++)
                if (snapshot.buffer[i] != null && snapshot.buffer[i].itemId == itemId) return new Vector2(75 + i * 67, 259);
            return new Vector2(210, 259);
        }

        private void Pulse(Vector2 boardPoint, Color color, float size)
        {
            if (!layer) return;
            var go = new GameObject("FeedbackPulse", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(layer, false); var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.anchoredPosition = new Vector2(boardPoint.x - 210, 450 - boardPoint.y);
            rt.sizeDelta = Vector2.one * 26; var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            effects.Add(go); Destroy(go, Mathf.Max(.12f, size));
        }

        private void ClearEffects() { foreach (var effect in effects) if (effect) Destroy(effect); effects.Clear(); }
        private void Play(AudioClip clip) { try { if (audioSource && clip) audioSource.PlayOneShot(clip); } catch { } }
        private static AudioClip Tone(string name, float frequency, float seconds)
        {
            try { int rate = 22050, count = Mathf.CeilToInt(rate * seconds); var clip = AudioClip.Create(name, count, 1, rate, false); var data = new float[count]; for (int i=0;i<count;i++) data[i] = Mathf.Sin(2*Mathf.PI*frequency*i/rate) * (1f-i/(float)count) * .12f; clip.SetData(data,0); return clip; }
            catch { return null; }
        }
    }
}
