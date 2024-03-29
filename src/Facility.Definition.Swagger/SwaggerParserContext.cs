using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.RepresentationModel;

namespace Facility.Definition.Swagger;

internal sealed class SwaggerParserContext
{
	public static SwaggerParserContext None => new SwaggerParserContext(serviceDefinitionText: null, isYaml: false);

	public static SwaggerParserContext FromYaml(ServiceDefinitionText serviceDefinitionText) => new SwaggerParserContext(serviceDefinitionText, isYaml: true);

	public static SwaggerParserContext FromJson(ServiceDefinitionText serviceDefinitionText) => new SwaggerParserContext(serviceDefinitionText, isYaml: false);

	public SwaggerParserContext Root => new SwaggerParserContext(m_serviceDefinitionText, m_isYaml);

	public ServiceDefinitionPosition? CreatePosition(string? path = null) => m_serviceDefinitionText == null ? null : new ServiceDefinitionPosition(m_serviceDefinitionText.Name, () => FindLineColumn(path));

	public ServicePart? CreatePart(string? path = null) => m_serviceDefinitionText == null ? null : new ServicePart(ServicePartKind.Keyword, CreatePosition(path)!);

	public ServiceDefinitionError CreateError(string error, string? path = null) => new ServiceDefinitionError(error, CreatePosition(path));

	public SwaggerParserContext CreateContext(string path) => new SwaggerParserContext(m_serviceDefinitionText, m_isYaml, ResolvePath(path));

	private SwaggerParserContext(ServiceDefinitionText? serviceDefinitionText, bool isYaml, string? path = null)
	{
		m_serviceDefinitionText = serviceDefinitionText;
		m_isYaml = isYaml;
		m_path = path;
	}

	private (int LineNumber, int ColumnNumber) FindLineColumn(string? path)
	{
		if (m_isYaml)
		{
			var yamlStream = new YamlStream();
			using (var stringReader = new StringReader(m_serviceDefinitionText!.Text))
				yamlStream.Load(stringReader);

			var node = yamlStream.Documents[0].RootNode;
			if (path != null && path.Length != 0)
			{
				foreach (var part in path.Split('/'))
				{
					if (!(node is YamlMappingNode mappingNode))
						break;

					mappingNode.Children.TryGetValue(new YamlScalarNode(part), out var childNode);
					if (childNode == null)
						break;

					node = childNode;
				}
			}

			return (node.End.Line, node.End.Column);
		}
		else
		{
			JToken token = JToken.Parse(m_serviceDefinitionText!.Text);

			if (path != null && path.Length != 0)
			{
				foreach (var part in path.Split('/'))
				{
					if (!(token is JObject jObject))
						break;

					var childToken = jObject[part];
					if (childToken == null)
						break;

					token = childToken;
				}
			}
			JToken pathToken = string.IsNullOrEmpty(path) ? token : token.SelectToken(ResolvePath(path)!)!;

			var lineInfo = (IJsonLineInfo) (pathToken ?? token);
			return (lineInfo.LineNumber, lineInfo.LinePosition);
		}
	}

	private string? ResolvePath(string? path) => string.IsNullOrEmpty(path) ? m_path : string.IsNullOrEmpty(m_path) ? path : m_path + "." + path;

	private readonly ServiceDefinitionText? m_serviceDefinitionText;
	private readonly bool m_isYaml;
	private readonly string? m_path;
}
