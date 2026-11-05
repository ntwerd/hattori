# C# Style

The author's C# style, rule by rule. It differs from the usual .NET conventions in several places: private methods are camelCase, private fields have no `_` prefix, private constants are `all_lower`, public constants are `ALL_UPPER`, `var` is used everywhere except for built-in types, every member gets an XML doc comment, and code uses explicit `new Foo()` and classic constructors instead of target-typed `new()`, collection expressions and primary constructors. Follow it over the .NET defaults.

The rules come from two files, `.editorconfig` and `<project-name>.sln.DotSettings`, both given in full in `references/project-setup.md`. Rider reads both, and where both set the same option, `.editorconfig` wins. Visual Studio and `dotnet format` read only `.editorconfig`.

| Label | Meaning |
| --- | --- |
| Error | Reported as an error in the IDE |
| Warning | Reported as a warning in the IDE |
| Suggestion | Preferred style, offered as a quick fix but not flagged |
| Not enforced | Preference recorded, but never flagged |
| No label | Applied by Rider's Reformat Code and Code Cleanup |

## Contents

- Files and basics
- Using directives
- Naming
- Braces and statement layout
- Spacing
- Blank lines
- Line wrapping and initializers
- Attributes and comments
- Switch statements
- Types, `var` and `this.`
- Modifiers
- Expression-bodied members
- Language preferences
- Parentheses, usings and unused code
- Doc comments and comments
- Logging exceptions

## Files and basics

| Setting | Rule |
| --- | --- |
| Encoding | UTF-8 |
| Line endings | LF |
| End of file | Always end with a newline |
| Trailing whitespace | Trimmed |
| C# indentation | 4 spaces |
| JSON, YAML, XML, `.csproj`, `.props`, `.targets` | 2 spaces |
| Maximum line length | 228 characters |
| Generated code | Files named `g_*.cs` are treated as generated |
| Usings | Written out in every file (`ImplicitUsings` is off), in Rider's order, none unused (see "Using directives") |
| Namespaces | File-scoped: `namespace Sample.Core.Configurations;` |

Every file starts with the license header (see `references/project-setup.md`).

## Using directives

Keywords: using, usings, import, import order, sort usings, using static, using alias, Optimize Imports, case

Every file lists its usings (`ImplicitUsings` is off), above the namespace, in the order Rider's Optimize Imports and Code Cleanup produce:

1. `System` and every `System.*` namespace first.
2. Every other namespace, alphabetically **ignoring letter case**: `Sakura.Framework…` comes before `SDL`, because `sa` < `sd`. A plain character-code sort would put `SDL` first, because capitals sort before lowercase letters. Don't do that.
3. `using static …` directives.
4. Aliases (`using Name = Namespace.Type;`), sorted by the alias name.

No blank lines between the groups. Namespaces sort segment by segment, so `System.Collections.Concurrent` comes before `System.Collections.Generic`.

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sakura.Framework.Input;
using Sakura.Framework.Logging;
using SDL;
using static SDL.SDL3;
using SakuraCursorState = Sakura.Framework.Input.CursorState;
using TextInputEvent = Sakura.Framework.Input.TextInputEvent;

namespace Sakura.Framework.Platform;
```

- An unused using is an **error**, not a gray hint: `resharper_redundant_using_directive_highlighting = error` in the `.editorconfig` (see `references/project-setup.md`, "Set up Rider"). Never leave one in; Optimize Imports or Code Cleanup removes it.
- Prefer an alias over a fully qualified name repeated in the code when two namespaces have a type with the same name.

## Naming

Casing depends on accessibility: anything not `private` (including `internal` and `protected`) counts as non-private.

| Symbol | Style | Example | Severity |
| --- | --- | --- | --- |
| Non-private properties, methods, fields, events | PascalCase | `public int ItemCount` | Error |
| Private properties, methods, fields, events | camelCase, no underscore | `private int itemCount`, `private void updateLayout()` | Warning |
| Local functions | camelCase | `int computeTotal()` | Warning |
| Non-private constants | ALL_UPPER with underscores | `public const int MAX_SIZE = 64;` | Warning |
| Non-private static readonly fields | ALL_UPPER with underscores | `public static readonly Vector2 DEFAULT_SIZE` | Warning |
| Private constants | all_lower with underscores | `private const int max_size = 64;` | Warning |
| Private static readonly fields | all_lower with underscores | `private static readonly TimeSpan retry_delay` | Warning |
| Local constants | all_lower with underscores | `const int buffer_size = 1024;` | Warning |

- Types, parameters and locals have no rule in the config: use PascalCase types and camelCase parameters and locals, as usual.
- camelCase private methods and properties are deliberate, and the most visible difference from usual .NET code. Keep them.
- American English in every name, comment and doc: `Color`, `Gray`, `Center`, `Initialize`, `Behavior`, `Normalize`, `License`, as .NET and Sakura do. An existing API keeps its own spelling, though: Sakura's anchors are `Anchor.Centre`, `TopCentre`, `CentreLeft` and its debug tool is `DrawVisualiser`, and `Anchor.Center` won't compile.

```csharp
public partial class CountBadge : Container
{
    public const float DEFAULT_HEIGHT = 24;

    private const int max_count = 99;
    private static readonly TimeSpan fade_duration = TimeSpan.FromMilliseconds(200);

    private readonly SpriteText label;
    private int count;

    public int Count
    {
        get => count;
        set
        {
            count = Math.Min(value, max_count);
            updateLabel();
        }
    }

    private void updateLabel() => label.Text = count.ToString();
}
```

## Braces and statement layout

Opening braces always go on their own line, and `else`, `catch` and `finally` start a new line.

| Statement | Braces |
| --- | --- |
| `for`, `while` | Always required |
| `foreach` | Required when the body spans several lines |
| `if` / `else` | Optional |

- Existing braces are never removed, even where they're optional.
- A body without braces goes on the next line, never on the same line as `if`.
- Lambdas and anonymous methods with block bodies are not collapsed onto one line.
- Single-line blocks and statements are preserved as written.
- Declaration blocks, embedded blocks and enums keep the single-line or multi-line layout the author chose.

```csharp
if (texture == null)
    return;

foreach (var child in children)
    child.Update();

while (queue.TryDequeue(out var item))
{
    process(item);
}
```

## Spacing

- Space after control-flow keywords: `if (x)`, `for (…)`, `while (…)`.
- No space after a cast: `(int)value`.
- Spaces on both sides of the inheritance colon: `class Sprite : Drawable`.
- No space between a method name and `(`, in calls and declarations: `Draw()`.
- No spaces inside parentheses: `Draw(x, y)`, never `Draw( x, y )`.

## Blank lines

The formatter adds blank lines in only two places; everywhere else it keeps the blank lines you write.

- One blank line around single-line local functions.
- One blank line around `case` sections that use a `{ }` block.
- No blank lines are forced around fields, properties, methods or regions, or after block statements.
- Blank lines just after `{` or just before `}` are kept, not removed.

## Line wrapping and initializers

Lines wrap at 228 characters, so most statements stay on one line.

- Object and collection initializers always put one member per line, even short ones.
- Array initializers put one element per line only when the line is too long.
- Ternary expressions wrap only when the line is too long; nested ternaries use Rider's compact style.
- LINQ query clauses (`from`, `where`, `select`) each go on their own line.

```csharp
var sprite = new Sprite
{
    Texture = texture,
    Size = new Vector2(64, 64)
};
```

## Attributes and comments

- Attributes on fields, properties, indexers and events go on their own line above the declaration.
- Consecutive end-of-line comments are aligned into one column.

```csharp
[JsonIgnore]
public string CachePath { get; set; }

private int width;   // in pixels
private int height;  // in pixels
```

## Switch statements

- `case` labels are indented inside `switch`, and statements are indented inside `case`.
- A `case` with a `{ }` block keeps its braces level with the `case` label.
- `goto` labels sit one indent level left of the surrounding code.
- Don't convert switch statements to switch expressions just because you can. A switch expression is fine where it reads better, such as mapping one value to another.

```csharp
switch (state)
{
    case State.Idle:
        resetTimer();
        break;

    case State.Loading:
    {
        var progress = loader.Progress;
        updateBar(progress);
        break;
    }
}
```

## Types, `var` and `this.`

- Use language keywords over framework type names: `int`, `string`, `string.Empty`, not `Int32`, `String.Empty`. (Warning)
- Use explicit types for built-in types: `int count = 0;`, `string name = …;`, `bool done = false;`, never `var count = 0;`. (Warning)
- Use `var` for other types: `var sprite = new Sprite();`. (Suggestion)
- Name the type in `new`, never target-typed `new()`: `private readonly Dictionary<string, int> scores = new Dictionary<string, int>();`, `var builder = new StringBuilder();`, `return new List<string>();`. (Rider: object creation "explicitly typed")
- Never qualify members with `this.`. (Warning) The constructor assigning a field from a parameter of the same name is the one place `this.` is needed.

## Modifiers

Every non-interface member states its accessibility explicitly, even when it's `private`. (Warning)

Modifiers follow this order (Warning): `public private protected internal new abstract virtual sealed override static readonly extern unsafe volatile async`

```csharp
public override async Task LoadAsync()
private static readonly TimeSpan retry_delay = TimeSpan.FromSeconds(1);
```

## Expression-bodied members

Use `=>` wherever a member is a single expression, and keep it on one line: `public float Area => Width * Height;`

| Member | Body style | Severity |
| --- | --- | --- |
| Methods | `=>` where possible | Suggestion |
| Properties | `=>` | Warning |
| Accessors (`get`, `set`) | `=>` | Warning |
| Indexers | `=>` | Warning |
| Operators | `=>` | Warning |
| Local functions | `=>` | Suggestion |
| Constructors | Block `{ }` | Not enforced |

## Language preferences

| Prefer | Over | Severity |
| --- | --- | --- |
| Object initializer `new Foo { X = 1 }` | Setting members after construction | Warning |
| Collection initializer `new List<int> { 1, 2 }` | Repeated `Add` calls | Warning |
| `a ?? b` | `a != null ? a : b` | Warning |
| `a?.B` | Null check before member access | Warning |
| `Changed?.Invoke()` | Null-checking a delegate before calling it | Warning |
| `if (obj is Sprite s)` | `is` check followed by a cast, or `as` followed by a null check | Warning |
| `x += 1` | `x = x + 1` | Warning |
| Inline `out var result` | Declaring the variable before the call | Warning |
| `var (x, y) = getPoint();` | Reading tuple items one by one | Warning |
| Inferred tuple and anonymous type names | Repeating the names | Warning |
| `default` | `default(T)` | Warning |
| Local function | Lambda stored in a local variable | Warning |
| `static` local function | Non-static local function that captures nothing | Warning |
| `items[^1]` | `items[items.Length - 1]` | Warning |
| `text[1..]` | `Substring` and manual slicing | Warning |
| Auto-property | Property with a plain backing field | Suggestion |
| Conditional expression | `if`/`else` that only assigns or returns | Suggestion |
| `throw` expression | Null check followed by `throw` | Suggestion |
| `is null` | `ReferenceEquals(x, null)` | Suggestion |
| `using var x = …;` | `using (…) { }` block | Suggestion |

- In a Sakura app or game, null checks are `.IsNull()` / `.IsNotNull()` and float comparisons are `Precision.AlmostEquals`, from Sakura (see `references/sakura-framework.md`, "Use Sakura's extensions and utilities first"). Elsewhere, `is null` / `is not null`, and compare floats with a tolerance, never `==`.
- Nullable reference types are on everywhere. A dependency filled in later (DI, a loader) is declared `= null!;`, never left nullable just to quiet the compiler.
- Use an enum, not a string, for anything with a fixed set of values.

## Parentheses, usings and unused code

- Remove redundant parentheses, and don't add them just to clarify operator precedence.
- Usings follow the order in "Using directives".
- Calls with a single argument are exempt from Rider's argument-style rules, so they never need named arguments.
- Assign unused values to the `_` discard. (Warning for assignments, Suggestion for expression results)
- Unused parameters in non-public methods are flagged. (Suggestion)
- Fields that are never reassigned should be `readonly`. (Suggestion)

## Doc comments and comments

Keywords: XML doc, summary, documentation comment, comment style, remarks

- Every class and every method gets an XML doc comment, even a trivial one. It's easier to delete a comment nobody needs than to guess what an undocumented member is for. When unsure, write it.
- XML tags go on their own lines, even for a one-line summary:

```csharp
// Do
/// <summary>
/// The share of the window the panel covers, on each axis.
/// </summary>

// Don't
/// <summary>The share of the window the panel covers, on each axis.</summary>
```

- Doc comments say what something is for and why, in plain sentences, not a restatement of the name.
- Inside method bodies, comment rarely: only a complex flow, a long operation, or a gotcha that needs knowledge of the project to understand. Write the comment the next person really needs, concisely.

## Logging exceptions

Keywords: logging, exception, error message, stack trace

- Log the exception object, not its message, so the stack trace survives:

```csharp
logger.LogError(ex, "Saving the result failed");       // ASP.NET, ILogger<T>
Logger.Error("Saving the result failed", ex);          // Sakura's Logger
```

- Never `Logger.Error($"Saving failed: {ex.Message}")`: the type and the stack trace are gone, and the failure can't be traced from the log.
- Never swallow an exception silently. If skipping it is right (one unreadable file in a folder), log it and carry on.
