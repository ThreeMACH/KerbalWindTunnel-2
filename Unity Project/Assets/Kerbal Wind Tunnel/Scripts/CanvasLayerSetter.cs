using Smooth.Pools;
using System.Collections.Generic;
using UnityEngine;

public class CanvasLayerSetter : MonoBehaviour
{
    [SerializeField]
    private int bias = 0;
    public int Bias
    {
        get => bias;
        set
        {
            bias = value;
            OnEnable();
        }
    }

    public void OnEnable()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            return;
        List<Canvas> canvases = ListPool<Canvas>.Instance.Borrow();
        GetComponentsInParent<Canvas>(false, canvases);
        Canvas parentCanvas = null;
        for (int i = 0; i < canvases.Count; i++)
        {
            if (canvases[i].isRootCanvas)
            {
                parentCanvas = canvases[i];
                break;
            }
        }
        ListPool<Canvas>.Instance.Release(canvases);
        if (parentCanvas == null)
            return;
        canvas.sortingLayerID = parentCanvas.sortingLayerID + bias;
    }
}
