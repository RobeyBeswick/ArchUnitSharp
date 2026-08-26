using System.Reflection;
using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class ICheckableTests
{
    [Fact]
    public void The_options_parameter_is_nullable_and_defaults_to_null()
    {
        var parameter = typeof(ICheckable).GetMethod(nameof(ICheckable.Check))!
            .GetParameters()
            .Single();

        Assert.Equal(typeof(CheckOptions), parameter.ParameterType);
        Assert.Equal(NullabilityState.Nullable, new NullabilityInfoContext().Create(parameter).ReadState);
        Assert.True(parameter.HasDefaultValue);
        Assert.Null(parameter.DefaultValue);
    }

    [Fact]
    public void Check_returns_a_read_only_list_of_violations()
    {
        var method = typeof(ICheckable).GetMethod(nameof(ICheckable.Check))!;

        Assert.Equal(typeof(IReadOnlyList<Violation>), method.ReturnType);
    }

    [Fact]
    public void Has_exactly_one_unexported_member_so_outsiders_cannot_implement_it()
    {
        var unexported = typeof(ICheckable)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(m => m.IsAssembly)
            .ToArray();

        Assert.Single(unexported);
        Assert.True(unexported[0].IsAbstract);
    }
}
