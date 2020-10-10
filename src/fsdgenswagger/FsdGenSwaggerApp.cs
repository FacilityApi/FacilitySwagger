using System.Collections.Generic;
using ArgsReading;
using Facility.CodeGen.Console;
using Facility.Definition;
using Facility.Definition.CodeGen;
using Facility.Definition.Fsd;
using Facility.Definition.Swagger;

namespace fsdgenswagger
{
	public sealed class FsdGenSwaggerApp : CodeGeneratorApp
	{
		public static int Main(string[] args) => new FsdGenSwaggerApp().Run(args);

		protected override IReadOnlyList<string> Description => new[]
		{
			"Converts Swagger (OpenAPI) 2.0 to/from a Facility Service Definition.",
		};

		protected override IReadOnlyList<string> ExtraUsage => new[]
		{
			"   --fsd",
			"      Generates a Facility Service Definition (instead of Swagger).",
			"   --json",
			"      Generates JSON (instead of YAML).",
			"   --service-name <name>",
			"      Overrides the service name.",
		};

		protected override ServiceParser CreateParser(ArgsReader args)
		{
			string serviceName = args.ReadOption("service-name");
			if (serviceName != null && ServiceDefinitionUtility.IsValidName(serviceName))
				throw new ArgsReaderException($"Invalid service name '{serviceName}'.");

			return new SwaggerParser { ServiceName = serviceName };
		}

		protected override CodeGenerator CreateGenerator(ArgsReader args)
		{
			if (args.ReadFlag("fsd"))
				return new FsdGenerator();
			else
				return new SwaggerGenerator { Json = args.ReadFlag("json") };
		}

		protected override bool SupportsSingleOutput => true;
	}
}
