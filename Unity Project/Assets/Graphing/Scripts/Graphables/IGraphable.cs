using System;

namespace Graphing
{
    /// <summary>
    /// Provides an interface for any object that can be drawn on a graph.
    /// </summary>
    public interface IGraphable
    {
        /// <summary>
        /// The name of the object.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// The display name of the object. Can be different than the <see cref="Name"/> of the object.
        /// </summary>
        string DisplayName { get; set; }
        /// <summary>
        /// The visibility status of the object.
        /// </summary>
        bool Visible { get; set; }
        /// <summary>
        /// Should the value be displayed on mouseover.
        /// </summary>
        bool DisplayValue { get; set; }
        /// <summary>
        /// The lower X bound of the object.
        /// </summary>
        float XMin { get; }
        /// <summary>
        /// The upper X bound of the object.
        /// </summary>
        float XMax { get; }
        /// <summary>
        /// The lower Y bound of the object.
        /// </summary>
        float YMin { get; }
        /// <summary>
        /// The upper Y bound of the object.
        /// </summary>
        float YMax { get; }
        /// <summary>
        /// The unit for the X axis.
        /// </summary>
        string XUnit { get; set; }
        /// <summary>
        /// The unit for the Y axis.
        /// </summary>
        string YUnit { get; set; }
        /// <summary>
        /// The name of the X axis.
        /// </summary>
        string XName { get; set; }
        /// <summary>
        /// The name of the Y axis.
        /// </summary>
        string YName { get; set; }
        /// <summary>
        /// Draws the object on the specified <see cref="UnityEngine.Texture2D"/>.
        /// </summary>
        /// <param name="texture">The texture on which to draw the object.</param>
        /// <param name="xLeft">The X axis lower bound.</param>
        /// <param name="xRight">The X axis upper bound.</param>
        /// <param name="yBottom">The Y axis lower bound.</param>
        /// <param name="yTop">The Y axis upper bound.</param>
        void Draw(ref UnityEngine.Texture2D texture, float xLeft, float xRight, float yBottom, float yTop);
        /// <summary>
        /// Gets a value from the object given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <returns></returns>
        float ValueAt(float x, float y);
        /// <summary>
        /// Gets a formatted value from the object given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <param name="withName">When true, requests the object include its name.</param>
        /// <returns></returns>
        string GetFormattedValueAt(float x, float y, bool withName = false);
        /// <summary>
        /// An event to be triggered when an object's values change.
        /// </summary>
        event EventHandler<IValueEventArgs> ValuesChanged;
        /// <summary>
        /// An event to be triggered when an object's display formatting changes.
        /// </summary>
        event EventHandler<IDisplayEventArgs> DisplayChanged;
        /// <summary>
        /// Outputs the object's values to file.
        /// </summary>
        /// <param name="directory">The directory in which to place the file.</param>
        /// <param name="filename">The filename for the file.</param>
        /// <param name="sheetName">An optional sheet name for within the file.</param>
        void WriteToFile(string directory, string filename, string sheetName = "");
    }

    /// <summary>
    /// Provides an interface for any object that can be drawn on a graph and that has a Z component.
    /// </summary>
    public interface IGraphable3 : IGraphable
    {
        /// <summary>
        /// The lower Z bound of the object.
        /// </summary>
        float ZMin { get; }
        /// <summary>
        /// The upper Z bound of the object.
        /// </summary>
        float ZMax { get; }
        /// <summary>
        /// The unit for the Z axis.
        /// </summary>
        string ZUnit { get; set; }
        /// <summary>
        /// The name of the Z axis.
        /// </summary>
        string ZName { get; set; }
    }

    /// <summary>
    /// Provides an interface for an object that is specifically a graph.
    /// </summary>
    public interface IGraph : IGraphable
    {
        /// <summary>
        /// Defines the color scheme for the graph.
        /// </summary>
        UnityEngine.Gradient ColorScheme { get; set; }
        /// <summary>
        /// Provides the mapping function as input to the <see cref="ColorScheme"/>.
        /// </summary>
        System.Func<UnityEngine.Vector3, float> ColorFunc { get; set; }
        /// <summary>
        /// Disables the <see cref="ColorScheme"/> and uses a single color.
        /// </summary>
        bool UseSingleColor { get; set; }

        /// <summary>
        /// A single color that should be used for the entire graph.
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        UnityEngine.Color color { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }

    /// <summary>
    /// Provides an interface for an object that is specifically a line-based graph.
    /// </summary>
    public interface ILineGraph : IGraph
    {
        /// <summary>
        /// The width of the line in pixels.
        /// </summary>
        float LineWidth { get; set; }
        /// <summary>
        /// Gets a value from the object given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <param name="width">The x domain of the graph.</param>
        /// <param name="height">The y range of the graph.</param>
        /// <returns></returns>
        float ValueAt(float x, float y, float width, float height);
        /// <summary>
        /// Gets a formatted value from the object given a selected coordinate.
        /// </summary>
        /// <param name="x">The x value of the selected coordinate.</param>
        /// <param name="y">The y value of the selected coordinate.</param>
        /// <param name="width">The x domain of the graph.</param>
        /// <param name="height">The y range of the graph.</param>
        /// <param name="withName">When true, requests the object include its name.</param>
        /// <returns></returns>
        string GetFormattedValueAt(float x, float y, float width, float height, bool withName = false);
    }

    /// <summary>
    /// Provides an interface for an object that demands a color scale.
    /// </summary>
    public interface IColorGraph : IGraph
    {
        /// <summary>
        /// The color axis lower bound.
        /// </summary>
        float CMin { get; set; }
        /// <summary>
        /// The color axis upper bound.
        /// </summary>
        float CMax { get; set; }
        /// <summary>
        /// The unit for the color axis.
        /// </summary>
        string CUnit { get; set; }
        /// <summary>
        /// The name of the color axis.
        /// </summary>
        string CName { get; set; }
    }
}
