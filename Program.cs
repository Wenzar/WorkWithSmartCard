using PCSC;
using PCSC.Iso7816;
using PCSC.Exceptions;
using PCSC.Monitoring;
using PCSC.Utils;
using System.Text;

namespace MonitorReaderEvents
{
    public class Program
    {
        private const byte MSB = 0x00;
        private const byte chosenBlock = 0x00;
        public static void Main()
        {
            using (var context = ContextFactory.Instance.Establish(SCardScope.System))
            {
                Console.WriteLine("This program will monitor all SmartCard readers and display all status changes.");

                var readerNames = "ACS ACR1281 1S Dual Reader PICC 0";

                using (var isoReader = new IsoReader(
                    context: context,
                    readerName: readerNames,
                    mode: SCardShareMode.Shared,
                    protocol: SCardProtocol.Any,
                    releaseContextOnDispose: false))
                {
                    var card = new MifareCard(isoReader);

                    using (var monitor = MonitorFactory.Instance.Create(SCardScope.System))
                    {
                        AttachToAllEvents(monitor); // Remember to detach, if you use this in production!

                        monitor.Start(readerNames);

                        // Let the program run until the user presses Shift-Q
                        while (true)
                        {
                            Console.WriteLine("[1] - Режим чтения");
                            Console.WriteLine("[2] - Режим записи");
                            Console.WriteLine("[3] - Считать UID");
                            Console.WriteLine("[Shift-Q] - Выход");

                            var key = Console.ReadKey(true);

                            if (key.Key == ConsoleKey.D1) ReadCard(card);

                            if (key.Key == ConsoleKey.D2)
                            {
                                Console.WriteLine("Write_Mode");
                            }

                            if (key.Key == ConsoleKey.D3)// WorkWithCard(key);

                                if (ExitRequested(key)) break;

                        }
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
            Console.WriteLine("State: {0}\n", unknown.State);
        }


        private static void MonitorException(object sender, PCSCException ex)
        {
            Console.WriteLine("Monitor exited due an error:");
            Console.WriteLine(SCardHelper.StringifyError(ex.SCardError));
        }

        // private static void WorkWithCard(ConsoleKeyInfo key)
        // {
        //     using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        //     {


        //         var readerName = "ACS ACR1281 1S Dual Reader PICC 0";

        //         using (var isoReader = new IsoReader(
        //             context: context,
        //             readerName: readerName,
        //             mode: SCardShareMode.Shared,
        //             protocol: SCardProtocol.Any,
        //             releaseContextOnDispose: false))
        //         {
        //             var card = new MifareCard(isoReader);

        //             if (key.Key == ConsoleKey.D3)
        //             {
        //                 try
        //                 {
        //                     var uid = card.GetData();
        //                     Console.WriteLine("UID: {0}",
        //                         (uid != null)
        //                             ? BitConverter.ToString(uid)
        //                             : throw new Exception("GET DATA failed."));
        //                 }
        //                 catch (Exception exception)
        //                 {
        //                     Console.Error.WriteLine("No card inserted in reader '{0}'.", readerName);
        //                     Console.Error.WriteLine("Error message: {0} ({1})\n", exception.Message, exception.GetType());
        //                 }
        //             }

        //             else
        //             {

        //                 byte keyNumber = chooseKeyNum();

        //                 var loadKeySuccessful = card.LoadKey(KeyStructure.NonVolatileMemory, keyNumber, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });

        //                 if (!loadKeySuccessful)
        //                 {
        //                     throw new Exception("LOAD KEY failed.");
        //                 }

        //                 var authSuccessful = card.Authenticate(MSB, chosenBlock, KeyType.KeyA, keyNumber);
        //                 if (!authSuccessful)
        //                 {
        //                     throw new Exception("AUTHENTICATE failed.");
        //                 }

        //                 var result = card.ReadBinary(MSB, chosenBlock, 16);
        //                 Console.WriteLine("Result (before BINARY UPDATE): {0}",
        //                     (result != null)
        //                         ? BitConverter.ToString(result)
        //                         : null);
        //             }
        //         }

        //     }
        // }

        private static void ReadCard(MonitorReaderEvents.MifareCard card)
        {
            Console.WriteLine("Read_Mode");
            byte keyNumber = loadKey(card);

            var authSuccessful = card.Authenticate(MSB, chosenBlock, KeyType.KeyA, keyNumber);
            if (!authSuccessful)
            {
                throw new Exception("AUTHENTICATE failed.");
            }

            var result = card.ReadBinary(MSB, chosenBlock, 16);
            Console.WriteLine("Result (before BINARY UPDATE): {0}",
                (result != null)
                    ? BitConverter.ToString(result)
                    : null);
        }

        private static byte loadKey(MonitorReaderEvents.MifareCard card)
        {

            byte keyNum = chooseKeyNum();
            scanKey(card, keyNum);

            return keyNum;
        }

        private static byte chooseKeyNum()
        {
            byte result = 32; // кол-во ключей не более 1F
            while (result == 32 || result > 31)
            {
                Console.WriteLine("Укажите номер ключа [0-31]:");
                try
                {
                    result = Convert.ToByte(Console.ReadLine());
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("Error message: {0} ({1})\n", exception.Message, exception.GetType());
                }
            }
            return result;
        }

        private static void scanKey(MonitorReaderEvents.MifareCard card, byte keyNum)
        {
            var loadKeySuccessful = false;
            byte[] result = { };
            while (!loadKeySuccessful)
            {
                Console.WriteLine("Введите ключ [0-]:");
                result = Encoding.Default.GetBytes(Console.ReadLine());

                Console.WriteLine(BitConverter.ToString(result));


                loadKeySuccessful = card.LoadKey(KeyStructure.NonVolatileMemory, keyNum, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });

                if (!loadKeySuccessful)
                {
                    throw new Exception("LOAD KEY failed.");
                }
            }
        }


        private static bool IsEmpty(ICollection<string> readerNames) =>
            readerNames == null || readerNames.Count < 1;

        private static bool ExitRequested(ConsoleKeyInfo key) =>
            key.Modifiers == ConsoleModifiers.Shift &&
            key.Key == ConsoleKey.Q;

    }
}