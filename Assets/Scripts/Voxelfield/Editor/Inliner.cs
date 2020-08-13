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
    public static class Inliner
    {
        [MenuItem("Voxelfield/Inline")]
        private static void CustomInline()
        {
            var client = new Client(VoxelfieldComponents.SessionElements, null, new ClientInjector());
            ServerSessionContainer element = client.ReceivedServerSession;
            Inline(element);

            // Inline(new MapContainer());
        }

        private static void Inline(ElementBase element)
        {
            var names = new Stack<(string, int)>();
            var builder = new StringBuilder();
            string GetCastString(Type type) => type.Name;
            element.Navigate(_element =>
            {
                if (_element.TryAttribute(out NoSerializationAttribute attribute) && !attribute.ExceptRead) return Navigation.SkipDescendents;
                // names.Push((GetBaseElementType(_element.GetType()).Name, breadthIndex));
                if (_element is PropertyBase)
                {
                    (string n, int bi) = names.Peek();
                    var propertyBuilder = new StringBuilder();
                    (string, int)[] stack = names.Reverse().Append((GetCastString(_element.GetType()), bi)).ToArray();
                    for (var di = 1; di < stack.Length; di++)
                    {
                        if (di == 1) propertyBuilder.Append("session");
                        propertyBuilder.Append("[").Append(stack[di - 1].Item2).Append("])").Insert(0, ")").Insert(0, stack[di].Item1).Insert(0, "((");
                        if (di == stack.Length - 1) propertyBuilder.Append(".Deserialize(reader);");
                    }
                    builder.AppendLine(propertyBuilder.ToString());
                    names.Pop();
                    names.Push((n, bi + 1));
                }
                else
                {
                    names.Push((GetBaseElementType(_element.GetType()).Name, 0));
                }
                return Navigation.Continue;
            }, _element =>
            {
                if (!(_element is PropertyBase))
                {
                    names.Pop();
                    if (names.Count > 0)
                    {
                        (string n, int bi) = names.Pop();
                        names.Push((n, bi + 1));
                    }
                }
                return default;
            });
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