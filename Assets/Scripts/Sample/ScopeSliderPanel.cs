using UnityEngine;

// 滑条面板管理器：根据 DamperSample2D.Func 只显示当前阻尼函数用到的参数行，
// 与 DamperSample2DEditor 的显隐规则保持一致
public class ScopeSliderPanel : MonoBehaviour
{
    public DamperSample2D Sample;
    // Base：Factor 行
    public GameObject FactorRow;
    // Exponential：Damping 行、Ft 行
    public GameObject DampingRow;
    public GameObject FtRow;
    // Exact：HalfLife 行、Eps 行
    public GameObject HalfLifeRow;
    public GameObject EpsRow;
    // 通用：Interval（Dt）行，始终显示
    public GameObject IntervalRow;

    private EDamperFunc _lastFunc;
    private bool _inited;

    private void Update()
    {
        if (Sample == null)
            return;
        if (_inited && Sample.Func == _lastFunc)
            return;

        _lastFunc = Sample.Func;
        _inited = true;
        Refresh();
    }

    // 按当前 Func 刷新各参数行的显隐
    private void Refresh()
    {
        bool isBase = Sample.Func == EDamperFunc.Base;
        bool isExponential = Sample.Func == EDamperFunc.Exponential;
        bool isExact = Sample.Func == EDamperFunc.Exact;

        SetRowActive(FactorRow, isBase);
        SetRowActive(DampingRow, isExponential);
        SetRowActive(FtRow, isExponential);
        SetRowActive(HalfLifeRow, isExact);
        SetRowActive(EpsRow, isExact);
        SetRowActive(IntervalRow, true);
    }

    private void SetRowActive(GameObject row, bool active)
    {
        if (row == null)
            return;
        if (row.activeSelf != active)
            row.SetActive(active);
    }
}
