using System;
using System.Collections;
using System.Reflection;
using System.Text;
using ModTools.Utils;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace ModTools.Explorer {
    internal sealed class ReferenceChain : ICloneable {
        private readonly object[] chainObjects = new object[MainWindow.Instance.Config.MaxHierarchyDepth];
        private readonly ReferenceType[] chainTypes = new ReferenceType[MainWindow.Instance.Config.MaxHierarchyDepth];
        private string uniqueId;

        public enum ReferenceType {
            None,
            GameObject,
            Component,
            Field,
            Property,
            Method,
            EnumerableItem,
            SpecialNamedProperty,
        }

        public int IndentationOffset { private get; set; }

        public int Indentation => Length - IndentationOffset - 1;

        public int Length { get; private set; }

        public object LastItem => chainObjects[Length - 1];

        public string LastItemName => ItemToString(Length - 1);

        public ReferenceType FirstItemType => chainTypes[0];

        public string UniqueId {
            get {
                if (!string.IsNullOrEmpty(uniqueId)) {
                    return uniqueId;
                }

                var stringBuilder = new StringBuilder(Length * 32);
                for (var i = 0; i < Length; ++i) {
                    var instanceString = chainObjects[i] is GameObject gameObject ? $"#{gameObject.GetInstanceID()}" : string.Empty;
                    stringBuilder
                        .Append(chainTypes[i])
                        .Append(':')
                        .Append(ItemToString(i))
                        .Append(instanceString)
                        .Append('.');
                }

                uniqueId = stringBuilder.ToString();
                return uniqueId;
            }
        }

        object ICloneable.Clone() => Clone();

        public ReferenceChain Clone() {
            var clone = new ReferenceChain { Length = Length };
            for (var i = 0; i < Length; i++) {
                clone.chainObjects[i] = chainObjects[i];
                clone.chainTypes[i] = chainTypes[i];
            }

            clone.IndentationOffset = IndentationOffset;

            return clone;
        }

        public bool CheckDepth() => Length >= MainWindow.Instance.Config.MaxHierarchyDepth;

        public ReferenceChain Add(GameObject go) {
            var copy = Clone();
            copy.chainObjects[Length] = go;
            copy.chainTypes[Length] = ReferenceType.GameObject;
            copy.Length++;
            return copy;
        }

        public ReferenceChain Add(Component component) {
            var copy = Clone();
            copy.chainObjects[Length] = component;
            copy.chainTypes[Length] = ReferenceType.Component;
            copy.Length++;
            return copy;
        }

        public ReferenceChain Add(FieldInfo fieldInfo) {
            var copy = Clone();
            copy.chainObjects[Length] = fieldInfo;
            copy.chainTypes[Length] = ReferenceType.Field;
            copy.Length++;
            return copy;
        }

        public ReferenceChain Add(PropertyInfo propertyInfo) {
            var copy = Clone();
            copy.chainObjects[Length] = propertyInfo;
            copy.chainTypes[Length] = ReferenceType.Property;
            copy.Length++;
            return copy;
        }

        public ReferenceChain Add(MethodInfo methodInfo) {
            var copy = Clone();
            copy.chainObjects[Length] = methodInfo;
            copy.chainTypes[Length] = ReferenceType.Method;
            copy.Length++;
            return copy;
        }

        public ReferenceChain Add(uint index) {
            var copy = Clone();
            copy.chainObjects[Length] = index;
            copy.chainTypes[Length] = ReferenceType.EnumerableItem;
            copy.Length++;
            return copy;
        }

        public ReferenceChain Add(string namedProperty) {
            var copy = Clone();
            copy.chainObjects[Length] = namedProperty;
            copy.chainTypes[Length] = ReferenceType.SpecialNamedProperty;
            copy.Length++;
            return copy;
        }

        public ReferenceChain SubChain(int num) {
            var copy = Clone();
            copy.Length = Mathf.Min(num, Length);
            return copy;
        }

        public object GetChainItem(int index) => index >= 0 && index < Length ? chainObjects[index] : null;

        public ReferenceType GetChainItemType(int index)
            => index >= 0 && index < Length ? chainTypes[index] : ReferenceType.None;

        public override string ToString() {
            switch (Length) {
                case 0:
                    return string.Empty;

                case 1:
                    return ItemToString(0);
            }

            var result = new StringBuilder();
            result.Append(ItemToString(0));

            for (var i = 1; i < Length; i++) {
                if (chainTypes[i] != ReferenceType.EnumerableItem) {
                    result.Append("->");
                }

                result.Append(ItemToString(i));
            }

            return result.ToString();
        }

        public ReferenceChain Reverse() {
            var copy = new ReferenceChain {
                Length = Length,
                IndentationOffset = IndentationOffset,
            };

            for (var i = 0; i < Length; i++) {
                copy.chainObjects[Length - i - 1] = chainObjects[i];
                copy.chainTypes[Length - i - 1] = chainTypes[i];
            }

            return copy;
        }

        private object Evaluate(int i, object current) {
            switch (chainTypes[i]) {
                case ReferenceType.GameObject:
                case ReferenceType.Component:
                    current = chainObjects[i];
                    break;

                case ReferenceType.Field:
                    current = ((FieldInfo)chainObjects[i]).GetValue(current);
                    break;

                case ReferenceType.Property:
                    current = ((PropertyInfo)chainObjects[i]).GetValue(current, null);
                    break;

                case ReferenceType.Method:
                    break;

                case ReferenceType.EnumerableItem:
                    try {
                        if (current is Array array) {
                            long index = Convert.ToInt64(chainObjects[i]);
                            current = array.GetValue(index);
                        } else if (current is IList list) {
                            int index = Convert.ToInt32(chainObjects[i]);
                            current = list[index];
                        } else if (current is IEnumerable enumerable) {
                            ulong index = Convert.ToUInt64(chainObjects[i]);
                            ulong currentIndex = 0;
                            foreach (var item in enumerable) {
                                if (currentIndex == index) {
                                    current = item;
                                    break;
                                }
                                currentIndex++;
                            }
                        } else {
                            Logger.Error($"'{current?.GetType()}' is not enumerable.");
                        }
                    } catch (Exception ex) {
                        Logger.Exception(ex);
                    }

                    break;

                case ReferenceType.SpecialNamedProperty when current is Material material:
                    current = ShaderUtil.GetProperty(material, (string)chainObjects[i]);
                    break;
                default:
                    Logger.Error($"unhandled case in refchain: " +
                        $"index={i} target=\"{current}\" ReferenceType=\"{chainTypes[i]}\"");
                    break;
            }

            return current;
        }

        public object Evaluate() {
            object current = null;
            for (var i = 0; i < Length; i++) {
                current = Evaluate(i, current);
            }

            return current;
        }

        private static void SetMemberValue(object target, MemberInfo memberInfo, object value) {
            try {
                if (memberInfo is FieldInfo fieldInfo) {
                    fieldInfo.SetValue(target, value);
                } else if (memberInfo is PropertyInfo propertyInfo) {
                    propertyInfo.SetValue(target, value, null);
                } else {
                    throw new Exception("unhandeled type: " + memberInfo);
                }
            } catch (Exception ex) {
                Debug.LogException(ex);
            }
        }

        void SetValueImpl (int i, object target, object value) {
            switch (chainTypes[i]) {
                case ReferenceType.Field:
                case ReferenceType.Property:
                    SetMemberValue(target, (MemberInfo)chainObjects[i], value);
                    break;

                case ReferenceType.EnumerableItem when target is IList list:
                    int index = (int)(uint)chainObjects[i];
                    list[index] = value;

                    break;

                case ReferenceType.EnumerableItem:
                case ReferenceType.Method:
                case ReferenceType.SpecialNamedProperty when target is Material material:
                case ReferenceType.GameObject:
                case ReferenceType.Component:
                default:
                    Logger.Error($"unhandled case in refchain: " +
                        $"index={i} target=\"{target}\" ReferenceType=\"{chainTypes[i]}\"");
                    break;
            }
        }

        private bool SetValueRecursive(int i, object target, object value) {
            if (i == Length - 1) {
                SetMemberValue(target, (MemberInfo)chainObjects[i], value);
                return true;
            }

            object current = Evaluate(i, target);

            if (SetValueRecursive(i + 1, current, value)) {
                if (current.GetType().IsValueType) {
                    SetValueImpl(i, target, current);
                    return true;
                }
            }

            return false; // stop setting values when we found the last struct in the ref chain.
        }

        public void SetValue(object target, MemberInfo memberInfo, object value) {
#if DEBUG
            Debug.Log($"SetValue({target}, {memberInfo}, {value}) called.");
#endif
            try {
                if (memberInfo.DeclaringType.IsValueType) {
                    SetValueRecursive(0, null, value);
                } else {
                    SetMemberValue(target, memberInfo, value);
                }
            } catch (Exception ex) {
                Debug.LogException(ex);
            }
        }

        public bool IsSameChain(ReferenceChain other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            if (Length != other.Length) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            for (var i = 0; i < Length; ++i) {
                if (chainTypes[i] != other.chainTypes[i]) {
                    return false;
                }

                if (chainObjects[i] != other.chainObjects[i]) {
                    return false;
                }
            }

            return true;
        }

        private string ItemToString(int i) {
            return chainTypes[i] switch {
                ReferenceType.GameObject => ((GameObject)chainObjects[i]).name,

                ReferenceType.Component => chainObjects[i].GetType().ToString(),

                ReferenceType.Field => ((FieldInfo)chainObjects[i]).Name,

                ReferenceType.Property => ((PropertyInfo)chainObjects[i]).Name,

                ReferenceType.Method => ((MethodInfo)chainObjects[i]).Name,

                ReferenceType.EnumerableItem => "[" + chainObjects[i] + "]",

                ReferenceType.SpecialNamedProperty => (string)chainObjects[i],

                _ => string.Empty,
            };
        }
    }
}