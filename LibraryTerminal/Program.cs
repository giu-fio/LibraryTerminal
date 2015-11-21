using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace LibraryTerminal
{
    public partial class Program
    {
        private const string IP_ADDRESS = "169.254.127.41";
        private ILibraryController controller;

        void ProgramStarted()
        {
            Debug.EnableGCMessages(true);
            InitModules();
            ILibraryView view = new LibraryView(displayT35, camera);           
            controller = new LibraryController(view);

        }

        private void InitModules()
        {
            //Network
            ethernetJ11D.NetworkInterface.Open();
            ethernetJ11D.UseStaticIP("169.254.125.10", "255.255.0.0", IP_ADDRESS);
            //Rfid
            rfidReader.IdReceived += rfidReader_IdReceived;
        }


        void rfidReader_IdReceived(RFIDReader sender, string e)
        {
            if (controller != null) controller.Login(e);
        }


    }
}