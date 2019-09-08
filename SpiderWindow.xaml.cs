using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using IWin32Window = System.Windows.Forms.IWin32Window;


namespace LarchSys.Bot {
    public partial class SpiderWindow : Window, IWin32Window {
        public Spider Spider { get; set; }

        public SpiderWindow(Spider spider)
        {
            InitializeComponent();

            spider.Window = this;

            DataContext = Spider = spider;
            Title = Spider.Title;
            LogoPanel.Children.Add(Spider.Logo);

            BtnSearch.Click += async (s, e) => await Spider.Search();
            BtnExport.Click += async (s, e) => await Spider.Export();
            BtnReset.Click += async (s, e) => await Spider.Reset();

            TxbSearch.KeyDown += async (s, e) => {
                if (e.Key == Key.Enter) await Spider.Search();
            };

            Spider.Clear();
        }

        public IntPtr Handle => new WindowInteropHelper(this).Owner;
    }
}
