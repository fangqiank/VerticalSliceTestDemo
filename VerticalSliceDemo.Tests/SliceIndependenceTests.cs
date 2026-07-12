using FluentAssertions;
using NetArchTest.Rules;
using System.Reflection;

namespace VerticalSliceDemo.Tests
{
    public class SliceIndependenceTests
    {
        [Fact]
        public void Orders_slice_should_not_reference_shipments()
        {
            var assembly = Assembly.GetAssembly(typeof(Program));

            var result = Types
                .InAssembly(assembly)
                .That()
                .ResideInNamespace("VerticalSliceDemo.Features.Orders")
                .ShouldNot()
                .HaveDependencyOn("VerticalSliceDemo.Features.Shipmemts")
                .GetResult();

            result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Shipments_slice_should_not_reference_orders()
        {
            var assembly = Assembly.GetAssembly(typeof(Program));

            var result = Types
                .InAssembly(assembly)
                .That()
                .ResideInNamespace("VerticalSliceDemo.Features.Shipmemts")
                .ShouldNot()
                .HaveDependencyOn("VerticalSliceDemo.Features.Orders")
                .GetResult();

            result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Domain_should_not_depend_on_features()
        {
            var assembly = Assembly.GetAssembly(typeof(Program))!;

            var result = Types
                .InAssembly(assembly)
                .That()
                .ResideInNamespace("VerticalSliceDemo.Domain")
                .ShouldNot()
                .HaveDependencyOn("VerticalSliceDemo.Features")
                .GetResult();

            result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Infrastructure_should_not_depend_on_features()
        {
            var assembly = Assembly.GetAssembly(typeof(Program))!;

            var result = Types
                .InAssembly(assembly)
                .That()
                .ResideInNamespace("VerticalSliceDemo.Infrastructure")
                .ShouldNot()
                .HaveDependencyOn("VerticalSliceDemo.Features")
                .GetResult();

            result.IsSuccessful.Should().BeTrue();
        }
    }
}
