﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using LarchSys.Bot.Annotations;
using MessageBox = System.Windows.MessageBox;


namespace LarchSys.Bot {
    public abstract class Spider : INotifyPropertyChanged {
        public IWin32Window Window { get; set; }

        public string Title { get; set; }
        public string ExportFileName { get; set; }


        public abstract Brush Background { get; }
        public abstract UIElement Logo { get; }
        protected abstract Task Search([NotNull] string search, CancellationToken token);


        public void Clear()
        {
            Status = "--";
            SearchText = "";

            // reset normal scan
            Progress = 0;
            PageCount = 0;
            ResultsCount = 0;
            SearchedLinks = new ObservableCollection<string>();
            Results = new ObservableCollection<SearchResult>();

            // reset deep scan
            DeepScanCount = 0;
            ProgressDeepScan = 0;

            // reset buttons
            BtnSearchIsEnabled = true;
            BtnExportIsEnabled = false;
            BtnResetIsEnabled = false;
            BtnCancelIsEnabled = false;
        }


        public virtual async Task Search()
        {
            var text = SearchText;
            if (string.IsNullOrEmpty(text)) {
                MessageBox.Show("Der Suchtext darf nicht leer sein!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BtnSearchIsEnabled = false;
            BtnResetIsEnabled = false;
            BtnExportIsEnabled = false;
            BtnCancelIsEnabled = true;

            Status = "Running";

            try {
                _tokenSource = new CancellationTokenSource();
                await Search(text, _tokenSource.Token);

                Status = "Finish";
                MessageBox.Show("Suche erfolgreich beendet", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
            catch (OperationCanceledException) {
                Status = "Aborted";
                MessageBox.Show("Suche abgebrochen", "Abbruch", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }

            BtnExportIsEnabled = true;
            BtnSearchIsEnabled = true;
            BtnResetIsEnabled = true;
            BtnCancelIsEnabled = false;
        }


        public virtual async Task Export()
        {
            try {
                using var dialog = new SaveFileDialog {
                    AddExtension = true,
                    AutoUpgradeEnabled = true,
                    CheckFileExists = false,
                    CheckPathExists = true,
                    DefaultExt = ".csv",
                    FileName = ExportFileName,
                };


                if (dialog.ShowDialog(Window) == System.Windows.Forms.DialogResult.OK) {
                    var file = new FileInfo(dialog.FileName);
                    using var fs = file.OpenWrite();
                    using var sw = new StreamWriter(fs, Encoding.UTF8);

                    await sw.WriteLineAsync(Row(
                        "Kategorie", "Name", "Adresse Straße", "Adresse PLZ", "Adresse Ort", "Adresse Gemeinde", "Adresse Zone", "Tel", "E-Mail", "Website", "Url", "Facebook"
                    ));

                    foreach (var _ in Results) {
                        await sw.WriteLineAsync(Row(
                            _.Category,
                            _.Name,
                            _.Address.StreatLine,
                            _.Address.Zip,
                            _.Address.Place,
                            _.Address.Community,
                            _.Address.Zone,
                            _.Tel,
                            _.Email,
                            _.Website,
                            _.Url,
                            _.Facebook
                        ));
                    }

                    sw.Close();

                    Status = $"{file.FullName} saved successful";

                    MessageBox.Show(Status, "saved", MessageBoxButton.OK);
                }
            }
            catch (Exception e) {
                MessageBox.Show(e.ToString(), e.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // creates a csv row: "some value";"some value with one "" quote";"a value with newline \n"
            static string Row(params string[] x) => string.Join(";", x.Select(_ => $"\"{_?.Replace("\"", "\"\"")}\""));
        }


        public virtual Task Reset()
        {
            if (MessageBox.Show("Es werden alle aktuellen Suchergebnisse unwiderruflich gelöscht!", "Zurücksetzen", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                Clear();
            }

            return Task.CompletedTask;
        }


        public virtual Task Cancel()
        {
            if (MessageBox.Show("Die Suche wirklich abbrechen?", "Abbrechen", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                _tokenSource?.Cancel();
                BtnCancelIsEnabled = false;
                BtnExportIsEnabled = true;
            }

            return Task.CompletedTask;
        }


        #region Props

        private ObservableCollection<SearchResult> _results;
        private ObservableCollection<string> _searchedLinks;
        private string _searchText;
        private int _progress;
        private int _progressDeepScan;
        private int _pageCount;
        private string _status;
        private int _resultsCount;
        private int _deepScanCount;
        private bool _btnSearchIsEnabled;
        private bool _btnExportIsEnabled;
        private bool _btnResetIsEnabled;
        private bool _btnCancelIsEnabled;
        private CancellationTokenSource _tokenSource;

        public bool BtnSearchIsEnabled {
            get => _btnSearchIsEnabled;
            set {
                _btnSearchIsEnabled = value;
                OnPropertyChanged();
            }
        }
        public bool BtnExportIsEnabled {
            get => _btnExportIsEnabled;
            set {
                _btnExportIsEnabled = value;
                OnPropertyChanged();
            }
        }
        public bool BtnResetIsEnabled {
            get => _btnResetIsEnabled;
            set {
                _btnResetIsEnabled = value;
                OnPropertyChanged();
            }
        }
        public bool BtnCancelIsEnabled {
            get => _btnCancelIsEnabled;
            set {
                _btnCancelIsEnabled = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> SearchedLinks {
            get => _searchedLinks;
            set {
                _searchedLinks = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<SearchResult> Results {
            get => _results;
            set {
                _results = value;
                OnPropertyChanged();
            }
        }
        public string SearchText {
            get => _searchText;
            set {
                _searchText = value;
                OnPropertyChanged();
            }
        }
        public int Progress {
            get => _progress;
            set {
                _progress = value;
                OnPropertyChanged();
            }
        }
        public int ProgressDeepScan {
            get => _progressDeepScan;
            set {
                _progressDeepScan = value;
                OnPropertyChanged();
            }
        }
        public int PageCount {
            get => _pageCount;
            set {
                _pageCount = value;
                OnPropertyChanged();
            }
        }
        public string Status {
            get => _status;
            set {
                _status = value;
                OnPropertyChanged();
            }
        }
        public int ResultsCount {
            get => _resultsCount;
            set {
                _resultsCount = value;
                OnPropertyChanged();
            }
        }
        public int DeepScanCount {
            get => _deepScanCount;
            set {
                _deepScanCount = value;
                OnPropertyChanged();
            }
        }

        #endregion


        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
