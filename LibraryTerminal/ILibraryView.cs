using schemas.datacontract.org;

namespace LibraryTerminal
{
    interface ILibraryView
    {
        ILibraryController Controller { get; set; }

        
        void ShowBooKForLoan();
        void ShowCamera();
        void ShowLoading();

        /// <summary>
        /// Shows the login p
        /// </summary>
        void ShowLoginPage();    
  
        void ShowMessage(string msg);
        void ShowMessageWithButton(string msg, Action action);
        void ShowWelcomePage();


        void ShowBooKForReturning();
    }
}
