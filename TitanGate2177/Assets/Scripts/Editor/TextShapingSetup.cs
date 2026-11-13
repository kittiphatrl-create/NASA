using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TitanGate.EditorTools
{
    /// <summary>The game requests advanced shaping for Thai; enable the matching Unity 6 project setting.</summary>
    public static class TextShapingSetup
    {
        [InitializeOnLoadMethod]
        static void Schedule() => EditorApplication.delayCall += Enable;

        [MenuItem("Titan Gate/Enable Thai Text Shaping")]
        public static void Enable()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("UnityEditor.UIElements.UIToolkitProjectSettings");
                var property = type?.GetProperty("enableAdvancedText", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (property == null || !property.CanWrite) continue;
                if (!(bool)property.GetValue(null)) property.SetValue(null, true);
                Debug.Log("Titan Gate: advanced Thai text shaping enabled in UI Toolkit project settings.");
                return;
            }
            Debug.LogWarning("Titan Gate: enable Advanced Text Generator in Project Settings > UI Toolkit for Thai text shaping.");
        }
    }
}
