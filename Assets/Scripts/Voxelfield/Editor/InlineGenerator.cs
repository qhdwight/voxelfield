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
            Inline(session, "session");

            // Inline(new MapContainer(), "readMap");
        }

        private static void Inline(ElementBase element, string rootName)
        {
            var names = new Stack<(string, string, int)>();
            var builder = new StringBuilder("#region Generated\n\n");
            string GetCastString(Type type)
            {
                string[] generic = type.GetGenericArguments().Select(arg => arg.Name).ToArray();
                string castString = type.Name;
                return generic.Length == 0 ? castString : $"{castString.Substring(0, castString.IndexOf("`", StringComparison.Ordinal))}<{string.Join(",", generic)}>";
            }
            var ni = 0;
            element.Navigate(_element =>
            {
                if (_element.TryAttribute(out NoSerializationAttribute attribute) && !attribute.ExceptRead) return Navigation.SkipDescendents;
                // names.Push((GetBaseElementType(_element.GetType()).Name, breadthIndex));
                if (_element is PropertyBase)
                {
                    (string t, string v, int bi) = names.Pop();
                    var b = new StringBuilder();
                    b.Append(v).Append(".m_Elements").Append("[").Append(bi).Append("])")
                     .Insert(0, ")").Insert(0, GetCastString(_element.GetType())).Insert(0, "((").AppendLine(".Deserialize(reader);");
                    builder.Append(b);
                    names.Push((t, v, bi + 1));
                }
                else
                {
                    var varBuilder = new StringBuilder();
                    (_, string v, int bi) = names.Count == 0 ? ("", rootName, 0) : names.Peek();
                    var vb = $"element{ni++}";
                    varBuilder.Append("var ").Append(vb).Append(" = ");
                    if (v != rootName) varBuilder.Append("(").Append(GetCastString(_element.GetType())).Append(") ");
                    varBuilder.Append(v);
                    if (v != rootName) varBuilder.Append(".m_Elements").Append("[").Append(bi).Append("]");
                    varBuilder.AppendLine(";");
                    builder.Append(varBuilder);
                    names.Push((GetCastString(_element.GetType()), vb, 0));
                }
                return Navigation.Continue;
            }, _element =>
            {
                if (!(_element is PropertyBase))
                {
                    names.Pop();
                    if (names.Count > 0)
                    {
                        (string n, string v, int bi) = names.Pop();
                        names.Push((n, v, bi + 1));
                    }
                }
                return default;
            });
            builder.Append("\n#endregion");
            string parentFolder = Directory.GetParent(Application.dataPath).FullName,
                   fileName = Path.ChangeExtension(Path.Combine(parentFolder, "inline"), "txt");
            File.WriteAllText(fileName, builder.ToString());
        }

        private static Type GetBaseElementType(Type t)
        {
            Type p = null;
            while (t != typeof(ElementBase))
            {
                p = t;
                t = t?.BaseType;
            }
            return p;
        }
    }
}