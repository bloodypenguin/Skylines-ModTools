using System;
using System.Collections;
using ModTools.UI;
using ModTools.Utils;
using UnityEngine;

namespace ModTools.Explorer
{
    internal static class GUICollection
    {
        public static void OnSceneTreeReflectICollection(SceneExplorerState state, ReferenceChain refChain, ICollection collection, TypeUtil.SmartType elementSmartType = TypeUtil.SmartType.Undefined)
        {
            if (!SceneExplorerCommon.SceneTreeCheckDepth(refChain))
            {
                return;
            }

            var oldRefChain = refChain;
            var collectionSize = collection.Count;
            if (collectionSize == 0)
            {
                GUILayout.BeginHorizontal();
                GUI.contentColor = Color.yellow;
                GUILayout.Label("Collection is empty!");
                GUI.contentColor = Color.white;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }

            var collectionItemType = collection.GetType().GetElementType();
            var flagsField = collectionItemType?.GetField("m_flags");
            var flagIsEnum = flagsField?.FieldType.IsEnum == true && Type.GetTypeCode(flagsField.FieldType) == TypeCode.Int32;

            GUICollectionNavigation.SetUpCollectionNavigation("Collection", state, refChain, oldRefChain, (uint)collectionSize, out var arrayStart, out var arrayEnd);
            uint count = 0;
            foreach (var value in collection)
            {
                if (count < arrayStart)
                {
                    count++;
                    continue;
                }

                refChain = oldRefChain.Add(count);

                GUILayout.BeginHorizontal(GUIWindow.HighlightStyle);
                SceneExplorerCommon.InsertIndent(refChain.Indentation);

                var type = value?.GetType() ?? collectionItemType;
                if (type != null)
                {
                    if (value != null)
                    {
                        GUIExpander.ExpanderControls(state, refChain, type);
                    }

                    GUI.contentColor = MainWindow.Instance.Config.TypeColor;

                    GUILayout.Label(type.ToString() + " ");
                }

                GUI.contentColor = MainWindow.Instance.Config.NameColor;

                GUILayout.Label($"{oldRefChain.LastItemName}.[{count}]");

                GUI.contentColor = Color.white;

                GUILayout.Label(" = ");

                GUI.contentColor = MainWindow.Instance.Config.ValueColor;
                GUILayout.Label(value?.ToString() ?? "null");

                GUI.contentColor = Color.white;

                GUILayout.FlexibleSpace();

                if (value != null)
                {
                    GUIButtons.SetupCommonButtons(refChain, value, count, elementSmartType);
                    GUIButtons.SetupJumpButton(value, refChain);
                }

                GUILayout.EndHorizontal();

                if (value != null && !TypeUtil.IsSpecialType(type) && state.ExpandedObjects.Contains(refChain.UniqueId))
                {
                    GUIReflect.OnSceneTreeReflect(state, refChain, value, false);
                }

                count++;
                if (count > arrayEnd)
                {
                    break;
                }
            }
        }
    }
}