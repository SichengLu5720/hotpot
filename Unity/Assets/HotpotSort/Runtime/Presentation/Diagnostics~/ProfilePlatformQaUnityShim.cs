namespace UnityEngine
{
    // Pure fixture JSON follows public-field Unity DTOs. Actual player API is compiled in Unity.
    public static class JsonUtility
    {
        static readonly System.Text.Json.JsonSerializerOptions Options=new System.Text.Json.JsonSerializerOptions{IncludeFields=true};
        public static string ToJson(object value)=>System.Text.Json.JsonSerializer.Serialize(value,value.GetType(),Options);
        public static T FromJson<T>(string value)=>System.Text.Json.JsonSerializer.Deserialize<T>(value,Options);
        public static void FromJsonOverwrite(string value,object target)=>throw new System.NotSupportedException("Use Unity for config JSON");
    }
}
