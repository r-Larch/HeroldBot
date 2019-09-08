using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using IWin32Window = System.Windows.Forms.IWin32Window;


namespace LarchSys.Bot {
    public partial class SpiderWindow : Window, IWin32Window {
        public Spider Context { get; set; }

        public SpiderWindow(Spider context)
        {
            InitializeComponent();

            context.Window = this;

            DataContext = Context = context;
            Title = Context.Title;
            LogoPanel.Children.Add(Context.Logo);

            BtnSearch.Click += async (s, e) => await Context.Search();
            BtnExport.Click += async (s, e) => await Context.Export();
            BtnReset.Click += async (s, e) => await Context.Reset();

            TxbSearch.KeyDown += async (s, e) => {
                if (e.Key == Key.Enter) await Context.Search();
            };

            Context.Clear();
        }

        public IntPtr Handle => new WindowInteropHelper(this).Owner;
    }
}
