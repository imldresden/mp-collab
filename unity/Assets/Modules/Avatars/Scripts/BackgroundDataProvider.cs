using System;
using System.Threading;
using System.Threading.Tasks;
using IMLD.MixedReality.Avatars;

public abstract class BackgroundDataProvider:IDisposable
{
    private KinectDataFrame _filteredData = new KinectDataFrame();
    private KinectDataFrame _unfilteredData = new KinectDataFrame();
    private bool _latest = false;
    object _lockObj = new object();
    public bool IsRunning { get; set; } = false;
    private CancellationTokenSource _cancellationTokenSource;
    private CancellationToken _token;

    public BackgroundDataProvider(int id)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.quitting += OnEditorClose;
#endif
        _cancellationTokenSource = new CancellationTokenSource();
        _token = _cancellationTokenSource.Token;
        Task.Run(() => RunBackgroundThreadAsync(id, _token));
    }

    private void OnEditorClose()
    {
        Dispose();
    }

    protected abstract void RunBackgroundThreadAsync(int id, CancellationToken token);

    public void SetCurrentFrameData(ref KinectDataFrame currentFrameData, bool isFilteredData)
    {
        lock (_lockObj)
        {
            if (isFilteredData)
            {
                var temp = currentFrameData;
                currentFrameData = _filteredData;
                _filteredData = temp;
                _latest = true;
            }
            else
            {
                var temp = currentFrameData;
                currentFrameData = _unfilteredData;
                _unfilteredData = temp;
            }
        }
    }

    public bool GetCurrentFrameData(ref KinectDataFrame dataBuffer, bool isFilteredData)
    {
        lock (_lockObj)
        {
            if (isFilteredData)
            {
                var temp = dataBuffer;
                dataBuffer = _filteredData;
                _filteredData = temp;
                bool result = _latest;
                _latest = false;
                return result;
            }
            else
            {
                var temp = dataBuffer;
                dataBuffer = _unfilteredData;
                _unfilteredData = temp;
                return false;
            }
        }
    }

    public void Dispose()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.quitting -= OnEditorClose;
#endif
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
}
