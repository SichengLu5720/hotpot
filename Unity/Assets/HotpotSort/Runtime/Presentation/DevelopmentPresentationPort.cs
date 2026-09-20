using System;
using UnityEngine;

namespace HotpotSort.Presentation
{
    // Explicit editor/development fixture. Never added by GameplayView or the production scene.
    public sealed class DevelopmentPresentationPort : MonoBehaviour, IPresentationPort
    {
        public event Action<ViewUpdate> Updated;
        public event Action<ViewAction> ActionObserved;
        public event Action<ViewTap> TapObserved;
        public event Action<ViewSupplyObservation> SupplyObserved;
        private ViewSnapshot current = new ViewSnapshot { sessionId = "development-fixture", phase = ViewPhase.Entry };
        public ViewSnapshot Read() { return current; }
        public void SessionAction(ViewAction action) { ActionObserved?.Invoke(action); }
        public void Tap(ViewTap command) { TapObserved?.Invoke(command); }
        public void ObserveSupply(ViewSupplyObservation observation) { SupplyObserved?.Invoke(observation); }
        public void Publish(ViewSnapshot snapshot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            current = snapshot; Updated?.Invoke(new ViewUpdate { snapshot = snapshot });
#else
            throw new InvalidOperationException("Development fixture is disabled in production.");
#endif
        }
    }
}
