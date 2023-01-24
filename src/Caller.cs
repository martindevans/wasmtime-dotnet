using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Represents caller information for a function.
    /// </summary>
    public readonly struct Caller
        : IDisposable
    {
        internal Caller(IntPtr handle)
        {
            context = CallerContext.Get(handle);
            epoch = context.Epoch;

            store = null!;
            store = Context.Store;
        }

        /// <summary>
        /// Gets an exported memory of the caller by the given name.
        /// </summary>
        /// <param name="name">The name of the exported memory.</param>
        /// <returns>Returns the exported memory if found or null if a memory of the requested name is not exported.</returns>
        public Memory? GetMemory(string name)
        {
            unsafe
            {
                var bytes = Encoding.UTF8.GetBytes(name);

                fixed (byte* ptr = bytes)
                {
                    if (!Native.wasmtime_caller_export_get(NativeHandle, ptr, (UIntPtr)bytes.Length, out var item))
                    {
                        return null;
                    }

                    if (item.kind != ExternKind.Memory)
                    {
                        item.Dispose();
                        return null;
                    }

                    return new Memory(Store, item.of.memory);
                }
            }
        }

        /// <summary>
        /// Gets an exported function of the caller by the given name.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <returns>Returns the exported function if found or null if a function of the requested name is not exported.</returns>
        public Function? GetFunction(string name)
        {
            unsafe
            {
                var bytes = Encoding.UTF8.GetBytes(name);

                fixed (byte* ptr = bytes)
                {
                    if (!Native.wasmtime_caller_export_get(NativeHandle, ptr, (UIntPtr)bytes.Length, out var item))
                    {
                        return null;
                    }

                    if (item.kind != ExternKind.Func)
                    {
                        item.Dispose();
                        return null;
                    }

                    return new Function(Store, item.of.func);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            context.Recycle(epoch);
        }

        /// <summary>
        /// Gets the <see cref="Store"/> associated with this caller.
        /// </summary>
        public Store Store
        {
            get
            {
                context.CheckEpoch(epoch);
                return store;
            }
        }

        internal StoreContext Context => new StoreContext(Native.wasmtime_caller_context(NativeHandle));

        private IntPtr NativeHandle => context.GetHandle(epoch);

        /// <summary>
        /// Adds fuel to this store for WebAssembly code to consume while executing.
        /// </summary>
        /// <param name="fuel">The fuel to add to the store.</param>
        public void AddFuel(ulong fuel) => Context.AddFuel(fuel);

        /// <summary>
        /// Synthetically consumes fuel from this store.
        ///
        /// For this method to work fuel consumption must be enabled via <see cref="Config.WithFuelConsumption(bool)"/>.
        ///
        /// WebAssembly execution will automatically consume fuel but if so desired the embedder can also consume fuel manually
        /// to account for relative costs of host functions, for example.
        ///
        /// This method will attempt to consume <paramref name="fuel"/> units of fuel from within this store. If the remaining
        /// amount of fuel allows this then the amount of remaining fuel is returned. Otherwise, a <see cref="WasmtimeException"/>
        /// is thrown and no fuel is consumed.
        /// </summary>
        /// <param name="fuel">The fuel to consume from the store.</param>
        /// <returns>Returns the remaining amount of fuel.</returns>
        /// <exception cref="WasmtimeException">Thrown if more fuel is consumed than the store currently has.</exception>
        public ulong ConsumeFuel(ulong fuel) => Context.ConsumeFuel(fuel);

        /// <summary>
        /// Gets the fuel consumed by the executing WebAssembly code.
        /// </summary>
        /// <returns>Returns the fuel consumed by the executing WebAssembly code or 0 if fuel consumption was not enabled.</returns>
        public ulong GetConsumedFuel() => Context.GetConsumedFuel();

        /// <summary>
        /// Gets the user-defined data from the Store. 
        /// </summary>
        /// <returns>An object represeting the user defined data from this Store</returns>
        public object? GetData() => Store.GetData();

        /// <summary>
        /// Replaces the user-defined data in the Store.
        /// </summary>
        public void SetData(object? data) => Store.SetData(data);

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static unsafe extern bool wasmtime_caller_export_get(IntPtr caller, byte* name, UIntPtr len, out Extern item);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_caller_context(IntPtr caller);
        }

        private readonly CallerContext context;
        private readonly uint epoch;
        private readonly Store store;
    }

    /// <summary>
    /// Internal representation of caller information. Public wrappers compare the "epoch" to check if they have been disposed.
    /// </summary>
    internal class CallerContext
    {
        public static CallerContext Get(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            if (!_pool.TryTake(out var ctx))
                ctx = new CallerContext();

            ctx.Epoch++;
            ctx.handle = handle;

            return ctx;
        }

        public void Recycle(uint epoch)
        {
            if (Epoch != epoch)
                return;

            Epoch++;
            handle = IntPtr.Zero;

            // Do not recycle if epoch is getting near max limit
            if (Epoch > uint.MaxValue - 10)
            {
                return;
            }

            if (_pool.Count < PoolMaxSize)
            {
                _pool.Add(this);
            }
        }

        internal IntPtr GetHandle(uint epoch)
        {
            CheckEpoch(epoch);
            return handle;
        }

        internal void CheckEpoch(uint epoch)
        {
            if (epoch != Epoch)
            {
                throw new ObjectDisposedException(typeof(Caller).FullName);
            }
        }

        internal uint Epoch { get; private set; }
        private IntPtr handle;

        private const int PoolMaxSize = 64;
        private static readonly ConcurrentBag<CallerContext> _pool = new();
    }
}