using System;
using NAudio.Wave;

namespace LiteAmpPlayer;

internal sealed class SoftLimiterSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly Func<float> _gainProvider;

    public SoftLimiterSampleProvider(ISampleProvider source, Func<float> gainProvider)
    {
        _source = source;
        _gainProvider = gainProvider;
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        float gain = _gainProvider();

        for (int i = offset; i < offset + read; i++)
        {
            float sample = buffer[i] * gain;
            buffer[i] = SoftLimit(sample);
        }

        return read;
    }

    private static float SoftLimit(float sample)
    {
        const float threshold = 0.92f;

        if (sample > threshold)
        {
            float excess = sample - threshold;
            return threshold + ((1f - threshold) * MathF.Tanh(excess / (1f - threshold)));
        }

        if (sample < -threshold)
        {
            float excess = sample + threshold;
            return -threshold + ((1f - threshold) * MathF.Tanh(excess / (1f - threshold)));
        }

        return sample;
    }
}