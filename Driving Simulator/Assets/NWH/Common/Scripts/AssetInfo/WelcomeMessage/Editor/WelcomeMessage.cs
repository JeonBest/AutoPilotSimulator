#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NWH.Common.WelcomeMessage
{
    [InitializeOnLoad]
    public class WelcomeMessage
    {
        private const string VERSION_KEY = "NWH_NVP2_VERSION";


        static WelcomeMessage()
        {
            foreach (AssetInfo.AssetInfo info in GetAllInstances<AssetInfo.AssetInfo>())
            {
                string versionKey = info.assetName + info.version;
                if (EditorPrefs.GetString(versionKey) != info.version)
                {
                    WelcomeMessageWindow window =
                        (WelcomeMessageWindow) ScriptableObject.CreateInstance(typeof(WelcomeMessageWindow));
                    if (window != null)
                    {
                        window.Init(info);
                    }
   
                    EditorPrefs.SetString(versionKey, info.version);
                }
            }
        }


        public static T[] GetAllInstances<T>() where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            T[]      a     = new T[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                a[i] = AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return a;
        }
    }
}

#endif
