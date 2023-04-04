using System;

namespace Wasmtime
{
    /// <summary>
    /// Marks a static method which is used as a callback from native code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class NativeCallbackAttribute
        : Attribute
    {
        public NativeCallbackAttribute(Type delegateType)
        {
        }
    }
}
