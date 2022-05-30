using UnityEngine;

namespace PG
{
    public static class DeepClone
    {
        public static T GetClone<T> (this T obj)
        {
            var jsonObj = JsonUtility.ToJson(obj);
            return JsonUtility.FromJson<T> (jsonObj);
        }
    }
}

