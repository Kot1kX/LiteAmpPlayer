using NAudio.Wave;

namespace LiteAmpPlayer;

internal sealed class AudioEngine : IDisposable
{
    private WaveOutEvent? _output;
    private AudioFileReader? _reader;
    private SoftLimiterSampleProvider? _limiter;
    private bool _manualStop;

    private int _volumePercent = 80;
    private float _boostMultiplier = 1.0f;

    public event EventHandler<AudioStoppedEventArgs>? PlaybackStopped;

    public bool HasFile => _reader != null && _output != null;

    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;

    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;

    public TimeSpan Position
    {
        get => _reader?.CurrentTime ?? TimeSpan.Zero;
        set
        {
            if (_reader == null)
            {
                return;
            }

            long ticks = Math.Clamp(value.Ticks, 0, Duration.Ticks);
            _reader.CurrentTime = new TimeSpan(ticks);
        }
    }

    public void Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Archivo no encontrado.", path);
        }

        DisposePlayback();

        _reader = new AudioFileReader(path);
        _limiter = new SoftLimiterSampleProvider(_reader);

        _output = new WaveOutEvent
        {
            DesiredLatency = 120
        };

        _output.PlaybackStopped += OnPlaybackStopped;

        UpdateGain();

        _output.Init(_limiter);
    }

    public void Play()
    {
        _output?.Play();
    }

    public void Pause()
    {
        _output?.Pause();
    }

    public void Stop()
    {
        if (_output == null)
        {
            return;
        }

        _manualStop = true;
        _output.Stop();

        if (_reader != null)
        {
            _reader.CurrentTime = TimeSpan.Zero;
        }
    }

    public void SetVolume(int percent)
    {
        _volumePercent = Math.Clamp(percent, 0, 100);
        UpdateGain();
    }

    public void SetBoost(float multiplier)
    {
        _boostMultiplier = Math.Clamp(multiplier, 1.0f, 2.0f);
        UpdateGain();
    }

    private void UpdateGain()
    {
        if (_limiter == null)
        {
            return;
        }

        float volume = _volumePercent / 100.0f;
        _limiter.Gain = Math.Clamp(volume * _boostMultiplier, 0.0f, 2.0f);
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        bool wasManual = _manualStop;
        _manualStop = false;

        PlaybackStopped?.Invoke(
            this,
            new AudioStoppedEventArgs(wasManual, e.Exception)
        );
    }

    private void DisposePlayback()
    {
        if (_output != null)
        {
            _output.PlaybackStopped -= OnPlaybackStopped;
            _output.Stop();
            _output.Dispose();
            _output = null;
        }

        _reader?.Dispose();
        _reader = null;
        _limiter = null;
        _manualStop = false;
    }

    public void Dispose()
    {
        DisposePlayback();
    }
}