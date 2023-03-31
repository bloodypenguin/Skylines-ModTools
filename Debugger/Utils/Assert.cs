namespace ModTools.Utils {
    using System;
    using System.Collections;

    public static class Assert {
        public static void InRange(IList list, int index, string m = null) {
            NotNull(list);
            if (index < 0 || index >= list.Count) {
                var ex = new Exception($"Assertion failed: index:{index} list.Count:{list.Count}. " + m);
                Logger.Exception(ex);
                throw ex;
            }
        }

        public static void NotNull(object obj, string m = null) {
            if (obj == null)
                throw new Exception("Assertion failed: " + m);
        }

        public static void True(bool con, string m = null) {
            if (!con)
                throw new Exception("Assertion failed: " + m);
        }
    }
}
