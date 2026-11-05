# Project Setup

How to start a .NET project the way the author does: naming, solution layout, the shared build files, configuration through environment variables, the license header and the library list. Code style itself is in `references/csharp-style.md`.

Placeholders used below:

- `<project-name>`: lower case, e.g. `sakura`, `sample-project`
- `<Project Name>`: display case, e.g. `Sakura`, `Sample Project`
- `<ProjectName>`: Pascal case, e.g. `Sakura`, `SampleProject`
- `<PROJECTNAME>`: upper case, for environment variables, e.g. `SAKURA`, `SAMPLEPROJECT`

## Contents

- Name the project
- Lay out the solution
- Name the projects inside it
- Copy the shared build files — `.editorconfig`, `Directory.Build.props`, license header
- Write a csproj
- Configure with environment variables
- Use these libraries
- Set up Rider
- Pick a version scheme

## Name the project

Keywords: project name, naming, solution name, repository name, anime, character name

- Only when picking a name for a new project: the author uses the first name of a Japanese anime character (`sakura`, `naruto`, `goku`). It's short, easy to remember, has no spaces, and doesn't tie the project to what it does today. Never rename an existing project to fit this.
- No spaces, numbers or special characters, in the name or anywhere it's used (folders, namespaces, data folders, environment variables).
- The repository and solution use `<project-name>`. Projects and namespaces use `<ProjectName>`.

## Lay out the solution

Keywords: solution, slnx, sln, folder structure, project folder, monorepo, single project, dotnet sln migrate

- Use `.slnx`, never `.sln`. Convert an old one with `dotnet sln migrate`, then delete the `.sln`.
- Put the solution file at the root and every project in its own subfolder. The root is what goes to Git, and any project can later be used as a library without moving files.

```
--- sakura
 |- sakura.slnx
 |- Directory.Build.props
 |- .editorconfig
 |- sakura.licenseheader
 |- Sakura.Core
 |  |- Sakura.Core.csproj
 |- Sakura.Web
 |  |- Sakura.Web.csproj
 |  |- Program.cs
 |- Sakura.Web.Tests
    |- Sakura.Web.Tests.csproj
```

- A single project may sit beside the solution file (`sakura.slnx` and `Sakura.csproj` in one folder), but avoid it: a second project almost always comes, and then everything has to move.

## Name the projects inside it

Keywords: project naming, Core, Web, Tests, Application, Desktop, Game, Tools, CommandLine, suffix, prefix

- Never a bare `<ProjectName>` project. Every project has a suffix that says what it is, even the shared one: `Sakura.Core`, not `Sakura`.
- Have a `.Core` project from the start for shared code. It costs nothing and makes reuse easy later.

| Kind | Projects | Details |
| --- | --- | --- |
| Web backend | `.Core`, `.Persistence`, `.Web`, `.Web.Tests`, `.Tools` | `references/aspnet.md` |
| Desktop tool on Sakura | `.Application`, `.Desktop`, `.Application.Tests`, `.CommandLine` | `references/sakura-application.md` |
| Game on Sakura | `.Game`, `.Game.Resources`, `.Game.Desktop`, `.Game.iOS`, `.Game.Android`, `.Game.Tests` | `references/sakura-game.md` |

- Tests go in `<Project>.Tests` beside the project they test.
- Command-line tools (seeding, admin tasks, batch jobs) go in `.Tools` or `.CommandLine`.

## Copy the shared build files — `.editorconfig`, `Directory.Build.props`, license header

Keywords: editorconfig, Directory.Build.props, licenseheader, license header, file header, MIT, proprietary, LangVersion, Nullable, GenerateDocumentationFile, CS1591

### `.editorconfig`

Use this file as is, changing only the `file_header_template` to the project's name and license (see "License header" below). It's the source of every rule in `references/csharp-style.md`. It's Sakura's `.editorconfig` plus the author's Rider inspection severities at the end of the `[*.cs]` section (see "Set up Rider").

```editorconfig
# EditorConfig is awesome: http://editorconfig.org
root = true

[*]
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
charset = utf-8

[*.{json,yaml,yml,xml,csproj,props,targets}]
indent_style = space
indent_size = 2

[g_*.cs]
generated_code = true

[*.cs]
indent_style = space
indent_size = 4

#license header
file_header_template= This code is part of the <Project Name> project. Licensed under the <License Name>.\nSee the LICENSE file for full license text.

#Roslyn naming styles

#PascalCase for public and protected members
dotnet_naming_style.pascalcase.capitalization = pascal_case
dotnet_naming_symbols.public_members.applicable_accessibilities = public,internal,protected,protected_internal,private_protected
dotnet_naming_symbols.public_members.applicable_kinds = property,method,field,event
dotnet_naming_rule.public_members_pascalcase.severity = error
dotnet_naming_rule.public_members_pascalcase.symbols = public_members
dotnet_naming_rule.public_members_pascalcase.style = pascalcase

#camelCase for private members
dotnet_naming_style.camelcase.capitalization = camel_case

dotnet_naming_symbols.private_members.applicable_accessibilities = private
dotnet_naming_symbols.private_members.applicable_kinds = property,method,field,event
dotnet_naming_rule.private_members_camelcase.severity = warning
dotnet_naming_rule.private_members_camelcase.symbols = private_members
dotnet_naming_rule.private_members_camelcase.style = camelcase

dotnet_naming_symbols.local_function.applicable_kinds = local_function
dotnet_naming_rule.local_function_camelcase.severity = warning
dotnet_naming_rule.local_function_camelcase.symbols = local_function
dotnet_naming_rule.local_function_camelcase.style = camelcase

#all_lower for private and local constants/static readonlys
dotnet_naming_style.all_lower.capitalization = all_lower
dotnet_naming_style.all_lower.word_separator = _

dotnet_naming_symbols.private_constants.applicable_accessibilities = private
dotnet_naming_symbols.private_constants.required_modifiers = const
dotnet_naming_symbols.private_constants.applicable_kinds = field
dotnet_naming_rule.private_const_all_lower.severity = warning
dotnet_naming_rule.private_const_all_lower.symbols = private_constants
dotnet_naming_rule.private_const_all_lower.style = all_lower

dotnet_naming_symbols.private_static_readonly.applicable_accessibilities = private
dotnet_naming_symbols.private_static_readonly.required_modifiers = static,readonly
dotnet_naming_symbols.private_static_readonly.applicable_kinds = field
dotnet_naming_rule.private_static_readonly_all_lower.severity = warning
dotnet_naming_rule.private_static_readonly_all_lower.symbols = private_static_readonly
dotnet_naming_rule.private_static_readonly_all_lower.style = all_lower

dotnet_naming_symbols.local_constants.applicable_kinds = local
dotnet_naming_symbols.local_constants.required_modifiers = const
dotnet_naming_rule.local_const_all_lower.severity = warning
dotnet_naming_rule.local_const_all_lower.symbols = local_constants
dotnet_naming_rule.local_const_all_lower.style = all_lower

#ALL_UPPER for non private constants/static readonlys
dotnet_naming_style.all_upper.capitalization = all_upper
dotnet_naming_style.all_upper.word_separator = _

dotnet_naming_symbols.public_constants.applicable_accessibilities = public,internal,protected,protected_internal,private_protected
dotnet_naming_symbols.public_constants.required_modifiers = const
dotnet_naming_symbols.public_constants.applicable_kinds = field
dotnet_naming_rule.public_const_all_upper.severity = warning
dotnet_naming_rule.public_const_all_upper.symbols = public_constants
dotnet_naming_rule.public_const_all_upper.style = all_upper

dotnet_naming_symbols.public_static_readonly.applicable_accessibilities = public,internal,protected,protected_internal,private_protected
dotnet_naming_symbols.public_static_readonly.required_modifiers = static,readonly
dotnet_naming_symbols.public_static_readonly.applicable_kinds = field
dotnet_naming_rule.public_static_readonly_all_upper.severity = warning
dotnet_naming_rule.public_static_readonly_all_upper.symbols = public_static_readonly
dotnet_naming_rule.public_static_readonly_all_upper.style = all_upper

#Roslyn formating options

#Formatting - indentation options
csharp_indent_case_contents = true
csharp_indent_case_contents_when_block = false
csharp_indent_labels = one_less_than_current
csharp_indent_switch_labels = true

#Formatting - new line options
csharp_new_line_before_catch = true
csharp_new_line_before_else = true
csharp_new_line_before_finally = true
csharp_new_line_before_open_brace = all
#csharp_new_line_before_members_in_anonymous_types = true
#csharp_new_line_before_members_in_object_initializers = true # Currently no effect in VS/dotnet format (16.4), and makes Rider confusing
csharp_new_line_between_query_expression_clauses = true

#Formatting - organize using options
dotnet_sort_system_directives_first = true

#Formatting - spacing options
csharp_space_after_cast = false
csharp_space_after_colon_in_inheritance_clause = true
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_before_colon_in_inheritance_clause = true
csharp_space_between_method_call_empty_parameter_list_parentheses = false
csharp_space_between_method_call_name_and_opening_parenthesis = false
csharp_space_between_method_call_parameter_list_parentheses = false
csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
csharp_space_between_method_declaration_parameter_list_parentheses = false

#Formatting - wrapping options
csharp_preserve_single_line_blocks = true
csharp_preserve_single_line_statements = true

#Roslyn language styles

#Style - this. qualification
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning

#Style - type names
dotnet_style_predefined_type_for_locals_parameters_members = true:warning
dotnet_style_predefined_type_for_member_access = true:warning
csharp_style_var_when_type_is_apparent = true:none
csharp_style_var_for_built_in_types = false:warning
csharp_style_var_elsewhere = true:silent

#Style - modifiers
dotnet_style_require_accessibility_modifiers = for_non_interface_members:warning
csharp_preferred_modifier_order = public,private,protected,internal,new,abstract,virtual,sealed,override,static,readonly,extern,unsafe,volatile,async:warning

#Style - parentheses
# Skipped because roslyn cannot separate +-*/ with << >>

#Style - expression bodies
csharp_style_expression_bodied_accessors = true:warning
csharp_style_expression_bodied_constructors = false:none
csharp_style_expression_bodied_indexers = true:warning
csharp_style_expression_bodied_methods = true:silent
csharp_style_expression_bodied_operators = true:warning
csharp_style_expression_bodied_properties = true:warning
csharp_style_expression_bodied_local_functions = true:silent

#Style - expression preferences
dotnet_style_object_initializer = true:warning
dotnet_style_collection_initializer = true:warning
dotnet_style_prefer_inferred_anonymous_type_member_names = true:warning
dotnet_style_prefer_auto_properties = true:silent
dotnet_style_prefer_conditional_expression_over_assignment = true:silent
dotnet_style_prefer_conditional_expression_over_return = true:silent
dotnet_style_prefer_compound_assignment = true:warning

#Style - null/type checks
dotnet_style_coalesce_expression = true:warning
dotnet_style_null_propagation = true:warning
csharp_style_pattern_matching_over_is_with_cast_check = true:warning
csharp_style_pattern_matching_over_as_with_null_check = true:warning
csharp_style_throw_expression = true:silent
csharp_style_conditional_delegate_call = true:warning

#Style - unused
dotnet_style_readonly_field = true:silent
dotnet_code_quality_unused_parameters = non_public:silent
csharp_style_unused_value_expression_statement_preference = discard_variable:silent
csharp_style_unused_value_assignment_preference = discard_variable:warning

#Style - variable declaration
csharp_style_inlined_variable_declaration = true:warning
csharp_style_deconstructed_variable_declaration = true:warning

#Style - other C# 7.x features
dotnet_style_prefer_inferred_tuple_names = true:warning
csharp_prefer_simple_default_expression = true:warning
csharp_style_pattern_local_over_anonymous_function = true:warning
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:silent

#Style - C# 8 features
csharp_prefer_static_local_function = true:warning
csharp_prefer_simple_using_statement = true:silent
csharp_style_prefer_index_operator = true:warning
csharp_style_prefer_range_operator = true:warning
csharp_style_prefer_switch_expression = false:none

#Rider inspection severities (the author's own, so they travel with the repository)
resharper_redundant_using_directive_highlighting = error
resharper_for_can_be_converted_to_foreach_highlighting = hint
resharper_possible_multiple_enumeration_highlighting = hint
resharper_convert_to_primary_constructor_highlighting = none
resharper_convert_constructor_to_member_initializers_highlighting = none
resharper_use_collection_expression_highlighting = none
resharper_redundant_array_creation_expression_highlighting = none
resharper_redundant_explicit_array_creation_highlighting = none
resharper_redundant_empty_object_creation_argument_list_highlighting = none
resharper_replace_with_field_keyword_highlighting = none
resharper_redundant_argument_default_value_highlighting = none
resharper_member_can_be_private_global_highlighting = none
resharper_class_with_virtual_members_never_inherited_global_highlighting = none
resharper_virtual_member_call_in_constructor_highlighting = none
resharper_empty_general_catch_clause_highlighting = none

[*.{yaml,yml}]
insert_final_newline = true
indent_style = space
indent_size = 2
trim_trailing_whitespace = true
```

### License header

Every source file starts with a license header, and the IDE adds it from `file_header_template` above. The header names the license in the project's `LICENSE` file, so the two never disagree. Pick the line for the project:

| Project | `file_header_template` |
| --- | --- |
| Open source, MIT (the author's usual choice for personal projects) | `This code is part of the <Project Name> project. Licensed under the MIT License.\nSee the LICENSE file for full license text.` |
| Open source, another license | The same sentence with its name: `Licensed under the Apache License 2.0.`, `Licensed under the GNU General Public License v3.0.`, … |
| Closed source for an employer or client | Their proprietary notice, for example the one below |
| Private, no license chosen yet | `Copyright (c) <Year> <Owner>. All rights reserved.` until a license is picked, then switch every header with Rider's Code Cleanup ("Update file header") |

```editorconfig
file_header_template=Copyright (c) <Company Name>. All rights reserved.\n\nThis code is part of the <Project Name> project and is proprietary to <Company Name>.\nUnauthorized use, modification, or distribution is strictly prohibited\nwithout the express written permission of <Company Name>.
```

Rider also reads a `<project-name>.licenseheader` file at the root, which covers XML files too. Use the same text as `file_header_template`:

```
extensions: .cs
// This code is part of the <Project Name> project. Licensed under the <License Name>.
// See the LICENSE file for full license text.

extensions:  .xml .config .xsd
<!--
This code is part of the <Project Name> project. Licensed under the <License Name>.
See the LICENSE file for full license text.
-->
```

- A NuGet package states the same license in its csproj: `<PackageLicenseExpression>` with the SPDX ID (`MIT`, `Apache-2.0`, `GPL-3.0-only`), or `<PackageLicenseFile>` for a proprietary license.
- Snippets in these references leave the header out. Real files always have it.

### `Directory.Build.props`

One at the solution root, for settings every project shares:

```xml
<Project>
  <PropertyGroup Label="C#">
    <LangVersion>14.0</LangVersion>  <!-- the latest the current .NET SDK supports; pin it, don't use "latest" -->
    <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <PropertyGroup Label="Documentation">
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
  <ItemGroup Label="License">
    <None Include="$(MSBuildThisFileDirectory)<project-name>.licenseheader">
      <Link><project-name>.licenseheader</Link>
    </None>
  </ItemGroup>
</Project>
```

- `GenerateDocumentationFile` keeps XML doc comments checked by the compiler. `CS1591` (missing doc comment) is silenced because the IDE and review catch those better than a wall of warnings.
- A Sakura app also embeds its resources here (see `references/sakura-framework.md`, "Create a new Sakura app").

## Write a csproj

Keywords: csproj, project file, TargetFramework, ImplicitUsings, PackageReference, ItemGroup, Label, InternalsVisibleTo, NuGet metadata, AllowUnsafeBlocks, JETBRAINS_ANNOTATIONS

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup Label="Project">
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>   <!-- every using is written out -->
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>   <!-- only if the project really needs unsafe code -->
    <NoWarn>$(NoWarn);CS1591;NU5104</NoWarn>
    <DefineConstants>$(DefineConstants);JETBRAINS_ANNOTATIONS</DefineConstants>
  </PropertyGroup>
  <PropertyGroup Label="NuGet">
    <!-- Only for a project published to NuGet. -->
    <Title>Sakura Framework</Title>
    <PackageId>Sakura.Framework</PackageId>
    <Description>A cross-platform graphic framework and set of toolkits for making basic UI application and 2D game</Description>
    <Authors>HelloYeew</Authors>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/HelloYeew/sakura</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageIcon>icon.png</PackageIcon>
  </PropertyGroup>
  <ItemGroup Label="Internals">
    <InternalsVisibleTo Include="Sakura.Framework.Tests" />
  </ItemGroup>
  <ItemGroup Label="Packages">
    <PackageReference Include="FFmpeg.AutoGen" Version="8.1.0" />
  </ItemGroup>
  <ItemGroup Label="Native libraries">
    <!-- Say why an unusual item is here, right next to it. -->
  </ItemGroup>
</Project>
```

- `ImplicitUsings` off: every file lists its usings, so it's clear where a type comes from.
- Group items by purpose, one `ItemGroup` each, with a `Label`. Anything unusual gets a comment saying why it's there.
- Pin exact package versions. Never a floating version such as `*-*`.

## Configure with environment variables

Keywords: configuration, settings, environment variable, envvar, appsettings, launchSettings.json, connection string, secrets, debug default, DebugUtils, IsDebugBuild, Configurations folder, fail fast, enum setting

Every setting is an environment variable named `<PROJECTNAME>_<SETTING_NAME>` (`SAMPLE_DATABASE_CONNECTION_STRING`, `SAMPLE_FRONTEND_BASE_URL`, `SAMPLE_LOG_TIMEZONE`). Environment variables suit containers and are easy to change without a rebuild.

This is for servers and web backends, which get their settings from where they're deployed. A Sakura app normally needs neither these variables nor `launchSettings.json`: its settings belong to the user and are saved by `ConfigManager<TEnum>` (`references/sakura-framework.md`, "Save user settings"), which also lives in a `Configurations/` folder.

- Read each one once, in a static class in a `Configurations/` folder, and use that class everywhere. Never read `Environment.GetEnvironmentVariable` anywhere else.
- In a Debug build, a missing variable may fall back to a default in the code, with a warning in the log saying so.
- In a Release build, a missing required variable throws before the app starts. A half-configured app fails later in ways that are much harder to trace.
- A setting with fixed choices is parsed into an enum, so a typo fails loudly.
- In development, set variables in `launchSettings.json`, not `appsettings.json`.

```csharp
namespace Sample.Core.Development;

public static class DebugUtils
{
    public static bool IsDebugBuild
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
```

```csharp
using Sample.Core.Development;
using Serilog;

namespace Sample.Core.Configurations;

public enum DatabaseConnectionTarget
{
    Production,
    Test
}

public static class DatabaseSettings
{
    private const string default_connection_string = "Host=localhost;Database=postgres;Username=postgres;Password=mysecretpassword";
    private const string default_test_connection_string = "Host=localhost;Database=postgres;Username=postgres;Password=mysecretpassword;Port=5433";

    public static string DatabaseConnectionString => GetConnectionString(DatabaseConnectionTarget.Production);

    public static string TestDatabaseConnectionString => GetConnectionString(DatabaseConnectionTarget.Test);

    public static string GetConnectionString(DatabaseConnectionTarget target)
    {
        (string environmentVariableName, string defaultValue) = target switch
        {
            DatabaseConnectionTarget.Production => ("SAMPLE_DATABASE_CONNECTION_STRING", default_connection_string),
            DatabaseConnectionTarget.Test => ("SAMPLE_TEST_DATABASE_CONNECTION_STRING", default_test_connection_string),
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported database connection target.")
        };

        string? value = Environment.GetEnvironmentVariable(environmentVariableName);

        if (string.IsNullOrWhiteSpace(value))
        {
            if (DebugUtils.IsDebugBuild)
            {
                Log.Warning(
                    "{EnvironmentVariableName} environment variable is not set. Using default connection string. (Default: {DefaultValue})",
                    environmentVariableName, defaultValue);
                value = defaultValue;
            }
            else
            {
                throw new InvalidOperationException(
                    $"{environmentVariableName} environment variable is not set."
                );
            }
        }

        return value;
    }
}
```

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "SAMPLE_FRONTEND_BASE_URL": "http://localhost:3000"
      }
    }
  }
}
```

- A Sakura app keeps user-facing settings in its own `ConfigManager` instead (see `references/sakura-framework.md`, "Save user settings"). Environment variables are for deployment settings.

## Use these libraries

Keywords: library, package, NuGet, dependency, JSON, System.Text.Json, Newtonsoft, test framework, NUnit, xUnit, ImageSharp, license, OpenCV, ImageMagick, JetBrains Annotations

| Need | Use | Not |
| --- | --- | --- |
| JSON | `System.Text.Json` | `Newtonsoft.Json` |
| Tests | NUnit | xUnit, MSTest |
| Image processing, simple | ImageSharp **3.x** | ImageSharp 4.x (license issues) |
| Image processing, advanced | OpenCvSharp or Magick.NET, after checking their usage terms | |
| Nullability and contract hints | JetBrains Annotations, the version matching your Rider (latest preferred) | |

- The web stack's list is in `references/aspnet.md` ("Packages"). Sakura apps use what the framework already ships before adding anything (`references/sakura-framework.md`, rule 9).

## Set up Rider

Keywords: Rider, IDE, DotSettings, sln.DotSettings, Code Cleanup, spell check, dictionary, shared settings

- Rider is the IDE. It reads `.editorconfig` and `<project-name>.sln.DotSettings`; where both set something, `.editorconfig` wins.
- Save this as `<project-name>.sln.DotSettings` beside the solution. It's Sakura's file with its two disagreements with `.editorconfig` settled (explicit types for built-in types, `.editorconfig`'s modifier order), plus the author's own Rider code-style and code-generation settings. It holds the Rider-only rules in `references/csharp-style.md`: braces, blank lines, wrapping at 228, initializer layout, explicit `new Foo()`.

```xml
<wpf:ResourceDictionary xml:space="preserve" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" xmlns:ss="urn:shemas-jetbrains-com:settings-storage-xaml" xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/ARGUMENTS_SKIP_SINGLE/@EntryValue">True</s:Boolean>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/BRACES_FOR_FOR/@EntryValue">Required</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/BRACES_FOR_FOREACH/@EntryValue">RequiredForMultiline</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/BRACES_FOR_IFELSE/@EntryValue">NotRequired</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/BRACES_FOR_WHILE/@EntryValue">Required</s:String>
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/BRACES_REDUNDANT/@EntryValue">False</s:Boolean>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/LOCAL_FUNCTION_BODY/@EntryValue">ExpressionBody</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/METHOD_OR_OPERATOR_BODY/@EntryValue">ExpressionBody</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/MODIFIERS_ORDER/@EntryValue">public private protected internal file new abstract virtual sealed override static readonly extern unsafe volatile async required</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/OBJECT_CREATION_WHEN_TYPE_EVIDENT/@EntryValue">ExplicitlyTyped</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/PARENTHESES_NON_OBVIOUS_OPERATIONS/@EntryValue">None</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/PARENTHESES_REDUNDANCY_STYLE/@EntryValue">Remove</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/ThisQualifier/INSTANCE_MEMBERS_QUALIFY_DECLARED_IN/@EntryValue">0</s:String>
	<s:Int64 x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/BLANK_LINES_AFTER_BLOCK_STATEMENTS/@EntryValue">0</s:Int64>
	<s:Int64 x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/BLANK_LINES_AROUND_BLOCK_CASE_SECTION/@EntryValue">1</s:Int64>
	<s:Int64 x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/BLANK_LINES_AROUND_FIELD/@EntryValue">0</s:Int64>
	<s:Int64 x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/BLANK_LINES_AROUND_INVOCABLE/@EntryValue">0</s:Int64>
	<s:Int64 x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/BLANK_LINES_AROUND_PROPERTY/@EntryValue">0</s:Int64>
	<s:Int64 x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/BLANK_LINES_AROUND_REGION/@EntryValue">0</s:Int64>
	<s:Int64 x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/BLANK_LINES_AROUND_SINGLE_LINE_LOCAL_METHOD/@EntryValue">1</s:Int64>
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/INT_ALIGN_COMMENTS/@EntryValue">True</s:Boolean>
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/KEEP_EXISTING_DECLARATION_BLOCK_ARRANGEMENT/@EntryValue">True</s:Boolean>
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/KEEP_EXISTING_EMBEDDED_BLOCK_ARRANGEMENT/@EntryValue">True</s:Boolean>
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/KEEP_EXISTING_ENUM_ARRANGEMENT/@EntryValue">True</s:Boolean>
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/KEEP_EXISTING_INITIALIZER_ARRANGEMENT/@EntryValue">False</s:Boolean>
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/LINE_FEED_AT_FILE_END/@EntryValue">True</s:Boolean>
	<s:Int64 x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/MAX_ATTRIBUTE_LENGTH_FOR_SAME_LINE/@EntryValue">10000</s:Int64>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/NESTED_TERNARY_STYLE/@EntryValue">COMPACT</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/PLACE_ACCESSORHOLDER_ATTRIBUTE_ON_SAME_LINE_EX/@EntryValue">NEVER</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/PLACE_EXPR_ACCESSOR_ON_SINGLE_LINE/@EntryValue">ALWAYS</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/PLACE_EXPR_METHOD_ON_SINGLE_LINE/@EntryValue">ALWAYS</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/PLACE_EXPR_PROPERTY_ON_SINGLE_LINE/@EntryValue">ALWAYS</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/PLACE_FIELD_ATTRIBUTE_ON_SAME_LINE_EX/@EntryValue">NEVER</s:String>
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/PLACE_SIMPLE_ANONYMOUSMETHOD_ON_SINGLE_LINE/@EntryValue">False</s:Boolean>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/PLACE_SIMPLE_EMBEDDED_STATEMENT_ON_SAME_LINE/@EntryValue">NEVER</s:String>
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/PLACE_SIMPLE_INITIALIZER_ON_SINGLE_LINE/@EntryValue">False</s:Boolean>
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/REMOVE_BLANK_LINES_NEAR_BRACES_IN_CODE/@EntryValue">False</s:Boolean>
	<s:Boolean x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/REMOVE_BLANK_LINES_NEAR_BRACES_IN_DECLARATIONS/@EntryValue">False</s:Boolean>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/WRAP_ARRAY_INITIALIZER_STYLE/@EntryValue">CHOP_IF_LONG</s:String>
	<s:Int64 x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/WRAP_LIMIT/@EntryValue">228</s:Int64>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/WRAP_OBJECT_AND_COLLECTION_INITIALIZER_STYLE/@EntryValue">CHOP_ALWAYS</s:String>
	<s:String x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/WRAP_TERNARY_EXPR_STYLE/@EntryValue">WRAP_IF_LONG</s:String>
	<s:String x:Key="/Default/CodeStyle/CSharpVarKeywordUsage/ForBuiltInTypes/@EntryValue">UseExplicitType</s:String>
	<s:String x:Key="/Default/CodeStyle/CSharpVarKeywordUsage/ForSimpleTypes/@EntryValue">UseVar</s:String>
	<s:Boolean x:Key="/Default/CodeStyle/Generate/=EqualityMembers/@KeyIndexDefined">True</s:Boolean>
	<s:String x:Key="/Default/CodeStyle/Generate/=EqualityMembers/Options/=EqualityOperators/@EntryIndexedValue">False</s:String>
	<s:String x:Key="/Default/CodeStyle/Generate/=EqualityMembers/Options/=UseSystemHashCode/@EntryIndexedValue">True</s:String>
	<s:Boolean x:Key="/Default/CodeStyle/Generate/=Implementations/@KeyIndexDefined">True</s:Boolean>
	<s:String x:Key="/Default/CodeStyle/Generate/=Implementations/Options/=Async/@EntryIndexedValue">False</s:String>
	<s:String x:Key="/Default/CodeStyle/Generate/=Implementations/Options/=Mutable/@EntryIndexedValue">False</s:String>
	<s:Boolean x:Key="/Default/CodeStyle/Generate/=Overrides/@KeyIndexDefined">True</s:Boolean>
	<s:String x:Key="/Default/CodeStyle/Generate/=Overrides/Options/=Async/@EntryIndexedValue">False</s:String>
	<s:String x:Key="/Default/CodeStyle/Generate/=Overrides/Options/=Mutable/@EntryIndexedValue">False</s:String>
</wpf:ResourceDictionary>
```

- The inspection severities aren't in this file: they're in the `.editorconfig` above (`resharper_…_highlighting`), which wins over the DotSettings anyway. One place to change them.
- The file ships no spell-check words. Add the project's own words in Rider (they land in this file under `UserDictionary`) instead of ignoring spell-check warnings.
- Code generation (Rider's Generate menu): equality members use `HashCode.Combine` and add no `==`/`!=` operators; generated implementations and overrides aren't made `async` or mutable by default.
- The author's Rider inspection severities, as set in the `.editorconfig` above. Code written for this author should pass cleanly with them:

| Inspection | Severity | Effect |
| --- | --- | --- |
| `RedundantUsingDirective` | Error | An unused using is an error |
| `ForCanBeConvertedToForeach`, `PossibleMultipleEnumeration` | Hint | Shown quietly, not as warnings |
| `ConvertToPrimaryConstructor`, `ConvertConstructorToMemberInitializers` | Not shown | Classic constructors are fine; Rider doesn't push primary constructors |
| `UseCollectionExpression`, `RedundantArrayCreationExpression`, `RedundantExplicitArrayCreation`, `RedundantEmptyObjectCreationArgumentList` | Not shown | `new[] { … }`, `new List<T> { … }` and `new Foo()` are fine; no push to `[ … ]` |
| `ReplaceWithFieldKeyword` | Not shown | No push to the C# 14 `field` keyword |
| `RedundantArgumentDefaultValue` | Not shown | Passing a default value explicitly is fine when it makes the call clearer |
| `MemberCanBePrivate.Global`, `ClassWithVirtualMembersNeverInherited.Global` | Not shown | Public members and virtual members are fine without a current caller or subclass |
| `VirtualMemberCallInConstructor`, `EmptyGeneralCatchClause` | Not shown | Not flagged by the IDE; still avoid a silent catch (see `references/csharp-style.md`, "Logging exceptions") |

- `<project-name>.sln.DotSettings.user` is personal IDE state. Keep it out of the repository.
- Copy `.editorconfig` and the DotSettings from this file, not from another project's copy, which may be out of date.

## Pick a version scheme — CalVer for projects released as they go

Keywords: version, versioning, release number, CalVer, SemVer, YYYY.MDD.PATCH, milestone, roadmap

The scheme follows how the project is released:

- **Released whenever something is ready, with no versions planned around goals** (Sakura is one): CalVer, `YYYY.MDD.PATCH` by the release date. 12 October 2026 is `2026.1012.0`, and a second release that day is `2026.1012.1`. With no milestone behind a version, the date is the most useful thing it can say. Use one time zone for the date, chosen for the project and never changed.
- **Versions planned around goals or compatibility promises** (a 2.0 with a feature list, a library promising no breaking changes within a major version): version by those, with SemVer.

Either way:

- Android needs a version code that only grows. With CalVer, `YYMMDDPP` does that.
- Compute the version in the release script, never by hand.
