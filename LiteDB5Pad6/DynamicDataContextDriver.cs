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

namespace LiteDBPad
{
    public class DynamicDataContextDriver : LINQPad.Extensibility.DataContext.DynamicDataContextDriver
    {
        public DynamicDataContextDriver()
        {
            DumpableBsonDocument.RegisterSerializer();
        }

        public override string Author => "adospace";

        public override string Name => "LiteDB v.5+ Dynamic Context for LinqPad 6 (.NET Core)";

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

            Log("Generating code");
            string code;
            var properties = new List<PropertyInfo>();
            using (var generator = new LiteDBPad.CodeGenerator(connectionProperties, @namespace, typeName))
            {
                //properties.AddRange(generator.CapitalizedCollectionNames.Select(_ => new PropertyInfo(_, typeof(LiteCollection<DumpableBsonDocument>))));
                properties.AddRange(generator.CapitalizedCollectionNames.Select(_ => new PropertyInfo("All" + _, typeof(DumpableBsonDocumentCollection))));
                code = generator.TransformText();
            }

            var assembliesToReference = GetCoreFxReferenceAssemblies().ToList();
            assembliesToReference.Add(typeof(DynamicDataContextDriver).Assembly.Location);
            assembliesToReference.Add(typeof(LiteDB.LiteDatabase).Assembly.Location);

            Log($"Assemblies: {string.Join(Environment.NewLine, assembliesToReference)}");

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

#if DEBUG
            Log("Found {0} items", items.Count);
#endif

            return items;
        }

        private List<ExplorerItem> GetSchema(IConnectionInfo cxInfo, PropertyInfo[] properties)
        {
            var topLevelProps =
            (
                 from prop in properties
                 orderby prop.Name

                 select new ExplorerItem(prop.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                 {
                     IsEnumerable = true,
                     ToolTipText = FormatTypeName(prop.PropertyType, false),
                 }

            ).ToList();


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

#if NETCORE
        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
#else
        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, bool isNewConnection)
#endif
        {
            var dlg = new ConnectionDialog();
            var connectionProperties = new ConnectionProperties(cxInfo);
#if NETCORE
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
