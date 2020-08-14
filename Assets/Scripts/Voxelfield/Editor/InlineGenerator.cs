using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using UnityEditor;
using UnityEngine;
using Voxelfield.Session;

namespace Voxelfield.Editor
{
    public static class InlineGenerator
    {
        [MenuItem("Voxelfield/Generate Inline")]
        private static void CustomInline()
        {
            var client = new Client(VoxelfieldComponents.SessionElements, null, new ClientInjector());
            ServerSessionContainer session = client.ReceivedServerSession;
            string updateCurrent = Inline(session, "previousServerSession", "serverSession", "receivedServerSession",
                                          (b, e, bi, v1, v2, v3) => b.AppendVariable(e, bi, v2).Append(".SetToIncludingOverride(").AppendVariable(e, bi, v3).Append(");\n")),
                   deserialize = Inline(session, "session", null, null,
                                        (b, e, bi, v1, v2, v3) => b.AppendVariable(e, bi, v1).Append(".Deserialize(reader);\n")),
                   projectPath = Directory.GetParent(Application.dataPath).FullName,
                   template = File.ReadAllText(Path.Combine(projectPath, "Assets", "Scripts", "Voxelfield", "Session", "InlineTemplate.cs.txt"));
            template = template.Replace("$1", deserialize);
            template = template.Replace("$2", updateCurrent);
            File.WriteAllText(Path.Combine(projectPath, "Assets", "Scripts", "Voxelfield", "Session", "ClientGenerated.cs"), template);
            Debug.Log($"Generated {template.Count(c => c == '\n')} lines of inline");
            // Inline(new MapContainer(), "readMap");
        }

        private static string GetCastString(Type type)
        {
            string[] generic = type.GetGenericArguments().Select(arg => arg.Name).ToArray();
            string castString = type.Name;
            return generic.Length == 0 ? castString : $"{castString.Substring(0, castString.IndexOf("`", StringComparison.Ordinal))}<{string.Join(",", generic)}>";
        }

        private static StringBuilder AppendVariable(this StringBuilder b, ElementBase e, int bi, string v)
            => b.Append("((").Append(GetCastString(e.GetType())).Append(")").Append(v).Append(".m_Elements").Append("[").Append(bi).Append("])");

        private static string Inline(ElementBase element, string rn1, string rn2, string rn3, Action<StringBuilder, ElementBase, int, string, string, string> action)
        {
            // Type, Variable Names 1, 2, 3, Breadth
            var stack = new Stack<(string, string, string, string, int)>();
            var mb = new StringBuilder("#region Generated\n\n");
            var vi = 0;
            ElementExtensions.NavigateZipped(element, element, element, (_e1, _e2, _e3) =>
            {
                // if (_e1.TryAttribute(out NoSerializationAttribute attribute) && !attribute.ExceptRead) return Navigation.SkipDescendents;
                // names.Push((GetBaseElementType(_element.GetType()).Name, breadthIndex));
                if (_e1 is PropertyBase)
                {
                    var b = new StringBuilder();
                    (string t, string v1, string v2, string v3, int bi) = stack.Pop();
                    action(b, _e1, bi, v1, v2, v3);
                    mb.Append(b);
                    stack.Push((t, v1, v2, v3, bi + 1));
                }
                else
                {
                    var b = new StringBuilder();
                    (_, string v1, string v2, string v3, int bi) = stack.Count == 0 ? ("", rn1, rn2, rn3, 0) : stack.Peek();
                    string BuildVariable(string v, string r, ElementBase e)
                    {
                        if (r == null) return null;
                        var vn = $"e{vi++}";
                        b.Append("var ").Append(vn).Append(" = ");
                        if (v != r) b.Append("(").Append(GetCastString(e.GetType())).Append(") ");
                        b.Append(v);
                        if (v != r) b.Append(".m_Elements").Append("[").Append(bi).Append("]");
                        b.Append("; ");
                        return vn;
                    }
                    stack.Push((GetCastString(_e1.GetType()), BuildVariable(v1, rn1, _e1), BuildVariable(v2, rn2, _e2), BuildVariable(v3, rn3, _e3), 0));
                    mb.Append(b).Append("\n");
                }
                return Navigation.Continue;
            }, (_e1, _e2, _e3) =>
            {
                if (!(_e1 is PropertyBase))
                {
                    stack.Pop();
                    if (stack.Count > 0)
                    {
                        (string n, string v1, string v2, string v3, int bi) = stack.Pop();
                        stack.Push((n, v1, v2, v3, bi + 1));
                    }
                }
                return default;
            });
            mb.Append("\n#endregion");
            return mb.ToString();
        }
    }
}