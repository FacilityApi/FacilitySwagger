using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Facility.Definition.CodeGen;
using Facility.Definition.Fsd;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
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
				.WithTypeConverter(new JTokenYamlTypeConverter())
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
					var errorMessage = GetMostHelpfulErrorMessage(exception);
					const string errorStart = "): ";
					var errorStartIndex = errorMessage.IndexOf(errorStart, StringComparison.OrdinalIgnoreCase);
					if (errorStartIndex != -1)
						errorMessage = errorMessage.Substring(errorStartIndex + errorStart.Length);

					errors = [new ServiceDefinitionError(errorMessage, new ServiceDefinitionPosition(text.Name, checked((int) exception.End.Line), checked((int) exception.End.Column)))];
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

	private static string GetMostHelpfulErrorMessage(Exception exception)
	{
		string? bestMessage = null;
		for (var current = exception; current != null; current = current.InnerException!)
		{
			if (!string.IsNullOrWhiteSpace(current.Message) && !IsUnhelpfulWrapperException(current))
				bestMessage = current.Message;
		}

		return bestMessage ?? exception.Message;
	}

	private static bool IsUnhelpfulWrapperException(Exception exception) =>
		exception is TargetInvocationException;

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
			if (value.Length == 0)
				return value;

			if (value[0] >= 'A' && value[0] <= 'Z')
				value = CodeGenUtility.ToCamelCase(value);
			return value;
		}

		public string Reverse(string value)
		{
			if (value.Length == 0)
				return value;

			if (value[0] >= 'a' && value[0] <= 'z')
				value = CodeGenUtility.ToPascalCase(value);
			return value;
		}
	}

	private sealed class JTokenYamlTypeConverter : IYamlTypeConverter
	{
		public bool Accepts(Type type) => typeof(JToken).IsAssignableFrom(type);

		public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
		{
			if (type == typeof(JObject))
				return ReadJToken(parser) as JObject ?? throw new YamlException("Expected an object value.");

			if (type == typeof(JArray))
				return ReadJToken(parser) as JArray ?? throw new YamlException("Expected an array value.");

			return ReadJToken(parser);
		}

		public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
		{
			throw new NotSupportedException("JToken YAML serialization is not supported by this converter.");
		}

		private static JToken ReadJToken(IParser parser)
		{
			if (parser.TryConsume<Scalar>(out var scalar))
				return ToJValue(scalar);

			if (parser.TryConsume<SequenceStart>(out _))
			{
				var array = new JArray();
				while (!parser.TryConsume<SequenceEnd>(out _))
					array.Add(ReadJToken(parser));
				return array;
			}

			if (parser.TryConsume<MappingStart>(out _))
			{
				var obj = new JObject();
				while (!parser.TryConsume<MappingEnd>(out _))
				{
					var keyToken = ReadJToken(parser);
					var key = keyToken.Type == JTokenType.String ? keyToken.Value<string>()! : keyToken.ToString();
					obj[key] = ReadJToken(parser);
				}
				return obj;
			}

			throw new YamlException("Unexpected YAML token while reading JToken value.");
		}

		private static JValue ToJValue(Scalar scalar)
		{
			if (scalar.Style != ScalarStyle.Plain)
				return new JValue(scalar.Value);

			if (scalar.IsKey)
				return new JValue(scalar.Value);

			if (scalar.Value == null || scalar.Value == "~" || scalar.Value.Equals("null", StringComparison.OrdinalIgnoreCase))
				return JValue.CreateNull();

			if (scalar.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
				return new JValue(true);

			if (scalar.Value.Equals("false", StringComparison.OrdinalIgnoreCase))
				return new JValue(false);

			if (long.TryParse(scalar.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
				return new JValue(longValue);

			if (double.TryParse(scalar.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
				return new JValue(doubleValue);

			return new JValue(scalar.Value);
		}
	}

	private static readonly Regex s_detectJsonRegex = new Regex(@"^\s*[{/]", RegexOptions.Singleline);
}
