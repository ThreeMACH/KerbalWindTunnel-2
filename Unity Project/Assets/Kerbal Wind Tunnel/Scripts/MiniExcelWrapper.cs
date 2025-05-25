using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using Graphing.IO;

namespace KerbalWindTunnel
{
    public class MiniExcelWrapper : ISpreadsheetWriter, IDisposable
    {
        private readonly AppDomain excelDomain;
        private readonly Assembly ownAssembly;
        private MiniExcelWorker worker;
        public MiniExcelWrapper()
        {
            excelDomain = AppDomain.CreateDomain("Excel Domain");
            ownAssembly = typeof(MiniExcelWrapper).Assembly;
            worker = (MiniExcelWorker)excelDomain.CreateInstanceFromAndUnwrap(ownAssembly.Location, typeof(MiniExcelWorker).FullName);
            string path = ownAssembly.Location;
            char pathChar = Path.DirectorySeparatorChar;
            path = string.Join($"{pathChar}", path.Substring(0, path.LastIndexOf(pathChar)), "References");
            worker.LoadAssemblies(path);
#if DEBUG
            Debug.Log(worker.ListAssemblies());
#endif
        }

        public void Write(string path, string sheet, object data, SpreadsheetOptions? options = null)
        {
            if (!(data is ISerializable))
                throw new SerializationException($"{nameof(data)} must implement {nameof(ISerializable)} in order to cross to the other AppDomain.");
            if (worker == null)
                throw new ObjectDisposedException(nameof(worker));
            Exception exception = worker.Write(path, sheet, data, options);
            if (exception != null)
                throw new AggregateException(exception);
        }

        public void Dispose()
        {
            worker = null;
            AppDomain.Unload(excelDomain);
        }
    }

    public class MiniExcelWorker : MarshalByRefObject
    {
        bool loaded = false;
        private ISpreadsheetWriter writer;
        /*public void PrintDomain()
        {
            UnityEngine.Debug.LogFormat("Object is executing in AppDomain \"{0}\"", AppDomain.CurrentDomain.FriendlyName);
            if (!AppDomain.CurrentDomain.FriendlyName.Contains("Excel"))
                return;
            return;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                UnityEngine.Debug.Log(assembly.FullName);
        }*/
        internal string ListAssemblies()
        {
            return string.Join("\n", AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName));
        }
        public void LoadAssemblies(string path)
        {
            if (loaded)
                return;
            string[] dlls = Directory.GetFiles(path, "*.dll*", SearchOption.AllDirectories);
            // System.Xml.Linq
            try
            {
                Assembly.Load("System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            }
            catch (FileNotFoundException)
            {
                Assembly.LoadFile(dlls.FirstOrDefault(s => s.EndsWith("System.Xml.Linq.dll.ignore")));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            // System.Numerics
            try
            {
                Assembly.Load("System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            }
            catch (FileNotFoundException)
            {
                Assembly.LoadFile(dlls.FirstOrDefault(s => s.EndsWith("System.Numerics.dll")));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            // MiniExcel
            try
            {
                Assembly.Load("MiniExcel, Version=1.41.2.0, Culture=neutral, PublicKeyToken=e7310002a53eac39");
            }
            catch (FileNotFoundException)
            {
                Assembly.LoadFile(dlls.FirstOrDefault(s => s.EndsWith("MiniExcel.dll")));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            loaded = true;
        }
        public bool CheckXml()
            => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.Contains("System.Xml.Linq"));

        public Exception Write(string path, string sheet, object data, SpreadsheetOptions? options = null)
        {
            if (writer == null)
                writer = new MiniExcelWriter();
            try
            {
                writer.Write(path, sheet, data, options);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
    }
}
