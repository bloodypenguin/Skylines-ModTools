namespace ModTools.Utils {
    using System;
    using System.Reflection;
    using UnityEngine;

    public enum AccessModifier {
        None,
        Private,
        ProtectedInternal,
        Internal,
        Protected,
        Public
    }

    public enum TypeModifier {
        None,
        Const,
        Static,
        Abstract,
        Virtual,
        Instance,
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "<no need>")]
    public static class ModifierUtil {
        #region AccessModifier
        public static AccessModifier GetAccessmodifier(this FieldInfo fieldInfo) {
            if (fieldInfo.IsPrivate)
                return AccessModifier.Private;
            else if (fieldInfo.IsFamilyOrAssembly)
                return AccessModifier.ProtectedInternal;
            else if (fieldInfo.IsFamily)
                return AccessModifier.Protected;
            else if (fieldInfo.IsAssembly)
                return AccessModifier.Internal;
            else if (fieldInfo.IsPublic)
                return AccessModifier.Public;

            //Logger.Exception(new ArgumentException("Did not find access modifier", nameof(fieldInfo)));
            return default;
        }

        public static AccessModifier GetAccessmodifier(this MethodInfo methodInfo) {
            if (methodInfo.IsPrivate)
                return AccessModifier.Private;
            else if (methodInfo.IsFamilyOrAssembly)
                return AccessModifier.ProtectedInternal;
            else if (methodInfo.IsFamily)
                return AccessModifier.Protected;
            else if (methodInfo.IsAssembly)
                return AccessModifier.Internal;
            else if (methodInfo.IsPublic)
                return AccessModifier.Public;

            //Logger.Exception(new ArgumentException("Did not find access modifier", nameof(methodInfo)));
            return default;
        }

        public static AccessModifier GetAccessmodifier(this PropertyInfo propertyInfo) {
            var get = propertyInfo.GetGetMethod(true)?.GetAccessmodifier() ?? default;
            var set = propertyInfo.GetSetMethod(true)?.GetAccessmodifier() ?? default;
            return Max(get, set);
        }

        public static AccessModifier Max(AccessModifier a, AccessModifier b) {
            return (AccessModifier)Math.Max((int)a, (int)b);
        }

        public static string ToString2(this AccessModifier modifier) => modifier switch {
            AccessModifier.ProtectedInternal => "protected internal",
            AccessModifier.None => string.Empty,
            _ => modifier.ToString().ToLower(),
        };
        #endregion

        #region TypeModifier
        public static TypeModifier GetTypeModifier(this FieldInfo fieldInfo) {
            if (fieldInfo.IsLiteral)
                return TypeModifier.Const;
            else if (fieldInfo.IsStatic)
                return TypeModifier.Static;
            else
                return TypeModifier.Instance;
        }

        public static TypeModifier GetTypeModifier(this MethodInfo methodInfo) {
            if (methodInfo.IsStatic)
                return TypeModifier.Static;
            else if (methodInfo.IsAbstract)
                return TypeModifier.Abstract;
            else if (methodInfo.IsVirtual)
                return TypeModifier.Virtual;
            else
                return TypeModifier.Instance;
        }

        public static TypeModifier GetTypeModifier(this PropertyInfo propertyInfo) {
            var get = propertyInfo.GetGetMethod(true)?.GetTypeModifier() ?? default;
            var set = propertyInfo.GetSetMethod(true)?.GetTypeModifier() ?? default;
            var ret = Max(get, set);
            return ret;
        }

        public static TypeModifier Max(TypeModifier a, TypeModifier b) {
            return (TypeModifier)Math.Max((int)a, (int)b);
        }

        public static string ToString2(this TypeModifier modifier) => modifier switch {
            TypeModifier.Instance => string.Empty,
            TypeModifier.None => string.Empty,
            _ => modifier.ToString().ToLower(),
        };
        #endregion

        public static bool HasReadOnlyModifier(this FieldInfo fieldInfo) => fieldInfo.IsInitOnly;

        public static bool CanWrie(this FieldInfo fieldInfo) => !fieldInfo.IsInitOnly && !fieldInfo.IsLiteral;

        public static string ConcatWithSpace(string a, string b) {
            if (string.IsNullOrEmpty(a))
                return b;
            else if (string.IsNullOrEmpty(b))
                return a;
            else return a + " " + b;
        }
    }
}
