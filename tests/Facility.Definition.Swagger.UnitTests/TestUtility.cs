using Facility.Definition.Fsd;

namespace Facility.Definition.Swagger.UnitTests;

internal static class TestUtility
{
	public static ServiceInfo ParseTestApi(string text)
	{
		return new FsdParser(new() { SupportsEvents = true }).ParseDefinition(new ServiceDefinitionText("TestApi.fsd", text));
	}
}
