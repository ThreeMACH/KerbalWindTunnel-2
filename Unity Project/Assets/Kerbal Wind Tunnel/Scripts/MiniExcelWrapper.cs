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
            Exception loadException = worker.LoadAssemblies(path);
            if (loadException != null)
                throw new AggregateException(loadException);
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
#if DEBUG
            Debug.Log("Writing data.");
#endif
            try
            {
                Exception exception = worker.Write(path, sheet, data, options);
                if (exception != null)
                    throw new AggregateException(exception);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
#if DEBUG
            Debug.Log("Done writing data.");
#endif
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
        private Assembly MiniExcelAssembly;
        internal string ListAssemblies()
        {
            return string.Join("\n", AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName));
        }
        public Exception LoadAssemblies(string path)
        {
            if (loaded)
                return null;
            string[] dlls = Directory.GetFiles(path, "*.dll*", SearchOption.AllDirectories);
            // System.Xml.Linq
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith("System.Xml.Linq,")))
            {
                try
                {
                    Assembly.Load("System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                }
                catch (FileNotFoundException)
                {
                    Debug.Log("Loading System.Xml.Linq from alternate package.");
                    Assembly.LoadFile(dlls.FirstOrDefault(s => s.EndsWith("System.Xml.Linq.dll.ignore")));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return ex;
                }
            }
            // System.Numerics
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith("System.Numerics,")))
            {
                try
                {
                    Assembly.Load("System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                }
                catch (FileNotFoundException)
                {
                    Debug.Log("Loading System.Numerics from alternate package.");
                    Assembly.LoadFile(dlls.FirstOrDefault(s => s.EndsWith("System.Numerics.dll")));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return ex;
                }
            }
            // System.IO.Compression
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith("System.IO.Compression,")))
            {
                try
                {
                    Assembly.Load("System.IO.Compression, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                }
                catch (FileNotFoundException)
                {
                    Debug.Log("Loading System.IO.Compression from alternate package.");
                    if (dlls.Any(s => s.EndsWith("System.IO.Compression.dll")))
                        Assembly.LoadFile(dlls.First(s => s.EndsWith("System.IO.Compression.dll")));
                    else
                        Assembly.LoadFile(dlls.FirstOrDefault(s => s.EndsWith("System.IO.Compression.dll.ignore")));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return ex;
                }
            }
            // MiniExcel
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith("MiniExcel,")))
            {
                try
                {
                    MiniExcelAssembly = Assembly.Load("MiniExcel, Version=1.41.2.0, Culture=neutral, PublicKeyToken=e7310002a53eac39");
                }
                catch (FileNotFoundException)
                {
                    MiniExcelAssembly = Assembly.LoadFile(dlls.FirstOrDefault(s => s.EndsWith("MiniExcel.dll")));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return ex;
                }
            }
            loaded = true;
            return null;
        }
#if DEBUG
        public bool CheckXml()
            => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.Contains("System.Xml.Linq"));
#endif

        public Exception Write(string path, string sheet, object data, SpreadsheetOptions? options = null)
        {
            try
            {
                if (writer == null)
                    writer = new MiniExcelWriter();
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
