using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace LiteDBPad
{
    public class DynamicDataContextDriver : LINQPad.Extensibility.DataContext.DynamicDataContextDriver
    {
        public DynamicDataContextDriver()
        {
            DumpableBsonDocument.RegisterSerializer();
        }

        public override string Author
        {
            get
            {
                return "adospace";
            }
        }

        public override string Name
        {
            get
            {
                return "LiteDB Dynamic Context for LinqPad";
            }
        }

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

        public override string GetConnectionDescription(IConnectionInfo cxInfo)
        {
            return new ConnectionProperties(cxInfo).Filename;
        }

        public override List<ExplorerItem> GetSchemaAndBuildAssembly(IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
        {
            var connectionProperties = new ConnectionProperties(cxInfo);

            string code;
            using (var generator = new CodeGenerator(connectionProperties, nameSpace, typeName))
                code = generator.TransformText();

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

            var customType = results.CompiledAssembly.GetType(string.Concat(nameSpace, ".", typeName));

            if (customType == null)
                throw new InvalidOperationException();

            return GetSchema(cxInfo, customType);
        }

        private List<ExplorerItem> GetSchema(IConnectionInfo cxInfo, Type customType)
        {
            // Return the objects with which to populate the Schema Explorer by reflecting over customType.

            // We'll start by retrieving all the properties of the custom type that implement IEnumerable<T>:
            var topLevelProps =
            (
                 from prop in customType.GetProperties()
                 where prop.PropertyType != typeof(string)

                    // Display all properties of type IEnumerable<T> (except for string!)
                    let ienumerableOfT = prop.PropertyType.GetInterface("System.Collections.Generic.IEnumerable`1")
                 where ienumerableOfT != null

                 orderby prop.Name

                 select new ExplorerItem(prop.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                 {
                     IsEnumerable = true,
                     ToolTipText = FormatTypeName(prop.PropertyType, false),

                        // Store the entity type to the Tag property. We'll use it later.
                        Tag = ienumerableOfT.GetGenericArguments()[0]
                 }

            ).ToList();

            // Create a lookup keying each element type to the properties of that type. This will allow
            // us to build hyperlink targets allowing the user to click between associations:
            var elementTypeLookup = topLevelProps.ToLookup(tp => (Type)tp.Tag);

            // Populate the columns (properties) of each entity:
            foreach (ExplorerItem table in topLevelProps)
                table.Children = ((Type)table.Tag)
                     .GetProperties()
                     .Select(childProp => GetChildItem(elementTypeLookup, childProp))
                     .OrderBy(childItem => childItem.Kind)
                     .ToList();

            return topLevelProps;
        }

        ExplorerItem GetChildItem(ILookup<Type, ExplorerItem> elementTypeLookup, PropertyInfo childProp)
        {
            // If the property's type is in our list of entities, then it's a Many:1 (or 1:1) reference.
            // We'll assume it's a Many:1 (we can't reliably identify 1:1s purely from reflection).
            if (elementTypeLookup.Contains(childProp.PropertyType))
                return new ExplorerItem(childProp.Name, ExplorerItemKind.ReferenceLink, ExplorerIcon.ManyToOne)
                {
                    HyperlinkTarget = elementTypeLookup[childProp.PropertyType].First(),
                    // FormatTypeName is a helper method that returns a nicely formatted type name.
                    ToolTipText = FormatTypeName(childProp.PropertyType, true)
                };

            // Is the property's type a collection of entities?
            Type ienumerableOfT = childProp.PropertyType.GetInterface("System.Collections.Generic.IEnumerable`1");
            if (ienumerableOfT != null)
            {
                Type elementType = ienumerableOfT.GetGenericArguments()[0];
                if (elementTypeLookup.Contains(elementType))
                    return new ExplorerItem(childProp.Name, ExplorerItemKind.CollectionLink, ExplorerIcon.OneToMany)
                    {
                        HyperlinkTarget = elementTypeLookup[elementType].First(),
                        ToolTipText = FormatTypeName(elementType, true)
                    };
            }

            // Ordinary property:
            return new ExplorerItem(childProp.Name + " (" + FormatTypeName(childProp.PropertyType, false) + ")",
                 ExplorerItemKind.Property, ExplorerIcon.Column);
        }

        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, bool isNewConnection)
        {
            var dlg = new ConnectionDialog();
            var connectionProperties = new ConnectionProperties(cxInfo);
            if (!isNewConnection)
                dlg.ViewModel.LoadFrom(connectionProperties);

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                dlg.ViewModel.SaveTo(connectionProperties);
                return true;
            }

            return false;
        }
    }
}
