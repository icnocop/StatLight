using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StatLight.Core.Events.Messaging;

namespace StatLight.Client.Harness.Hosts
{
    public class ThisXapData : ILoadedXapData
    {
        public ThisXapData(string entryPointAssembly, IEnumerable<string> testAssemblyFormalNames)
        {
            if (entryPointAssembly == null) throw new ArgumentNullException("entryPointAssembly");
            if (testAssemblyFormalNames == null) throw new ArgumentNullException("testAssemblyFormalNames");

            Server.Debug("ThisXapData.Expected EntryPointAssembly - {0}".FormatWith(entryPointAssembly));
            Server.Debug("ThisXapData - looking for test assemblies");
            Server.Debug("testAssemblyFormalNames.Count() =" + testAssemblyFormalNames.Count());

            _testAssemblies = new List<Assembly>();

            foreach (string name in testAssemblyFormalNames)
            {
                if (IsSpecialAssembly(name.Substring(0, name.IndexOf(','))))
                {
                    // do not load it
                    continue;
                }

                Server.Debug("ThisXapData - Loading assembly - {0}".FormatWith(name));
                Assembly assembly = Assembly.Load(name);
                _testAssemblies.Add(assembly);
            }

            foreach (Assembly t in _testAssemblies)
            {
                if (t.FullName == entryPointAssembly)
                {
                    Server.Debug("ThisXapData.FoundFile (EntryPointAssembly) - {0}".FormatWith(t.FullName));
                    _entryPointAssembly = t;
                }

                Server.Debug("ThisXapData.FoundFile - {0}".FormatWith(t.FullName));
            }
        }

        private static bool IsSpecialAssembly(string name)
        {
            if (name.EndsWith(".resources"))
                return true;

            var specialAssemblies = new[]
            {
                "System.Xml.Linq.dll",
                "System.Xml.Serialization.dll",
                "Microsoft.Silverlight.Testing.dll",
                "Microsoft.VisualStudio.QualityTools.UnitTesting.Silverlight.dll",
                "StatLight.Client.Harness.MSTest.dll",
                "StatLight.Client.Harness.dll",
            };

            foreach (var specialAssembly in specialAssemblies)
            {
                if (name.Equals(specialAssembly, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private readonly List<Assembly> _testAssemblies;
        private readonly Assembly _entryPointAssembly;

        public IEnumerable<Assembly> TestAssemblies
        {
            get { return _testAssemblies; }
        }

        public Assembly EntryPointAssembly
        {
            get { return _entryPointAssembly; }
        }
    }
}