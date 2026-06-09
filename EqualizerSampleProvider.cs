using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace LiteAmpPlayer;

internal sealed class EqualizerSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _channels;
    private readonly int _sampleRate;
    private readonly float[] _frequencies;
    private readonly float[] _gains;
    private readonly BiQuadFilter?[,] _filters;
    private readonly object _sync = new();

    public EqualizerSampleProvider(ISampleProvider source, params float[] frequencies)
    {
        _source = source;
        _channels = Math.Max(1, source.WaveFormat.Channels);
        _sampleRate = Math.Max(8000, source.WaveFormat.SampleRate);
        _frequencies = frequencies.Length > 0
            ? frequencies
            : [60f, 250f, 1000f, 4000f, 10000f];

        _gains = new float[_frequencies.Length];
        _filters = new BiQuadFilter?[_frequencies.Length, _channels];

        RebuildFilters();
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public void SetGains(params float[] gains)
    {
        lock (_sync)
        {
            for (int i = 0; i < _gains.Length; i++)
                _gains[i] = i < gains.Length ? Math.Clamp(gains[i], -9f, 9f) : 0f;

            RebuildFilters();
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);

        lock (_sync)
        {
            for (int i = offset; i < offset + read; i++)
            {
                int channel = (i - offset) % _channels;
                float sample = buffer[i];

                for (int band = 0; band < _frequencies.Length; band++)
                {
                    BiQuadFilter? filter = _filters[band, channel];

                    if (filter is not null)
                        sample = filter.Transform(sample);
                }

                if (float.IsNaN(sample) || float.IsInfinity(sample))
                    sample = 0f;

                buffer[i] = Math.Clamp(sample, -1.25f, 1.25f);
            }
        }

        return read;
    }

    private void RebuildFilters()
    {
        const float q = 0.85f;

        for (int band = 0; band < _frequencies.Length; band++)
        {
            float gain = _gains[band];
            float frequency = ClampFrequency(_frequencies[band]);

            for (int channel = 0; channel < _channels; channel++)
            {
                if (Math.Abs(gain) < 0.01f || frequency <= 0f)
                {
                    _filters[band, channel] = null;
                    continue;
                }

                _filters[band, channel] = BiQuadFilter.PeakingEQ(
                    _sampleRate,
                    frequency,
                    q,
                    gain
                );
            }
        }
    }

    private float ClampFrequency(float frequency)
    {
        float nyquistSafe = (_sampleRate / 2f) - 250f;

        if (nyquistSafe < 1000f)
            return 0f;

        if (frequency >= nyquistSafe)
            return nyquistSafe;

        if (frequency < 20f)
            return 20f;

        return frequency;
    }
}