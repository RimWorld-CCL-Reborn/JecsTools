using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace JecsTools
{
    public static class ReflectionExtensions
    {
        // The MoveNext method may be either public or non-public, depending on the compiler.
        public static Type FindIteratorType(this Type type, string parentMethodName, Func<Type, bool> predicate = null)
        {
            // Iterator code is in a compiler-generated non-public nested class that implements IEnumerable.
            // In RW 1.1+ assemblies and modern VS-compiled assemblies, the nested class's name starts with "<{parentMethodName}>".
            foreach (var innerType in type.GetNestedTypes(BindingFlags.NonPublic))
            {
                if (innerType.IsDefined(typeof(CompilerGeneratedAttribute)) &&
                    typeof(IEnumerator).IsAssignableFrom(innerType) &&
                    innerType.Name.StartsWith("<" + parentMethodName + ">") &&
                    (predicate is null || predicate(innerType)))
                {
                    return innerType;
                }
            }
            throw new ArgumentException($"Could not find any iterator type for parent type {type} and method {parentMethodName}" +
                " that satisfied given predicate");
        }

        private const BindingFlags moveNextMethodBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static MethodInfo FindIteratorMethod(this Type type, string parentMethodName, Func<Type, bool> predicate = null)
        {
            return type.FindIteratorType(parentMethodName, predicate).GetMethod(nameof(IEnumerator.MoveNext), moveNextMethodBindingFlags);
        }
    }
}
