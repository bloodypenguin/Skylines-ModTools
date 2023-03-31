using System;
using System.Collections;
using ModTools.UI;
using ModTools.Utils;
using UnityEngine;

namespace ModTools.Explorer
{
    internal static class GUIList
    {
        public static void OnSceneTreeReflectIList(SceneExplorerState state, ReferenceChain refChain, IList list, TypeUtil.SmartType elementSmartType = TypeUtil.SmartType.Undefined)
        {
            if (!SceneExplorerCommon.SceneTreeCheckDepth(refChain))
            {
                return;
            }

            var oldRefChain = refChain;
            var collectionSize = list.Count;
            if (collectionSize == 0)
            {
                GUILayout.BeginHorizontal();
                GUI.contentColor = Color.yellow;
                GUILayout.Label("List is empty!");
                GUI.contentColor = Color.white;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }

            var listItemType = list.GetType().GetElementType();

            GUICollectionNavigation.SetUpCollectionNavigation("List", state, refChain, oldRefChain, (uint)collectionSize, out var arrayStart, out var arrayEnd);
            for (var i = arrayStart; i <= arrayEnd; i++)
            {
                refChain = oldRefChain.Add(i);

                GUILayout.BeginHorizontal(GUIWindow.HighlightStyle);
                SceneExplorerCommon.InsertIndent(refChain.Indentation);

                GUI.contentColor = Color.white;

                object value = null;
                try {
                    value = list[(int)i];
                } catch (IndexOutOfRangeException ex) {
                    Logger.Error($"list.Count={list.Count} index={i} collectionSize={collectionSize} arrayEnd={arrayEnd}\n" + ex);
                }

                var type = value?.GetType() ?? listItemType;
                if (type != null)
                {
                    if (value != null)
                    {
                        GUIExpander.ExpanderControls(state, refChain, type);
                    }

                    GUI.contentColor = MainWindow.Instance.Config.TypeColor;

                    GUILayout.Label($"{type} ");
                }

                GUI.contentColor = MainWindow.Instance.Config.NameColor;

                GUILayout.Label($"{oldRefChain.LastItemName}.[{i}]");

                GUI.contentColor = Color.white;

                GUILayout.Label(" = ");

                GUI.contentColor = MainWindow.Instance.Config.ValueColor;

                GUILayout.Label(value?.ToString() ?? "null");

                GUI.contentColor = Color.white;

                GUILayout.FlexibleSpace();

                if (value != null)
                {
                    GUIButtons.SetupCommonButtons(refChain, value, i, elementSmartType);
                    GUIButtons.SetupJumpButton(value, refChain);
                }

                GUILayout.EndHorizontal();

                if (value != null && !TypeUtil.IsSpecialType(type) && state.ExpandedObjects.Contains(refChain.UniqueId))
                {
                    GUIReflect.OnSceneTreeReflect(state, refChain, value, false);
                }
            }
        }
    }
}