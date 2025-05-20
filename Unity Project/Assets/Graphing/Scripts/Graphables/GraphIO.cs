using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Linq;
using UnityEngine;

namespace Graphing
{
    public static class GraphIO
    {

        public static readonly char[] invalidSheetChars = new char[] { '[', ']', '*', '/', '\\', '?', ':' };
        public enum FileFormat
        {
            CSV,
            [Obsolete]
            XLS,
            XLSX,
            PNG,
            JPG
        }

        /// <summary>
        /// Outputs the object's values to file.
        /// </summary>
        /// <param name="directory">The directory in which to place the file.</param>
        /// <param name="filename">The filename for the file.</param>
        /// <param name="format">The format to be written.</param>
        /// <param name="sheetName">An optional sheet name for within the file.</param>
        /// <param name="resolution">An optional value for the resolution of the target image.</param>
        /// <exception cref="System.NotImplementedException">
        /// Image format is not supported.
        /// or
        /// File format is not supported.
        /// </exception>
        public static void WriteToFile(this IGraphable graph, string directory, string filename, FileFormat format = FileFormat.CSV, string sheetName = "", (int width, int height)? resolution = null)
        {
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            string path = ValidateFilePath(directory, filename, format);

            if (format == FileFormat.CSV && !string.IsNullOrEmpty(sheetName))
            {
                sheetName = sheetName.Replace("/", "-").Replace("\\", "-");
                sheetName = StripInvalidFileChars(sheetName);
                path.Insert(path.Length - 4, sheetName);
            }

            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch (Exception ex) { Debug.Log($"Unable to delete file:{ex.Message}"); }

            switch (format)
            {
                case FileFormat.CSV:
                    graph.WriteToFileCSV(path);
                    break;
                case FileFormat.XLS:
                case FileFormat.XLSX:
                    throw new NotImplementedException();
                    if (string.IsNullOrEmpty(sheetName))
                        sheetName = graph.DisplayName;
                    if (string.IsNullOrEmpty(sheetName))
                        sheetName = "Sheet1";
                    sheetName = StripInvalidSheetChars(sheetName);
                    break;
                case FileFormat.PNG:
                case FileFormat.JPG:
                    Texture2D texture = new Texture2D(resolution?.width ?? 1024, resolution?.height ?? 1024, TextureFormat.ARGB32, false);
                    graph.Draw(ref texture, graph.XMin, graph.XMax, graph.YMin, graph.YMax);
                    byte[] byteStream;
                    switch (format)
                    {
                        case FileFormat.PNG:
                            byteStream = texture.EncodeToPNG();
                            break;
                        case FileFormat.JPG:
                            byteStream = texture.EncodeToJPG();
                            break;
                        default:
                            throw new NotImplementedException("Image format is not supported.");
                    }
                    System.IO.File.WriteAllBytes(path, byteStream);
                    UnityEngine.Object.Destroy(texture);
                    break;
                default:
                    throw new NotImplementedException("File format is not supported.");
            }
        }

        /// <summary>
        /// Outputs the grapher's RenderTexture values to file.
        /// </summary>
        /// <param name="directory">The directory in which to place the file.</param>
        /// <param name="filename">The filename for the file.</param>
        /// <param name="format">The format to be written.</param>
        /// <exception cref="System.ArgumentException">Format must be an image format.</exception>
        /// <exception cref="System.NotImplementedException">Image format is not supported.</exception>
        public static void WriteRendering(this Grapher grapher, string directory, string filename, FileFormat format)
        {
            if (format != FileFormat.PNG && format != FileFormat.JPG)
                throw new ArgumentException("Format must be an image format.", nameof(format));

            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            string path = ValidateFilePath(directory, filename, format);

            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch (Exception ex) { Debug.Log($"Unable to delete file:{ex.Message}"); }

            RenderTexture active = RenderTexture.active;
            RenderTexture renderTexture = grapher.GetComponentInChildren<Camera>(true).targetTexture;
            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            RenderTexture.active = active;

            byte[] byteStream;
            switch (format)
            {
                case FileFormat.PNG:
                    byteStream = texture.EncodeToPNG();
                    break;
                case FileFormat.JPG:
                    byteStream = texture.EncodeToJPG();
                    break;
                default:
                    throw new NotImplementedException("Image format is not supported.");
            }
            System.IO.File.WriteAllBytes(path, byteStream);
            UnityEngine.Object.Destroy(texture);
        }

        private static string ValidateFilePath(string directory, string filename, FileFormat format)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));
            if (!PathCharsAreValid(directory))
                throw new System.IO.DirectoryNotFoundException(directory);
            int extensionIndex = filename.LastIndexOf('.');
            string extension;
            string formatExtension;
            switch (format)
            {
                case FileFormat.CSV:
                    formatExtension = ".csv";
                    break;
                case FileFormat.XLS:
                    formatExtension = ".xls";
                    break;
                case FileFormat.XLSX:
                    formatExtension = ".xlsx";
                    break;
                case FileFormat.PNG:
                    formatExtension = ".png";
                    break;
                case FileFormat.JPG:
                    formatExtension = ".jpg";
                    break;
                default:
                    throw new NotImplementedException("File format extension is missing");
            }
            if (extensionIndex == -1)
                extension = formatExtension;
            else
            {
                extension = filename.Substring(extensionIndex);
                filename = filename.Substring(0, extensionIndex);
                if (!string.Equals(extension, formatExtension, StringComparison.OrdinalIgnoreCase))
                    extension = formatExtension;
            }
            filename = StripInvalidFileChars(filename);
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("The resulting filename is empty", nameof(filename));
            filename = string.Concat(filename, extension);
            return string.Join($"{System.IO.Path.DirectorySeparatorChar}", directory, filename);
        }

        public static bool PathCharsAreValid(string directory)
        {
            char[] invalidChars = System.IO.Path.GetInvalidPathChars();
            foreach (char c in directory.ToCharArray())
                if (invalidChars.Contains(c))
                    return false;
            return true;
        }

        public static string StripInvalidFileChars(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            char[] chars = filename.ToCharArray();
            for (int i = chars.Length - 1; i >= 0; i--)
                if (invalidChars.Contains(chars[i]))
                    filename.Remove(i, 1);
            return filename;
        }

        public static string StripInvalidSheetChars(string sheetName)
        {
            if (sheetName == null)
                throw new ArgumentNullException(nameof(sheetName));
            sheetName = sheetName.Replace("/", "-").Replace("\\", "-");
            char[] chars = sheetName.ToCharArray();
            for (int i = chars.Length - 1; i >= 0; i--)
                if (invalidSheetChars.Contains(chars[i]))
                    sheetName.Remove(i, 1);
            return sheetName;
        }
    }
}
