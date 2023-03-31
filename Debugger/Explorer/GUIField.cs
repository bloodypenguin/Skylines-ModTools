using System;
using System.Reflection;
using ModTools.UI;
using ModTools.Utils;
using UnityEngine;

namespace ModTools.Explorer
{
    internal static class GUIField
    {
        public static void OnSceneTreeReflectField(SceneExplorerState state, ReferenceChain refChain, object obj, FieldInfo field, TypeUtil.SmartType smartType = TypeUtil.SmartType.Undefined, int nameHighlightFrom = -1, int nameHighlightLength = 0)
        {
            if (!SceneExplorerCommon.SceneTreeCheckDepth(refChain))
            {
                return;
            }

            if (obj == null || field == null)
            {
                SceneExplorerCommon.OnSceneTreeMessage(refChain, "null");
                return;
            }

            GUILayout.BeginHorizontal(GUIWindow.HighlightStyle);
            SceneExplorerCommon.InsertIndent(refChain.Indentation);

            GUI.contentColor = Color.white;

            object value = null;

            try
            {
                value = field.GetValue(obj);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (SceneExplorer.FirstFrame) {
                return; // avoid exceptions (cached values might change after fist call to GetValue() of a property)
            }

            if (value != null)
            {
                GUIExpander.ExpanderControls(state, refChain, field.FieldType);
            }

            if (!field.CanWrie())
            {
                GUI.enabled = false;
            }

            if (MainWindow.Instance.Config.ShowModifiers) {
                try {
                    GUI.contentColor = MainWindow.Instance.Config.MemberTypeColor;
                    //GUILayout.Label("field ");

                    {
                        GUI.contentColor = MainWindow.Instance.Config.ModifierColor;
                        string modifiers = field.GetAccessmodifier().ToString2();
                        if (!string.IsNullOrEmpty(modifiers)) {
                            GUILayout.Label(modifiers + " ");
                        }
                    }

                    {
                        GUI.contentColor = MainWindow.Instance.Config.MemberTypeColor;
                        string modifiers = field.GetTypeModifier().ToString2();
                        if (field.HasReadOnlyModifier()) {
                            modifiers = ModifierUtil.ConcatWithSpace(modifiers, "readonly");
                        }

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
            GUILayout.Label(field.FieldType + " ");

            GUIMemberName.MemberName(field, nameHighlightFrom, nameHighlightLength);

            GUI.contentColor = Color.white;
            GUILayout.Label(" = ");
            GUI.contentColor = MainWindow.Instance.Config.ValueColor;

            if (value == null || !TypeUtil.IsSpecialType(field.FieldType))
            {
                GUILayout.Label(value?.ToString() ?? "null");
            }
            else
            {
                try
                {
                    var newValue = GUIControls.EditorValueField(refChain.UniqueId, field.FieldType, value);
                    if (!newValue.Equals(value))
                    {
                        UnityEngine.Debug.Log($"setting '{obj}'.'{field}' to '{value}' ...");
                        refChain.SetValue(obj, field, newValue);
                    }
                }
                catch (Exception)
                {
                    GUILayout.Label(value.ToString());
                }
            }

            GUI.enabled = true;
            GUI.contentColor = Color.white;

            GUILayout.FlexibleSpace();

            GUIButtons.SetupCommonButtons(refChain, value, valueIndex: 0, smartType);
            object paste = null;
            var doPaste = field.CanWrie();
            if (doPaste)
            {
                doPaste = GUIButtons.SetupPasteButon(field.FieldType, value, out paste);
            }

            if (value != null)
            {
                GUIButtons.SetupJumpButton(value, refChain);
            }

            GUILayout.EndHorizontal();
            if (value != null && !TypeUtil.IsSpecialType(field.FieldType) && state.ExpandedObjects.Contains(refChain.UniqueId))
            {
                GUIReflect.OnSceneTreeReflect(state, refChain, value, false, smartType);
            }

            if (doPaste)
            {
                try
                {
                    refChain.SetValue(obj, field, paste);
                }
                catch (Exception e)
                {
                    Logger.Warning(e.Message);
                }
            }
        }
    }
}