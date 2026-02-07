using System;
using System.IO;
using UnityEngine;

namespace LinguaStars.Client.Data
{
    public static class JsonFileStorage
    {
        public static void Save<T>(string fileName, T data)
        {
            string path = GetPath(fileName);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }

        public static T Load<T>(string fileName, Func<T> fallbackFactory) where T : class
        {
            string path = GetPath(fileName);
            if (!File.Exists(path))
            {
                return fallbackFactory();
            }

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return fallbackFactory();
            }

            T data = JsonUtility.FromJson<T>(json);
            return data ?? fallbackFactory();
        }

        private static string GetPath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }
    }
}
