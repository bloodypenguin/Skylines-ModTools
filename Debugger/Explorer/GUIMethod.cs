using ModTools.UI;
using ModTools.Utils;
using System;
using System.Reflection;
using UnityEngine;

namespace ModTools.Explorer
{
    internal static class GUIMethod
    {
        public static void OnSceneTreeReflectMethod(ReferenceChain refChain, object obj, MethodInfo method, int nameHighlightFrom = -1, int nameHighlightLength = 0)
        {
            if (!SceneExplorerCommon.SceneTreeCheckDepth(refChain))
            {
                return;
            }

            if (obj == null || method == null)
            {
                SceneExplorerCommon.OnSceneTreeMessage(refChain, "null");
                return;
            }

            GUILayout.BeginHorizontal(GUIWindow.HighlightStyle);
            SceneExplorerCommon.InsertIndent(refChain.Indentation);

            if (MainWindow.Instance.Config.ShowModifiers) {
                try {
                    GUI.contentColor = MainWindow.Instance.Config.MemberTypeColor;
                    //GUILayout.Label("method ");

                    {
                        GUI.contentColor = MainWindow.Instance.Config.ModifierColor;
                        string modifiers = method.GetAccessmodifier().ToString2();
                        if (!string.IsNullOrEmpty(modifiers)) {
                            GUILayout.Label(modifiers + " ");
                        }
                    }

                    {
                        GUI.contentColor = MainWindow.Instance.Config.MemberTypeColor;
                        string modifiers = method.GetTypeModifier().ToString2();
                        if (!string.IsNullOrEmpty(modifiers)) {
                            GUILayout.Label(modifiers + " ");
                        }
                    }
                } catch (Exception ex) {
                    Logger.Exception(ex);
                    GUI.contentColor = MainWindow.Instance.Config.ModifierColor;
                    GUILayout.Label(ex.GetType().Name + " ");
                }
            }

            GUI.contentColor = MainWindow.Instance.Config.TypeColor;
            GUILayout.Label(method.ReturnType + " ");
            GUIMemberName.MemberName(method, nameHighlightFrom, nameHighlightLength);
            GUI.contentColor = Color.white;
            GUILayout.Label("(");
            GUI.contentColor = MainWindow.Instance.Config.NameColor;

            var parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; ++i) {
                if (i > 0) {
                    GUI.contentColor = Color.white;
                    GUILayout.Label(", ");
                }

                var param = parameters[i];
                GUI.contentColor = MainWindow.Instance.Config.TypeColor;
                GUILayout.Label(param.ParameterType.ToString() + " ");

                GUI.contentColor = MainWindow.Instance.Config.NameColor;
                GUILayout.Label(param.Name);
            }

            GUI.contentColor = Color.white;
            GUILayout.Label(")");

            GUILayout.FlexibleSpace();
            if (!method.IsGenericMethod)
            {
                if (method.GetParameters().Length == 0)
                {
                    if (GUILayout.Button("Invoke", GUILayout.ExpandWidth(false)))
                    {
                        method.Invoke(method.IsStatic ? null : obj, new object[] { });
                    }
                }
                else if (method.GetParameters().Length == 1
                         && method.GetParameters()[0].ParameterType.IsInstanceOfType(obj))
                {
                    if (GUILayout.Button("Invoke", GUILayout.ExpandWidth(false)))
                    {
                        method.Invoke(method.IsStatic ? null : obj, new[] { obj });
                    }
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}