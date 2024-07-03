using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Facility.Definition.Swagger;

/// <summary>
/// Helpers for Swagger (OpenAPI) 2.0.
/// </summary>
public static class SwaggerUtility
{
	/// <summary>
	/// The Swagger version.
	/// </summary>
	public static readonly string SwaggerVersion = "2.0";

	/// <summary>
	/// JSON serializer settings for Swagger DTOs.
	/// </summary>
	public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
	{
		ContractResolver = new CamelCaseExceptDictionaryKeysContractResolver(),
		DateParseHandling = DateParseHandling.None,
		NullValueHandling = NullValueHandling.Ignore,
		MissingMemberHandling = MissingMemberHandling.Ignore,
		MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
	};

	internal static IReadOnlyList<T> EmptyIfNull<T>(this IReadOnlyList<T>? list) => list ?? [];

	internal static IList<T> EmptyIfNull<T>(this IList<T>? list) => list ?? Array.Empty<T>();

	internal static IReadOnlyDictionary<TKey, TValue> EmptyIfNull<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue>? list)
		where TKey : notnull => list ?? new Dictionary<TKey, TValue>();

	internal static IDictionary<TKey, TValue> EmptyIfNull<TKey, TValue>(this IDictionary<TKey, TValue>? list)
		where TKey : notnull => list ?? new Dictionary<TKey, TValue>();

#if NET6_0_OR_GREATER
	internal static bool ContainsOrdinal(this string text, string value) => text.Contains(value, StringComparison.Ordinal);
	internal static string ReplaceOrdinal(this string text, string oldValue, string newValue) => text.Replace(oldValue, newValue, StringComparison.Ordinal);
#else
	internal static bool ContainsOrdinal(this string text, string value) => text.Contains(value);
	internal static string ReplaceOrdinal(this string text, string oldValue, string newValue) => text.Replace(oldValue, newValue);
#endif

	private sealed class CamelCaseExceptDictionaryKeysContractResolver : CamelCasePropertyNamesContractResolver
	{
		protected override string ResolveDictionaryKey(string dictionaryKey) => dictionaryKey;
	}
}
