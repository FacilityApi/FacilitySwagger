using Facility.Definition.CodeGen;

namespace Facility.Definition.Swagger;

/// <summary>
/// Settings for generating Swagger.
/// </summary>
public sealed class SwaggerGeneratorSettings : FileGeneratorSettings
{
	/// <summary>
	/// Generates a Facility Service Definition (instead of Swagger).
	/// </summary>
	public bool GeneratesFsd { get; set; }

	/// <summary>
	/// Generates JSON (instead of YAML).
	/// </summary>
	public bool GeneratesJson { get; set; }

	/// <summary>
	/// Overrides the service name.
	/// </summary>
	public string? ServiceName { get; set; }
}
