using System.Text.RegularExpressions;
using Facility.Definition.CodeGen;
using Facility.Definition.Fsd;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Facility.Definition.Swagger;

/// <summary>
/// Parses Swagger (OpenAPI) 2.0.
/// </summary>
public sealed class SwaggerParser : ServiceParser
{
	/// <summary>
	/// The service name (defaults to 'info/x-identifier' or 'info/title').
	/// </summary>
	public string? ServiceName { get; set; }

	/// <summary>
	/// Implements TryParseDefinition.
	/// </summary>
	protected override bool TryParseDefinitionCore(ServiceDefinitionText text, out ServiceInfo? service, out IReadOnlyList<ServiceDefinitionError> errors)
	{
		var isFsd = new FsdParser(new() { SupportsEvents = true }).TryParseDefinition(text, out service, out errors);
		if (isFsd || text.Name.EndsWith(".fsd", StringComparison.OrdinalIgnoreCase))
			return isFsd;

		service = null;

		if (string.IsNullOrWhiteSpace(text.Text))
		{
			errors = [new ServiceDefinitionError("Service definition is missing.", new ServiceDefinitionPosition(text.Name, 1, 1))];
			return false;
		}

		SwaggerService swaggerService;
		SwaggerParserContext context;

		if (!s_detectJsonRegex.IsMatch(text.Text))
		{
			// parse YAML
			var yamlDeserializer = new DeserializerBuilder()
				.IgnoreUnmatchedProperties()
				.WithNamingConvention(new OurNamingConvention())
				.Build();
			using (var stringReader = new StringReader(text.Text))
			{
				try
				{
					swaggerService = yamlDeserializer.Deserialize<SwaggerService>(stringReader);
				}
				catch (YamlException exception)
				{
					var errorMessage = exception.InnerException?.Message ?? exception.Message;
					const string errorStart = "): ";
					var errorStartIndex = errorMessage.IndexOf(errorStart, StringComparison.OrdinalIgnoreCase);
					if (errorStartIndex != -1)
						errorMessage = errorMessage.Substring(errorStartIndex + errorStart.Length);

					errors = [new ServiceDefinitionError(errorMessage, new ServiceDefinitionPosition(text.Name, exception.End.Line, exception.End.Column))];
					return false;
				}
			}

			if (swaggerService == null)
			{
				errors = [new ServiceDefinitionError("Service definition is missing.", new ServiceDefinitionPosition(text.Name, 1, 1))];
				return false;
			}

			context = SwaggerParserContext.FromYaml(text);
		}
		else
		{
			// parse JSON
			using (var stringReader = new StringReader(text.Text))
			using (var jsonTextReader = new JsonTextReader(stringReader))
			{
				try
				{
					swaggerService = JsonSerializer.Create(SwaggerUtility.JsonSerializerSettings).Deserialize<SwaggerService>(jsonTextReader)!;
				}
				catch (JsonException exception)
				{
					errors = [new ServiceDefinitionError(exception.Message, new ServiceDefinitionPosition(text.Name, jsonTextReader.LineNumber, jsonTextReader.LinePosition))];
					return false;
				}

				context = SwaggerParserContext.FromJson(text);
			}
		}

		var conversion = SwaggerConversion.Create(swaggerService, ServiceName, context);
		service = conversion.Service;
		errors = conversion.Errors;
		return errors.Count == 0;
	}

	/// <summary>
	/// Converts Swagger (OpenAPI) 2.0 into a service definition.
	/// </summary>
	/// <exception cref="ServiceDefinitionException">Thrown if the service would be invalid.</exception>
	public ServiceInfo ConvertSwaggerService(SwaggerService swaggerService)
	{
		if (TryConvertSwaggerService(swaggerService, out var service, out var errors))
			return service!;
		else
			throw new ServiceDefinitionException(errors);
	}

	/// <summary>
	/// Attempts to convert Swagger (OpenAPI) 2.0 into a service definition.
	/// </summary>
	public bool TryConvertSwaggerService(SwaggerService swaggerService, out ServiceInfo? service, out IReadOnlyList<ServiceDefinitionError> errors)
	{
		var conversion = SwaggerConversion.Create(swaggerService, ServiceName, SwaggerParserContext.None);
		service = conversion.Service;
		errors = conversion.Errors;
		return errors.Count == 0;
	}

	private sealed class OurNamingConvention : INamingConvention
	{
		public string Apply(string value)
		{
			if (value[0] >= 'A' && value[0] <= 'Z')
				value = CodeGenUtility.ToCamelCase(value);
			return value;
		}
	}

	private static readonly Regex s_detectJsonRegex = new Regex(@"^\s*[{/]", RegexOptions.Singleline);
}
