using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class WasmExceptionFixture : ModuleFixture
    {
        protected override string ModuleFileName => "WasmException.wat";

        public override Config GetEngineConfig()
        {
            return base
                .GetEngineConfig()
                .WithExceptions(true);
        }
    }

    public class WasmExceptionTests
        : IClassFixture<WasmExceptionFixture>, IDisposable
    {
        private WasmExceptionFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public WasmExceptionTests(WasmExceptionFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }

        [Fact]
        public void ItCanThrowInternally()
        {
            var logs = new List<int>();
            Linker.DefineFunction("env", "log", (int parameter) => logs.Add(parameter));
            
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction("try_and_catch");
            func.Should().NotBeNull();

            // Call with a positive number, nothing is thrown or logged
            var result = func!.Invoke(8);
            result.Should().BeNull();
            Assert.Empty(logs);

            // Call with a negative number, 42 is thrown and logged
            result = func!.Invoke(-8);
            result.Should().BeNull();
            Assert.Equal(42, logs.Single());
        }

        [Fact]
        public void ItCanCatch()
        {
            var logs = new List<int>();
            Linker.DefineFunction("env", "log", (int parameter) => logs.Add(parameter));

            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction("$might_throw");
            func.Should().NotBeNull();

            // Call with a positive number, nothing is thrown or logged
            var result = func!.Invoke(8);
            result.Should().BeNull();
            Assert.Empty(logs);

            // Call with a negative number, 42 is thrown
            try
            {
                func!.Invoke(-8);
            }
            catch (WasmException ex)
            {
                //todo: value 42 returned in exception?
                
                return;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Wrong exception type thrown: {ex.GetType().Name}");
            }
            
            Assert.Fail("No exception thrown");
        }

        [Fact]
        public void ItCanCatch_Wrapped()
        {
            var logs = new List<int>();
            Linker.DefineFunction("env", "log", (int parameter) => logs.Add(parameter));

            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction("$might_throw")!.WrapAction<int>()!;
            func.Should().NotBeNull();

            // Call with a positive number, nothing is thrown or logged
            func(8);
            Assert.Empty(logs);

            // Call with a negative number, 42 is thrown
            try
            {
                func(-8);
            }
            catch (WasmException ex)
            {
                //todo: value 42 returned in exception?

                return;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Wrong exception type thrown: {ex.GetType().Name}");
            }

            Assert.Fail("No exception thrown");
        }
    }
}
