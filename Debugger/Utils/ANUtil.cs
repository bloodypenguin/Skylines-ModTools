namespace ModTools.Utils {
    using ModTools.Explorer;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using UnityEngine;

    internal static class ANUtil {
        #region netman
        internal static MonoBehaviour ANNetMan {
            get {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => assembly.GetName().Name == "AdaptiveRoads");
                var type = assembly?.GetType("AdaptiveRoads.Manager.NetworkExtensionManager", throwOnError: false);
                if (type != null) {
                    return GameObject.FindObjectOfType(type) as MonoBehaviour;
                } else {
                    return null;
                }
            }
        }

        static object GetFieldValue(this object instance, string name) {
            return instance.GetType().GetField(name).GetValue(instance);
        }

        static object GetBufferElement(this object instance, string bufferName, ushort index) =>
            (instance.GetFieldValue(bufferName) as Array).GetValue(index);

        static object GetBufferElement(this object instance, string bufferName, uint index) =>
            (instance.GetFieldValue(bufferName) as Array).GetValue(index);

        static object GetBufferElement(this object instance, string bufferName, int index) =>
            (instance.GetFieldValue(bufferName) as Array).GetValue(index);

        internal static object GetSegmentExt(ushort segmentId) =>
            ANNetMan?.GetBufferElement("SegmentBuffer", segmentId);

        internal static object GetNodeExt(ushort nodeId) =>
            ANNetMan?.GetBufferElement("NodeBuffer", nodeId);

        internal static object GetLaneExt(uint laneId) =>
            ANNetMan?.GetBufferElement("LaneBuffer", laneId);

        internal static ReferenceChain ForNodeExt(ushort nodeId) {
            var anMan = ANNetMan;
            var bufferFieldInfo = anMan.GetType().GetField("NodeBuffer");
            return new ReferenceChain()
                .Add(anMan.gameObject)
                .Add(anMan)
                .Add(bufferFieldInfo)
                .Add(nodeId);
        }

        internal static ReferenceChain ForSegmentExt(ushort segmentId) {
            var anMan = ANNetMan;
            var bufferFieldInfo = anMan.GetType().GetField("SegmentBuffer");
            return new ReferenceChain()
                .Add(anMan.gameObject)
                .Add(anMan)
                .Add(bufferFieldInfo)
                .Add(segmentId);
        }

        internal static ReferenceChain ForLaneExt(uint laneId) {
            var anMan = ANNetMan;
            var bufferFieldInfo = anMan.GetType().GetField("LaneBuffer");
            return new ReferenceChain()
                .Add(anMan.gameObject)
                .Add(anMan)
                .Add(bufferFieldInfo)
                .Add(laneId);
        }

        #endregion

        #region netinfo
        internal static object GetANNetInfo(this NetInfo info) {
            return info.gameObject.GetComponent("AdaptiveRoads.Manager.NetMetadataContainer")?.GetFieldValue("Metadata");
        }

        internal static Array GetTracks(this NetInfo info) {
            return info.GetANNetInfo().GetFieldValue("Tracks") as Array;
        }

        internal static ReferenceChain ForNetInfoExt(NetInfo info) {
            var component = info?.gameObject?.GetComponent("AdaptiveRoads.Manager.NetMetadataContainer");
            if (component == null) return null;
            var field = component.GetType().GetField("Metadata");
            return new ReferenceChain()
                .Add(component.gameObject)
                .Add(component)
                .Add(field);
        }

        #endregion
    }
}
