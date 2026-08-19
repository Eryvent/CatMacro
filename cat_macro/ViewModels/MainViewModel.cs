using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using CatMacro.Models;
using CatMacro.Services;

namespace CatMacro.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MacroRecorderService _recorder;
        private readonly MacroPlayerService _player;
        private readonly FileService _fileService;

        private string _status = "Ready";
        private string _recordingStatus = "";
        private int _currentLoop = 0;
        private int _totalLoops = 0;
        private string _repeatMode = "Infinite";
        private int _repeatCount = 10;
        private double _playbackSpeed = 1.0;
        private bool _isRecording = false;
        private bool _isPlaying = false;
        private string _selectedRecording = "";
        private string _countdownText = "";
        private bool _showCountdown = false;
        private CancellationTokenSource? _playbackCancellation;

        public ObservableCollection<string> RecordedActions { get; } = new();
        public ObservableCollection<string> SavedRecordings { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string RecordingStatus
        {
            get => _recordingStatus;
            set { _recordingStatus = value; OnPropertyChanged(); }
        }

        public int CurrentLoop
        {
            get => _currentLoop;
            set { _currentLoop = value; OnPropertyChanged(); OnPropertyChanged(nameof(LoopDisplay)); }
        }

        public int TotalLoops
        {
            get => _totalLoops;
            set { _totalLoops = value; OnPropertyChanged(); OnPropertyChanged(nameof(LoopDisplay)); }
        }

        public string LoopDisplay
        {
            get => _totalLoops == -1 ? $"Loop: {_currentLoop} / ∞" : $"Loop: {_currentLoop} / {_totalLoops}";
        }

        public string RepeatMode
        {
            get => _repeatMode;
            set { _repeatMode = value; OnPropertyChanged(); }
        }

        public int RepeatCount
        {
            get => _repeatCount;
            set { _repeatCount = value; OnPropertyChanged(); }
        }

        public double PlaybackSpeed
        {
            get => _playbackSpeed;
            set { _playbackSpeed = value; OnPropertyChanged(); }
        }

        public bool IsRecording
        {
            get => _isRecording;
            set { _isRecording = value; OnPropertyChanged(); }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set { _isPlaying = value; OnPropertyChanged(); }
        }

        public string SelectedRecording
        {
            get => _selectedRecording;
            set { _selectedRecording = value; OnPropertyChanged(); }
        }

        public string CountdownText
        {
            get => _countdownText;
            set { _countdownText = value; OnPropertyChanged(); }
        }

        public bool ShowCountdown
        {
            get => _showCountdown;
            set { _showCountdown = value; OnPropertyChanged(); }
        }

        public ICommand RecordCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand EmergencyStopCommand { get; }

        public MainViewModel()
        {
            _recorder = new MacroRecorderService();
            _player = new MacroPlayerService();
            _fileService = new FileService();

            _recorder.OnActionRecorded += (s, e) =>
            {
                RecordedActions.Add(e);
            };

            _recorder.OnCountdown += (s, count) =>
            {
                if (count > 0)
                {
                    CountdownText = count.ToString();
                    ShowCountdown = true;
                }
                else
                {
                    ShowCountdown = false;
                    CountdownText = "";
                }
            };

            _player.OnCountdown += (s, count) =>
            {
                if (count > 0)
                {
                    CountdownText = count.ToString();
                    ShowCountdown = true;
                    Status = $"▶ {count}";
                }
                else
                {
                    ShowCountdown = false;
                    CountdownText = "";
                    Status = "▶ Playing";
                }
            };

            _player.OnLoopUpdate += (s, loop) =>
            {
                CurrentLoop = loop;
            };

            _player.OnPlaybackComplete += (s, e) =>
            {
                Status = "✓ Ready";
                IsPlaying = false;
                ShowCountdown = false;
            };

            _player.OnMouseMovedDuringPlayback += (s, e) =>
            {
                Status = "⚠ Mouse Moved - Stopped";
                IsPlaying = false;
            };

            RecordCommand = new RelayCommand(_ => OnRecord());
            StopCommand = new RelayCommand(_ => OnStop());
            PlayCommand = new RelayCommand(_ => OnPlay());
            NewCommand = new RelayCommand(_ => OnNew());
            SaveCommand = new RelayCommand(_ => OnSave());
            OpenCommand = new RelayCommand(_ => OnOpen());
            DeleteCommand = new RelayCommand(_ => OnDelete());
            EmergencyStopCommand = new RelayCommand(_ => OnEmergencyStop());

            LoadSavedRecordings();
            Status = "✓ Ready";
        }

        private async void OnRecord()
        {
            if (IsRecording) return;

            Status = "⏺ Recording Starting...";
            RecordingStatus = "🔴 RECORDING IN PROGRESS...";
            IsRecording = true;
            RecordedActions.Clear();

            await _recorder.StartWithCountdown();
            Status = "⏺ Recording";
            RecordingStatus = "🔴 RECORDING - Actions: 0";
        }

        private void OnStop()
        {
            if (!IsRecording) return;

            _recorder.Stop();
            Status = "⏹ Stopped";
            RecordingStatus = $"✓ Recorded {RecordedActions.Count} actions";
            IsRecording = false;
            ShowCountdown = false;
        }

        private void OnPlay()
        {
            if (RecordedActions.Count == 0)
            {
                MessageBox.Show("No recording! Record first.", "Alert");
                return;
            }

            if (IsPlaying) return;

            Status = "▶ Starting...";
            IsPlaying = true;
            CurrentLoop = 0;
            TotalLoops = RepeatMode == "Infinite" ? -1 : RepeatCount;
            ShowCountdown = true;

            var recording = _recorder.GetRecording();
            _playbackCancellation = new CancellationTokenSource();

            _ = _player.PlayAsync(recording, RepeatMode, RepeatCount, PlaybackSpeed, _playbackCancellation.Token);
        }

        private void OnNew()
        {
            _recorder.Stop();
            RecordedActions.Clear();
            Status = "✓ Ready";
            RecordingStatus = "";
            IsRecording = false;
            IsPlaying = false;
            CurrentLoop = 0;
            TotalLoops = 0;
            ShowCountdown = false;
        }

        private void OnSave()
        {
            if (RecordedActions.Count == 0)
            {
                MessageBox.Show("Nothing to save!", "Error");
                return;
            }

            string name = $"Macro_{DateTime.Now:yyyyMMdd_HHmmss}";
            var result = Microsoft.VisualBasic.Interaction.InputBox("Macro name:", "Save", name);

            if (string.IsNullOrWhiteSpace(result))
                return;

            var recording = _recorder.GetRecording();
            recording.Name = result;

            if (_fileService.SaveRecording(recording, result))
            {
                MessageBox.Show($"Saved: {result}", "Success");
                LoadSavedRecordings();
            }
            else
            {
                MessageBox.Show("Save failed!", "Error");
            }
        }

        private void OnOpen()
        {
            if (string.IsNullOrWhiteSpace(SelectedRecording))
            {
                MessageBox.Show("Select a macro first!", "Alert");
                return;
            }

            var loaded = _fileService.LoadRecording(SelectedRecording);
            if (loaded != null)
            {
                RecordedActions.Clear();
                foreach (var action in loaded.Actions)
                {
                    RecordedActions.Add(action.GetDescription());
                }
                Status = "✓ Loaded";
                RecordingStatus = $"✓ Loaded: {SelectedRecording} ({RecordedActions.Count} actions)";
                MessageBox.Show($"✓ Loaded: {SelectedRecording}", "Success");
            }
            else
            {
                MessageBox.Show("Load failed!", "Error");
            }
        }

        private void OnDelete()
        {
            if (string.IsNullOrWhiteSpace(SelectedRecording))
            {
                MessageBox.Show("Select a macro first!", "Alert");
                return;
            }

            var result = MessageBox.Show($"Delete '{SelectedRecording}'?", "Confirm", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                if (_fileService.DeleteRecording(SelectedRecording))
                {
                    MessageBox.Show("✓ Deleted", "Success");
                    LoadSavedRecordings();
                }
            }
        }

        private void OnEmergencyStop()
        {
            _recorder.Stop();
            _player.Stop();
            _playbackCancellation?.Cancel();
            Status = "⏹ Stopped";
            RecordingStatus = "";
            IsRecording = false;
            IsPlaying = false;
            ShowCountdown = false;
        }

        private void LoadSavedRecordings()
        {
            SavedRecordings.Clear();
            var saved = _fileService.GetSavedRecordings();
            foreach (var recording in saved)
            {
                SavedRecordings.Add(recording);
            }
        }

        protected void OnPropertyChanged(string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
