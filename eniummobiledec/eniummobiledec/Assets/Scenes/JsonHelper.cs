using System;

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = UnityEngine.JsonUtility.FromJson<Wrapper<T>>(FixJson(json));
        return wrapper.Items;
    }

    private static string FixJson(string value)
    {
        return "{\"Items\":" + value + "}";
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}
