using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.IO;
using LiteDB;
#if NETCOREAPP3_0
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
#endif

namespace LiteDBPad
{
    public class DynamicDataContextDriver : LINQPad.Extensibility.DataContext.DynamicDataContextDriver
    {
        public DynamicDataContextDriver()
        {
            DumpableBsonDocument.RegisterSerializer();
        }

        public override string Author => "adospace";

        public override string Name => "LiteDB Dynamic Context for LinqPad";

        public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo)
        {
            return new string[]
            {
                typeof(LiteDB.LiteDatabase).Assembly.Location
            };
        }

        public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo)
        {
            return new string[]
            {
                "LiteDB",
                "LiteDBPad",
#if NETCOREAPP3_0
                "LiteDBPad6",
#endif
            };
        }

        public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo)
        {
            return new object[] { new ConnectionProperties(cxInfo).GetConnectionString() };
        }

        public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo)
        {
            return new ParameterDescriptor[] { new ParameterDescriptor("connectionString", typeof(LiteDB.ConnectionString).FullName) };
        }

        public override string GetConnectionDescription(IConnectionInfo cxInfo) => new ConnectionProperties(cxInfo).Filename;

        public override List<ExplorerItem> GetSchemaAndBuildAssembly(IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string @namespace, ref string typeName)
        {
            try
            {
                return InternalGetSchemaAndBuildAssembly(cxInfo, assemblyToBuild, ref @namespace, ref typeName);
            }
            catch(Exception ex)
            {
                Log(ex.ToString());
                throw;
            }
        }

        private class PropertyInfo
        {
            public PropertyInfo(string name, Type propertyType)
            {
                Name = name;
                PropertyType = propertyType;
            }

            public string Name { get; }
            public Type PropertyType { get; }
        }

        private List<ExplorerItem> InternalGetSchemaAndBuildAssembly(IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string @namespace, ref string typeName)
        {
            var connectionProperties = new ConnectionProperties(cxInfo);

#if NETCOREAPP3_0
            Log("Generating code");
            string code;
            var properties = new List<PropertyInfo>();
            using (var generator = new LiteDBPad6.CodeGenerator(connectionProperties, @namespace, typeName))
            {
                //properties.AddRange(generator.CapitalizedCollectionNames.Select(_ => new PropertyInfo(_, typeof(LiteCollection<DumpableBsonDocument>))));
                properties.AddRange(generator.CapitalizedCollectionNames.Select(_ => new PropertyInfo("All" + _, typeof(DumpableBsonDocumentCollection))));
                code = generator.TransformText();
            }

            var assembliesToReference = GetCoreFxReferenceAssemblies().ToList();
            assembliesToReference.Add(typeof(DynamicDataContextDriver).Assembly.Location);
            assembliesToReference.Add(typeof(LiteDB.LiteDatabase).Assembly.Location);

            //Log($"Assemblies: {string.Join(Environment.NewLine, assembliesToReference)}");

            var compileResult = CompileSource(new CompilationInput
            {
                FilePathsToReference = assembliesToReference.ToArray(),
                OutputPath = assemblyToBuild.CodeBase,
                SourceCode = new[] { code }
            });

            if (compileResult.Errors.Length > 0)
            {
#if DEBUG
                Log("Error compiling generated code");
                Log(string.Join(Environment.NewLine, compileResult.Errors));
#endif
                throw new Exception($"Cannot compile typed context: {string.Join(Environment.NewLine, compileResult.Errors)}");
            }

            var items = GetSchema(cxInfo, properties.ToArray());

#else
            string code;
            //using (var generator = new LiteDBPad.CodeGenerator(connectionProperties, @namespace, typeName))
            //    code = generator.TransformText();
            var properties = new List<PropertyInfo>();
            using (var generator = new LiteDBPad.CodeGenerator(connectionProperties, @namespace, typeName))
            {
                //properties.AddRange(generator.CapitalizedCollectionNames.Select(_ => new PropertyInfo(_, typeof(LiteCollection<DumpableBsonDocument>))));
                properties.AddRange(generator.CapitalizedCollectionNames.Select(_ => new PropertyInfo("All" + _, typeof(DumpableBsonDocumentCollection))));
                code = generator.TransformText();
            }
            
            // Use the CSharpCodeProvider to compile the generated code:
            CompilerResults results;
            using (var codeProvider = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } }))
            {
                var options = new CompilerParameters(
                     "System.dll System.Core.dll System.Xml.dll System.Data.Services.Client.dll".Split(),
                     assemblyToBuild.CodeBase,
                     true);

                options.ReferencedAssemblies.Add(typeof(LiteDB.LiteDatabase).Assembly.Location);
                options.ReferencedAssemblies.Add(typeof(LiteDBPad.DumpableBsonDocument).Assembly.Location);
                options.ReferencedAssemblies.Add(typeof(LINQPad.ICustomMemberProvider).Assembly.Location);

                results = codeProvider.CompileAssemblyFromSource(options, code);
            }
            if (results.Errors.Count > 0)
                throw new Exception
                     ("Cannot compile typed context: " + results.Errors[0].ErrorText + " (line " + results.Errors[0].Line + ")");

            //var customType = results.CompiledAssembly.GetType(string.Concat(@namespace, ".", typeName));

            //if (customType == null)
            //    throw new InvalidOperationException();

            var items = GetSchema(cxInfo, properties.ToArray());
#endif

#if DEBUG
            Log("Found {0} items", items.Count);
#endif

            return items;
        }

        private List<ExplorerItem> GetSchema(IConnectionInfo cxInfo, PropertyInfo[] properties)
        {
//#if DEBUG
//            var topLevelProperties = from prop in properties
//                                     where prop.PropertyType != typeof(string)

//                                     // Display all properties of type IEnumerable<T> (except for string!)
//                                     let ienumerableOfT = prop.PropertyType.GetInterface("System.Collections.Generic.IEnumerable`1")
//                                     where ienumerableOfT != null
//                                     select prop;
//            foreach (var tp in topLevelProperties)
//                Log("TopLevelProperty={0}", tp.Name);
//#endif
            // Return the objects with which to populate the Schema Explorer by reflecting over customType.

                                     // We'll start by retrieving all the properties of the custom type that implement IEnumerable<T>:
            var topLevelProps =
            (
                 from prop in properties
                 //where prop.PropertyType != typeof(string)

                 //   // Display all properties of type IEnumerable<T> (except for string!)
                 //   let ienumerableOfT = prop.PropertyType.GetInterface("System.Collections.Generic.IEnumerable`1")
                 //where ienumerableOfT != null

                 orderby prop.Name

                 select new ExplorerItem(prop.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                 {
                     IsEnumerable = true,
                     ToolTipText = FormatTypeName(prop.PropertyType, false),

                        // Store the entity type to the Tag property. We'll use it later.
                        //Tag = ienumerableOfT.GetGenericArguments()[0]
                 }

            ).ToList();

            // Create a lookup keying each element type to the properties of that type. This will allow
            // us to build hyperlink targets allowing the user to click between associations:
            //var elementTypeLookup = topLevelProps.ToLookup(tp => (Type)tp.Tag);

            // Populate the columns (properties) of each entity:
            //foreach (ExplorerItem table in topLevelProps)
            //    table.Children = ((Type)table.Tag)
            //         .GetProperties()
            //         .Select(childProp => GetChildItem(elementTypeLookup, childProp))
            //         .OrderBy(childItem => childItem.Kind)
            //         .ToList();

            return topLevelProps;
        }

        //ExplorerItem GetChildItem(ILookup<Type, ExplorerItem> elementTypeLookup, PropertyInfo childProp)
        //{
        //    // If the property's type is in our list of entities, then it's a Many:1 (or 1:1) reference.
        //    // We'll assume it's a Many:1 (we can't reliably identify 1:1s purely from reflection).
        //    if (elementTypeLookup.Contains(childProp.PropertyType))
        //        return new ExplorerItem(childProp.Name, ExplorerItemKind.ReferenceLink, ExplorerIcon.ManyToOne)
        //        {
        //            HyperlinkTarget = elementTypeLookup[childProp.PropertyType].First(),
        //            // FormatTypeName is a helper method that returns a nicely formatted type name.
        //            ToolTipText = FormatTypeName(childProp.PropertyType, true)
        //        };

        //    // Is the property's type a collection of entities?
        //    Type ienumerableOfT = childProp.PropertyType.GetInterface("System.Collections.Generic.IEnumerable`1");
        //    if (ienumerableOfT != null)
        //    {
        //        Type elementType = ienumerableOfT.GetGenericArguments()[0];
        //        if (elementTypeLookup.Contains(elementType))
        //            return new ExplorerItem(childProp.Name, ExplorerItemKind.CollectionLink, ExplorerIcon.OneToMany)
        //            {
        //                HyperlinkTarget = elementTypeLookup[elementType].First(),
        //                ToolTipText = FormatTypeName(elementType, true)
        //            };
        //    }

        //    // Ordinary property:
        //    return new ExplorerItem(childProp.Name + " (" + FormatTypeName(childProp.PropertyType, false) + ")",
        //         ExplorerItemKind.Property, ExplorerIcon.Column);
        //}

#if NETCOREAPP3_0
        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
#else
        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, bool isNewConnection)
#endif
        {
            var dlg = new ConnectionDialog();
            var connectionProperties = new ConnectionProperties(cxInfo);
#if NETCOREAPP3_0
            if (!dialogOptions.IsNewConnection)
#else
            if (!isNewConnection)
#endif
                dlg.ViewModel.LoadFrom(connectionProperties);

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                dlg.ViewModel.SaveTo(connectionProperties);
                return true;
            }

            return false;
        }

        public static void Log(string message, params object[] values)
        {
#if DEBUG
            File.AppendAllText(@"d:\temp\litedbpad.log", $"{DateTime.Now} {string.Format(message, values)}{Environment.NewLine}");
#endif
        }
    }
}
