using System;
using System.Reflection;

namespace BlurredRoom.Tests
{
    /// <summary>Sets private [SerializeField] fields and statics in tests (works in EditMode and PlayMode).</summary>
    public static class TestReflection
    {
        const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static void Set(object target, string field, object value)
        {
            FindField(target.GetType(), field, InstanceFlags).SetValue(target, value);
        }

        public static T Get<T>(object target, string field)
        {
            return (T)FindField(target.GetType(), field, InstanceFlags).GetValue(target);
        }

        /// <summary>Sets a static field or property (private setters allowed).</summary>
        public static void SetStatic(Type type, string member, object value)
        {
            PropertyInfo p = type.GetProperty(member, StaticFlags);
            if (p != null)
            {
                MethodInfo setter = p.GetSetMethod(true);
                if (setter == null) throw new ArgumentException($"{type.Name}.{member} has no setter");
                setter.Invoke(null, new[] { value });
                return;
            }
            FindField(type, member, StaticFlags).SetValue(null, value);
        }

        public static object InvokeStatic(Type type, string method, object[] args = null)
        {
            MethodInfo m = type.GetMethod(method, StaticFlags);
            if (m == null) throw new ArgumentException($"{type.Name}.{method} not found");
            return m.Invoke(null, args);
        }

        static FieldInfo FindField(Type type, string name, BindingFlags flags)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (f != null) return f;
            }
            throw new ArgumentException($"Field '{name}' not found on {type.Name}");
        }
    }
}
