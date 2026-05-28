using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class WasmExceptionCallBoundaryFixture : ModuleFixture
    {
        protected override string ModuleFileName => "WasmExceptionCrossCallBoundary.wat";

        public override Config GetEngineConfig()
        {
            return base
                .GetEngineConfig()
                .WithExceptions(true);
        }
    }

    public class WasmExceptionCallBoundaryTests
        : IClassFixture<WasmExceptionCallBoundaryFixture>, IDisposable
    {
        private WasmExceptionCallBoundaryFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public WasmExceptionCallBoundaryTests(WasmExceptionCallBoundaryFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
            
            Linker.DefineFunction("env", "cs_test", (Caller caller) => {
                caller.GetFunction("throw")!.Invoke();
                return 222;
            });
            
            Linker.DefineFunction("env", "cs_test_catch", (Caller caller) => {
                try {
                	caller.GetFunction("throw")!.Invoke();
                } catch (WasmException e) {
                	Console.WriteLine("Caught the following exception:");
                	Console.WriteLine(e);
                    return 333;
                }
                return 222;
            });
            
            Linker.DefineFunction("env", "cs_test_rethrow", (Caller caller) => {
                try {
                    caller.GetFunction("throw")!.Invoke();
                } catch (WasmException e) {
                    Console.WriteLine("Caught the following exception:");
                    Console.WriteLine(e);
                    throw;
                }
                return 222;
            });
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }

        // Just a sanity check that the .wat is doing what we expect when the C# call boundary isn't in the way.
        [Fact]
        public void ItDoesAsExpectedWithoutCallBoundary()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction("run_wat");
            func.Should().NotBeNull();

            var result = func!.Invoke();
            result.Should().Be(111);
        }

        [Fact]
        public void ItDoesAsExpectedAcrossCallBoundary()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction("run_cs");
            func.Should().NotBeNull();

            var result = func!.Invoke();
            result.Should().Be(111);
        }

        [Fact]
        public void ItCanCatchInCSharp()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction("run_cs_catch");
            func.Should().NotBeNull();

            var result = func!.Invoke();
            result.Should().Be(333);
        }

        [Fact]
        public void ItCanCatchAndReThrowInCSharp()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction("run_cs_rethrow");
            func.Should().NotBeNull();

            var result = func!.Invoke();
            result.Should().Be(111);
        }
    }
}
