using System;
using Microsoft.SPOT;
using GHI.Glide;
using GHI.Glide.Display;
using GHI.Glide.UI;
using Gadgeteer.Modules.GHIElectronics;
using System.Collections;
using System.Text;
using schemas.datacontract.org;

namespace LibraryTerminal
{
    class LibraryView : ILibraryView
    {
        #region fields - constants - properties
        //constants
        private const string HI = "Hi ";
        private const string WELCOME_MESSAGE = ", select your option";

        //private fields
        private DisplayT35 display;
        private Camera camera;
        private Hashtable windows;
        private Action action;
        private enum WindowsPosition { LOGIN = 0, LOADING, WELCOME, CAMERA, PICTURE, MSG_OK, MSG, BOOK_LOAN, BOOK_RET }

        //properties
        public ILibraryController Controller { get; set; }
        #endregion

        public LibraryView(DisplayT35 display, Camera camera)
        {
            this.display = display;
            this.camera = camera;
            windows = new Hashtable(10);
            GlideTouch.Initialize();
        }

        public void ShowLoginPage()
        {
            Window window = (Window)windows[WindowsPosition.LOGIN];
            if (window == null)
            {
                window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.login_window));
                windows.Add(WindowsPosition.LOGIN, window);
            }
            Glide.MainWindow = window;
        }

        public void ShowLoading()
        {
            Window window = (Window)windows[WindowsPosition.LOADING];
            if (window == null)
            {
                window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.loading_window));
                windows.Add(WindowsPosition.LOADING, window);
                TextBlock text = (TextBlock)window.GetChildByName("text");
                text.Text = "Loading...";

            }
            Glide.MainWindow = window;
        }

        public void ShowWelcomePage()
        {
            Window window = (Window)windows[WindowsPosition.WELCOME];
            if (window == null)
            {
                window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.welcome_window));
                windows.Add(WindowsPosition.WELCOME, window);
                Button borrowButton = (Button)window.GetChildByName("borrowButton");
                Button returnButton = (Button)window.GetChildByName("returnButton");
                Button infoButton = (Button)window.GetChildByName("infoButton");
                Button logoutButton = (Button)window.GetChildByName("logoutButton");
                borrowButton.TapEvent += (o) => { Controller.StartLoanTransaction(); };
                returnButton.TapEvent += (o) => { Controller.StartReturnTransaction(); };
                infoButton.TapEvent += (o) => { Controller.StartInfoTransaction(); };
                logoutButton.TapEvent += (o) => { Controller.Logout(); };
            }
            LibraryModelSingleton model = LibraryModelSingleton.Instance;
            TextBlock text = (TextBlock)window.GetChildByName("text");
            text.Text = new StringBuilder().Append(HI).Append(model.User.FirstName).Append(WELCOME_MESSAGE).ToString();
            Glide.MainWindow = window;
        }

        public void ShowMessageWithButton(string msg, Action action)
        {
            Window window = (Window)windows[WindowsPosition.MSG_OK];
            if (window == null)
            {
                window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.message_button_window));
                windows.Add(WindowsPosition.MSG_OK, window);
                Button okButton = (Button)window.GetChildByName("okButton");
                okButton.TapEvent += (sender) => { if (action != null) action(); };
            }
            this.action = action;
            TextBlock text = (TextBlock)window.GetChildByName("text");
            text.Text = msg;
            Glide.MainWindow = window;
        }

        public void ShowMessage(string msg, int millis)
        {

        }

        public void ShowMessage(string msg)
        {
            Window window = (Window)windows[WindowsPosition.MSG];
            if (window == null)
            {
                window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.welcome_window));
                windows.Add(WindowsPosition.MSG, window);
            }
            TextBlock text = (TextBlock)window.GetChildByName("text");
            text.Text = msg;
            Glide.MainWindow = window;
        }

        public void ShowCamera()
        {
            Window window = (Window)windows[WindowsPosition.CAMERA];
            if (window == null)
            {
                window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.camera_window));
                windows.Add(WindowsPosition.CAMERA, window);
                Button takeButton = (Button)window.GetChildByName("takeButton");
                Button cancelButton = (Button)window.GetChildByName("cancelButton");
                Image cameraImage = (Image)window.GetChildByName("cameraImage");
                cameraImage.Bitmap.Clear();
                cameraImage.Bitmap.DrawText("LOADING", Resources.GetFont(Resources.FontResources.MyFont), Gadgeteer.Color.White, 90, 90);
                camera.BitmapStreamed += (sender, bitmap) =>
                {
                    cameraImage.Bitmap = bitmap;
                    cameraImage.Invalidate();
                };
                takeButton.TapEvent += (o) =>
                {
                    camera.StopStreaming();
                    ShowPicture(cameraImage.Bitmap);
                };
                cancelButton.TapEvent += (o) =>
                {
                    Controller.CancelCameraTransaction();
                };
            }
            camera.StartStreaming();
            Glide.MainWindow = window;
        }

        public void ShowBooKForLoan()
        {
            Window window = (Window)windows[WindowsPosition.BOOK_LOAN];
            DataGrid dataGrid = null;
            if (window == null)
            {
                window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.book_loan_window));
                windows.Add(WindowsPosition.BOOK_LOAN, window);

                Button borrowButton = (Button)window.GetChildByName("actionButton");
                Button cancelButton = (Button)window.GetChildByName("cancelButton");
                borrowButton.TapEvent += (o) => Controller.BorrowBook();
                cancelButton.TapEvent += (o) => Controller.CancelLoanTransaction();

                dataGrid = (DataGrid)window.GetChildByName("dataGrid");
                DataGridColumn colLabels = new DataGridColumn("", 80);
                DataGridColumn colValue = new DataGridColumn("", 320 - colLabels.Width);
                DataGridItem titleItem = new DataGridItem(new string[] { "Title", null });
                DataGridItem authorsItem = new DataGridItem(new string[] { "Authors", null });
                DataGridItem publisherItem = new DataGridItem(new string[] { "Publisher", null });
                DataGridItem ISBNItem = new DataGridItem(new string[] { "ISBN", null });
                DataGridItem publicationDateItem = new DataGridItem(new string[] { "Published", null });
                dataGrid.RowCount = 5;
                dataGrid.AddColumn(colLabels);
                dataGrid.AddColumn(colValue);
                dataGrid.AddItem(titleItem);
                dataGrid.AddItem(authorsItem);
                dataGrid.AddItem(publisherItem);
                dataGrid.AddItem(ISBNItem);
                dataGrid.AddItem(publicationDateItem);
            }
            BookCompositeType book = LibraryModelSingleton.Instance.Book;
            if (dataGrid == null) dataGrid = (DataGrid)window.GetChildByName("dataGrid");
            dataGrid.SetCellData(1, 0, book.Title);
            dataGrid.SetCellData(1, 1, ConcatString(book.Authors.STRING));
            dataGrid.SetCellData(1, 2, book.Publisher);
            dataGrid.SetCellData(1, 3, book.ISBN);
            dataGrid.SetCellData(1, 4, book.PublicationDate.ToString("yyyy"));

            Glide.MainWindow = window;
        }

        private void ShowPicture(Bitmap bitmap)
        {
            Window window = (Window)windows[WindowsPosition.PICTURE];

            if (window == null)
            {
                window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.picture_window));
                windows.Add(WindowsPosition.PICTURE, window);
                Button okButton = (Button)window.GetChildByName("okButton");
                Button cancelButton = (Button)window.GetChildByName("cancelButton");
                okButton.TapEvent += (o) =>
                {
                    Controller.RecognizeBarcode(bitmap);
                };
                cancelButton.TapEvent += (o) =>
                {
                    ShowCamera();
                };
            }
            Image cameraImage = (Image)window.GetChildByName("cameraImage");
            cameraImage.Bitmap = bitmap;
            Glide.MainWindow = window;
        }
        private string ConcatString(string[] strings)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < strings.Length; i++)
            {
                sb.Append(strings[i]);
                if (i != strings.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            return sb.ToString();
        }

        public void ShowBooKForReturning()
        {
            Window window = (Window)windows[WindowsPosition.BOOK_RET];
            DataGrid dataGrid = null;
            if (window == null)
            {
                window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.book_loan_window));
                windows.Add(WindowsPosition.BOOK_RET, window);

                Button returnButton = (Button)window.GetChildByName("actionButton");
                Button cancelButton = (Button)window.GetChildByName("cancelButton");
                returnButton.Text = "Return";
                returnButton.TapEvent += (o) => Controller.ReturnBook();
                cancelButton.TapEvent += (o) => Controller.CancelReturnTransaction();

                dataGrid = (DataGrid)window.GetChildByName("dataGrid");
                DataGridColumn colLabels = new DataGridColumn("", 80);
                DataGridColumn colValue = new DataGridColumn("", 320 - colLabels.Width);
                DataGridItem titleItem = new DataGridItem(new string[] { "Title", null });
                DataGridItem publisherItem = new DataGridItem(new string[] { "Publisher", null });
                DataGridItem ISBNItem = new DataGridItem(new string[] { "ISBN", null });
                DataGridItem startDateItem = new DataGridItem(new string[] { "Start date", null });
                DataGridItem dueDateItem = new DataGridItem(new string[] { "Due date", null });
                dataGrid.RowCount = 5;
                dataGrid.AddColumn(colLabels);
                dataGrid.AddColumn(colValue);
                dataGrid.AddItem(titleItem);
                dataGrid.AddItem(publisherItem);
                dataGrid.AddItem(ISBNItem);
                dataGrid.AddItem(startDateItem);
                dataGrid.AddItem(dueDateItem);
            }
            BookCompositeType book = LibraryModelSingleton.Instance.Book;
            if (dataGrid == null) dataGrid = (DataGrid)window.GetChildByName("dataGrid");
            dataGrid.SetCellData(1, 0, book.Title);
            dataGrid.SetCellData(1, 1, book.Publisher);
            dataGrid.SetCellData(1, 2, book.ISBN);
            dataGrid.SetCellData(1, 3, LibraryModelSingleton.Instance.StartDate.ToString("dd/MM/yyyy"));
            dataGrid.SetCellData(1, 4, LibraryModelSingleton.Instance.DueDate.ToString("dd/MM/yyyy"));

            Glide.MainWindow = window;
        }
    }
}
