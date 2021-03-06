﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorldDataExporter.Model;

namespace RimWorldDataExporter.Helper.Serialization {
    class ITypeable { }

    static class TypeScriptHelper {
        private readonly static BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public readonly static List<Type> additinalTypes = new List<Type> {
            typeof(FloatRange),
        };

        public readonly static HashSet<Type> enumTypes = new HashSet<Type>();

        #region To TypeScript type name

        private static Dictionary<Type, string> basicTypeMap = new Dictionary<Type, string> {
            { typeof(bool), "boolean" },
            { typeof(int), "number" },
            { typeof(float), "number" },
            { typeof(double), "number" },
            { typeof(string), "string | null" },
            { typeof(Color), "string | null" }
        };

        private static string IListToTypeName<T> () {
            return $"ReadonlyArray<{typeof(T).ToTypeName()}> | null";
        }

        private static string IDictToTypeName<TKey, TValue>() {
            return $"ReadonlyDict<{typeof(TValue).ToTypeName()}> | null";
        }

        public static string ToTypeName(this Type type) {
            if (basicTypeMap.TryGetValue(type, out string typeName)) {
                return typeName;
            }

            if (type.IsSubclassOf(typeof(Def))) {
                return "string | null";
            }

            if (type == typeof(EObj)
                || type.IsSubclassOf(typeof(EObj)) 
                || type.IsSubclassOf(typeof(ITypeable))
                || additinalTypes.Contains(type)
                ) {
                return type.Name;
            }

            if (type.IsEnum) {
                enumTypes.Add(type);
                return type.Name;
            }
            
            foreach (Type iType in type.GetInterfaces()) {
                if (iType.IsGenericType) {
                    Type genericTypeDefinition = iType.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(IList<>)) {
                        return (string)typeof(TypeScriptHelper).GetMethod("IListToTypeName", BindingFlags.Static | BindingFlags.NonPublic)
                            .MakeGenericMethod(iType.GetGenericArguments())
                            .Invoke(null, null);
                    }
                    if (genericTypeDefinition == typeof(IDictionary<,>)) {
                        return (string)typeof(TypeScriptHelper).GetMethod("IDictToTypeName", BindingFlags.Static | BindingFlags.NonPublic)
                            .MakeGenericMethod(iType.GetGenericArguments())
                            .Invoke(null, null);
                    }
                }
            }
            
            return "any";
        }

        #endregion

        #region Save Declaration

        private static Dictionary<string, Dictionary<string, StringBuilder>> declarationMap = new Dictionary<string, Dictionary<string, StringBuilder>> {
            { "Database", new Dictionary<string, StringBuilder>() },
            { "Langbase", new Dictionary<string, StringBuilder>() },
        };

        public static void AddEbaseDeclaration(string ebase, string category, StringBuilder sb) {
            if (declarationMap[ebase].ContainsKey(category)) {
                return;
            }
            declarationMap[ebase].Add(category, sb);
        }

        public static void SaveAllTypesDeclaration(string path) {
            SaveDeclaration(Path.Combine(path, "base.d.ts"), new[] { typeof(EData), typeof(ELang), typeof(EAggr) }, typeof(EObj), "base");
            SaveAllEObjSubclassesDeclaration(Path.Combine(path, "data.d.ts"), typeof(EData), "data");
            SaveAllEObjSubclassesDeclaration(Path.Combine(path, "lang.d.ts"), typeof(ELang), "language");
            SaveAllEObjSubclassesDeclaration(Path.Combine(path, "aggr.d.ts"), typeof(EAggr), "aggregation");

            SaveAllITypeableClassesDeclaration(Path.Combine(path, "basic.d.ts"));
            SaveAllEnumDeclaration(Path.Combine(path, "enum.d.ts"));
        }

        private static void SaveDeclaration(string path, Type[] types, Type baseType, string comment) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"// Declaration for {comment} types.");

            sb.AppendLine();
            sb.AppendLine($"declare interface {baseType.Name} {{");
            foreach (var fieldInfo in baseType.GetFields(flags)) {
                sb.AppendLine($"  readonly {fieldInfo.Name}: {fieldInfo.FieldType.ToTypeName()};");
            }
            sb.AppendLine("}");

            foreach (Type type in types) {
                sb.AppendLine();
                if (type.IsSubclassOf(baseType)) {
                    sb.AppendLine($"declare interface {type.Name} extends {baseType.Name} {{");
                } else {
                    sb.AppendLine($"declare interface {type.Name} {{");
                }
                foreach (var fieldInfo in type.GetFields(flags)) {
                    sb.AppendLine($"  readonly {fieldInfo.Name}: {fieldInfo.FieldType.ToTypeName()};");
                }
                sb.AppendLine("}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        private static void SaveAllEObjSubclassesDeclaration(string path, Type baseType, string comment) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"// Declaration for {comment} types.");

            var allSubclasses = baseType.AllSubclasses().ToList();
            allSubclasses.Sort((Type a, Type b) => {
                return a.Name.CompareTo(b.Name);
            });

            foreach (var subclass in allSubclasses) {
                sb.AppendLine();
                sb.AppendLine($"declare interface {subclass.Name} extends {subclass.BaseType.Name} {{");
                foreach (var fieldInfo in subclass.GetFields(flags)) {
                    sb.AppendLine($"  readonly {fieldInfo.Name}: {fieldInfo.FieldType.ToTypeName()};");
                }
                sb.AppendLine("}");
            }

            if (baseType.IsSubclassOf(typeof(EObj))) {
                string ebase = baseType == typeof(EData) ? "Database" : "Langbase";
                foreach (var kvp in declarationMap[ebase]) {
                    sb.AppendLine();
                    sb.AppendLine(kvp.Value.ToString());
                }
            }

            File.WriteAllText(path, sb.ToString());
        }

        private static void SaveAllITypeableClassesDeclaration(string path) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// Declaration for basic model types.");

            sb.AppendLine("declare interface ReadonlyDict<T> {");
            sb.AppendLine("  readonly [key: string]: T");
            sb.AppendLine("}");

            var allITypeableClasses = typeof(ITypeable).AllSubclassesNonAbstract().Concat(additinalTypes).ToList();
            allITypeableClasses.Sort((Type a, Type b) => {
                return a.Name.CompareTo(b.Name);
            });

            foreach (var iTypeableClass in allITypeableClasses) {
                sb.AppendLine();
                sb.AppendLine($"declare interface {iTypeableClass.Name} {{");
                foreach (var fieldInfo in iTypeableClass.GetFields(flags)) {
                    sb.AppendLine($"  readonly {fieldInfo.Name}: {fieldInfo.FieldType.ToTypeName()};");
                }
                sb.AppendLine("}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        private static void SaveAllEnumDeclaration(string path) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// Declaration for enum types.");

            var allEnumTypes = enumTypes.ToList();
            allEnumTypes.Sort((Type a, Type b) => {
                return a.Name.CompareTo(b.Name);
            });

            foreach (var enumType in allEnumTypes) {
                bool isFlag = Attribute.GetCustomAttribute(enumType, typeof(FlagsAttribute)) != null;

                sb.AppendLine();
                sb.AppendLine($"declare enum {enumType.Name} {{");
                foreach (var name in Enum.GetNames(enumType)) {
                    if (isFlag) {
                        sb.AppendLine($"  {name} = 0x{Convert.ToUInt64(Enum.Parse(enumType, name)).ToString("x")},");
                    } else {
                        sb.AppendLine($"  {name} = {Convert.ToUInt64(Enum.Parse(enumType, name))},");
                    }
                }
                sb.AppendLine("}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        #endregion
    }
}
