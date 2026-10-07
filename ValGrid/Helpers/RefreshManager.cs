using System;

namespace ValGrid.Helpers;

public static class RefreshManager
{
    private static bool _isPaused;
    public static event Action<bool>? OnPauseChanged;

    public static bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (_isPaused != value)
            {
                _isPaused = value;
                try
                {
                    OnPauseChanged?.Invoke(value);
                }
                catch (Exception ex)
                {
                    Constants.Log?.Error(ex, "Error in RefreshManager.OnPauseChanged");
                }
            }
        }
    }
}

