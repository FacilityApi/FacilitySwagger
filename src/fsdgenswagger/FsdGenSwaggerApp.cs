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
		var serviceName = args.ReadOption("service-name");
		if (serviceName != null && ServiceDefinitionUtility.IsValidName(serviceName))
			throw new ArgsReaderException($"Invalid service name '{serviceName}'.");

		return new SwaggerGeneratorSettings
		{
			GeneratesFsd = args.ReadFlag("fsd"),
			GeneratesJson = args.ReadFlag("json"),
			ServiceName = serviceName,
		};
	}

	protected override ServiceParser CreateParser() => new SwaggerParser();
}
