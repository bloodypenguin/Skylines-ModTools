using ModTools.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModTools.Explorer {
    internal static class GUICollectionNavigation {
        internal static TVal GetOrAdd<TKey, TVal>(this Dictionary<TKey, TVal> dict, TKey key, TVal def = default) {
            if (dict.TryGetValue(key, out TVal ret)) {
                return ret;
            } else {
                return dict[key] = def;
            }
        }

        private const uint MAX_PAGE_SIZE = 32;

        public static void SetUpCollectionNavigation(
            string collectionLabel,
            SceneExplorerState state,
            ReferenceChain refChain,
            ReferenceChain oldRefChain,
            uint collectionSize,
            out uint startIndex,
            out uint endIndex) {
            GUILayout.BeginHorizontal();
            SceneExplorerCommon.InsertIndent(refChain.Indentation);

            GUILayout.Label($"{collectionLabel} size: {collectionSize}");

            startIndex = state.SelectedArrayStartIndices.GetOrAdd(
                refChain.UniqueId,
                0u);
            endIndex = state.SelectedArrayEndIndices.GetOrAdd(
                refChain.UniqueId,
                Math.Min(MAX_PAGE_SIZE - 1, collectionSize - 1));

            if (endIndex >= collectionSize) {
                // if user press back/up, scene explorer will try to auto open collection before it knows list count.
                // therefore it can set end index too large. here we fix that:
                endIndex = state.SelectedArrayEndIndices[refChain.UniqueId] = collectionSize - 1;
            }

            uint startIndex2 = GUIControls.NumericValueField($"{oldRefChain}.arrayStart", "Start index", startIndex);
            uint endIndex2 = GUIControls.NumericValueField($"{oldRefChain}.arrayEnd", "End index", endIndex);
            if (startIndex2 > endIndex2) {
                endIndex2 = startIndex2;
            } else if (endIndex2 - startIndex2 > MAX_PAGE_SIZE) {
                if (endIndex != endIndex2) {
                    startIndex2 = endIndex2 - MAX_PAGE_SIZE + 1;
                } else {
                    endIndex2 = startIndex2 + MAX_PAGE_SIZE - 1;
                }
            }

            startIndex2 = Math.Min(startIndex2, collectionSize - 1);
            endIndex2 = Math.Min(endIndex2, collectionSize - 1);

            uint pageSize = endIndex - startIndex2 + 1;
            GUILayout.Label($"({MAX_PAGE_SIZE} items max)");
            if (GUILayout.Button("◄", GUILayout.ExpandWidth(false))) {
                if (startIndex2 - pageSize >= 0) {
                    startIndex2 -= pageSize;
                    endIndex2 -= pageSize;
                } else {
                    startIndex2 = 0;
                    endIndex2 = pageSize;
                }
            }

            if (GUILayout.Button("►", GUILayout.ExpandWidth(false))) {
                if (endIndex2 + pageSize < collectionSize) {
                    startIndex2 += pageSize;
                    endIndex2 += pageSize;
                } else {
                    startIndex2 = collectionSize - pageSize;
                    endIndex2 = collectionSize - 1;
                }
            }

            if (startIndex2 != startIndex || endIndex2 != endIndex) {
                Debug.Log($"[ModTools] Changing start/end index of '{refChain}' from [{startIndex}:{endIndex}] to [{startIndex2}:{endIndex2}]");
            }

            // do not output the newly change values until the next frame to avoid GUI element count mismatch.
            state.SelectedArrayStartIndices[refChain.UniqueId] = startIndex2;
            state.SelectedArrayEndIndices[refChain.UniqueId] = endIndex2;


            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}