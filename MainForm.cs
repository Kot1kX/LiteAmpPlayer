using System.Drawing;
using System.Windows.Forms;

namespace LiteAmpPlayer;

internal sealed class MainForm : Form
{
    private static readonly string[] SupportedExtensions =
    [
        ".mp3",
        ".wav",
        ".flac",
        ".aac",
        ".m4a"
    ];

    private readonly AudioEngine _audio = new();
    private readonly List<string> _playlist = [];
    private readonly System.Windows.Forms.Timer _timer = new();

    private int _currentIndex = -1;
    private bool _seeking;

    private Label _titleLabel = null!;
    private Label _timeLabel = null!;
    private Label _statusLabel = null!;
    private Label _volumeValueLabel = null!;
    private Label _boostValueLabel = null!;

    private TrackBar _progressBar = null!;
    private TrackBar _volumeBar = null!;

    private Button _openButton = null!;
    private Button _folderButton = null!;
    private Button _playButton = null!;
    private Button _stopButton = null!;
    private Button _previousButton = null!;
    private Button _nextButton = null!;
    private Button _clearButton = null!;

    private ComboBox _boostBox = null!;
    private ListBox _playlistBox = null!;
    private CheckBox _topMostCheck = null!;

    public MainForm()
    {
        BuildUi();
        WireEvents();

        _timer.Interval = 250;
        _timer.Tick += (_, _) => UpdateProgress();
        _timer.Start();
    }

    private void BuildUi()
    {
        Text = "LiteAmp Player";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(620, 430);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(245, 245, 245);
        AllowDrop = true;

        Label headerLabel = new()
        {
            Text = "LiteAmp Player",
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            AutoSize = false,
            Location = new Point(16, 14),
            Size = new Size(420, 28)
        };

        _topMostCheck = new CheckBox
        {
            Text = "Siempre encima",
            AutoSize = true,
            Location = new Point(480, 18)
        };

        _titleLabel = new Label
        {
            Text = "Ning\u00FAn archivo cargado",
            AutoEllipsis = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            Location = new Point(16, 55),
            Size = new Size(588, 28),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _timeLabel = new Label
        {
            Text = "00:00 / 00:00",
            AutoSize = false,
            Location = new Point(16, 92),
            Size = new Size(120, 22),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _progressBar = new TrackBar
        {
            Location = new Point(140, 88),
            Size = new Size(464, 45),
            Minimum = 0,
            Maximum = 10000,
            TickStyle = TickStyle.None
        };

        _previousButton = new Button
        {
            Text = "Anterior",
            Location = new Point(16, 132),
            Size = new Size(90, 32)
        };

        _playButton = new Button
        {
            Text = "Reproducir",
            Location = new Point(112, 132),
            Size = new Size(90, 32)
        };

        _stopButton = new Button
        {
            Text = "Parar",
            Location = new Point(208, 132),
            Size = new Size(90, 32)
        };

        _nextButton = new Button
        {
            Text = "Siguiente",
            Location = new Point(304, 132),
            Size = new Size(90, 32)
        };

        Label volumeLabel = new()
        {
            Text = "Volumen",
            Location = new Point(16, 180),
            Size = new Size(80, 22)
        };

        _volumeBar = new TrackBar
        {
            Location = new Point(96, 172),
            Size = new Size(220, 45),
            Minimum = 0,
            Maximum = 100,
            Value = 80,
            TickFrequency = 10
        };

        _volumeValueLabel = new Label
        {
            Text = "80%",
            Location = new Point(322, 180),
            Size = new Size(50, 22)
        };

        Label boostLabel = new()
        {
            Text = "Amplificador",
            Location = new Point(388, 180),
            Size = new Size(90, 22)
        };

        _boostBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(480, 176),
            Size = new Size(124, 26)
        };

        _boostBox.Items.AddRange(["100%", "125%", "150%", "200%"]);
        _boostBox.SelectedIndex = 0;

        _boostValueLabel = new Label
        {
            Text = "Sin amplificar",
            Location = new Point(480, 205),
            Size = new Size(124, 22)
        };

        _openButton = new Button
        {
            Text = "Abrir archivos",
            Location = new Point(16, 232),
            Size = new Size(120, 32)
        };

        _folderButton = new Button
        {
            Text = "A\u00F1adir carpeta",
            Location = new Point(142, 232),
            Size = new Size(120, 32)
        };

        _clearButton = new Button
        {
            Text = "Limpiar lista",
            Location = new Point(268, 232),
            Size = new Size(120, 32)
        };

        _playlistBox = new ListBox
        {
            Location = new Point(16, 276),
            Size = new Size(588, 104),
            IntegralHeight = false
        };

        _statusLabel = new Label
        {
            Text = "Creado por Kot1kX",
            AutoEllipsis = true,
            Location = new Point(16, 392),
            Size = new Size(588, 22)
        };

        Controls.AddRange(
        [
            headerLabel,
            _topMostCheck,
            _titleLabel,
            _timeLabel,
            _progressBar,
            _previousButton,
            _playButton,
            _stopButton,
            _nextButton,
            volumeLabel,
            _volumeBar,
            _volumeValueLabel,
            boostLabel,
            _boostBox,
            _boostValueLabel,
            _openButton,
            _folderButton,
            _clearButton,
            _playlistBox,
            _statusLabel
        ]);
    }

    private void WireEvents()
    {
        FormClosing += (_, _) => _audio.Dispose();

        _openButton.Click += (_, _) => OpenFiles();
        _folderButton.Click += (_, _) => AddFolder();
        _clearButton.Click += (_, _) => ClearPlaylist();

        _playButton.Click += (_, _) => PlayOrPause();
        _stopButton.Click += (_, _) => StopPlayback();
        _previousButton.Click += (_, _) => PlayPrevious();
        _nextButton.Click += (_, _) => PlayNext(true);

        _volumeBar.Scroll += (_, _) => ApplyAudioSettings();
        _boostBox.SelectedIndexChanged += (_, _) => ApplyAudioSettings();

        _topMostCheck.CheckedChanged += (_, _) => TopMost = _topMostCheck.Checked;

        _playlistBox.DoubleClick += (_, _) =>
        {
            if (_playlistBox.SelectedIndex >= 0)
            {
                LoadTrack(_playlistBox.SelectedIndex, true);
            }
        };

        _progressBar.MouseDown += (_, _) => _seeking = true;

        _progressBar.MouseUp += (_, _) =>
        {
            SeekFromProgressBar();
            _seeking = false;
        };

        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
        };

        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
            {
                AddPaths(paths);
            }
        };

        _audio.PlaybackStopped += (_, e) =>
        {
            if (IsDisposed)
            {
                return;
            }

            BeginInvoke((MethodInvoker)(() =>
            {
                if (e.Exception != null)
                {
                    SetStatus("Error de audio: " + e.Exception.Message);
                    _playButton.Text = "Reproducir";
                    return;
                }

                if (!e.ManualStop)
                {
                    PlayNext(true);
                }
            }));
        };
    }

    private void OpenFiles()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Abrir m\u00FAsica",
            Filter = "Audio compatible|*.mp3;*.wav;*.flac;*.aac;*.m4a|Todos los archivos|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AddPaths(dialog.FileNames);
        }
    }

    private void AddFolder()
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "A\u00F1adir carpeta de m\u00FAsica",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AddPaths([dialog.SelectedPath]);
        }
    }

    private void AddPaths(IEnumerable<string> paths)
    {
        int added = 0;

        foreach (string path in paths)
        {
            try
            {
                if (File.Exists(path) && IsSupported(path))
                {
                    _playlist.Add(path);
                    added++;
                }
                else if (Directory.Exists(path))
                {
                    string[] files = Directory
                        .EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(IsSupported)
                        .OrderBy(Path.GetFileName)
                        .ToArray();

                    _playlist.AddRange(files);
                    added += files.Length;
                }
            }
            catch (Exception ex)
            {
                SetStatus("No se pudo leer: " + ex.Message);
            }
        }

        RefreshPlaylist();

        if (_currentIndex < 0 && _playlist.Count > 0)
        {
            LoadTrack(0, false);
        }

        SetStatus(added == 1 ? "1 archivo a\u00F1adido." : $"{added} archivos a\u00F1adidos.");
    }

    private void RefreshPlaylist()
    {
        _playlistBox.BeginUpdate();
        _playlistBox.Items.Clear();

        foreach (string path in _playlist)
        {
            _playlistBox.Items.Add(Path.GetFileName(path));
        }

        if (_currentIndex >= 0 && _currentIndex < _playlistBox.Items.Count)
        {
            _playlistBox.SelectedIndex = _currentIndex;
        }

        _playlistBox.EndUpdate();
    }

    private void LoadTrack(int index, bool autoPlay)
    {
        if (index < 0 || index >= _playlist.Count)
        {
            return;
        }

        string path = _playlist[index];

        try
        {
            _currentIndex = index;
            _audio.Load(path);
            ApplyAudioSettings();

            _titleLabel.Text = Path.GetFileName(path);
            _playlistBox.SelectedIndex = index;

            UpdateProgress();
            SetStatus("Cargado: " + Path.GetFileName(path));

            if (autoPlay)
            {
                _audio.Play();
                _playButton.Text = "Pausa";
                SetStatus("Reproduciendo.");
            }
            else
            {
                _playButton.Text = "Reproducir";
            }
        }
        catch (Exception ex)
        {
            SetStatus("No se pudo abrir el archivo: " + ex.Message);
            _playButton.Text = "Reproducir";
        }
    }

    private void PlayOrPause()
    {
        if (_playlist.Count == 0)
        {
            SetStatus("A\u00F1ade un archivo primero.");
            return;
        }

        int selectedIndex = _playlistBox.SelectedIndex >= 0
            ? _playlistBox.SelectedIndex
            : Math.Max(_currentIndex, 0);

        if (!_audio.HasFile || selectedIndex != _currentIndex)
        {
            LoadTrack(selectedIndex, false);
        }

        if (!_audio.HasFile)
        {
            return;
        }

        if (_audio.IsPlaying)
        {
            _audio.Pause();
            _playButton.Text = "Reproducir";
            SetStatus("Pausado.");
        }
        else
        {
            _audio.Play();
            _playButton.Text = "Pausa";
            SetStatus("Reproduciendo.");
        }
    }

    private void StopPlayback()
    {
        _audio.Stop();
        _playButton.Text = "Reproducir";
        UpdateProgress();
        SetStatus("Detenido.");
    }

    private void PlayPrevious()
    {
        if (_playlist.Count == 0)
        {
            return;
        }

        int previous = _currentIndex <= 0 ? 0 : _currentIndex - 1;
        LoadTrack(previous, true);
    }

    private void PlayNext(bool autoPlay)
    {
        if (_playlist.Count == 0)
        {
            return;
        }

        int next = _currentIndex + 1;

        if (next >= _playlist.Count)
        {
            _playButton.Text = "Reproducir";
            SetStatus("Fin de lista.");
            return;
        }

        LoadTrack(next, autoPlay);
    }

    private void ClearPlaylist()
    {
        _audio.Dispose();

        _playlist.Clear();
        _playlistBox.Items.Clear();
        _currentIndex = -1;

        _titleLabel.Text = "Ning\u00FAn archivo cargado";
        _timeLabel.Text = "00:00 / 00:00";
        _progressBar.Value = 0;
        _playButton.Text = "Reproducir";

        SetStatus("Lista limpia.");
    }

    private void ApplyAudioSettings()
    {
        int volume = _volumeBar.Value;
        float boost = GetBoostMultiplier();

        _audio.SetVolume(volume);
        _audio.SetBoost(boost);

        _volumeValueLabel.Text = volume + "%";
        _boostValueLabel.Text = boost <= 1.0f ? "Sin amplificar" : $"Amp x{boost:0.##}";
    }

    private float GetBoostMultiplier()
    {
        return _boostBox.SelectedIndex switch
        {
            1 => 1.25f,
            2 => 1.50f,
            3 => 2.00f,
            _ => 1.00f
        };
    }

    private void UpdateProgress()
    {
        if (!_audio.HasFile)
        {
            return;
        }

        TimeSpan duration = _audio.Duration;
        TimeSpan position = _audio.Position;

        _timeLabel.Text = $"{FormatTime(position)} / {FormatTime(duration)}";

        if (!_seeking && duration.TotalMilliseconds > 0)
        {
            double ratio = position.TotalMilliseconds / duration.TotalMilliseconds;
            int value = (int)Math.Clamp(ratio * _progressBar.Maximum, 0, _progressBar.Maximum);
            _progressBar.Value = value;
        }
    }

    private void SeekFromProgressBar()
    {
        if (!_audio.HasFile || _audio.Duration.TotalMilliseconds <= 0)
        {
            return;
        }

        double ratio = _progressBar.Value / (double)_progressBar.Maximum;
        double targetMilliseconds = _audio.Duration.TotalMilliseconds * ratio;
        _audio.Position = TimeSpan.FromMilliseconds(targetMilliseconds);

        UpdateProgress();
    }

    private static bool IsSupported(string path)
    {
        string extension = Path.GetExtension(path);
        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss")
            : time.ToString(@"mm\:ss");
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = text;
    }
}
