using System;
using System.Collections.Generic;
using Graphing;

namespace KerbalWindTunnel
{
    public class GraphExportDialog
    {
        public const string popupWindowName = "KWTExport";
        public enum OutputMode
        {
            Visible = 0,
            All = 1,
            Vessel = 2
        }

        private string filename;
        public static GraphIO.FileFormat format = GraphIO.FileFormat.XLSX;
        private GraphableCollection collection;
        private OutputMode outputMode;
        public readonly PopupDialog dialog;

        public GraphExportDialog(GraphableCollection collection)
        {
            filename = EditorLogic.fetch.ship.shipName;
            this.collection = collection;
            outputMode = WindTunnelWindow.Instance.GraphMode == 0 ? OutputMode.Visible : OutputMode.All;
            List<DialogGUIBase> dialogItems = new List<DialogGUIBase>()
            {
                new DialogGUIContentSizer(UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize, UnityEngine.UI.ContentSizeFitter.FitMode.MinSize),
                new DialogGUIHorizontalLayout(UnityEngine.TextAnchor.MiddleLeft,
                    new DialogGUILabel("Save As: ", false, true),
                    new DialogGUITextInput("", filename, false, 60, value => filename = value, 300, 30)
                    ),
                new DialogGUISpace(5),
                new DialogGUIHorizontalLayout(UnityEngine.TextAnchor.MiddleLeft,
                    new DialogGUILabel("Format: ", false, true),
                    new DialogGUIToggleGroup(
                        new DialogGUIToggle(() => format == GraphIO.FileFormat.XLSX, "Spreadsheet", _ => format = GraphIO.FileFormat.XLSX),
                        new DialogGUIToggle(() => format == GraphIO.FileFormat.CSV, "CSV", _ => format = GraphIO.FileFormat.CSV)
                        ),
                    new DialogGUIFlexibleSpace()
                    ),
                new DialogGUIHorizontalLayout(UnityEngine.TextAnchor.MiddleLeft,
                    new DialogGUIToggleGroup(
                        new DialogGUIToggleButton(() => outputMode == OutputMode.Visible, "Visible graph(s)", _ => outputMode = OutputMode.Visible, h: 25),
                        new DialogGUIToggleButton(() => outputMode == OutputMode.All, "All graphs", _ => outputMode = OutputMode.All, h: 25),
                        new DialogGUIToggleButton(() => outputMode == OutputMode.Vessel, "Vessel", _ => outputMode = OutputMode.Vessel, h: 25)
                        )
                ),
                new DialogGUIHorizontalLayout(
                    new DialogGUIFlexibleSpace(),
                    new DialogGUIButton("Save", Export),
                    new DialogGUIButton("Cancel", Dismiss),
                    new DialogGUIFlexibleSpace()
                    )
            };

            dialog = PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog(popupWindowName, "", "Export", UISkinManager.defaultSkin, dialogItems.ToArray()),
                false, UISkinManager.defaultSkin, isModal: true);
            dialog.GetComponentInChildren<TMPro.TMP_InputField>(true).gameObject.AddComponent<Extensions.InputLockSelectHandler>().Setup("test", ControlTypes.KEYBOARDINPUT);
        }

        public void Dismiss()
        {
            dialog?.Dismiss();
        }

        private void Export()
        {
            Dismiss();
            if (outputMode <= OutputMode.All)
                collection.WriteToFile(WindTunnel.graphPath, filename, outputMode == OutputMode.Visible, format);
            else
                throw new NotImplementedException();
        }
    }
}
