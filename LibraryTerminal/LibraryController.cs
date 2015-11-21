using System;
using Microsoft.SPOT;
using Ws.Services.Binding;
using tempuri.org;
using schemas.datacontract.org;
using Ws.Services;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LibraryTerminal
{
    class LibraryController : ILibraryController
    {
        private enum InnerState { INITIAL, WELCOME, BORROWING, RETURNING, INFO }

        #region fields - constants - properties
        //private fields
        private InnerState state;
        private WS2007HttpBinding binding;
        

        //constant
        private const string CODE_NOT_RECOGNIZED_MSG = "The code was not recognized.";
        private const string INVALID_BARCODE_MSG = "The code is not valid.";
        private const string UNREGISTERED_USER_MSG = "The user is not registered.";
        private const string BORROW_FAILED_MSG = "You can not borrow this book.";
        private const string OPERATION_COMPLETED_MSG = "Operation complited!";
        private const string OPERATION_FAILED_MSG = "Operation failed!";
        private const string ERROR_MSG = "Unexpected error. Please try later";
        private const string IP_ADDRESS = "169.254.127.41";

        //properties  
        public ILibraryView View { get; set; }
        #endregion

        public LibraryController(ILibraryView view)
        {
            this.state = InnerState.INITIAL;
            binding = new WS2007HttpBinding(new HttpTransportBindingConfig(new Uri("http://" + IP_ADDRESS + "/LibraryWS/Service.svc")));
            View = view;
            View.Controller = this;
            View.ShowLoginPage();
        }

        public void Login(string id)
        {
            if (state != InnerState.INITIAL) throw new InvalidOperationException("The user is already logged in!");
            IServiceClientProxy proxy = null;
            try
            {
                //using (IServiceClientProxy proxy = new IServiceClientProxy(binding, new ProtocolVersion11()))
                //{
                 proxy = new IServiceClientProxy(binding, new ProtocolVersion11());

                //login request to the WS
                LogIn loginRequest = new LogIn() { card_number = id };
                LogInResponse response = proxy.LogIn(loginRequest);
                if (response.LogInResult.FirstName == null)
                {
                    //show a message and return in the login page
                    Action action = () => { View.ShowLoginPage(); };
                    View.ShowMessageWithButton(UNREGISTERED_USER_MSG, action);
                    return;
                }
                //set the the logged user and show the welcome page
                LibraryModelSingleton.Instance.User = response.LogInResult;
                state = InnerState.WELCOME;
                View.ShowWelcomePage();
                //}
            }
            catch (Exception)
            {
                //show a message and then returns in the login page
                state = InnerState.INITIAL;
                View.ShowMessageWithButton(ERROR_MSG, () => { View.ShowLoginPage(); });
            }
            finally
            {
             //  if (proxy != null) { proxy.Dispose(); }
            }
        }

        public void StartLoanTransaction()
        {
            if (state != InnerState.WELCOME) throw new InvalidOperationException("The user is not in the Welcome Page!");
            //change the inner state and show the camera page
            state = InnerState.BORROWING;
            View.ShowCamera();
        }

        public void CancelCameraTransaction()
        {
            if (state != InnerState.BORROWING && state != InnerState.RETURNING) { throw new InvalidOperationException("The user is in an invalid state!"); }
            //change the inner state and show the camera page
            state = InnerState.WELCOME;
            View.ShowWelcomePage();
        }

        public void RecognizeBarcode(Bitmap bitmap)
        {
            if (state != InnerState.BORROWING && state != InnerState.RETURNING) { throw new InvalidOperationException("The user is in an invalid state!"); }
           
            if (bitmap == null)
            {
                //invalid request, show in the welcome page
                View.ShowWelcomePage();
                return;
            }

            try
            {
                short port = 0;
                //show a loading message
                View.ShowLoading();
                //call the web service to send the picture
                using (IServiceClientProxy proxy = new IServiceClientProxy(binding, new ProtocolVersion11()))
                {
                    SendBarcodeImage sendImageRequest = new SendBarcodeImage();
                    SendBarcodeImageResponse response = proxy.SendBarcodeImage(sendImageRequest);
                    port = response.SendBarcodeImageResult;

                    //send the image
                    string code = SendImage(port, GHI.Utilities.Bitmaps.ConvertToFile(bitmap));
                    if (code == null)
                    {
                        //the bar code was not recognized 
                        //show the camera page 
                        View.ShowMessageWithButton(CODE_NOT_RECOGNIZED_MSG, () => { View.ShowCamera(); });
                        return;
                    }

                    //get book info
                    GetBookInformationsResponse bookInfoResponse = proxy.GetBookInformations(new GetBookInformations() { barcode = code });
                    if (bookInfoResponse.GetBookInformationsResult.ISBN == null)
                    {
                        //the book is not in the WS database
                        View.ShowMessageWithButton(INVALID_BARCODE_MSG, () => { View.ShowCamera(); });
                        return;
                    }

                    LibraryModelSingleton.Instance.Book = bookInfoResponse.GetBookInformationsResult;
                    if (state == InnerState.BORROWING)
                    {
                        //show the book information
                        View.ShowBooKForLoan();
                    }
                    else if (state == InnerState.RETURNING)
                    {
                        //call the ws in order to get the loan information
                        GetLoanState loanStateRequest = new GetLoanState();
                        loanStateRequest.bookID = bookInfoResponse.GetBookInformationsResult.ID;
                        loanStateRequest.userID = LibraryModelSingleton.Instance.User.ID;
                        loanStateRequest.returned = false;
                        GetLoanStateResponse loanStateResponse = proxy.GetLoanState(loanStateRequest);
                        if (loanStateResponse.GetLoanStateResult.LoanStateCompositeType.Length == 0)
                        {
                            //the book was not borrowed by the user
                            View.ShowMessageWithButton(INVALID_BARCODE_MSG, () => { View.ShowWelcomePage(); });
                        }
                        else
                        {
                            //show the book information
                            LibraryModelSingleton.Instance.StartDate = loanStateResponse.GetLoanStateResult.LoanStateCompositeType[0].LoanStartDate;
                            LibraryModelSingleton.Instance.DueDate = loanStateResponse.GetLoanStateResult.LoanStateCompositeType[0].LoanExpirationDate;
                            View.ShowBooKForReturning();
                        }
                    }
                }
            }
            catch (Exception)
            { //show a message and then returns in the login page
                state = InnerState.INITIAL;
                View.ShowMessageWithButton(ERROR_MSG, () => { this.Logout(); });
            }
        }

        public void Logout()
        {
            //log out and show the login page
            state = InnerState.INITIAL;
            LibraryModelSingleton.Instance.Logout();
            View.ShowLoginPage();
        }

        public void CancelLoanTransaction()
        {
            //show the welcome page
            state = InnerState.WELCOME;
            View.ShowWelcomePage();
        }

        private string SendImage(short port, byte[] picture)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(IP_ADDRESS), port);
            socket.Connect(ep);
            string code = null;
            byte[] buffer = new byte[128];
            using (NetworkStream ns = new NetworkStream(socket))
            {
                ns.ReadTimeout = 2000;
                //send picture length and bytes
                ns.Write(BitConverter.GetBytes(picture.Length), 0, 4);
                ns.Write(picture, 0, picture.Length);

                //read code length and value
                ns.Read(buffer, 0, 4);
                int len = BitConverter.ToInt32(buffer, 0);
                if (len > 0)
                {
                    ns.Read(buffer, 0, len);
                    code = new string(Encoding.UTF8.GetChars(buffer, 0, len));
                }
            }
            return code;
        }

        public void BorrowBook()
        {
            try
            {   //show a loading message
                View.ShowLoading();
                using (IServiceClientProxy proxy = new IServiceClientProxy(binding, new ProtocolVersion11()))
                {
                    //execute the borrow request
                    int bookId = LibraryModelSingleton.Instance.Book.ID;
                    int userId = LibraryModelSingleton.Instance.User.ID;
                    Borrow borrowRequest = new Borrow() { bookID = bookId, userID = userId };
                    BorrowResponse response = proxy.Borrow(borrowRequest);
                    //show positive a message if the result is valid otherwise a negative message 
                    //and return in the welcome page
                    state = InnerState.WELCOME;
                    string msg = (response.BorrowResult.StartDate.Equals(DateTime.MinValue)) ? BORROW_FAILED_MSG : OPERATION_COMPLETED_MSG;
                    View.ShowMessageWithButton(msg, () => { View.ShowWelcomePage(); });
                }
            }
            //show a message and then returns in the login page
            catch (Exception) { View.ShowMessageWithButton(ERROR_MSG, () => { this.Logout(); }); }
        }


        public void StartReturnTransaction()
        {
            if (state != InnerState.WELCOME) throw new InvalidOperationException("The user is not in the Welcome Page!");
            //show the camera page
            state = InnerState.RETURNING;
            View.ShowCamera();
        }


        public void ReturnBook()
        {
            try
            {//show a loading message
                View.ShowLoading();
                using (IServiceClientProxy proxy = new IServiceClientProxy(binding, new ProtocolVersion11()))
                {
                    //execute the return request
                    int bookId = LibraryModelSingleton.Instance.Book.ID;
                    int userId = LibraryModelSingleton.Instance.User.ID;
                    GiveBackBook returnRequest = new GiveBackBook() { bookID = bookId, userID = userId };
                    var response = proxy.GiveBackBook(returnRequest);
                    //show positive a message if the result is valid otherwise a negative message 
                    //and return in the welcome page
                    state = InnerState.WELCOME;
                    string msg = (response.GiveBackBookResult) ? OPERATION_COMPLETED_MSG : OPERATION_FAILED_MSG;
                    View.ShowMessageWithButton(msg, () => { View.ShowWelcomePage(); });
                }
            }
            //show a message and then returns in the login page
            catch (Exception) { View.ShowMessageWithButton(ERROR_MSG, () => { this.Logout(); }); }
        }

        public void CancelReturnTransaction()
        {
            //show the welcome page
            state = InnerState.WELCOME;
            View.ShowWelcomePage();
        }


        public void StartInfoTransaction()
        {
            throw new NotImplementedException();
        }
    }
}
