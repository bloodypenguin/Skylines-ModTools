using System;
using System.Collections;
using ModTools.Utils;
using UnityEngine;

namespace ModTools.Explorer
{
    internal static class GUIExpander
    {
        public static void ExpanderControls(SceneExplorerState state, ReferenceChain refChain, Type type, object o = null)
        {
            GUI.contentColor = Color.white;
            if (TypeUtil.IsSpecialType(type) || (o != null && o is IEnumerable))
            {
                return;
            }

            if (state.ExpandedObjects.Contains(refChain.UniqueId))
            {
                if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                {
                    state.ExpandedObjects.Remove(refChain.UniqueId);
                }
            }
            else if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                state.ExpandedObjects.Add(refChain.UniqueId);
            }
        }
    }
}