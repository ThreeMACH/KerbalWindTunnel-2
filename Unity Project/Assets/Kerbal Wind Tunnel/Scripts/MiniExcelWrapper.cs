using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Graphing.IO;
using UnityEngine;

namespace KerbalWindTunnel
{
    public class MiniExcelWrapper : ISpreadsheetWriter, IDisposable
    {
        private readonly AppDomain excelDomain;
        private IDomainWriter worker;
        public MiniExcelWrapper()
        {
            excelDomain = AppDomain.CreateDomain("Excel Domain");

            string path = typeof(MiniExcelWrapper).Assembly.Location;
            char pathChar = Path.DirectorySeparatorChar;

            string dir = Directory.GetFiles(path.Substring(0, path.LastIndexOf(pathChar)), "*.dll", SearchOption.AllDirectories).First(s => s.Contains("MiniExcelDomainWorker"));
            worker = (IDomainWriter)excelDomain.CreateInstanceFromAndUnwrap(dir, "MiniExcelDomain.MiniExcelWorker");

            path = string.Join($"{pathChar}", path.Substring(0, path.LastIndexOf(pathChar)), "References");
            Exception loadException = worker.SetupDomain(path);
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
            Debug.Log("[KWT] Writing data.");
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
            Debug.Log("[KWT] Done writing data.");
#endif
        }

        public async Task WriteToCSV(string path, System.Data.DataTable table, bool printHeader, string sheetName = "")
        {
            if (worker == null)
                throw new ObjectDisposedException(nameof(worker));
#if DEBUG
            Debug.Log("[KWT] Writing data.");
#endif
            try
            {
                Task result = Task.Run(() => worker.WriteToCSV(path, table, printHeader, sheetName));
                await result;
                if (result.Exception != null)
                    throw new AggregateException(result.Exception);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
#if DEBUG
            Debug.Log("[KWT] Done writing data.");
#endif
        }

        public void Dispose()
        {
            worker = null;
            AppDomain.Unload(excelDomain);
        }
    }

    public interface IDomainWriter
    {
        public string ListAssemblies();
        public Exception SetupDomain(string path);
#if DEBUG
        public bool CheckAssembly(string assemblyName);
#endif
        public Exception Write(string path, string sheet, object data, SpreadsheetOptions? options = null);
        public void WriteToCSV(string path, System.Data.DataTable table, bool printHeader, string sheetName = "");
    }
}
