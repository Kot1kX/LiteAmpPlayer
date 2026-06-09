using System;

namespace LiteAmpPlayer;

internal sealed class AudioStoppedEventArgs : EventArgs
{
    public AudioStoppedEventArgs(bool endOfTrack, Exception? exception)
    {
        EndOfTrack = endOfTrack;
        Exception = exception;
    }

    public bool EndOfTrack { get; }

    public Exception? Exception { get; }
}