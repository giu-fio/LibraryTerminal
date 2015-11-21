using System;
using Microsoft.SPOT;
using schemas.datacontract.org;

namespace LibraryTerminal
{
    class LibraryModelSingleton
    {
        private static LibraryModelSingleton instance = null;
        private LibraryModelSingleton() { User = null; }
        public static LibraryModelSingleton Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LibraryModelSingleton();
                }
                return instance;
            }
        }

        public BookCompositeType Book { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public UserCompositeType User { get; set; }

        public void Logout()
        {
            User = null;
        }
    }
}

