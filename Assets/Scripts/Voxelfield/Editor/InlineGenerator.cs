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
        [MenuItem("Voxelfield/Inline")]
        private static void CustomInline()
        {
            var client = new Client(VoxelfieldComponents.SessionElements, null, new ClientInjector());
            ServerSessionContainer session = client.ReceivedServerSession;
            Inline(session, "previousServerSession", null, null);

            // Inline(new MapContainer(), "readMap");
        }

        private static void Inline(ElementBase element, string rn1, string rn2, string rn3)
        {
            var stack = new Stack<(string, string, string, string, int)>();
            var mb = new StringBuilder("#region Generated\n\n");
            string GetCastString(Type type)
            {
                string[] generic = type.GetGenericArguments().Select(arg => arg.Name).ToArray();
                string castString = type.Name;
                return generic.Length == 0 ? castString : $"{castString.Substring(0, castString.IndexOf("`", StringComparison.Ordinal))}<{string.Join(",", generic)}>";
            }
            var vi = 0;
            ElementExtensions.NavigateZipped(element, element, element, (_e1, _e2, _e3) =>
            {
                // if (_e1.TryAttribute(out NoSerializationAttribute attribute) && !attribute.ExceptRead) return Navigation.SkipDescendents;
                // names.Push((GetBaseElementType(_element.GetType()).Name, breadthIndex));
                if (_e1 is PropertyBase)
                {
                    var b = new StringBuilder();
                    (string t, string v1, string v2, string v3, int bi) = stack.Pop();
                    StringBuilder AppendVariable(string v)
                        => b.Append("((").Append(GetCastString(_e1.GetType())).Append(")").Append(v).Append(".m_Elements").Append("[").Append(bi).Append("])");
                    if (v2 == null)
                    {
                        // One
                        AppendVariable(v1).AppendLine(".Deserialize(reader);");
                    }
                    else if (v3 == null)
                    {
                        // Two
                        AppendVariable(v1).Append(".SetTo(");
                        AppendVariable(v2).AppendLine(");");
                    }
                    else
                    {
                        // Three
                        AppendVariable(v1).Append(".SetTo(");
                        AppendVariable(v2).AppendLine(");");
                    }
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
                        var vn = $"element{vi++}";
                        b.Append("var ").Append(vn).Append(" = ");
                        if (v != r) b.Append("(").Append(GetCastString(e.GetType())).Append(") ");
                        b.Append(v1);
                        if (v != r) b.Append(".m_Elements").Append("[").Append(bi).Append("]");
                        b.AppendLine(";");
                        return vn;
                    }
                    stack.Push((GetCastString(_e1.GetType()), BuildVariable(v1, rn1, _e1), BuildVariable(v2, rn2, _e2), BuildVariable(v3, rn3, _e3), 0));
                    mb.Append(b);
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
            string parentFolder = Directory.GetParent(Application.dataPath).FullName,
                   fileName = Path.ChangeExtension(Path.Combine(parentFolder, "inline"), "txt");
            File.WriteAllText(fileName, mb.ToString());
        }
    }
}