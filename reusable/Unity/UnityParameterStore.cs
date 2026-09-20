using System;
using System.Collections.Generic;
using UnityEngine;

namespace Harness.Reusable.Unity
{
    public sealed class UnityParameterStore : IParameterStore
    {
        [Serializable] private sealed class Item { public string key; public double value; }
        [Serializable] private sealed class Snapshot { public int version = 1; public List<Item> values = new List<Item>(); }
        private readonly string key;
        public UnityParameterStore(string projectKey) { key = projectKey + ".dev.parameters.v1"; }
        public IDictionary<string, double> Load()
        {
            var result = new Dictionary<string, double>();
            if (!PlayerPrefs.HasKey(key)) return result;
            var snapshot = JsonUtility.FromJson<Snapshot>(PlayerPrefs.GetString(key));
            if (snapshot == null || snapshot.version != 1 || snapshot.values == null)
                throw new InvalidOperationException("Unsupported developer settings.");
            foreach (var item in snapshot.values) result.Add(item.key, item.value);
            return result;
        }
        public void Save(IDictionary<string, double> values)
        {
            var snapshot = new Snapshot();
            foreach (var pair in values) snapshot.values.Add(new Item { key = pair.Key, value = pair.Value });
            PlayerPrefs.SetString(key, JsonUtility.ToJson(snapshot)); PlayerPrefs.Save();
        }
    }
}
