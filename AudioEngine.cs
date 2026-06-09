using System;
using NAudio.Wave;

namespace LiteAmpPlayer;

internal sealed class AudioEngine : IDisposable
{
    private AudioFileReader? _reader;
    private WaveOutEvent? _output;
    private SoftLimiterSampleProvider? _provider;
    private bool _ignoreNextStoppedEvent;

    public event EventHandler<AudioStoppedEventArgs>? PlaybackStopped;

    public string? CurrentFile { get; private set; }

    public float Volume { get; set; } = 0.80f;

    public float Boost { get; set; } = 1.00f;

    public bool HasTrack => _reader is not null;

    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;

    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;

    public TimeSpan Position
    {
        get => _reader?.CurrentTime ?? TimeSpan.Zero;
        set
        {
            if (_reader is null)
                return;

            if (value < TimeSpan.Zero)
                value = TimeSpan.Zero;

            if (value > _reader.TotalTime)
                value = _reader.TotalTime;

            _reader.CurrentTime = value;
        }
    }

    public void Load(string path)
    {
        DisposePlayback();

        _reader = new AudioFileReader(path);
        _provider = new SoftLimiterSampleProvider(_reader, GetEffectiveGain);

        _output = new WaveOutEvent
        {
            DesiredLatency = 120,
            NumberOfBuffers = 2
        };

        _output.Init(_provider);
        _output.PlaybackStopped += OutputOnPlaybackStopped;

        CurrentFile = path;
    }

    public void Play()
    {
        _output?.Play();
    }

    public void Pause()
    {
        _output?.Pause();
    }

    public void Stop(bool resetPosition)
    {
        if (_output is null)
        {
            if (resetPosition)
                Position = TimeSpan.Zero;

            return;
        }

        _ignoreNextStoppedEvent = true;
        _output.Stop();

        if (resetPosition)
            Position = TimeSpan.Zero;
    }

    public void Dispose()
    {
        DisposePlayback();
    }

    private float GetEffectiveGain()
    {
        float combined = Volume * Boost;

        if (combined <= 1f)
            return combined;

        float compressedBoost = 1f + ((combined - 1f) * 0.70f);

        return Math.Min(compressedBoost, 1.70f);
    }

    private void OutputOnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (_ignoreNextStoppedEvent)
        {
            _ignoreNextStoppedEvent = false;
            return;
        }

        bool endOfTrack = false;

        if (_reader is not null && _reader.TotalTime > TimeSpan.Zero)
        {
            endOfTrack = _reader.CurrentTime >= _reader.TotalTime - TimeSpan.FromMilliseconds(450);
        }

        PlaybackStopped?.Invoke(this, new AudioStoppedEventArgs(endOfTrack, e.Exception));
    }

    private void DisposePlayback()
    {
        if (_output is not null)
        {
            _output.PlaybackStopped -= OutputOnPlaybackStopped;

            try
            {
                _output.Stop();
            }
            catch
            {
            }

            _output.Dispose();
            _output = null;
        }

        _reader?.Dispose();
        _reader = null;

        _provider = null;
        CurrentFile = null;
        _ignoreNextStoppedEvent = false;
    }
}