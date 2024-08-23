using ArgsReading;
using Facility.CodeGen.Console;
using Facility.Definition;
using Facility.Definition.CodeGen;
using Facility.Definition.Fsd;
using Facility.Definition.Swagger;

namespace fsdgenswagger;

public sealed class FsdGenSwaggerApp : CodeGeneratorApp
{
	public static int Main(string[] args) => new FsdGenSwaggerApp().Run(args);

	protected override IReadOnlyList<string> Description =>
	[
		"Converts Swagger (OpenAPI) 2.0 to/from a Facility Service Definition.",
	];

	protected override IReadOnlyList<string> ExtraUsage =>
	[
		"   --fsd",
		"      Generates a Facility Service Definition (instead of Swagger).",
		"   --json",
		"      Generates JSON (instead of YAML).",
		"   --service-name <name>",
		"      Overrides the service name.",
	];

	protected override CodeGenerator CreateGenerator() => new SwaggerGenerator();

	protected override FileGeneratorSettings CreateSettings(ArgsReader args)
	{
		m_serviceName = args.ReadOption("service-name");
		if (m_serviceName != null && !ServiceDefinitionUtility.IsValidName(m_serviceName))
			throw new ArgsReaderException($"Invalid service name '{m_serviceName}'.");

		return new SwaggerGeneratorSettings
		{
			GeneratesFsd = args.ReadFlag("fsd"),
			GeneratesJson = args.ReadFlag("json"),
			ServiceName = m_serviceName,
		};
	}

	protected override ServiceParser CreateParser() => new SwaggerParser { ServiceName = m_serviceName };

	private string? m_serviceName;
}
