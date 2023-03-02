using PCSC;
using PCSC.Iso7816;
using PCSC.Exceptions;
using PCSC.Monitoring;
using PCSC.Utils;

namespace MonitorReaderEvents
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("This program will monitor all SmartCard readers and display all status changes.");

            // Retrieve the names of all installed readers.
            var readerNames = "ACS ACR1281 1S Dual Reader PICC 0";

            // if (IsEmpty(readerNames))
            // {
            //     Console.WriteLine("There are currently no readers installed.");
            //     Console.ReadKey();
            //     return;
            // }

            // Create smart-card monitor using a context factory. 
            // The context will be automatically released after monitor.Dispose()
            using (var monitor = MonitorFactory.Instance.Create(SCardScope.System))
            {
                AttachToAllEvents(monitor); // Remember to detach, if you use this in production!

                //ShowUserInfo(readerNames);

                monitor.Start(readerNames);

                // Let the program run until the user presses CTRL-Q
                while (true)
                {
                    var key = Console.ReadKey();
                    if (ExitRequested(key))
                    {
                        break;
                    }

                    if (monitor.Monitoring)
                    {
                        monitor.Cancel();
                        Console.WriteLine("Monitoring paused. (Press CTRL-Q to quit)");
                    }
                    else
                    {
                        monitor.Start(readerNames);
                        Console.WriteLine("Monitoring started. (Press CTRL-Q to quit)");
                    }
                }
            }
        }

        private static void ShowUserInfo(IEnumerable<string> readerNames)
        {
            foreach (var reader in readerNames)
            {
                Console.WriteLine($"Start monitoring for reader {reader}.");
            }

            Console.WriteLine("Press Ctrl-Q to exit or any key to toggle monitor.");
        }

        private static void AttachToAllEvents(ISCardMonitor monitor)
        {
            // Point the callback function(s) to the anonymous & static defined methods below.
            monitor.CardInserted += (sender, args) => DisplayEvent("CardInserted", args);
            monitor.CardRemoved += (sender, args) => DisplayEvent("CardRemoved", args);
            monitor.Initialized += (sender, args) => DisplayEvent("Initialized", args);
            monitor.MonitorException += MonitorException;
        }

        private static void DisplayEvent(string eventName, CardStatusEventArgs unknown)
        {
            Console.WriteLine(">> {0} Event for reader: {1}", eventName, unknown.ReaderName);
            if(eventName == "CardInserted") ReadUid();
            Console.WriteLine("State: {0}\n", unknown.State);
        }


        private static void MonitorException(object sender, PCSCException ex)
        {
            Console.WriteLine("Monitor exited due an error:");
            Console.WriteLine(SCardHelper.StringifyError(ex.SCardError));
        }

        private static void ReadUid()
        {
            using (var context = ContextFactory.Instance.Establish(SCardScope.System))
            {
                var readerName = "ACS ACR1281 1S Dual Reader PICC 0";

                using (var isoReader = new IsoReader(
                    context: context,
                    readerName: readerName,
                    mode: SCardShareMode.Shared,
                    protocol: SCardProtocol.Any,
                    releaseContextOnDispose: false))
                {
                    var card = new MifareCard(isoReader);

                    var uid = card.GetData();
                    Console.WriteLine("UID: {0}",
                        (uid != null)
                            ? BitConverter.ToString(uid)
                            : '0');

                }
            }
        }

        private static bool IsEmpty(ICollection<string> readerNames) =>
            readerNames == null || readerNames.Count < 1;

        private static bool ExitRequested(ConsoleKeyInfo key) =>
            key.Modifiers == ConsoleModifiers.Alt &&
            key.Key == ConsoleKey.Q;

    }
}