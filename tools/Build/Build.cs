return BuildRunner.Execute(args, build =>
{
	var codegen = "fsdgenswagger";

	var gitLogin = new GitLoginInfo("FacilityApiBot", Environment.GetEnvironmentVariable("BUILD_BOT_PASSWORD") ?? "");

	var dotNetBuildSettings = new DotNetBuildSettings
	{
		NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
		DocsSettings = new DotNetDocsSettings
		{
			GitLogin = gitLogin,
			GitAuthor = new GitAuthorInfo("FacilityApiBot", "facilityapi@gmail.com"),
			SourceCodeUrl = "https://github.com/FacilityApi/FacilitySwagger/tree/master/src",
			ProjectHasDocs = name => !name.StartsWith("fsdgen", StringComparison.Ordinal),
		},
		PackageSettings = new DotNetPackageSettings
		{
			GitLogin = gitLogin,
			PushTagOnPublish = x => $"nuget.{x.Version}",
		},
	};

	build.AddDotNetTargets(dotNetBuildSettings);

	build.Target("codegen")
		.DependsOn("build")
		.Describe("Generates code from the FSD")
		.Does(() => CodeGen(verify: false));

	build.Target("verify-codegen")
		.DependsOn("build")
		.Describe("Ensures the generated code is up-to-date")
		.Does(() => CodeGen(verify: true));

	build.Target("test")
		.DependsOn("verify-codegen");

	void CodeGen(bool verify)
	{
		var configuration = dotNetBuildSettings.GetConfiguration();
		var toolPath = FindFiles($"src/{codegen}/bin/{configuration}/net6.0/{codegen}.dll").FirstOrDefault() ?? throw new BuildException($"Missing {codegen}.dll.");

		var verifyOption = verify ? "--verify" : null;

		RunDotNet(toolPath, "example/ExampleApi.fsd", "example/output/swagger", "--json", "--newline", "lf", verifyOption);
		RunDotNet(toolPath, "example/ExampleApi.fsd", "example/output/swagger", "--newline", "lf", verifyOption);
		RunDotNet(toolPath, "example/output/swagger/ExampleApi.json", "example/output/swagger/fsd", "--fsd", "--newline", "lf", verifyOption);
		if (verify)
			RunDotNet(toolPath, "example/output/swagger/ExampleApi.yaml", "example/output/swagger/fsd", "--fsd", "--newline", "lf", verifyOption);

		foreach (var yamlPath in FindFiles("example/*.yaml"))
			RunDotNet(toolPath, yamlPath, "example/output/fsd", "--fsd", "--newline", "lf", verifyOption);

		Directory.CreateDirectory("example/output/fsd/swagger");
		foreach (var fsdPath in FindFiles("example/output/fsd/*.fsd"))
			RunDotNet(toolPath, fsdPath, $"example/output/fsd/swagger/{Path.GetFileNameWithoutExtension(fsdPath)}.yaml", "--newline", "lf", verifyOption);
	}
});
