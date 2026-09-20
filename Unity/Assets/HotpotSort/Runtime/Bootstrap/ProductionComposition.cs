using System;
using HotpotSort.Contracts;
using HotpotSort.Session;
using UnityEngine;
namespace HotpotSort.Bootstrap
{
    // Final integration supplies a concrete component from fixed real A/B commits.
    public abstract class ProductionComposition : MonoBehaviour
    {
        public abstract string ContentVersion { get; }
        public abstract string ConfigurationDigest { get; }
        public abstract string BuildIdentity { get; }
        public abstract string RuntimeNamespace { get; }
        public abstract IGameSessionFactory CoreFactory { get; }
        public abstract IGameViewFactory ViewFactory { get; }
        public virtual ITimeProvider TrustedTime => null;
        public abstract bool UsesDevelopmentDoubles { get; }
        // Bind diagnostic output explicitly; no implicit remote upload.
        public abstract void BindDiagnostics(IDiagnosticSink sink);
        public abstract void BindSessionObservation(SessionController controller);
    }
}
