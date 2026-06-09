using NAudio.Wave;

namespace LiteAmpPlayer;

internal sealed class SoftLimiterSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;

    public SoftLimiterSampleProvider(ISampleProvider source)
    {
        _source = source;
    }

    public float Gain { get; set; } = 1.0f;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        float gain = Math.Clamp(Gain, 0.0f, 2.0f);

        for (int i = offset; i < offset + read; i++)
        {
            buffer[i] = Limit(buffer[i] * gain);
        }

        return read;
    }

    private static float Limit(float sample)
    {
        const float threshold = 0.92f;
        const float ceiling = 0.98f;

        if (sample > threshold)
        {
            return threshold + (ceiling - threshold) *
                (1.0f - MathF.Exp(-(sample - threshold) / (ceiling - threshold)));
        }

        if (sample < -threshold)
        {
            return -threshold - (ceiling - threshold) *
                (1.0f - MathF.Exp(-(-sample - threshold) / (ceiling - threshold)));
        }

        return sample;
    }
}