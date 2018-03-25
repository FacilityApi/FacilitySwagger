using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Facility.Definition.UnitTests.Fsd
{
	public sealed class FieldTests
	{
		[TestCase("string", ServiceTypeKind.String)]
		[TestCase("boolean", ServiceTypeKind.Boolean)]
		[TestCase("double", ServiceTypeKind.Double)]
		[TestCase("int32", ServiceTypeKind.Int32)]
		[TestCase("int64", ServiceTypeKind.Int64)]
		[TestCase("decimal", ServiceTypeKind.Decimal)]
		[TestCase("bytes", ServiceTypeKind.Bytes)]
		[TestCase("object", ServiceTypeKind.Object)]
		[TestCase("error", ServiceTypeKind.Error)]
		public void PrimitiveFields(string name, ServiceTypeKind kind)
		{
			var service = TestUtility.ParseTestApi("service TestApi { data One { x: xyzzy; } }".Replace("xyzzy", name));

			var dto = service.Dtos.Single();
			var field = dto.Fields.Single();
			field.Name.Should().Be("x");
			field.Attributes.Count.Should().Be(0);
			field.Summary.Should().Be("");
			var type = service.GetFieldType(field);
			type.Kind.Should().Be(kind);
			type.Dto.Should().BeNull();
			type.Enum.Should().BeNull();
			type.ValueType.Should().BeNull();

			TestUtility.GenerateFsd(service).Should().Equal(new[]
			{
				"// DO NOT EDIT: generated by TestUtility",
				"",
				"service TestApi",
				"{",
				"\tdata One",
				"\t{",
				$"\t\tx: {name};",
				"\t}",
				"}",
				"",
			});
		}

		[Test]
		public void CaseSensitivePrimitive()
		{
			TestUtility.ParseInvalidTestApi("service TestApi { data One { x: Boolean; } }")
				.Message.Should().Be("TestApi.fsd(1,33): Unknown field type 'Boolean'.");
		}

		[Test]
		public void EnumField()
		{
			var service = TestUtility.ParseTestApi("service TestApi { enum MyEnum { X } data One { x: MyEnum; } }");

			var dto = service.Dtos.Single();
			var field = dto.Fields.Single();
			field.Name.Should().Be("x");
			field.Attributes.Count.Should().Be(0);
			field.Summary.Should().Be("");
			var type = service.GetFieldType(field);
			type.Kind.Should().Be(ServiceTypeKind.Enum);
			type.Dto.Should().BeNull();
			type.Enum.Name.Should().Be("MyEnum");
			type.ValueType.Should().BeNull();

			TestUtility.GenerateFsd(service).Should().Equal(new[]
			{
				"// DO NOT EDIT: generated by TestUtility",
				"",
				"service TestApi",
				"{",
				"\tenum MyEnum",
				"\t{",
				"\t\tX,",
				"\t}",
				"",
				"\tdata One",
				"\t{",
				"\t\tx: MyEnum;",
				"\t}",
				"}",
				"",
			});
		}

		[Test]
		public void DtoField()
		{
			var service = TestUtility.ParseTestApi("service TestApi { data MyDto { x: int32; } data One { x: MyDto; } }");

			var dto = service.Dtos.First(x => x.Name == "One");
			var field = dto.Fields.Single();
			field.Name.Should().Be("x");
			field.Attributes.Count.Should().Be(0);
			field.Summary.Should().Be("");
			var type = service.GetFieldType(field);
			type.Kind.Should().Be(ServiceTypeKind.Dto);
			type.Dto.Name.Should().Be("MyDto");
			type.Enum.Should().BeNull();
			type.ValueType.Should().BeNull();

			TestUtility.GenerateFsd(service).Should().Equal(new[]
			{
				"// DO NOT EDIT: generated by TestUtility",
				"",
				"service TestApi",
				"{",
				"\tdata MyDto",
				"\t{",
				"\t\tx: int32;",
				"\t}",
				"",
				"\tdata One",
				"\t{",
				"\t\tx: MyDto;",
				"\t}",
				"}",
				"",
			});
		}

		[Test]
		public void RecursiveDtoField()
		{
			var service = TestUtility.ParseTestApi("service TestApi { data MyDto { x: MyDto; } }");

			var dto = service.Dtos.Single();
			var field = dto.Fields.Single();
			field.Name.Should().Be("x");
			field.Attributes.Count.Should().Be(0);
			field.Summary.Should().Be("");
			var type = service.GetFieldType(field);
			type.Kind.Should().Be(ServiceTypeKind.Dto);
			type.Dto.Name.Should().Be("MyDto");
			type.Enum.Should().BeNull();
			type.ValueType.Should().BeNull();

			TestUtility.GenerateFsd(service).Should().Equal(new[]
			{
				"// DO NOT EDIT: generated by TestUtility",
				"",
				"service TestApi",
				"{",
				"\tdata MyDto",
				"\t{",
				"\t\tx: MyDto;",
				"\t}",
				"}",
				"",
			});
		}

		[Test]
		public void TwoFieldsSameName()
		{
			TestUtility.ParseInvalidTestApi("service TestApi { data One { x: int32; x: int64;} }")
				.Message.Should().Be("TestApi.fsd(1,40): Duplicate field: x");
		}

		[Test]
		public void InvalidFieldType()
		{
			TestUtility.ParseInvalidTestApi("service TestApi { data One { x: X; } }")
				.Message.Should().Be("TestApi.fsd(1,33): Unknown field type 'X'.");
		}

		[Test]
		public void ResultOfDto()
		{
			var service = TestUtility.ParseTestApi("service TestApi { data One { x: result<One>; } }");

			var dto = service.Dtos.Single();
			var field = dto.Fields.Single();
			field.Name.Should().Be("x");
			field.Attributes.Count.Should().Be(0);
			field.Summary.Should().Be("");
			var type = service.GetFieldType(field);
			type.Kind.Should().Be(ServiceTypeKind.Result);
			type.ValueType.Kind.Should().Be(ServiceTypeKind.Dto);
			type.ValueType.Dto.Name.Should().Be("One");
		}

		[Test]
		public void ResultOfEnumInvalid()
		{
			TestUtility.ParseInvalidTestApi("service TestApi { enum Xs { x, xx }; data One { x: result<Xs>; } }");
		}

		[TestCase("string", ServiceTypeKind.String)]
		[TestCase("boolean", ServiceTypeKind.Boolean)]
		[TestCase("double", ServiceTypeKind.Double)]
		[TestCase("int32", ServiceTypeKind.Int32)]
		[TestCase("int64", ServiceTypeKind.Int64)]
		[TestCase("decimal", ServiceTypeKind.Decimal)]
		[TestCase("bytes", ServiceTypeKind.Bytes)]
		[TestCase("object", ServiceTypeKind.Object)]
		[TestCase("error", ServiceTypeKind.Error)]
		[TestCase("Dto", ServiceTypeKind.Dto)]
		[TestCase("Enum", ServiceTypeKind.Enum)]
		[TestCase("result<Dto>", ServiceTypeKind.Result)]
		[TestCase("int32[]", ServiceTypeKind.Array)]
		[TestCase("map<int32>", ServiceTypeKind.Map)]
		public void ArrayOfAnything(string name, ServiceTypeKind kind)
		{
			var service = TestUtility.ParseTestApi("service TestApi { enum Enum { x, y } data Dto { x: xyzzy[]; } }".Replace("xyzzy", name));

			var dto = service.Dtos.Single();
			var field = dto.Fields.Single();
			field.Name.Should().Be("x");
			field.Attributes.Count.Should().Be(0);
			field.Summary.Should().Be("");
			var type = service.GetFieldType(field);
			type.Kind.Should().Be(ServiceTypeKind.Array);
			type.ValueType.Kind.Should().Be(kind);
		}

		[TestCase("string", ServiceTypeKind.String)]
		[TestCase("boolean", ServiceTypeKind.Boolean)]
		[TestCase("double", ServiceTypeKind.Double)]
		[TestCase("int32", ServiceTypeKind.Int32)]
		[TestCase("int64", ServiceTypeKind.Int64)]
		[TestCase("decimal", ServiceTypeKind.Decimal)]
		[TestCase("bytes", ServiceTypeKind.Bytes)]
		[TestCase("object", ServiceTypeKind.Object)]
		[TestCase("error", ServiceTypeKind.Error)]
		[TestCase("Dto", ServiceTypeKind.Dto)]
		[TestCase("Enum", ServiceTypeKind.Enum)]
		[TestCase("result<Dto>", ServiceTypeKind.Result)]
		[TestCase("int32[]", ServiceTypeKind.Array)]
		[TestCase("map<int32>", ServiceTypeKind.Map)]
		public void MapOfAnything(string name, ServiceTypeKind kind)
		{
			var service = TestUtility.ParseTestApi("service TestApi { enum Enum { x, y } data Dto { x: map<xyzzy>; } }".Replace("xyzzy", name));

			var dto = service.Dtos.Single();
			var field = dto.Fields.Single();
			field.Name.Should().Be("x");
			field.Attributes.Count.Should().Be(0);
			field.Summary.Should().Be("");
			var type = service.GetFieldType(field);
			type.Kind.Should().Be(ServiceTypeKind.Map);
			type.ValueType.Kind.Should().Be(kind);
		}

		[TestCase("string", ServiceTypeKind.String)]
		[TestCase("boolean", ServiceTypeKind.Boolean)]
		[TestCase("double", ServiceTypeKind.Double)]
		[TestCase("int32", ServiceTypeKind.Int32)]
		[TestCase("int64", ServiceTypeKind.Int64)]
		[TestCase("decimal", ServiceTypeKind.Decimal)]
		[TestCase("bytes", ServiceTypeKind.Bytes)]
		[TestCase("object", ServiceTypeKind.Object)]
		[TestCase("error", ServiceTypeKind.Error)]
		[TestCase("Dto", ServiceTypeKind.Dto)]
		[TestCase("Enum", ServiceTypeKind.Enum)]
		[TestCase("result<Dto>", ServiceTypeKind.Result)]
		[TestCase("int32[]", ServiceTypeKind.Array)]
		[TestCase("map<int32>", ServiceTypeKind.Map)]
		public void ResultOfAnything(string name, ServiceTypeKind kind)
		{
			var service = TestUtility.ParseTestApi("service TestApi { enum Enum { x, y } data Dto { x: result<xyzzy>; } }".Replace("xyzzy", name));

			var dto = service.Dtos.Single();
			var field = dto.Fields.Single();
			field.Name.Should().Be("x");
			field.Attributes.Count.Should().Be(0);
			field.Summary.Should().Be("");
			var type = service.GetFieldType(field);
			type.Kind.Should().Be(ServiceTypeKind.Result);
			type.ValueType.Kind.Should().Be(kind);
		}
	}
}
