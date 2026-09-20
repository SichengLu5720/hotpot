using UnityEngine;

namespace HotpotSort.Bootstrap
{
    // Sole production scene entry. TASK-003 owns final composition.
    // K0 deliberately has no runtime initializer or prototype game fallback.
    public sealed class Bootstrap : MonoBehaviour
    {
        public bool IsConfigured => false;
        public string Status => "K0: real core, view and platform composition pending";
    }
}
