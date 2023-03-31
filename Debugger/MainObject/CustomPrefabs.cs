using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModTools.MainObject
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0052", Justification = "Intended for self-reflection in-game")]
    internal sealed class CustomPrefabs : MonoBehaviour, IDestroyableObject, IAwakingObject
    {
        private static GameObject thisGameObject;

        private VehicleInfo[] vehicles;

        private BuildingInfo[] buildings;

        private PropInfo[] props;

        private TreeInfo[] trees;

        private NetInfo[] nets;

        private EventInfo[] events;

        private TransportInfo[] transports;

        private CitizenInfo[] citizens;

        public static void Bootstrap()
        {
            if (thisGameObject == null)
            {
                thisGameObject = new GameObject("Custom Prefabs");
                thisGameObject.transform.parent = GameObject.Find(ModToolsMod.ModToolsName).transform;
                thisGameObject.AddComponent<CustomPrefabs>();
            }
        }

        public static void Revert()
        {
            if (thisGameObject != null)
            {
                Destroy(thisGameObject);
                thisGameObject = null;
            }
        }

        public void Awake()
        {
            vehicles = GetCustomPrefabs<VehicleInfo>();
            buildings = GetCustomPrefabs<BuildingInfo>();
            props = GetCustomPrefabs<PropInfo>();
            trees = GetCustomPrefabs<TreeInfo>();
            nets = GetCustomPrefabs<NetInfo>();
            events = GetCustomPrefabs<EventInfo>();
            transports = GetCustomPrefabs<TransportInfo>();
            citizens = GetCustomPrefabs<CitizenInfo>();
        }

        public void OnDestroy()
        {
            vehicles = null;
            buildings = null;
            props = null;
            trees = null;
            nets = null;
            events = null;
            transports = null;
            citizens = null;
        }

        private static T[] GetCustomPrefabs<T>()
            where T : PrefabInfo
        {
            var count = PrefabCollection<T>.LoadedCount();
            var result = new List<T>(count);
            for (uint i = 0; i < count; i++)
            {
                var prefab = PrefabCollection<T>.GetPrefab(i);
                if (prefab == null || !prefab.m_isCustomContent && prefab.name?.Contains('.') == false)
                {
                    continue;
                }

                result.Add(prefab);
            }

            result.Sort((x, y) => string.CompareOrdinal(x?.name, y?.name));
            return result.ToArray();
        }
    }
}