using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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

    private Button _boost100Button = null!;
    private Button _boost125Button = null!;
    private Button _boost150Button = null!;
    private Button _boost175Button = null!;
    private Button _boost200Button = null!;
    private int _selectedBoostPercent = 100;
    private Button _eqNormalButton = null!;
    private Button _eqBassButton = null!;
    private Button _eqVocalButton = null!;
    private Button _eqRockButton = null!;
    private Button _eqPopButton = null!;
    private string _selectedEqPreset = "Normal";
    private ListBox _playlistBox = null!;
    private CheckBox _topMostCheck = null!;

    public MainForm()
    {

        
        TryLoadIcon();
ApplyAudioStabilityTuning();
BuildUi();
        WireEvents();
        _timer.Interval = 250;
        _timer.Tick += (_, _) => UpdateProgress();
        _timer.Start();

        _creditFadeTimer.Interval = 35;
        _creditFadeTimer.Tick += (_, _) => RunCreditFadeStep();

        _audio.PlaybackStopped += AudioOnPlaybackStopped;

        StartCreditFade();
        RefreshPlaybackVisualState();
    
        Shown += (_, _) => BeginInvoke(new Action(AttachPlaylistSearchOnce));

        Shown += (_, _) => BeginInvoke(new Action(AttachPlaylistSearchOnce));
        Resize += (_, _) => PositionPlaylistSearchControls();

        Shown += (_, _) => BeginInvoke(new Action(AttachKeyboardShortcutsOnce));

        Shown += (_, _) => BeginInvoke(new Action(AttachTimelineKeyboardGuardOnce));

        Shown += (_, _) => BeginInvoke(new Action(AttachStartupFocusFixOnce));

        Shown += (_, _) => BeginInvoke(new Action(AttachPlaylistNativeNavigationOnce));

        Shown += (_, _) => BeginInvoke(new Action(LiteAmpInstallPlaylistDeselectFilterOnce));

        Shown += (_, _) => BeginInvoke(new Action(AttachLiteAmpArrowMessageFilterOnce));
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

        var headerIcon = TryLoadHeaderBitmap();

        var headerIconBox = new PictureBox
        {
            Image = headerIcon,
            Location = new Point(16, 17),
            Size = new Size(30, 30),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = _background,
            Visible = headerIcon is not null
        };

        var brandLiteLabel = new Label
        {
            Text = "LiteAmp",
            AutoSize = true,
            ForeColor = _gold,
            BackColor = _background,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            Location = new Point(54, 10)
        };

        var brandPlayerLabel = new Label
        {
            Text = "Player",
            AutoSize = true,
            ForeColor = _accent,
            BackColor = _background,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            Location = new Point(144, 10)
        };

        _creditLabel = new Label
        {
            Text = "Creado por Kot1kX",
            AutoEllipsis = true,
            Location = new Point(56, 39),
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
        _shuffleButton = CreateButton("Aleatorio", 672, 178, 78, 34);

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
            Text = "AMP",
            Location = new Point(418, 225),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _textDark
        };

        _boost100Button = CreateButton("100%", 452, 219, 58, 30);
        _boost125Button = CreateButton("125%", 512, 219, 58, 30);
        _boost150Button = CreateButton("150%", 572, 219, 58, 30);
        _boost175Button = CreateButton("175%", 632, 219, 58, 30);
        _boost200Button = CreateButton("200%", 692, 219, 58, 30);

        _boost100Button.TabStop = false;
        _boost125Button.TabStop = false;
        _boost150Button.TabStop = false;
        _boost175Button.TabStop = false;
        _boost200Button.TabStop = false;

        RefreshBoostButtonsVisualState();


        _openButton = CreateButton("A\u00F1adir archivo", 16, 266, 130, 34);
        _folderButton = CreateButton("A\u00F1adir carpeta", 152, 266, 130, 34);
        _clearButton = CreateButton("Limpiar lista", 288, 266, 120, 34);

        var eqLabel = new Label
        {
            Text = "EQ",
            Location = new Point(418, 273),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _textDark
        };

        _eqNormalButton = CreateButton("Normal", 452, 266, 58, 34);
        _eqBassButton = CreateButton("Bass", 512, 266, 58, 34);
        _eqVocalButton = CreateButton("Vocal", 572, 266, 58, 34);
        _eqRockButton = CreateButton("Rock", 632, 266, 58, 34);
        _eqPopButton = CreateButton("Pop", 692, 266, 58, 34);

        RefreshEqButtonsVisualState();

        _playlistBox = new ListBox
        {
            Location = new Point(16, 312),
            Size = new Size(734, 174),
            IntegralHeight = true,
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
            headerIconBox,
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
            _boost100Button,
            _boost125Button,
            _boost150Button,
            _boost175Button,
            _boost200Button,
            _openButton,
            _folderButton,
            _clearButton,
            eqLabel,
            _eqNormalButton,
            _eqBassButton,
            _eqVocalButton,
            _eqRockButton,
            _eqPopButton,
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
        _boost100Button.Click += (_, _) => SelectBoostPercent(100);
        _boost125Button.Click += (_, _) => SelectBoostPercent(125);
        _boost150Button.Click += (_, _) => SelectBoostPercent(150);
        _boost175Button.Click += (_, _) => SelectBoostPercent(175);
        _boost200Button.Click += (_, _) => SelectBoostPercent(200);

        _eqNormalButton.Click += (_, _) => SelectEqPreset("Normal");
        _eqBassButton.Click += (_, _) => SelectEqPreset("Bass Boost");
        _eqVocalButton.Click += (_, _) => SelectEqPreset("Vocal");
        _eqRockButton.Click += (_, _) => SelectEqPreset("Rock");
        _eqPopButton.Click += (_, _) => SelectEqPreset("Pop");
_topMostCheck.CheckedChanged += (_, _) =>
        {
            TopMost = _topMostCheck.Checked;
        };

        _progressBar.SeekRequested += seconds =>
        {
            if (!_audio.HasTrack)
            {
                _seeking = false;

                try
                {
                    if (_progressBar.Value != 0)
                        _progressBar.Value = 0;
                }
                catch
                {
                }

                return;
            }

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

    private bool AddTrack(string path)
    {
        if (!IsSupported(path))
            return false;

        if (_playlist.Contains(path, StringComparer.OrdinalIgnoreCase))
            return false;

        _playlist.Add(path);
        _playlistBox.Items.Add(GetTrackDisplayName(path));

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
            _audio.Boost = _selectedBoostPercent / 100f;
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

        string displayName = GetTrackDisplayName(_playlist[_currentIndex]);

        _titleLabel.Text = displayName;
        Text = $"LiteAmp Player - {displayName}";
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

        private void SelectBoostPercent(int percent)
    {
        _selectedBoostPercent = Math.Clamp(percent, 100, 200);
        _audio.Boost = _selectedBoostPercent / 100f;
        RefreshBoostButtonsVisualState();
        ShowStatus($"Amplificador: {_selectedBoostPercent}%");
    }

    private void RefreshBoostButtonsVisualState()
    {
        if (_boost100Button is null)
            return;

        ApplyModeStyle(_boost100Button, _selectedBoostPercent == 100);
        ApplyModeStyle(_boost125Button, _selectedBoostPercent == 125);
        ApplyModeStyle(_boost150Button, _selectedBoostPercent == 150);
        ApplyModeStyle(_boost175Button, _selectedBoostPercent == 175);
        ApplyModeStyle(_boost200Button, _selectedBoostPercent == 200);
    }

    private void ShowStatus(string text)
    {
        _statusLabel.Text = text;
    }

    private Bitmap? TryLoadHeaderBitmap()
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
                    using var icon = new Icon(path, 64, 64);
                    using Bitmap raw = icon.ToBitmap();

                    return BuildHeaderIconBitmap(raw, 30, 30);
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static Bitmap BuildHeaderIconBitmap(Bitmap source, int targetWidth, int targetHeight)
    {
        using var transparent = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        int minX = source.Width;
        int minY = source.Height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color pixel = source.GetPixel(x, y);

                if (pixel.A == 0 || IsNearWhiteBackground(pixel))
                {
                    transparent.SetPixel(x, y, Color.Transparent);
                    continue;
                }

                transparent.SetPixel(x, y, pixel);

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
            return new Bitmap(targetWidth, targetHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        const int padding = 2;

        minX = Math.Max(0, minX - padding);
        minY = Math.Max(0, minY - padding);
        maxX = Math.Min(source.Width - 1, maxX + padding);
        maxY = Math.Min(source.Height - 1, maxY + padding);

        var crop = Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
        var result = new Bitmap(targetWidth, targetHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using Graphics graphics = Graphics.FromImage(result);
        graphics.Clear(Color.Transparent);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        float scale = Math.Min(targetWidth / (float)crop.Width, targetHeight / (float)crop.Height);
        int width = Math.Max(1, (int)Math.Round(crop.Width * scale));
        int height = Math.Max(1, (int)Math.Round(crop.Height * scale));
        int left = (targetWidth - width) / 2;
        int top = (targetHeight - height) / 2;

        graphics.DrawImage(transparent, new Rectangle(left, top, width, height), crop, GraphicsUnit.Pixel);

        return result;
    }

    private static bool IsNearWhiteBackground(Color color)
    {
        if (color.A < 20)
            return true;

        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));

        bool veryLight = color.R >= 235 && color.G >= 235 && color.B >= 235;
        bool lowSaturation = (max - min) <= 18;

        return veryLight && lowSaturation;
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

    private static string GetTrackDisplayName(string path)
    {
        string fileName = Path.GetFileName(path);

        if (!LooksLikeGeneratedFileName(path))
            return fileName;

        AudioMetadata metadata = TryReadAudioMetadata(path);

        if (!string.IsNullOrWhiteSpace(metadata.Title) && !string.IsNullOrWhiteSpace(metadata.Artist))
            return $"{metadata.Artist} - {metadata.Title}";

        if (!string.IsNullOrWhiteSpace(metadata.Title))
            return metadata.Title;

        return fileName;
    }

    private static bool LooksLikeGeneratedFileName(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);

        if (name.Length < 16)
            return false;

        int separators = name.Count(ch => ch is ' ' or '-' or '_' or '.' or '(' or ')' or '[' or ']');
        int alnum = name.Count(char.IsLetterOrDigit);
        int lower = name.Count(char.IsLower);
        int upper = name.Count(char.IsUpper);
        int digits = name.Count(char.IsDigit);

        bool mostlyCompact = alnum >= name.Length * 0.80 && separators <= 2;
        bool mixedGenerated = lower >= 8 && (upper + digits) >= 2;

        return mostlyCompact && mixedGenerated;
    }

    private static AudioMetadata TryReadAudioMetadata(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();

        try
        {
            if (extension == ".mp3")
                return TryReadMp3Id3Metadata(path);

            if (extension is ".m4a" or ".aac")
                return TryReadMp4Metadata(path);
        }
        catch
        {
        }

        return default;
    }

    private static AudioMetadata TryReadMp3Id3Metadata(string path)
    {
        using var stream = File.OpenRead(path);

        if (stream.Length < 10)
            return default;

        byte[] header = new byte[10];

        if (stream.Read(header, 0, header.Length) != header.Length)
            return default;

        if (header[0] != 'I' || header[1] != 'D' || header[2] != '3')
            return default;

        int version = header[3];
        int tagSize = ReadSynchsafeInt(header, 6);

        if (tagSize <= 0)
            return default;

        tagSize = Math.Min(tagSize, 1024 * 1024);

        byte[] tag = new byte[tagSize];
        int read = stream.Read(tag, 0, tag.Length);

        string? title = null;
        string? artist = null;

        int offset = 0;

        while (offset + 10 <= read)
        {
            string frameId = Encoding.ASCII.GetString(tag, offset, 4);

            if (string.IsNullOrWhiteSpace(frameId) || frameId.Any(ch => !char.IsUpper(ch) && !char.IsDigit(ch)))
                break;

            int frameSize = version >= 4
                ? ReadSynchsafeInt(tag, offset + 4)
                : ReadBigEndianInt(tag, offset + 4);

            if (frameSize <= 0 || offset + 10 + frameSize > read)
                break;

            byte[] payload = new byte[frameSize];
            Array.Copy(tag, offset + 10, payload, 0, frameSize);

            if (frameId == "TIT2")
                title = DecodeId3Text(payload);
            else if (frameId == "TPE1")
                artist = DecodeId3Text(payload);

            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(artist))
                break;

            offset += 10 + frameSize;
        }

        return new AudioMetadata(CleanMetadataText(title), CleanMetadataText(artist));
    }

    private static AudioMetadata TryReadMp4Metadata(string path)
    {
        long length = new FileInfo(path).Length;
        int bytesToRead = (int)Math.Min(length, 8L * 1024L * 1024L);

        byte[] data = new byte[bytesToRead];

        using (var stream = File.OpenRead(path))
        {
            _ = stream.Read(data, 0, data.Length);
        }

        string? title = FindMp4TextAtom(data, "\u00A9nam");
        string? artist = FindMp4TextAtom(data, "\u00A9ART") ?? FindMp4TextAtom(data, "aART");

        return new AudioMetadata(CleanMetadataText(title), CleanMetadataText(artist));
    }

    private static string? FindMp4TextAtom(byte[] data, string atomName)
    {
        byte[] atom = Encoding.GetEncoding("ISO-8859-1").GetBytes(atomName);

        for (int i = 0; i + 8 < data.Length; i++)
        {
            if (data[i] != atom[0] || data[i + 1] != atom[1] || data[i + 2] != atom[2] || data[i + 3] != atom[3])
                continue;

            int atomStart = i - 4;

            if (atomStart < 0)
                continue;

            int atomSize = ReadBigEndianInt(data, atomStart);

            if (atomSize <= 16 || atomStart + atomSize > data.Length)
                continue;

            int atomEnd = atomStart + atomSize;

            for (int j = i + 4; j + 16 < atomEnd; j++)
            {
                if (data[j + 4] == 'd' && data[j + 5] == 'a' && data[j + 6] == 't' && data[j + 7] == 'a')
                {
                    int dataSize = ReadBigEndianInt(data, j);

                    if (dataSize <= 16 || j + dataSize > atomEnd)
                        continue;

                    int textOffset = j + 16;
                    int textLength = dataSize - 16;

                    if (textLength <= 0)
                        continue;

                    return Encoding.UTF8.GetString(data, textOffset, textLength);
                }
            }
        }

        return null;
    }

    private static string? DecodeId3Text(byte[] payload)
    {
        if (payload.Length <= 1)
            return null;

        byte encoding = payload[0];
        byte[] textBytes = payload.Skip(1).ToArray();

        return encoding switch
        {
            0 => Encoding.GetEncoding("ISO-8859-1").GetString(textBytes),
            1 => Encoding.Unicode.GetString(textBytes),
            2 => Encoding.BigEndianUnicode.GetString(textBytes),
            3 => Encoding.UTF8.GetString(textBytes),
            _ => Encoding.UTF8.GetString(textBytes)
        };
    }

    private static string? CleanMetadataText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Replace("\0", string.Empty).Trim();

        while (value.Contains("  ", StringComparison.Ordinal))
            value = value.Replace("  ", " ", StringComparison.Ordinal);

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int ReadSynchsafeInt(byte[] data, int offset)
    {
        if (offset + 4 > data.Length)
            return 0;

        return (data[offset] << 21)
            | (data[offset + 1] << 14)
            | (data[offset + 2] << 7)
            | data[offset + 3];
    }

    private static int ReadBigEndianInt(byte[] data, int offset)
    {
        if (offset + 4 > data.Length)
            return 0;

        return (data[offset] << 24)
            | (data[offset + 1] << 16)
            | (data[offset + 2] << 8)
            | data[offset + 3];
    }

    private readonly record struct AudioMetadata(string? Title, string? Artist);

    
    
    
    private sealed class DirectSeekTrackBar : TrackBar
    {
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEWHEEL = 0x020A;

        private const int WM_USER = 0x0400;
        private const int TBM_GETTHUMBRECT = WM_USER + 25;
        private const int TBM_GETCHANNELRECT = WM_USER + 26;

        private const int ThumbHitPadding = 5;
        private const int DragStartThresholdPixels = 1;
        private const int WheelDelta = 120;

        private bool _mouseDown;
        private bool _draggingThumb;
        private bool _dragMoved;
        private int _dragOffsetPixels;
        private int _mouseDownX;

        public event Action<int>? SeekRequested;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public Rectangle ToRectangle()
            {
                return Rectangle.FromLTRB(Left, Top, Right, Bottom);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref NativeRect lParam);

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            if (CanFocus)
                Focus();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_LBUTTONDOWN)
            {
                HandleNativeLeftDown(GetPointFromLParam(m.LParam));
                return;
            }

            if (m.Msg == WM_MOUSEMOVE && _mouseDown)
            {
                HandleNativeMouseMove(GetPointFromLParam(m.LParam));
                return;
            }

            if (m.Msg == WM_LBUTTONUP && _mouseDown)
            {
                HandleNativeLeftUp(GetPointFromLParam(m.LParam));
                return;
            }

            if (m.Msg == WM_MOUSEWHEEL)
            {
                HandleNativeMouseWheel(m.WParam);
                m.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            HandleMouseWheelDelta(e.Delta);
        }

    private void HandleNativeLeftDown(Point point)
        {
            Focus();
            Capture = true;

            _mouseDown = true;
            _dragMoved = false;
            _mouseDownX = point.X;

            Rectangle thumb = GetThumbRectangle();
            Rectangle hit = thumb;
            hit.Inflate(ThumbHitPadding, ThumbHitPadding);

            if (hit.Contains(point))
            {
                _draggingThumb = true;
                _dragOffsetPixels = point.X - GetThumbCenterX();
                return;
            }

            _draggingThumb = false;
            _dragOffsetPixels = 0;
            SetValueFromTrackX(point.X);
        }

    private void HandleNativeMouseMove(Point point)
        {
            if (!_mouseDown)
                return;

            if (_draggingThumb)
            {
                int adjustedCenterX = point.X - _dragOffsetPixels;

                if (!_dragMoved && Math.Abs(point.X - _mouseDownX) < DragStartThresholdPixels)
                    return;

                _dragMoved = true;
                SetValueFromTrackX(adjustedCenterX);
                return;
            }

            SetValueFromTrackX(point.X);
        }

    private void HandleNativeLeftUp(Point point)
        {
            if (_draggingThumb)
            {
                if (_dragMoved)
                    SetValueFromTrackX(point.X - _dragOffsetPixels);
            }
            else
            {
                SetValueFromTrackX(point.X);
            }

            _mouseDown = false;
            _draggingThumb = false;
            _dragMoved = false;
            _dragOffsetPixels = 0;
            _mouseDownX = 0;
            Capture = false;
        }

    private void HandleNativeMouseWheel(IntPtr wParam)
        {
            long raw = wParam.ToInt64();
            int delta = unchecked((short)((raw >> 16) & 0xFFFF));

            HandleMouseWheelDelta(delta);
        }

    private void HandleMouseWheelDelta(int delta)
        {
            if (delta == 0 || Maximum <= Minimum)
                return;

            Focus();

            int notches = delta / WheelDelta;

            if (notches == 0)
                notches = delta > 0 ? 1 : -1;

            int step = GetWheelSeekStep();
            int newValue = Value + (step * notches);

            SetValueAndNotify(newValue);
        }

    private int GetWheelSeekStep()
        {
            int range = Math.Max(1, Maximum - Minimum);

            return Math.Clamp(range / 40, 1, 5);
        }

    private Rectangle GetThumbRectangle()
        {
            if (!IsHandleCreated)
                return Rectangle.Empty;

            var rect = new NativeRect();
            SendMessage(Handle, TBM_GETTHUMBRECT, IntPtr.Zero, ref rect);

            return rect.ToRectangle();
        }

    private Rectangle GetChannelRectangle()
        {
            if (!IsHandleCreated)
                return new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));

            var rect = new NativeRect();
            SendMessage(Handle, TBM_GETCHANNELRECT, IntPtr.Zero, ref rect);

            Rectangle channel = rect.ToRectangle();

            if (channel.Width <= 0)
                channel = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));

            return channel;
        }

    private int GetThumbCenterX()
        {
            Rectangle thumb = GetThumbRectangle();

            if (thumb.Width <= 0)
                return ValueToTrackX(Value);

            return thumb.Left + (thumb.Width / 2);
        }

    private void SetValueFromTrackX(int x)
        {
            SetValueAndNotify(TrackXToValue(x));
        }

    private int TrackXToValue(int x)
        {
            if (Maximum <= Minimum)
                return Minimum;

            Rectangle channel = GetChannelRectangle();

            int left = channel.Left;
            int right = Math.Max(left + 1, channel.Right);
            int clampedX = Math.Clamp(x, left, right);

            double ratio = (clampedX - left) / (double)(right - left);
            int value = Minimum + (int)Math.Round((Maximum - Minimum) * ratio);

            return Math.Clamp(value, Minimum, Maximum);
        }

    private int ValueToTrackX(int value)
        {
            if (Maximum <= Minimum)
                return 0;

            Rectangle channel = GetChannelRectangle();

            int left = channel.Left;
            int right = Math.Max(left + 1, channel.Right);

            double ratio = (value - Minimum) / (double)(Maximum - Minimum);

            return left + (int)Math.Round((right - left) * ratio);
        }

    private void SetValueAndNotify(int value)
        {
            value = Math.Clamp(value, Minimum, Maximum);

            if (Value == value)
                return;

            Value = value;
            SeekRequested?.Invoke(Value);
        }

    private static Point GetPointFromLParam(IntPtr lParam)
        {
            long raw = lParam.ToInt64();

            int x = unchecked((short)(raw & 0xFFFF));
            int y = unchecked((short)((raw >> 16) & 0xFFFF));

            return new Point(x, y);
        }
    }

    private void ApplyAudioStabilityTuning()
    {
        try
        {
            using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();

            // Perfil Opera Survivor +15:
            // High sin RealTime. Agresivo, pero no suicida.
            currentProcess.PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
            currentProcess.PriorityBoostEnabled = true;
        }
        catch
        {
        }

        try
        {
            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
        }
        catch
        {
        }

        try
        {
            System.Threading.ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);

            int targetWorkerThreads = Math.Max(workerThreads, Environment.ProcessorCount * 4);
            int targetCompletionThreads = Math.Max(completionPortThreads, 8);

            System.Threading.ThreadPool.SetMinThreads(targetWorkerThreads, targetCompletionThreads);
        }
        catch
        {
        }
    }

    private NAudio.Wave.WaveOutEvent CreateStableWaveOut()
    {
        return new NAudio.Wave.WaveOutEvent
        {
            // Opera Survivor +15%:
            // 2500 ms -> 2875 ms
            // 10 buffers -> 12 buffers
            DesiredLatency = 2875,
            NumberOfBuffers = 12
        };
    }

    private NAudio.Wave.AudioFileReader CreateStableAudioFileReader(string path)
    {
        PrewarmAudioFile(path);
        return new NAudio.Wave.AudioFileReader(path);
    }

    private void PrewarmAudioFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                return;

            const int bufferSize = 1024 * 1024;

            byte[] buffer = new byte[bufferSize];

            using var stream = new System.IO.FileStream(
                path,
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read,
                System.IO.FileShare.ReadWrite,
                bufferSize,
                System.IO.FileOptions.SequentialScan
            );

            while (stream.Read(buffer, 0, buffer.Length) > 0)
            {
            }
        }
        catch
        {
        }
    }

    private void TryLoadIcon()
    {
        try
        {
            string[] candidates =
            [
                System.IO.Path.Combine(AppContext.BaseDirectory, "LiteAmp.ico"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "LiteAmp.ico"),
                System.IO.Path.Combine(Environment.CurrentDirectory, "LiteAmp.ico")
            ];

            foreach (string candidate in candidates)
            {
                try
                {
                    string path = System.IO.Path.GetFullPath(candidate);

                    if (System.IO.File.Exists(path))
                    {
                        Icon = new System.Drawing.Icon(path);
                        return;
                    }
                }
                catch
                {
                }
            }

            try
            {
                System.Drawing.Icon? extractedIcon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);

                if (extractedIcon is not null)
                    Icon = extractedIcon;
            }
            catch
            {
            }
        }
        catch
        {
        }
    }

    private System.Windows.Forms.TextBox? _playlistSearchTextBox;
    private void PlayPlaylistSelectionFromSearch()
    {
        if (_playlistBox.Items.Count == 0 || _playlistBox.SelectedIndex < 0)
            return;

        System.Windows.Forms.Button? playButton =
            FindButtonByText(this, "Reproducir") ??
            FindButtonByText(this, "Pausar") ??
            FindButtonByText(this, "Play");

        if (playButton is not null)
        {
            playButton.PerformClick();
            return;
        }

        System.Media.SystemSounds.Beep.Play();
    }

    private void PositionPlaylistSearchControls()
    {
        if (_playlistSearchTextBox is null)
            return;

        System.Windows.Forms.CheckBox? alwaysOnTopCheckBox =
            FindControlByText<System.Windows.Forms.CheckBox>(this, "Siempre encima");

        const int gap = 16;
        const int minLeft = 310;
        const int preferredWidth = 220;
        const int minWidth = 180;

        if (alwaysOnTopCheckBox is not null)
        {
            int rightEdge = alwaysOnTopCheckBox.Left - gap;
            int width = System.Math.Min(preferredWidth, System.Math.Max(minWidth, rightEdge - minLeft));
            int left = System.Math.Max(minLeft, rightEdge - width);

            int top = alwaysOnTopCheckBox.Top +
                      ((alwaysOnTopCheckBox.Height - _playlistSearchTextBox.Height) / 2);

            if (top < 18)
                top = 18;

            _playlistSearchTextBox.Size = new System.Drawing.Size(width, 24);
            _playlistSearchTextBox.Location = new System.Drawing.Point(left, top);
            _playlistSearchTextBox.BringToFront();
            return;
        }

        // Fallback si se cambia el texto del checkbox en el futuro.
        _playlistSearchTextBox.Size = new System.Drawing.Size(220, 24);
        _playlistSearchTextBox.Location = new System.Drawing.Point(System.Math.Max(330, ClientSize.Width - 382), 54);
        _playlistSearchTextBox.BringToFront();
    }

    private static T? FindControlByText<T>(System.Windows.Forms.Control parent, string text)
        where T : System.Windows.Forms.Control
    {
        foreach (System.Windows.Forms.Control control in parent.Controls)
        {
            if (control is T typedControl &&
                string.Equals(control.Text?.Trim(), text, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return typedControl;
            }

            T? nested = FindControlByText<T>(control, text);

            if (nested is not null)
                return nested;
        }

        return null;
    }

    private bool _keyboardShortcutsAttached;

    private void AttachKeyboardShortcutsOnce()
    {
        if (_keyboardShortcutsAttached)
            return;

        _keyboardShortcutsAttached = true;

        KeyPreview = true;
        KeyDown += HandleLiteAmpGlobalKeyDown;

        _playlistBox.KeyDown += HandlePlaylistKeyboardShortcut;
    }

    private void HandlePlaylistKeyboardShortcut(object? sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyCode == System.Windows.Forms.Keys.Enter)
        {
            e.SuppressKeyPress = true;
            PerformPlayPauseFromKeyboard();
            return;
        }

        if (e.KeyCode == System.Windows.Forms.Keys.Space)
        {
            e.SuppressKeyPress = true;
            PerformPlayPauseFromKeyboard();
        }
    }

    private void HandleLiteAmpGlobalKeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyCode != System.Windows.Forms.Keys.Space)
            return;

        if (IsTimelineKeyboardBlockedControl(ActiveControl))
            return;

        e.SuppressKeyPress = true;
        PerformPlayPauseFromKeyboard();
    }

    private static bool IsTextInputControl(System.Windows.Forms.Control? control)
    {
        while (control is not null)
        {
            if (control is System.Windows.Forms.TextBoxBase ||
                control is System.Windows.Forms.ComboBox)
            {
                return true;
            }

            control = control.Parent;
        }

        return false;
    }

    private void PerformPlayPauseFromKeyboard()
    {
        System.Windows.Forms.Button? playPauseButton =
            FindButtonByText(this, "Reproducir") ??
            FindButtonByText(this, "Pausar") ??
            FindButtonByText(this, "Play") ??
            FindButtonByText(this, "Pause");

        if (playPauseButton is not null)
        {
            playPauseButton.PerformClick();
            return;
        }

        System.Media.SystemSounds.Beep.Play();
    }

    private static System.Windows.Forms.Button? FindButtonByText(System.Windows.Forms.Control parent, string text)
    {
        foreach (System.Windows.Forms.Control control in parent.Controls)
        {
            if (control is System.Windows.Forms.Button button &&
                string.Equals(button.Text?.Trim(), text, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return button;
            }

            System.Windows.Forms.Button? nested = FindButtonByText(control, text);

            if (nested is not null)
                return nested;
        }

        return null;
    }

    private readonly System.Collections.Generic.List<System.Collections.Generic.List<string>> _playlistSearchIndex = new();
    private int _playlistSearchIndexedCount = -1;

    private void RebuildPlaylistSearchIndex()
    {
        _playlistSearchIndex.Clear();

        for (int i = 0; i < _playlistBox.Items.Count; i++)
        {
            string itemText = System.Convert.ToString(_playlistBox.Items[i]) ?? string.Empty;
            _playlistSearchIndex.Add(LiteAmpSearchBuildKeys(itemText));
        }

        _playlistSearchIndexedCount = _playlistBox.Items.Count;
    }

    private void SearchPlaylistLive()
    {
        if (_playlistSearchTextBox is null)
            return;

        string query = _playlistSearchTextBox.Text.Trim();

        if (query.Length == 0)
            return;

        if (_playlistBox.Items.Count == 0)
            return;

        if (_playlistSearchIndexedCount != _playlistBox.Items.Count)
            RebuildPlaylistSearchIndex();

        string normalizedQuery = LiteAmpSearchNormalize(query);

        if (normalizedQuery.Length == 0)
            return;

        int bestContainsIndex = -1;

        for (int i = 0; i < _playlistSearchIndex.Count; i++)
        {
            foreach (string key in _playlistSearchIndex[i])
            {
                if (key.StartsWith(normalizedQuery, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    _playlistBox.SelectedIndex = i;
                    _playlistBox.TopIndex = System.Math.Max(0, i - 2);
                    return;
                }

                if (bestContainsIndex < 0 &&
                    key.IndexOf(normalizedQuery, System.StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    bestContainsIndex = i;
                }
            }
        }

        if (bestContainsIndex >= 0)
        {
            _playlistBox.SelectedIndex = bestContainsIndex;
            _playlistBox.TopIndex = System.Math.Max(0, bestContainsIndex - 2);
        }
    }

    private static System.Collections.Generic.List<string> LiteAmpSearchBuildKeys(string itemText)
    {
        var keys = new System.Collections.Generic.List<string>();

        LiteAmpSearchAddKey(keys, itemText);

        string fileName = LiteAmpSearchTryFileName(itemText);

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            LiteAmpSearchAddKey(keys, fileName);

            try
            {
                string withoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
                LiteAmpSearchAddKey(keys, withoutExtension);
            }
            catch
            {
            }
        }

        return keys;
    }

    private static void LiteAmpSearchAddKey(System.Collections.Generic.List<string> keys, string value)
    {
        string normalized = LiteAmpSearchNormalize(value);

        if (normalized.Length == 0)
            return;

        if (!keys.Contains(normalized, System.StringComparer.CurrentCultureIgnoreCase))
            keys.Add(normalized);
    }

    private static string LiteAmpSearchTryFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        try
        {
            string fileName = System.IO.Path.GetFileName(value);

            if (!string.IsNullOrWhiteSpace(fileName))
                return fileName;
        }
        catch
        {
        }

        return value;
    }

    private static string LiteAmpSearchNormalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string normalized = text
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Replace('.', ' ')
            .Replace('[', ' ')
            .Replace(']', ' ')
            .Replace('(', ' ')
            .Replace(')', ' ')
            .Replace('{', ' ')
            .Replace('}', ' ')
            .Trim();

        while (normalized.Contains("  ", System.StringComparison.Ordinal))
            normalized = normalized.Replace("  ", " ");

        return normalized;
    }

    private void AttachPlaylistSearchOnce()
    {
        if (_playlistSearchTextBox is not null)
            return;

        _playlistSearchTextBox = new System.Windows.Forms.TextBox
        {
            Name = "playlistSearchTextBox",
            PlaceholderText = "Buscar canción...",
            Size = new System.Drawing.Size(210, 24),
            TabStop = true
        };

        _playlistSearchTextBox.TextChanged += (_, _) => SearchPlaylistLive();

                        _playlistSearchTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                PlayPlaylistSelectionFromSearch();
                return;
            }

            if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _playlistSearchTextBox.Clear();
            }
        };
Controls.Add(_playlistSearchTextBox);

        PositionPlaylistSearchControls();

        _playlistSearchTextBox.BringToFront();
    }

    private void MovePlaylistSelectionFromSearch(int delta)
    {
        if (_playlistBox.Items.Count == 0)
            return;

        int currentIndex = _playlistBox.SelectedIndex;

        if (currentIndex < 0)
            currentIndex = 0;

        int nextIndex;

        if (delta == int.MinValue)
        {
            nextIndex = 0;
        }
        else if (delta == int.MaxValue)
        {
            nextIndex = _playlistBox.Items.Count - 1;
        }
        else
        {
            nextIndex = currentIndex + delta;
        }

        nextIndex = System.Math.Max(0, System.Math.Min(_playlistBox.Items.Count - 1, nextIndex));

        if (nextIndex == _playlistBox.SelectedIndex)
            return;

        _playlistBox.SelectedIndex = nextIndex;
        _playlistBox.TopIndex = System.Math.Max(0, nextIndex - 2);
    }

    private bool _timelineKeyboardGuardAttached;

    private void AttachTimelineKeyboardGuardOnce()
    {
        if (_timelineKeyboardGuardAttached)
            return;

        _timelineKeyboardGuardAttached = true;

        GuardTrackBarsFromKeyboard(this);
    }

    private void GuardTrackBarsFromKeyboard(System.Windows.Forms.Control parent)
    {
        foreach (System.Windows.Forms.Control control in parent.Controls)
        {
            if (control is System.Windows.Forms.TrackBar trackBar)
            {
                trackBar.TabStop = false;

                trackBar.KeyDown -= HandleTrackBarKeyDown;
                trackBar.KeyDown += HandleTrackBarKeyDown;

                trackBar.PreviewKeyDown -= HandleTrackBarPreviewKeyDown;
                trackBar.PreviewKeyDown += HandleTrackBarPreviewKeyDown;

                trackBar.Enter += (_, _) => BeginInvoke(new Action(ReturnFocusToPlaylistFromTrackBar));
            }

            if (control.HasChildren)
                GuardTrackBarsFromKeyboard(control);
        }
    }

    private void HandleTrackBarPreviewKeyDown(object? sender, System.Windows.Forms.PreviewKeyDownEventArgs e)
    {
        if (e.KeyCode is System.Windows.Forms.Keys.Left or
            System.Windows.Forms.Keys.Right or
            System.Windows.Forms.Keys.Up or
            System.Windows.Forms.Keys.Down or
            System.Windows.Forms.Keys.Home or
            System.Windows.Forms.Keys.End or
            System.Windows.Forms.Keys.PageUp or
            System.Windows.Forms.Keys.PageDown or
            System.Windows.Forms.Keys.Space)
        {
            BeginInvoke(new Action(ReturnFocusToPlaylistFromTrackBar));
        }
    }

    private void HandleTrackBarKeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyCode is System.Windows.Forms.Keys.Left or
            System.Windows.Forms.Keys.Right or
            System.Windows.Forms.Keys.Up or
            System.Windows.Forms.Keys.Down or
            System.Windows.Forms.Keys.Home or
            System.Windows.Forms.Keys.End or
            System.Windows.Forms.Keys.PageUp or
            System.Windows.Forms.Keys.PageDown or
            System.Windows.Forms.Keys.Space)
        {
            e.SuppressKeyPress = true;
            BeginInvoke(new Action(ReturnFocusToPlaylistFromTrackBar));
        }
    }

    private static bool IsTimelineKeyboardBlockedControl(System.Windows.Forms.Control? control)
    {
        while (control is not null)
        {
            if (control is System.Windows.Forms.TextBoxBase ||
                control is System.Windows.Forms.ComboBox ||
                control is System.Windows.Forms.TrackBar)
            {
                return true;
            }

            control = control.Parent;
        }

        return false;
    }

    private bool _startupFocusFixAttached;
    private bool _emptyPlaylistFocusGuardAttached;
    private LiteAmpFocusSink? _emptyPlaylistFocusSink;

    private void AttachStartupFocusFixOnce()
    {
        if (_startupFocusFixAttached)
            return;

        _startupFocusFixAttached = true;

        EnsureEmptyPlaylistFocusSink();
        DisableComboBoxTabStops(this);
        HookEmptyPlaylistListBoxFocus();

        BeginInvoke(new Action(FocusPlaylistOrSink));
    }

    private void EnsureEmptyPlaylistFocusSink()
    {
        if (_emptyPlaylistFocusSink is not null && !_emptyPlaylistFocusSink.IsDisposed)
            return;

        _emptyPlaylistFocusSink = new LiteAmpFocusSink
        {
            Name = "emptyPlaylistFocusSink",
            Size = new System.Drawing.Size(1, 1),
            Location = new System.Drawing.Point(-200, -200),
            TabStop = false
        };

        Controls.Add(_emptyPlaylistFocusSink);
        _emptyPlaylistFocusSink.SendToBack();
    }

    private void HookEmptyPlaylistListBoxFocus()
    {
        if (_emptyPlaylistFocusGuardAttached)
            return;

        _emptyPlaylistFocusGuardAttached = true;

        _playlistBox.Enter += (_, _) =>
        {
            if (_playlistBox.Items.Count == 0)
                BeginInvoke(new Action(FocusEmptyPlaylistSink));
        };

        _playlistBox.GotFocus += (_, _) =>
        {
            if (_playlistBox.Items.Count == 0)
                BeginInvoke(new Action(FocusEmptyPlaylistSink));
        };

        _playlistBox.MouseDown += (_, _) =>
        {
            if (_playlistBox.Items.Count == 0)
                BeginInvoke(new Action(FocusEmptyPlaylistSink));
        };
    }

    private void FocusEmptyPlaylistSink()
    {
        try
        {
            EnsureEmptyPlaylistFocusSink();

            if (_emptyPlaylistFocusSink is null || _emptyPlaylistFocusSink.IsDisposed)
                return;

            ActiveControl = _emptyPlaylistFocusSink;
            _emptyPlaylistFocusSink.Select();
            _emptyPlaylistFocusSink.Focus();
        }
        catch
        {
        }
    }

    private void ReturnFocusToPlaylistFromTrackBar()
    {
        FocusPlaylistOrSink();
    }

    private void DisableComboBoxTabStops(System.Windows.Forms.Control parent)
    {
        foreach (System.Windows.Forms.Control control in parent.Controls)
        {
            if (control is System.Windows.Forms.ComboBox comboBox)
            {
                comboBox.TabStop = false;
                comboBox.PreviewKeyDown -= HandleComboBoxPreviewKeyDown;
                comboBox.PreviewKeyDown += HandleComboBoxPreviewKeyDown;
            }

            if (control.HasChildren)
                DisableComboBoxTabStops(control);
        }
    }

    private void HandleComboBoxPreviewKeyDown(object? sender, System.Windows.Forms.PreviewKeyDownEventArgs e)
    {
        if (e.KeyCode is System.Windows.Forms.Keys.Up or
            System.Windows.Forms.Keys.Down or
            System.Windows.Forms.Keys.Left or
            System.Windows.Forms.Keys.Right)
        {
            BeginInvoke(new Action(FocusPlaylistOrSink));
        }
    }

    private sealed class LiteAmpFocusSink : System.Windows.Forms.Control
    {
        public LiteAmpFocusSink()
        {
            SetStyle(System.Windows.Forms.ControlStyles.Selectable, true);
        }
    }

    private bool _playlistNativeNavigationAttached;

    private const int LB_SETCURSEL = 0x0186;
    private const int LB_GETCURSEL = 0x0188;
    private const int LB_GETTOPINDEX = 0x018E;
    private const int LB_SETTOPINDEX = 0x0197;

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern System.IntPtr LiteAmpListBoxSendMessage(
        System.IntPtr hWnd,
        int msg,
        System.IntPtr wParam,
        System.IntPtr lParam
    );

    private void AttachPlaylistNativeNavigationOnce()
    {
        if (_playlistNativeNavigationAttached)
            return;

        _playlistNativeNavigationAttached = true;

        EnablePlaylistNativeDoubleBuffering();

        _playlistBox.KeyDown -= HandlePlaylistNativeKeyDown;
        _playlistBox.KeyDown += HandlePlaylistNativeKeyDown;

        _playlistBox.MouseWheel -= HandlePlaylistNativeMouseWheel;
        _playlistBox.MouseWheel += HandlePlaylistNativeMouseWheel;

        if (_playlistSearchTextBox is not null)
        {
            _playlistSearchTextBox.KeyDown -= HandlePlaylistSearchNativeKeyDown;
            _playlistSearchTextBox.KeyDown += HandlePlaylistSearchNativeKeyDown;
        }
    }

    private void HandlePlaylistNativeKeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (!IsPlaylistNativeNavigationKey(e.KeyCode))
            return;

        e.Handled = true;
        e.SuppressKeyPress = true;

        if (e.KeyCode == System.Windows.Forms.Keys.Down)
        {
            MovePlaylistSelectionNative(1);
            return;
        }

        if (e.KeyCode == System.Windows.Forms.Keys.Up)
        {
            MovePlaylistSelectionNative(-1);
            return;
        }

        if (e.KeyCode == System.Windows.Forms.Keys.PageDown)
        {
            MovePlaylistSelectionNative(System.Math.Max(4, GetPlaylistNativeVisibleCount() - 2));
            return;
        }

        if (e.KeyCode == System.Windows.Forms.Keys.PageUp)
        {
            MovePlaylistSelectionNative(-System.Math.Max(4, GetPlaylistNativeVisibleCount() - 2));
            return;
        }

        if (e.KeyCode == System.Windows.Forms.Keys.Home)
        {
            SetPlaylistSelectionNative(0);
            return;
        }

        if (e.KeyCode == System.Windows.Forms.Keys.End)
        {
            SetPlaylistSelectionNative(_playlistBox.Items.Count - 1);
        }
    }

    private void HandlePlaylistSearchNativeKeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (!IsPlaylistNativeNavigationKey(e.KeyCode))
            return;

        e.Handled = true;
        e.SuppressKeyPress = true;

        if (e.KeyCode == System.Windows.Forms.Keys.Down)
        {
            MovePlaylistSelectionNative(1);
            return;
        }

        if (e.KeyCode == System.Windows.Forms.Keys.Up)
        {
            MovePlaylistSelectionNative(-1);
            return;
        }

        if (e.KeyCode == System.Windows.Forms.Keys.PageDown)
        {
            MovePlaylistSelectionNative(System.Math.Max(4, GetPlaylistNativeVisibleCount() - 2));
            return;
        }

        if (e.KeyCode == System.Windows.Forms.Keys.PageUp)
        {
            MovePlaylistSelectionNative(-System.Math.Max(4, GetPlaylistNativeVisibleCount() - 2));
            return;
        }

        if (e.KeyCode == System.Windows.Forms.Keys.Home)
        {
            SetPlaylistSelectionNative(0);
            return;
        }

        if (e.KeyCode == System.Windows.Forms.Keys.End)
        {
            SetPlaylistSelectionNative(_playlistBox.Items.Count - 1);
        }
    }

    private void HandlePlaylistNativeMouseWheel(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (_playlistBox.Items.Count == 0)
            return;

        if (e is System.Windows.Forms.HandledMouseEventArgs handled)
            handled.Handled = true;

        int notches = System.Math.Max(1, System.Math.Abs(e.Delta) / 120);
        int direction = e.Delta > 0 ? -1 : 1;
        int step = System.Math.Max(3, GetPlaylistNativeVisibleCount() / 3);

        ApplyPlaylistNativeWheelScroll(direction * step * notches);
    }

    private void MovePlaylistSelectionNative(int delta)
    {
        if (_playlistBox.Items.Count == 0)
            return;

        int currentIndex = GetPlaylistSelectedIndexNative();

        if (currentIndex < 0)
        {
            if (delta > 0)
            {
                SetPlaylistSelectionNative(0);
                return;
            }

            if (delta < 0)
            {
                SetPlaylistSelectionNative(_playlistBox.Items.Count - 1);
                return;
            }

            return;
        }

        SetPlaylistSelectionNative(currentIndex + delta);
    }

    private int GetPlaylistSelectedIndexNative()
    {
        if (!_playlistBox.IsHandleCreated)
            return _playlistBox.SelectedIndex;

        return LiteAmpListBoxSendMessage(
            _playlistBox.Handle,
            LB_GETCURSEL,
            System.IntPtr.Zero,
            System.IntPtr.Zero
        ).ToInt32();
    }

    private int GetPlaylistTopIndexNative()
    {
        if (!_playlistBox.IsHandleCreated)
            return _playlistBox.TopIndex;

        int topIndex = LiteAmpListBoxSendMessage(
            _playlistBox.Handle,
            LB_GETTOPINDEX,
            System.IntPtr.Zero,
            System.IntPtr.Zero
        ).ToInt32();

        return System.Math.Max(0, topIndex);
    }

    private void SetPlaylistTopIndexNative(int index)
    {
        if (_playlistBox.Items.Count == 0 || !_playlistBox.IsHandleCreated)
            return;

        int maxTop = System.Math.Max(0, _playlistBox.Items.Count - GetPlaylistNativeVisibleCount());
        int safeIndex = System.Math.Max(0, System.Math.Min(maxTop, index));

        LiteAmpListBoxSendMessage(
            _playlistBox.Handle,
            LB_SETTOPINDEX,
            new System.IntPtr(safeIndex),
            System.IntPtr.Zero
        );
    }

    private void EnsurePlaylistNativeSelectionVisible(int selectedIndex)
    {
        if (_playlistBox.Items.Count == 0)
            return;

        int visibleCount = GetPlaylistNativeVisibleCount();
        int topIndex = GetPlaylistTopIndexNative();
        int bottomIndex = System.Math.Min(_playlistBox.Items.Count - 1, topIndex + visibleCount - 1);

        const int edgeMargin = 1;

        if (selectedIndex < topIndex + edgeMargin)
        {
            SetPlaylistTopIndexNative(selectedIndex - edgeMargin);
            return;
        }

        if (selectedIndex > bottomIndex - edgeMargin)
        {
            SetPlaylistTopIndexNative(selectedIndex - visibleCount + 1 + edgeMargin);
        }
    }

    private int GetPlaylistNativeVisibleCount()
    {
        int itemHeight = _playlistBox.ItemHeight;

        if (itemHeight <= 0)
            itemHeight = 16;

        return System.Math.Max(1, _playlistBox.ClientSize.Height / itemHeight);
    }

    private void EnablePlaylistNativeDoubleBuffering()
    {
        try
        {
            typeof(System.Windows.Forms.Control)
                .GetProperty(
                    "DoubleBuffered",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic
                )
                ?.SetValue(_playlistBox, true, null);
        }
        catch
        {
        }
    }

    private static bool IsPlaylistNativeNavigationKey(System.Windows.Forms.Keys key)
    {
        return key == System.Windows.Forms.Keys.Up ||
               key == System.Windows.Forms.Keys.Down ||
               key == System.Windows.Forms.Keys.PageUp ||
               key == System.Windows.Forms.Keys.PageDown ||
               key == System.Windows.Forms.Keys.Home ||
               key == System.Windows.Forms.Keys.End;
    }


    protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, System.Windows.Forms.Keys keyData)
    {
        if (LiteAmpHandlePlaylistIntentKey(keyData))
            return true;

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private bool LiteAmpHandlePlaylistIntentKey(System.Windows.Forms.Keys keyData)
    {
        System.Windows.Forms.Keys key = keyData & System.Windows.Forms.Keys.KeyCode;

        if (key == System.Windows.Forms.Keys.Enter)
            return LiteAmpHandleEnterIntent();

        if (key == System.Windows.Forms.Keys.Space)
            return LiteAmpHandleSpaceIntent();

        return false;
    }

    private bool LiteAmpHandleEnterIntent()
    {
        if (ActiveControl is System.Windows.Forms.Button)
            return false;

        if (LiteAmpHasPlaylistSelection())
        {
            LiteAmpPlayMarkedPlaylistItem();
            return true;
        }

        LiteAmpTogglePlayPause();
        return true;
    }

    private bool LiteAmpHandleSpaceIntent()
    {
        // Si el usuario está escribiendo en búsqueda, Space debe seguir siendo espacio.
        // Romper búsquedas tipo "Linkin Park" sería una estupidez con interfaz.
        if (LiteAmpIsPlaylistSearchBoxActive())
            return false;

        if (LiteAmpIsEditableTextInputActive())
            return false;

        LiteAmpTogglePlayPause();
        return true;
    }

    private bool LiteAmpHasPlaylistSelection()
    {
        try
        {
            return _playlistBox.Items.Count > 0 && _playlistBox.SelectedIndex >= 0;
        }
        catch
        {
            return false;
        }
    }

    private void LiteAmpPlayMarkedPlaylistItem()
    {
        try
        {
            if (!LiteAmpRaisePlaylistDoubleClick())
            {
                // Fallback seguro: solo pulsa Reproducir si el botón está en modo reproducir.
                // No pulsa Pausar aquí, porque Enter sobre canción marcada no debe pausar.
                LiteAmpClickButtonByText(false, "Reproducir", "▶", "Play");
            }
        }
        catch
        {
        }
    }

    private void LiteAmpTogglePlayPause()
    {
        try
        {
            if (LiteAmpClickButtonByText(true, "Pausar", "Reanudar", "Reproducir", "⏸", "▶", "Pause", "Play"))
                return;
        }
        catch
        {
        }
    }

    private bool LiteAmpRaisePlaylistDoubleClick()
    {
        try
        {
            System.Reflection.MethodInfo? method = typeof(System.Windows.Forms.Control).GetMethod(
                "OnDoubleClick",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );

            if (method is null)
                return false;

            method.Invoke(_playlistBox, new object[] { System.EventArgs.Empty });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool LiteAmpClickButtonByText(bool allowPauseButton, params string[] texts)
    {
        System.Windows.Forms.Button? button = LiteAmpFindButtonByText(this, allowPauseButton, texts);

        if (button is null)
            return false;

        button.PerformClick();
        return true;
    }

    private System.Windows.Forms.Button? LiteAmpFindButtonByText(
        System.Windows.Forms.Control parent,
        bool allowPauseButton,
        params string[] texts
    )
    {
        foreach (System.Windows.Forms.Control control in parent.Controls)
        {
            if (control is System.Windows.Forms.Button button)
            {
                string text = (button.Text ?? string.Empty).Trim();

                foreach (string wanted in texts)
                {
                    if (text.Equals(wanted, System.StringComparison.OrdinalIgnoreCase) ||
                        text.Contains(wanted, System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (!allowPauseButton &&
                            text.Contains("Pausar", System.StringComparison.OrdinalIgnoreCase))
                            continue;

                        return button;
                    }
                }
            }

            if (control.HasChildren)
            {
                System.Windows.Forms.Button? child = LiteAmpFindButtonByText(control, allowPauseButton, texts);

                if (child is not null)
                    return child;
            }
        }

        return null;
    }

    private bool LiteAmpIsPlaylistSearchBoxActive()
    {
        return ActiveControl == _playlistSearchTextBox;
    }

    private bool LiteAmpIsEditableTextInputActive()
    {
        if (ActiveControl is System.Windows.Forms.TextBoxBase textBox)
            return !textBox.ReadOnly;

        return false;
    }

    private bool _playlistDeselectFilterAttached;
    private LiteAmpPlaylistDeselectMessageFilter? _playlistDeselectFilter;
    private LiteAmpDeselectFocusSink? _playlistDeselectFocusSink;

    private const int LiteAmpDeselect_LB_SETCURSEL = 0x0186;

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern System.IntPtr LiteAmpDeselectSendMessage(
        System.IntPtr hWnd,
        int msg,
        System.IntPtr wParam,
        System.IntPtr lParam
    );

    private void LiteAmpInstallPlaylistDeselectFilterOnce()
    {
        if (_playlistDeselectFilterAttached)
            return;

        _playlistDeselectFilterAttached = true;

        LiteAmpEnsureDeselectFocusSink();

        _playlistDeselectFilter = new LiteAmpPlaylistDeselectMessageFilter(this);
        System.Windows.Forms.Application.AddMessageFilter(_playlistDeselectFilter);

        FormClosed += (_, _) =>
        {
            if (_playlistDeselectFilter is not null)
                System.Windows.Forms.Application.RemoveMessageFilter(_playlistDeselectFilter);
        };
    }

    private void LiteAmpHandleGlobalPlaylistDeselectMouseDown(System.IntPtr hwnd)
    {
        try
        {
            System.Windows.Forms.Control? clickedControl = System.Windows.Forms.Control.FromChildHandle(hwnd) ?? System.Windows.Forms.Control.FromHandle(hwnd);

            if (clickedControl is null)
                return;

            if (LiteAmpIsControlInsidePlaylist(clickedControl))
                return;

            if (LiteAmpShouldIgnoreGlobalDeselectClick(clickedControl))
                return;

            bool moveFocusToSink = !(clickedControl is System.Windows.Forms.TextBoxBase);

        if (ReferenceEquals(clickedControl, _playlistSearchTextBox))
            moveFocusToSink = false;

            LiteAmpClearPlaylistSelectionStrong(moveFocusToSink);
        }
        catch
        {
        }
    }

    private bool LiteAmpShouldIgnoreGlobalDeselectClick(System.Windows.Forms.Control control)
    {
        System.Windows.Forms.Control? current = control;

        while (current is not null)
        {
            if (current is System.Windows.Forms.Button ||
                
                current is System.Windows.Forms.ComboBox ||
                current is System.Windows.Forms.CheckBox ||
                current is System.Windows.Forms.RadioButton)
            {
                return true;
            }

            if (ReferenceEquals(current, _playlistBox))
                return true;

            current = current.Parent;
        }

        return false;
    }

    private bool LiteAmpIsControlInsidePlaylist(System.Windows.Forms.Control control)
    {
        return LiteAmpIsSameOrChild(control, _playlistBox);
    }

    private static bool LiteAmpIsSameOrChild(System.Windows.Forms.Control control, System.Windows.Forms.Control parent)
    {
        System.Windows.Forms.Control? current = control;

        while (current is not null)
        {
            if (ReferenceEquals(current, parent))
                return true;

            current = current.Parent;
        }

        return false;
    }

    private void LiteAmpClearPlaylistSelectionStrong(bool moveFocusToSink)
    {
        try
        {
            if (_playlistBox.Items.Count == 0)
                return;

            if (_playlistBox.IsHandleCreated)
            {
                LiteAmpDeselectSendMessage(
                    _playlistBox.Handle,
                    LiteAmpDeselect_LB_SETCURSEL,
                    new System.IntPtr(-1),
                    System.IntPtr.Zero
                );
            }

            _playlistBox.ClearSelected();
            _playlistBox.SelectedIndex = -1;
            _playlistBox.Invalidate();

            if (moveFocusToSink)
                LiteAmpFocusDeselectSink();
        }
        catch
        {
        }
    }

    private void LiteAmpEnsureDeselectFocusSink()
    {
        if (_playlistDeselectFocusSink is not null && !_playlistDeselectFocusSink.IsDisposed)
            return;

        _playlistDeselectFocusSink = new LiteAmpDeselectFocusSink
        {
            Name = "playlistDeselectFocusSink",
            Size = new System.Drawing.Size(1, 1),
            Location = new System.Drawing.Point(-280, -280),
            TabStop = false
        };

        Controls.Add(_playlistDeselectFocusSink);
        _playlistDeselectFocusSink.SendToBack();
    }

    private void LiteAmpFocusDeselectSink()
    {
        try
        {
            LiteAmpEnsureDeselectFocusSink();

            if (_playlistDeselectFocusSink is null || _playlistDeselectFocusSink.IsDisposed)
                return;

            ActiveControl = _playlistDeselectFocusSink;
            _playlistDeselectFocusSink.Select();
            _playlistDeselectFocusSink.Focus();
        }
        catch
        {
        }
    }

    private void FocusPlaylistOrSink()
    {
        try
        {
            if (_playlistBox.Items.Count > 0 &&
                _playlistBox.SelectedIndex >= 0 &&
                _playlistBox.CanFocus)
            {
                ActiveControl = _playlistBox;
                _playlistBox.Focus();
                return;
            }

            LiteAmpFocusDeselectSink();
        }
        catch
        {
        }
    }

    private void FocusPlaylistIfUseful()
    {
        FocusPlaylistOrSink();
    }

    private void SetStartupFocusWithoutEmptyPlaylistCue()
    {
        try
        {
            if (_playlistBox.Items.Count > 0 &&
                _playlistBox.SelectedIndex >= 0 &&
                _playlistBox.CanFocus)
            {
                ActiveControl = _playlistBox;
                _playlistBox.Focus();
                return;
            }

            LiteAmpFocusDeselectSink();
        }
        catch
        {
        }
    }

    private sealed class LiteAmpDeselectFocusSink : System.Windows.Forms.Control
    {
        public LiteAmpDeselectFocusSink()
        {
            SetStyle(System.Windows.Forms.ControlStyles.Selectable, true);
        }
    }

    private sealed class LiteAmpPlaylistDeselectMessageFilter : System.Windows.Forms.IMessageFilter
    {
        private const int WM_LBUTTONDOWN = 0x0201;
        private readonly MainForm _owner;

        public LiteAmpPlaylistDeselectMessageFilter(MainForm owner)
        {
            _owner = owner;
        }

        public bool PreFilterMessage(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == WM_LBUTTONDOWN)
                _owner.LiteAmpHandleGlobalPlaylistDeselectMouseDown(m.HWnd);

            return false;
        }
    }

    private void SetPlaylistSelectionNative(int index)
    {
        if (_playlistBox.Items.Count == 0)
            return;

        int safeIndex = System.Math.Max(0, System.Math.Min(_playlistBox.Items.Count - 1, index));

        if (!_playlistBox.IsHandleCreated)
        {
            if (_playlistBox.SelectedIndex != safeIndex)
                _playlistBox.SelectedIndex = safeIndex;

            return;
        }

        int currentIndex = GetPlaylistSelectedIndexNative();

        // Evita redibujado fantasma cuando se mantiene flecha en el borde.
        if (currentIndex == safeIndex)
            return;

        LiteAmpListBoxSendMessage(
            _playlistBox.Handle,
            LB_SETCURSEL,
            new System.IntPtr(safeIndex),
            System.IntPtr.Zero
        );

        // No EnsureVisible manual. No Invalidate manual.
        // LB_SETCURSEL ya mueve el ListBox cuando debe.
    }

    private void ApplyPlaylistNativeWheelScroll(int delta)
    {
        if (_playlistBox.Items.Count == 0)
            return;

        int topIndex = GetPlaylistTopIndexNative();
        SetPlaylistTopIndexNative(topIndex + delta);

        // No Invalidate manual: SetTopIndex repinta lo necesario.
    }

    private void AddPaths(IEnumerable<string> paths)
    {
        int added = 0;
        var candidates = new System.Collections.Generic.List<string>();

        foreach (string path in paths)
        {
            if (System.IO.File.Exists(path))
            {
                candidates.Add(path);
                continue;
            }

            if (System.IO.Directory.Exists(path))
            {
                foreach (string file in EnumerateSupportedFiles(path))
                    candidates.Add(file);
            }
        }

        if (candidates.Count == 0)
        {
            ShowStatus("No se añadieron pistas nuevas");
            return;
        }

        _playlistBox.BeginUpdate();

        try
        {
            foreach (string file in candidates)
            {
                if (AddTrack(file))
                    added++;
            }

            if (_currentIndex < 0 && _playlist.Count > 0)
            {
                _currentIndex = 0;
                _playlistBox.SelectedIndex = 0;
                UpdateTitleLabel();
            }
        }
        finally
        {
            _playlistBox.EndUpdate();
        }

        if (added > 0)
        {
                try
                {
                    RebuildPlaylistSearchIndex();
                }
                catch
                {
                }
            ShowStatus($"Añadidas {added} pista(s)");
        }
        else
        {
            ShowStatus("No se añadieron pistas nuevas");
        }
    }

    private void RefreshPlaybackVisualState()
    {
        bool hasTrack = _audio.HasTrack;
        bool isPlaying = _audio.IsPlaying;

        if (isPlaying)
        {
            ApplyAccentButtonStyle(_playButton);
            _titleLabel.BackColor = _playingSurface;
            _statusLabel.ForeColor = _accent;
            _timeLabel.ForeColor = _accent;
            return;
        }

        ApplyDefaultButtonStyle(_playButton);

        if (hasTrack)
        {
            _titleLabel.BackColor = _pausedSurface;
            _statusLabel.ForeColor = _textMuted;
            _timeLabel.ForeColor = _textMuted;
            return;
        }

        _titleLabel.BackColor = _panel;
        _statusLabel.ForeColor = _textMuted;
        _timeLabel.ForeColor = _textMuted;
    }

    private bool _liteAmpArrowFilterAttached;
    private LiteAmpArrowMessageFilter? _liteAmpArrowMessageFilter;
    private System.Windows.Forms.Timer? _liteAmpArrowTimer;
    private int _liteAmpArrowDelta;
    private bool _liteAmpArrowActive;

    private void AttachLiteAmpArrowMessageFilterOnce()
    {
        if (_liteAmpArrowFilterAttached)
            return;

        _liteAmpArrowFilterAttached = true;

        _liteAmpArrowTimer = new System.Windows.Forms.Timer
        {
            Interval = 180
        };

        _liteAmpArrowTimer.Tick += (_, _) =>
        {
            if (_liteAmpArrowActive && _liteAmpArrowDelta != 0)
            {
                if (_liteAmpArrowTimer.Interval != 55)
                    _liteAmpArrowTimer.Interval = 55;

                LiteAmpArrowMove(_liteAmpArrowDelta);
            }
        };

        _playlistBox.KeyDown -= HandlePlaylistNativeKeyDown;

        if (_playlistSearchTextBox is not null)
            _playlistSearchTextBox.KeyDown -= HandlePlaylistSearchNativeKeyDown;

        _liteAmpArrowMessageFilter = new LiteAmpArrowMessageFilter(this);
        System.Windows.Forms.Application.AddMessageFilter(_liteAmpArrowMessageFilter);

        FormClosed += (_, _) =>
        {
            try
            {
                if (_liteAmpArrowMessageFilter is not null)
                    System.Windows.Forms.Application.RemoveMessageFilter(_liteAmpArrowMessageFilter);

                _liteAmpArrowTimer?.Stop();
                _liteAmpArrowTimer?.Dispose();
            }
            catch
            {
            }
        };
    }

    private bool LiteAmpArrowShouldHandle()
    {
        try
        {
            System.Windows.Forms.Control? active = ActiveControl;

            return ReferenceEquals(active, _playlistBox) ||
                   ReferenceEquals(active, _playlistSearchTextBox);
        }
        catch
        {
            return false;
        }
    }

    private void LiteAmpArrowStop()
    {
        _liteAmpArrowActive = false;
        _liteAmpArrowDelta = 0;
        _liteAmpArrowTimer?.Stop();
    }

    private void LiteAmpArrowMove(int delta)
    {
        if (_playlistBox.Items.Count == 0)
            return;

        int currentIndex = _playlistBox.SelectedIndex;

        if (currentIndex < 0)
            currentIndex = delta > 0 ? -1 : _playlistBox.Items.Count;

        LiteAmpArrowSetSelection(currentIndex + delta);
    }

    private void LiteAmpArrowSetSelection(int index)
    {
        if (_playlistBox.Items.Count == 0)
            return;

        int safeIndex = System.Math.Max(0, System.Math.Min(_playlistBox.Items.Count - 1, index));

        if (_playlistBox.SelectedIndex == safeIndex)
            return;

        _playlistBox.SelectedIndex = safeIndex;
        LiteAmpArrowEnsureVisible(safeIndex);
    }

    private void LiteAmpArrowEnsureVisible(int selectedIndex)
    {
        try
        {
            if (_playlistBox.Items.Count == 0)
                return;

            int itemHeight = _playlistBox.ItemHeight;

            if (itemHeight <= 0)
                itemHeight = 16;

            int visibleCount = System.Math.Max(1, _playlistBox.ClientSize.Height / itemHeight);
            int topIndex = _playlistBox.TopIndex;
            int bottomIndex = System.Math.Min(_playlistBox.Items.Count - 1, topIndex + visibleCount - 1);

            const int edgeMargin = 2;

            if (selectedIndex < topIndex + edgeMargin)
            {
                int newTop = System.Math.Max(0, selectedIndex - edgeMargin);

                if (_playlistBox.TopIndex != newTop)
                    _playlistBox.TopIndex = newTop;

                return;
            }

            if (selectedIndex > bottomIndex - edgeMargin)
            {
                int maxTop = System.Math.Max(0, _playlistBox.Items.Count - visibleCount);
                int newTop = selectedIndex - visibleCount + 1 + edgeMargin;
                newTop = System.Math.Max(0, System.Math.Min(maxTop, newTop));

                if (_playlistBox.TopIndex != newTop)
                    _playlistBox.TopIndex = newTop;
            }
        }
        catch
        {
        }
    }

    private static bool LiteAmpArrowIsNavKey(System.Windows.Forms.Keys key)
    {
        return key == System.Windows.Forms.Keys.Up ||
               key == System.Windows.Forms.Keys.Down ||
               key == System.Windows.Forms.Keys.PageUp ||
               key == System.Windows.Forms.Keys.PageDown ||
               key == System.Windows.Forms.Keys.Home ||
               key == System.Windows.Forms.Keys.End;
    }

    private sealed class LiteAmpArrowMessageFilter : System.Windows.Forms.IMessageFilter
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private readonly MainForm _owner;

        public LiteAmpArrowMessageFilter(MainForm owner)
        {
            _owner = owner;
        }

        public bool PreFilterMessage(ref System.Windows.Forms.Message m)
        {
            if (m.Msg != WM_KEYDOWN &&
                m.Msg != WM_KEYUP &&
                m.Msg != WM_SYSKEYDOWN &&
                m.Msg != WM_SYSKEYUP)
            {
                return false;
            }

            var key = (System.Windows.Forms.Keys)((int)m.WParam) & System.Windows.Forms.Keys.KeyCode;

            if (!LiteAmpArrowIsNavKey(key))
                return false;

            if (!_owner.LiteAmpArrowShouldHandle())
                return false;

            if (m.Msg == WM_KEYUP || m.Msg == WM_SYSKEYUP)
            {
                if (key == System.Windows.Forms.Keys.Down ||
                    key == System.Windows.Forms.Keys.Up)
                {
                    _owner.LiteAmpArrowStop();
                }

                return true;
            }

            if (key == System.Windows.Forms.Keys.Down)
            {
                _owner.LiteAmpArrowStart(1);
                return true;
            }

            if (key == System.Windows.Forms.Keys.Up)
            {
                _owner.LiteAmpArrowStart(-1);
                return true;
            }

            _owner.LiteAmpArrowStop();

            if (key == System.Windows.Forms.Keys.PageDown)
            {
                _owner.LiteAmpArrowMove(System.Math.Max(4, _owner.GetPlaylistNativeVisibleCount() - 2));
                return true;
            }

            if (key == System.Windows.Forms.Keys.PageUp)
            {
                _owner.LiteAmpArrowMove(-System.Math.Max(4, _owner.GetPlaylistNativeVisibleCount() - 2));
                return true;
            }

            if (key == System.Windows.Forms.Keys.Home)
            {
                _owner.LiteAmpArrowSetSelection(0);
                return true;
            }

            if (key == System.Windows.Forms.Keys.End)
            {
                _owner.LiteAmpArrowSetSelection(_owner._playlistBox.Items.Count - 1);
                return true;
            }

            return false;
        }
    }


    private void SelectEqPreset(string preset)
    {
        _selectedEqPreset = preset;
        ApplyEqPreset();
        RefreshEqButtonsVisualState();
    }

    private void ApplyEqPreset(bool updateStatus = true)
    {
        string preset = string.IsNullOrWhiteSpace(_selectedEqPreset) ? "Normal" : _selectedEqPreset;
        float[] gains = GetEqPresetGains(preset);

        _audio.SetEqualizer(gains);

        if (updateStatus)
            ShowStatus($"EQ: {GetEqPresetDisplayName(preset)}");
    }

    private static float[] GetEqPresetGains(string preset)
    {
        return preset switch
        {
            "Bass Boost" => [5.0f, 3.0f, 0.0f, -1.0f, 0.0f],
            "Vocal" => [-2.0f, -1.0f, 3.5f, 2.5f, 0.0f],
            "Rock" => [3.0f, 1.5f, -1.0f, 2.5f, 3.0f],
            "Pop" => [2.0f, 1.0f, 1.0f, 2.0f, 1.5f],
            _ => [0.0f, 0.0f, 0.0f, 0.0f, 0.0f]
        };
    }

    private static string GetEqPresetDisplayName(string preset)
    {
        return preset switch
        {
            "Bass Boost" => "Bass",
            "Normal" => "Normal",
            _ => preset
        };
    }

    private void RefreshEqButtonsVisualState()
    {
        if (_eqNormalButton is null)
            return;

        ApplyModeStyle(_eqNormalButton, _selectedEqPreset == "Normal");
        ApplyModeStyle(_eqBassButton, _selectedEqPreset == "Bass Boost");
        ApplyModeStyle(_eqVocalButton, _selectedEqPreset == "Vocal");
        ApplyModeStyle(_eqRockButton, _selectedEqPreset == "Rock");
        ApplyModeStyle(_eqPopButton, _selectedEqPreset == "Pop");
    }

    private void LiteAmpArrowStart(int delta)
    {
        if (_playlistBox.Items.Count == 0)
            return;

        bool newDirection = !_liteAmpArrowActive || _liteAmpArrowDelta != delta;

        _liteAmpArrowDelta = delta;
        _liteAmpArrowActive = true;

        if (newDirection)
            LiteAmpArrowMove(delta);

        if (_liteAmpArrowTimer is not null)
        {
            if (!_liteAmpArrowTimer.Enabled)
            {
                _liteAmpArrowTimer.Interval = 180;
                _liteAmpArrowTimer.Start();
            }
        }
    }
}