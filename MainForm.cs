using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
    private readonly List<string> _playlist = new();
    private readonly Random _random = new();

    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly System.Windows.Forms.Timer _creditFadeTimer = new();

    private readonly Color _accent = Color.FromArgb(0, 120, 215);
    private readonly Color _gold = Color.FromArgb(170, 125, 35);
    private readonly Color _background = Color.FromArgb(245, 247, 250);
    private readonly Color _panel = Color.White;
    private readonly Color _textDark = Color.FromArgb(30, 35, 40);
    private readonly Color _textMuted = Color.FromArgb(95, 102, 112);

    private readonly Color _playingSurface = Color.FromArgb(235, 243, 255);
    private readonly Color _pausedSurface = Color.FromArgb(248, 250, 252);
    private readonly Color _playingListSurface = Color.FromArgb(249, 252, 255);

    private int _currentIndex = -1;
    private int _creditFadeStep;
    private bool _seeking;
    private bool _repeatOne;
    private bool _repeatList;
    private bool _shuffle;

    private Label _titleLabel = null!;
    private Label _timeLabel = null!;
    private Label _statusLabel = null!;
    private Label _creditLabel = null!;
    private Label _volumeValueLabel = null!;
    private Label _boostValueLabel = null!;

    private DirectSeekTrackBar _progressBar = null!;
    private TrackBar _volumeBar = null!;

    private Button _openButton = null!;
    private Button _folderButton = null!;
    private Button _playButton = null!;
    private Button _stopButton = null!;
    private Button _previousButton = null!;
    private Button _nextButton = null!;
    private Button _clearButton = null!;
    private Button _repeatOneButton = null!;
    private Button _repeatListButton = null!;
    private Button _shuffleButton = null!;

    private ComboBox _boostBox = null!;
    private ComboBox _eqBox = null!;
    private ListBox _playlistBox = null!;
    private CheckBox _topMostCheck = null!;

    public MainForm()
    {
        BuildUi();
        WireEvents();
        TryLoadIcon();

        _timer.Interval = 250;
        _timer.Tick += (_, _) => UpdateProgress();
        _timer.Start();

        _creditFadeTimer.Interval = 35;
        _creditFadeTimer.Tick += (_, _) => RunCreditFadeStep();

        _audio.PlaybackStopped += AudioOnPlaybackStopped;

        StartCreditFade();
        RefreshPlaybackVisualState();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _creditFadeTimer.Stop();

        _timer.Dispose();
        _creditFadeTimer.Dispose();

        _audio.Dispose();

        base.OnFormClosed(e);
    }

    private void BuildUi()
    {
        Text = "LiteAmp Player";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 530);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 9F);
        BackColor = _background;
        AllowDrop = true;

        var headerStrip = new Panel
        {
            BackColor = _accent,
            Location = new Point(16, 14),
            Size = new Size(5, 44)
        };

        var brandLiteLabel = new Label
        {
            Text = "LiteAmp",
            AutoSize = true,
            ForeColor = _gold,
            BackColor = _background,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            Location = new Point(28, 10)
        };

        var brandPlayerLabel = new Label
        {
            Text = "Player",
            AutoSize = true,
            ForeColor = _accent,
            BackColor = _background,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            Location = new Point(118, 10)
        };

        _creditLabel = new Label
        {
            Text = "Creado por Kot1kX",
            AutoEllipsis = true,
            Location = new Point(30, 39),
            Size = new Size(260, 18),
            ForeColor = _background,
            BackColor = _background,
            Font = new Font("Segoe UI", 8.3F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _topMostCheck = new CheckBox
        {
            Text = "Siempre encima",
            AutoSize = true,
            Location = new Point(620, 21),
            BackColor = _background,
            ForeColor = _textDark
        };

        _titleLabel = new Label
        {
            Text = "Ning\u00FAn archivo cargado",
            AutoEllipsis = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = _panel,
            ForeColor = _textDark,
            Location = new Point(16, 72),
            Size = new Size(728, 30),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0)
        };

        _timeLabel = new Label
        {
            Text = "00:00 / 00:00",
            AutoSize = false,
            Location = new Point(16, 108),
            Size = new Size(728, 20),
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = _textMuted
        };

        _progressBar = new DirectSeekTrackBar
        {
            Location = new Point(16, 131),
            Size = new Size(728, 36),
            Minimum = 0,
            Maximum = 1,
            Value = 0,
            TickStyle = TickStyle.None
        };

        _previousButton = CreateButton("Anterior", 16, 178, 84, 34);
        _playButton = CreateButton("Reproducir", 106, 178, 100, 34);
        _stopButton = CreateButton("Detener", 212, 178, 84, 34);
        _nextButton = CreateButton("Siguiente", 302, 178, 94, 34);

        _repeatOneButton = CreateButton("Repetir canci\u00F3n", 416, 178, 126, 34);
        _repeatListButton = CreateButton("Repetir lista", 548, 178, 112, 34);
        _shuffleButton = CreateButton("Aleatorio", 666, 178, 78, 34);

        var volumeLabel = new Label
        {
            Text = "Volumen",
            Location = new Point(16, 225),
            Size = new Size(70, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _textDark
        };

        _volumeBar = new TrackBar
        {
            Location = new Point(90, 218),
            Size = new Size(270, 36),
            Minimum = 0,
            Maximum = 100,
            Value = 80,
            TickStyle = TickStyle.None
        };

        _volumeValueLabel = new Label
        {
            Text = "80%",
            Location = new Point(366, 225),
            Size = new Size(55, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _textMuted
        };

        var boostLabel = new Label
        {
            Text = "Amplificador",
            Location = new Point(440, 225),
            Size = new Size(90, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _textDark
        };

        _boostBox = new ComboBox
        {
            Location = new Point(535, 222),
            Size = new Size(90, 28),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _boostBox.Items.AddRange(new object[]
        {
            "100%",
            "125%",
            "150%",
            "175%",
            "200%"
        });

        _boostBox.SelectedIndex = 0;

        _boostValueLabel = new Label
        {
            Text = "100%",
            Location = new Point(632, 225),
            Size = new Size(70, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _textMuted
        };

        _openButton = CreateButton("A\u00F1adir archivo", 16, 266, 130, 34);
        _folderButton = CreateButton("A\u00F1adir carpeta", 152, 266, 130, 34);
        _clearButton = CreateButton("Limpiar lista", 288, 266, 120, 34);

        var eqLabel = new Label
        {
            Text = "EQ",
            Location = new Point(440, 273),
            Size = new Size(30, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _textDark
        };

        _eqBox = new ComboBox
        {
            Location = new Point(475, 270),
            Size = new Size(150, 28),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _eqBox.Items.AddRange(new object[]
        {
            "Plano",
            "Bass Boost",
            "Vocal",
            "Rock",
            "Pop",
            "Night"
        });

        _eqBox.SelectedIndex = 0;

        _playlistBox = new ListBox
        {
            Location = new Point(16, 312),
            Size = new Size(728, 166),
            IntegralHeight = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = _panel,
            ForeColor = _textDark,
            Font = new Font("Segoe UI", 9.2F)
        };

        _statusLabel = new Label
        {
            Text = "Listo",
            AutoEllipsis = true,
            Location = new Point(16, 492),
            Size = new Size(728, 22),
            ForeColor = _textMuted,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Controls.AddRange(
        [
            headerStrip,
            brandLiteLabel,
            brandPlayerLabel,
            _creditLabel,
            _topMostCheck,
            _titleLabel,
            _timeLabel,
            _progressBar,
            _previousButton,
            _playButton,
            _stopButton,
            _nextButton,
            _repeatOneButton,
            _repeatListButton,
            _shuffleButton,
            volumeLabel,
            _volumeBar,
            _volumeValueLabel,
            boostLabel,
            _boostBox,
            _boostValueLabel,
            _openButton,
            _folderButton,
            _clearButton,
            eqLabel,
            _eqBox,
            _playlistBox,
            _statusLabel
        ]);
    }

    private void WireEvents()
    {
        _openButton.Click += (_, _) => AddFilesFromDialog();
        _folderButton.Click += (_, _) => AddFolderFromDialog();
        _clearButton.Click += (_, _) => ClearPlaylist();

        _previousButton.Click += (_, _) => PreviousTrack();
        _playButton.Click += (_, _) => TogglePlayPause();
        _stopButton.Click += (_, _) => StopPlayback();
        _nextButton.Click += (_, _) => NextTrack(manual: true);

        _repeatOneButton.Click += (_, _) =>
        {
            _repeatOne = !_repeatOne;

            if (_repeatOne)
                _repeatList = false;

            RefreshModeButtons();
        };

        _repeatListButton.Click += (_, _) =>
        {
            _repeatList = !_repeatList;

            if (_repeatList)
                _repeatOne = false;

            RefreshModeButtons();
        };

        _shuffleButton.Click += (_, _) =>
        {
            _shuffle = !_shuffle;
            RefreshModeButtons();
        };

        _playlistBox.DoubleClick += (_, _) =>
        {
            if (_playlistBox.SelectedIndex >= 0)
                LoadTrack(_playlistBox.SelectedIndex, playImmediately: true);
        };

        _playlistBox.SelectedIndexChanged += (_, _) =>
        {
            if (_playlistBox.SelectedIndex >= 0)
                _currentIndex = _playlistBox.SelectedIndex;
        };

        _volumeBar.ValueChanged += (_, _) =>
        {
            _audio.Volume = _volumeBar.Value / 100f;
            _volumeValueLabel.Text = $"{_volumeBar.Value}%";
        };

        _boostBox.SelectedIndexChanged += (_, _) =>
        {
            int boost = GetSelectedBoostPercent();
            _audio.Boost = boost / 100f;
            _boostValueLabel.Text = $"{boost}%";
        };

        _eqBox.SelectedIndexChanged += (_, _) =>
        {
            ApplyEqPreset();
        };

        _topMostCheck.CheckedChanged += (_, _) =>
        {
            TopMost = _topMostCheck.Checked;
        };

        _progressBar.SeekRequested += seconds =>
        {
            if (!_audio.HasTrack)
                return;

            _seeking = true;
            _audio.Position = TimeSpan.FromSeconds(seconds);
            _seeking = false;

            UpdateProgress();
        };

        DragEnter += (_, e) =>
        {
            if (e.Data is not null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        };

        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
                AddPaths(paths);
        };
    }

    private void StartCreditFade()
    {
        _creditFadeStep = 0;
        _creditLabel.ForeColor = _background;
        _creditFadeTimer.Start();
    }

    private void RunCreditFadeStep()
    {
        const int totalSteps = 32;

        _creditFadeStep++;

        double amount = Math.Clamp(_creditFadeStep / (double)totalSteps, 0d, 1d);
        double eased = 1d - Math.Pow(1d - amount, 3d);

        _creditLabel.ForeColor = BlendColor(_background, _gold, eased);

        if (_creditFadeStep >= totalSteps)
        {
            _creditLabel.ForeColor = _gold;
            _creditFadeTimer.Stop();
        }
    }

    private static Color BlendColor(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0d, 1d);

        int r = (int)Math.Round(from.R + ((to.R - from.R) * amount));
        int g = (int)Math.Round(from.G + ((to.G - from.G) * amount));
        int b = (int)Math.Round(from.B + ((to.B - from.B) * amount));

        return Color.FromArgb(r, g, b);
    }

    private Button CreateButton(string text, int x, int y, int width, int height)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(242, 244, 247),
            ForeColor = _textDark,
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 222);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 238, 246);

        return button;
    }

    private void ApplyDefaultButtonStyle(Button button)
    {
        button.BackColor = Color.FromArgb(242, 244, 247);
        button.ForeColor = _textDark;
        button.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 222);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 238, 246);
    }

    private void ApplyAccentButtonStyle(Button button)
    {
        button.BackColor = _accent;
        button.ForeColor = Color.White;
        button.FlatAppearance.BorderColor = _accent;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 100, 190);
    }

    private void RefreshPlaybackVisualState()
    {
        bool hasTrack = _audio.HasTrack;
        bool isPlaying = _audio.IsPlaying;

        if (isPlaying)
        {
            ApplyAccentButtonStyle(_playButton);
            _titleLabel.BackColor = _playingSurface;
            _playlistBox.BackColor = _playingListSurface;
            _statusLabel.ForeColor = _accent;
            _timeLabel.ForeColor = _accent;
            return;
        }

        ApplyDefaultButtonStyle(_playButton);

        if (hasTrack)
        {
            _titleLabel.BackColor = _pausedSurface;
            _playlistBox.BackColor = _panel;
            _statusLabel.ForeColor = _textMuted;
            _timeLabel.ForeColor = _textMuted;
            return;
        }

        _titleLabel.BackColor = _panel;
        _playlistBox.BackColor = _panel;
        _statusLabel.ForeColor = _textMuted;
        _timeLabel.ForeColor = _textMuted;
    }

    private void AddFilesFromDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "A\u00F1adir m\u00FAsica",
            Multiselect = true,
            Filter = "Audio compatible|*.mp3;*.wav;*.flac;*.aac;*.m4a|Todos los archivos|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        AddPaths(dialog.FileNames);
    }

    private void AddFolderFromDialog()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Selecciona una carpeta con m\u00FAsica",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        AddPaths([dialog.SelectedPath]);
    }

    private void AddPaths(IEnumerable<string> paths)
    {
        int added = 0;

        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                if (AddTrack(path))
                    added++;

                continue;
            }

            if (Directory.Exists(path))
            {
                foreach (string file in EnumerateSupportedFiles(path))
                {
                    if (AddTrack(file))
                        added++;
                }
            }
        }

        if (_currentIndex < 0 && _playlist.Count > 0)
        {
            _currentIndex = 0;
            _playlistBox.SelectedIndex = 0;
            UpdateTitleLabel();
        }

        if (added > 0)
            ShowStatus($"A\u00F1adidas {added} pista(s)");
        else
            ShowStatus("No se a\u00F1adieron pistas nuevas");
    }

    private bool AddTrack(string path)
    {
        if (!IsSupported(path))
            return false;

        if (_playlist.Contains(path, StringComparer.OrdinalIgnoreCase))
            return false;

        _playlist.Add(path);
        _playlistBox.Items.Add(Path.GetFileName(path));

        return true;
    }

    private void ClearPlaylist()
    {
        _audio.Stop(resetPosition: true);

        _playlist.Clear();
        _playlistBox.Items.Clear();

        _currentIndex = -1;

        _titleLabel.Text = "Ning\u00FAn archivo cargado";
        _timeLabel.Text = "00:00 / 00:00";
        _progressBar.Maximum = 1;
        _progressBar.Value = 0;
        _playButton.Text = "Reproducir";

        ShowStatus("Lista limpiada");
        RefreshPlaybackVisualState();
    }

    private void TogglePlayPause()
    {
        if (_playlist.Count == 0)
        {
            AddFilesFromDialog();
            return;
        }

        if (_currentIndex < 0)
            _currentIndex = _playlistBox.SelectedIndex >= 0 ? _playlistBox.SelectedIndex : 0;

        if (!_audio.HasTrack || !string.Equals(_audio.CurrentFile, _playlist[_currentIndex], StringComparison.OrdinalIgnoreCase))
        {
            LoadTrack(_currentIndex, playImmediately: true);
            return;
        }

        if (_audio.IsPlaying)
        {
            _audio.Pause();
            _playButton.Text = "Reproducir";
            ShowStatus("Pausado");
            RefreshPlaybackVisualState();
            return;
        }

        _audio.Play();
        _playButton.Text = "Pausar";
        ShowStatus("Reproduciendo");
        RefreshPlaybackVisualState();
    }

    private void StopPlayback()
    {
        _audio.Stop(resetPosition: true);
        _playButton.Text = "Reproducir";
        UpdateProgress();
        ShowStatus("Detenido");
        RefreshPlaybackVisualState();
    }

    private void PreviousTrack()
    {
        if (_playlist.Count == 0)
            return;

        if (_audio.HasTrack && _audio.Position.TotalSeconds > 3)
        {
            _audio.Position = TimeSpan.Zero;
            UpdateProgress();
            return;
        }

        int previous = _currentIndex - 1;

        if (previous < 0)
            previous = _repeatList ? _playlist.Count - 1 : 0;

        LoadTrack(previous, playImmediately: true);
    }

    private void NextTrack(bool manual)
    {
        if (_playlist.Count == 0)
            return;

        int next = GetNextIndex(manual);

        if (next < 0)
        {
            _audio.Stop(resetPosition: true);
            _playButton.Text = "Reproducir";
            ShowStatus("Fin de lista");
            UpdateProgress();
            RefreshPlaybackVisualState();
            return;
        }

        LoadTrack(next, playImmediately: true);
    }

    private int GetNextIndex(bool manual)
    {
        if (_playlist.Count == 0)
            return -1;

        if (_repeatOne && !manual)
            return _currentIndex;

        if (_shuffle && _playlist.Count > 1)
        {
            int next = _currentIndex;
            int guard = 0;

            while (next == _currentIndex && guard < 30)
            {
                next = _random.Next(0, _playlist.Count);
                guard++;
            }

            return next;
        }

        int sequentialNext = _currentIndex + 1;

        if (sequentialNext < _playlist.Count)
            return sequentialNext;

        return _repeatList ? 0 : -1;
    }

    private void LoadTrack(int index, bool playImmediately)
    {
        if (index < 0 || index >= _playlist.Count)
            return;

        try
        {
            _currentIndex = index;
            _playlistBox.SelectedIndex = index;

            _audio.Load(_playlist[index]);
            _audio.Volume = _volumeBar.Value / 100f;
            _audio.Boost = GetSelectedBoostPercent() / 100f;
            ApplyEqPreset(updateStatus: false);

            UpdateTitleLabel();
            UpdateProgress();

            if (playImmediately)
            {
                _audio.Play();
                _playButton.Text = "Pausar";
                ShowStatus("Reproduciendo");
            }
            else
            {
                _playButton.Text = "Reproducir";
                ShowStatus("Pista cargada");
            }

            RefreshPlaybackVisualState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"No se pudo abrir la pista:\n\n{ex.Message}",
                "LiteAmp Player",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            ShowStatus("Error al abrir la pista");
            RefreshPlaybackVisualState();
        }
    }

    private void AudioOnPlaybackStopped(object? sender, AudioStoppedEventArgs e)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AudioOnPlaybackStopped(sender, e)));
            return;
        }

        if (e.Exception is not null)
        {
            MessageBox.Show(
                this,
                $"Error durante la reproducci\u00F3n:\n\n{e.Exception.Message}",
                "LiteAmp Player",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            _playButton.Text = "Reproducir";
            ShowStatus("Error durante la reproducci\u00F3n");
            RefreshPlaybackVisualState();
            return;
        }

        if (e.EndOfTrack)
            NextTrack(manual: false);
    }

    private void UpdateProgress()
    {
        TimeSpan duration = _audio.Duration;
        TimeSpan position = _audio.Position;

        int durationSeconds = Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds));
        int positionSeconds = Math.Clamp((int)Math.Round(position.TotalSeconds), 0, durationSeconds);

        if (_progressBar.Maximum != durationSeconds)
            _progressBar.Maximum = durationSeconds;

        if (!_seeking)
            _progressBar.Value = Math.Clamp(positionSeconds, _progressBar.Minimum, _progressBar.Maximum);

        _timeLabel.Text = $"{FormatTime(position)} / {FormatTime(duration)}";

        if (_audio.HasTrack)
            _playButton.Text = _audio.IsPlaying ? "Pausar" : "Reproducir";

        RefreshPlaybackVisualState();
    }

    private void UpdateTitleLabel()
    {
        if (_currentIndex < 0 || _currentIndex >= _playlist.Count)
        {
            _titleLabel.Text = "Ning\u00FAn archivo cargado";
            Text = "LiteAmp Player";
            return;
        }

        string fileName = Path.GetFileName(_playlist[_currentIndex]);

        _titleLabel.Text = fileName;
        Text = $"LiteAmp Player - {fileName}";
    }

    private void RefreshModeButtons()
    {
        ApplyModeStyle(_repeatOneButton, _repeatOne);
        ApplyModeStyle(_repeatListButton, _repeatList);
        ApplyModeStyle(_shuffleButton, _shuffle);
    }

    private void ApplyModeStyle(Button button, bool active)
    {
        if (active)
        {
            ApplyAccentButtonStyle(button);
            return;
        }

        ApplyDefaultButtonStyle(button);
    }

    private void ApplyEqPreset(bool updateStatus = true)
    {
        string preset = _eqBox.SelectedItem as string ?? "Plano";
        float[] gains = GetEqPresetGains(preset);

        _audio.SetEqualizer(gains);
        RefreshEqVisualState();

        if (updateStatus)
            ShowStatus($"EQ: {preset}");
    }

    private static float[] GetEqPresetGains(string preset)
    {
        return preset switch
        {
            "Bass Boost" => [5.0f, 3.0f, 0.0f, -1.0f, 0.0f],
            "Vocal" => [-2.0f, -1.0f, 3.5f, 2.5f, 0.0f],
            "Rock" => [3.0f, 1.5f, -1.0f, 2.5f, 3.0f],
            "Pop" => [2.0f, 1.0f, 1.0f, 2.0f, 1.5f],
            "Night" => [-3.0f, -2.0f, 0.0f, 1.0f, -1.5f],
            _ => [0.0f, 0.0f, 0.0f, 0.0f, 0.0f]
        };
    }

    private void RefreshEqVisualState()
    {
        bool active = (_eqBox.SelectedItem as string ?? "Plano") != "Plano";

        if (active)
        {
            _eqBox.BackColor = Color.FromArgb(235, 243, 255);
            _eqBox.ForeColor = _accent;
            return;
        }

        _eqBox.BackColor = Color.White;
        _eqBox.ForeColor = _textDark;
    }
    private int GetSelectedBoostPercent()
    {
        if (_boostBox.SelectedItem is not string text)
            return 100;

        text = text.Replace("%", string.Empty).Trim();

        return int.TryParse(text, out int value) ? value : 100;
    }

    private void ShowStatus(string text)
    {
        _statusLabel.Text = text;
    }

    private void TryLoadIcon()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "LiteAmp.ico"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "LiteAmp.ico"),
            Path.Combine(Environment.CurrentDirectory, "LiteAmp.ico")
        ];

        foreach (string candidate in candidates)
        {
            try
            {
                string path = Path.GetFullPath(candidate);

                if (File.Exists(path))
                {
                    Icon = new Icon(path);
                    return;
                }
            }
            catch
            {
            }
        }
    }

    private static IEnumerable<string> EnumerateSupportedFiles(string folder)
    {
        var pending = new Stack<string>();
        pending.Push(folder);

        while (pending.Count > 0)
        {
            string current = pending.Pop();

            string[] files;

            try
            {
                files = Directory.GetFiles(current);
            }
            catch
            {
                continue;
            }

            foreach (string file in files)
            {
                if (IsSupported(file))
                    yield return file;
            }

            string[] directories;

            try
            {
                directories = Directory.GetDirectories(current);
            }
            catch
            {
                continue;
            }

            foreach (string directory in directories)
                pending.Push(directory);
        }
    }

    private static bool IsSupported(string path)
    {
        string extension = Path.GetExtension(path);
        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return time.ToString(@"h\:mm\:ss");

        return time.ToString(@"mm\:ss");
    }

    private sealed class DirectSeekTrackBar : TrackBar
    {
        public event Action<int>? SeekRequested;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Focus();
                Capture = true;
                SetValueFromMouse(e.X);
                SeekRequested?.Invoke(Value);
                return;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Capture)
            {
                SetValueFromMouse(e.X);
                SeekRequested?.Invoke(Value);
                return;
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                SetValueFromMouse(e.X);
                SeekRequested?.Invoke(Value);
                Capture = false;
                return;
            }

            base.OnMouseUp(e);
        }

        private void SetValueFromMouse(int mouseX)
        {
            if (Maximum <= Minimum || Width <= 0)
                return;

            double ratio = Math.Clamp(mouseX / (double)Math.Max(1, Width), 0d, 1d);
            int value = Minimum + (int)Math.Round((Maximum - Minimum) * ratio);

            Value = Math.Clamp(value, Minimum, Maximum);
        }
    }
}