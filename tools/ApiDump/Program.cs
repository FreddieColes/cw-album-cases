// Signature-only dump of Content Warning's assemblies: type names plus member signatures for the
// types we care about. No method bodies, no game code.
// Usage: ApiDump <ManagedDir> <outFile> [focusRegex] [all]
//   default: lists every type in Assembly-CSharp, then focus types in Assembly-CSharp
//   "all":   only focus types (exact names), searched across every DLL in ManagedDir
using System.Reflection;
using System.Text.RegularExpressions;

var managed = args[0];
var outPath = args[1];
bool allAssemblies = args.Length > 3 && args[3] == "all";
var focus = new Regex(args.Length > 2 && args[2] != "" ? args[2]
    : @"Item|Shop|Sound|SFX|Audio|Radio|Speaker|Boom|Music|Emote|Hear|Noise|Pickup|Interact|Tooltip|Battery|Flashlight|Player(Items|Data|Refs)?$|PhotonView|Database|ContentWarningPlugin|Plugin|Mod",
    RegexOptions.IgnoreCase);

var dlls = Directory.GetFiles(managed, "*.dll").ToList();
using var mlc = new MetadataLoadContext(new PathAssemblyResolver(dlls), "mscorlib");

Type[] TypesOf(string path)
{
    try { var a = mlc.LoadFromAssemblyPath(path); try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); } }
    catch { return Array.Empty<Type>(); }
}
string N(Type t) { try { return t.IsGenericType ? t.Name.Split('`')[0] + "<" + string.Join(",", t.GetGenericArguments().Select(N)) + ">" : t.Name; } catch { return "?"; } }
string Base(Type t) { try { return t.BaseType == null ? "" : N(t.BaseType); } catch { return "?"; } }
string Vis(MethodBase m) => m.IsPublic ? "public" : m.IsFamily ? "protected" : m.IsAssembly ? "internal" : "private";
string Par(ParameterInfo p) { try { return (p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : "") + N(p.ParameterType) + " " + p.Name; } catch { return "?"; } }
const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

using var w = new StreamWriter(outPath);
var sources = allAssemblies ? dlls : new List<string> { Path.Combine(managed, "Assembly-CSharp.dll") };
if (!allAssemblies)
{
    w.WriteLine("# Managed assemblies"); foreach (var d in dlls.OrderBy(x => x)) w.WriteLine("  " + Path.GetFileName(d));
    w.WriteLine("\n# All types in Assembly-CSharp");
    foreach (var t in TypesOf(sources[0]).OrderBy(t => t.FullName)) w.WriteLine($"  {t.FullName} : {Base(t)}");
}
w.WriteLine("\n# Focus types (signatures)");
foreach (var src in sources)
foreach (var t in TypesOf(src).Where(t => focus.IsMatch(t.Name)).OrderBy(t => t.FullName))
{
    w.WriteLine($"\n## {(t.IsEnum ? "enum" : t.IsInterface ? "interface" : "class")} {t.FullName} : {Base(t)}   [{Path.GetFileName(src)}]");
    try {
        if (t.IsEnum) { foreach (var n in t.GetFields(BindingFlags.Public | BindingFlags.Static)) w.WriteLine($"  {n.Name}"); continue; }
        foreach (var c in t.GetConstructors(F)) w.WriteLine($"  ctor {Vis(c)} ({string.Join(", ", c.GetParameters().Select(Par))})");
        foreach (var f in t.GetFields(F)) w.WriteLine($"  field {(f.IsPublic ? "public" : "private")}{(f.IsStatic ? " static" : "")} {N(f.FieldType)} {f.Name}");
        foreach (var p in t.GetProperties(F)) w.WriteLine($"  prop {N(p.PropertyType)} {p.Name}");
        foreach (var m in t.GetMethods(F).Where(m => !m.IsSpecialName))
            w.WriteLine($"  method {Vis(m)}{(m.IsStatic ? " static" : "")}{(m.IsVirtual ? " virtual" : "")} {N(m.ReturnType)} {m.Name}{(m.IsGenericMethodDefinition ? "<" + string.Join(",", m.GetGenericArguments().Select(N)) + ">" : "")}({string.Join(", ", m.GetParameters().Select(Par))})");
    } catch (Exception e) { w.WriteLine("  (could not read: " + e.GetType().Name + ")"); }
}
Console.WriteLine($"Wrote {outPath}");
