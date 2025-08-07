using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Graphing.IO;
using UnityEngine;

namespace MiniExcelDomain
{
    public class MiniExcelWorker : MarshalByRefObject, KerbalWindTunnel.IDomainWriter
    {
        bool loaded = false;
        private ISpreadsheetWriter writer;
        public string ListAssemblies()
        {
            return string.Join("\n", AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName));
        }
        private Exception LoadAssembly(string assemblyName, string assemblyFile, string[] dlls)
        {
            int simpleNameLength = assemblyName.IndexOf(',');
            if (simpleNameLength < 0)
                simpleNameLength = assemblyName.Length;
            string simpleName = assemblyName.Substring(0, simpleNameLength);
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith(simpleName + ",")))
                return null;
            if (string.IsNullOrEmpty(simpleName) || simpleNameLength == assemblyName.Length)
            {
                try
                {
                    Assembly.LoadFile(dlls.FirstOrDefault(s => s.Contains(assemblyFile)));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return ex;
                }
                return null;
            }
            try
            {
                Assembly.Load(assemblyName);
            }
            catch (FileNotFoundException)
            {
                Debug.Log($"Loading {simpleName} from alternate package.");
                try
                {
                    Assembly.LoadFile(dlls.FirstOrDefault(s => s.Contains(assemblyFile)));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return ex;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return ex;
            }
            return null;
        }
        public Exception SetupDomain(string path)
        {
            if (loaded)
                return null;
            string[] dlls = Directory.GetFiles(path, "*.dll*", SearchOption.AllDirectories);
            Exception exception;
            // System.Xml.Linq
            exception = LoadAssembly(
                "System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Xml.Linq.dll", dlls);
            if (exception != null)
                return exception;
            // System.Numerics
            exception = LoadAssembly(
                "System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Numerics.dll", dlls);
            if (exception != null)
                return exception;
            // System.IO.Compression
            exception = LoadAssembly(
                "System.IO.Compression, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.IO.Compression.dll", dlls);
            if (exception != null)
                return exception;
            // MiniExcelInterface
            exception = LoadAssembly(
                "MiniExcelInterface",
                "MiniExcelInterface.dll", dlls);
            if (exception != null)
                return exception;
            // MiniExcel
            exception = LoadAssembly(
                "MiniExcel, Version=1.41.2.0, Culture=neutral, PublicKeyToken=e7310002a53eac39",
                "MiniExcel.dll", dlls);
            if (exception != null)
                return exception;

            loaded = true;
            return null;
        }
#if DEBUG
        public bool CheckAssembly(string assemblyName)
            => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.Contains(assemblyName));
#endif

        public Exception Write(string path, string sheet, object data, SpreadsheetOptions? options = null)
        {
            try
            {
                if (writer == null)
                    writer = new MiniExcelInterface.MiniExcelWriter();
                writer.Write(path, sheet, data, options);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        public async void WriteToCSV(string path, System.Data.DataTable table, bool printHeader, string sheetName = "")
        {
            await MiniExcelLibs.MiniExcel.SaveAsAsync(path, table, excelType: MiniExcelLibs.ExcelType.CSV, printHeader: printHeader, sheetName: sheetName, configuration: new MiniExcelLibs.Csv.CsvConfiguration() { FastMode = true });
        }
    }
}
