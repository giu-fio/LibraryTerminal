using System;
using Microsoft.SPOT;
using GT = Gadgeteer;

namespace LibraryTerminal
{
    interface ILibraryController
    {
        ILibraryView View { get; set; }

        void Login(string id);
        void StartLoanTransaction();
        void CancelCameraTransaction();
        void CancelLoanTransaction();
        void BorrowBook();
        void RecognizeBarcode(Bitmap picture);
        void Logout();

        void StartReturnTransaction();

        void ReturnBook();

        void CancelReturnTransaction();

        void StartInfoTransaction();
    }
}
