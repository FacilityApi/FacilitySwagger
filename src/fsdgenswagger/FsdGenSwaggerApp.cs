using System.Collections.Generic;
using ArgsReading;
using Facility.CodeGen.Console;
using Facility.Definition;
using Facility.Definition.CodeGen;
using Facility.Definition.Fsd;
using Facility.Definition.Swagger;

namespace fsdgenswagger
{
	public sealed class FsdGenFsdApp : CodeGeneratorApp
	{
		public static int Main(string[] args) => new FsdGenFsdApp().Run(args);

		protected override IReadOnlyList<string> Description => new[]
		{
			"Interprets Swagger (OpenAPI) 2.0 as a Facility Service Definition.",
		};

		protected override IReadOnlyList<string> ExtraUsage => new[]
		{
			"   --swagger",
			"      Generates Swagger (OpenAPI) 2.0.",
			"   --yaml",
			"      Generates YAML instead of JSON.",
		};

		protected override ServiceParser CreateParser(ArgsReader args)
		{
			string serviceName = args.ReadOption("serviceName");
			if (serviceName != null && ServiceDefinitionUtility.IsValidName(serviceName))
				throw new ArgsReaderException($"Invalid service name '{serviceName}'.");

			return new SwaggerParser { ServiceName = serviceName };
		}

		protected override CodeGenerator CreateGenerator(ArgsReader args)
		{
			if (args.ReadFlag("swagger"))
				return new SwaggerGenerator { Yaml = args.ReadFlag("yaml") };
			else
				return new FsdGenerator();
		}

		protected override bool SupportsSingleOutput => true;
	}
}
