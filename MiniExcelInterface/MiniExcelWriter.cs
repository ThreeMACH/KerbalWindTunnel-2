using Graphing.IO;
using MiniExcelLibs;
using MiniExcelLibs.OpenXml;

namespace MiniExcelInterface
{
    public class MiniExcelWriter : ISpreadsheetWriter
    {
        /// <summary>
        /// Initializes <see cref="GraphIO.SpreadsheetWriter"/> as a <see cref="MiniExcelWriter"/>.
        /// This only needs to be called once before outputting to any spreadsheets.
        /// </summary>
        public static void InitializeMiniExcelWriter()
            => GraphIO.SpreadsheetWriter = new MiniExcelWriter();

        private static readonly OpenXmlConfiguration defaultConfig = new OpenXmlConfiguration()
        {
            FastMode = true,
            TableStyles = TableStyles.None
        };
        private static readonly OpenXmlConfiguration config2x2 = new OpenXmlConfiguration()
        {
            FastMode = true,
            TableStyles = TableStyles.None,
            AutoFilter = false,
            FreezeRowCount = 2,
            FreezeColumnCount = 2,
        };
        private static readonly OpenXmlConfiguration config1x1 = new OpenXmlConfiguration()
        {
            FastMode = true,
            TableStyles = TableStyles.None,
            AutoFilter = false,
            FreezeRowCount = 1,
            FreezeColumnCount = 1
        };
        public static bool ConfigEquals(SpreadsheetOptions options, OpenXmlConfiguration config)
            => options.filter == config.AutoFilter && options.freezeRowCount == config.FreezeRowCount && options.freezeColumnCount == config.FreezeColumnCount;

        public void Write(string path, string sheet, object data, SpreadsheetOptions? options = null)
        {
            IConfiguration configuration;
            SpreadsheetOptions options_ = options ?? new SpreadsheetOptions();
            if (options == null)
                configuration = defaultConfig;
            else if (ConfigEquals(options_, defaultConfig))
                configuration = defaultConfig;
            else if (ConfigEquals(options_, config2x2))
                configuration = config2x2;
            else if (ConfigEquals(options_, config1x1))
                configuration = config1x1;
            else
                configuration = new OpenXmlConfiguration()
                {
                    FastMode = true,
                    AutoFilter = options_.filter,
                    FreezeRowCount = options_.freezeRowCount,
                    FreezeColumnCount = options_.freezeColumnCount
                };
            MiniExcel.Insert(path, data, sheet, configuration: configuration, printHeader: options_.headers, overwriteSheet: true);
        }
    }
}
