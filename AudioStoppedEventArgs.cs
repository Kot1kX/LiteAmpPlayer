namespace LiteAmpPlayer;

internal sealed class AudioStoppedEventArgs : EventArgs
{
    public AudioStoppedEventArgs(bool manualStop, Exception? exception)
    {
        ManualStop = manualStop;
        Exception = exception;
    }

    public bool ManualStop { get; }
    public Exception? Exception { get; }
}