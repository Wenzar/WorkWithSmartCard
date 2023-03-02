using PCSC;
using PCSC.Iso7816;

namespace MonitorReaderEvents
{
    public class MifareCard
    {
        private const byte CUSTOM_CLA = 0xFF;
        private readonly IIsoReader _isoReader;

        public MifareCard(IIsoReader isoReader)
        {
            _isoReader = isoReader ?? throw new ArgumentNullException(nameof(isoReader));
        }

        public byte[] GetData()
        {
            var getDataCmd = new CommandApdu(IsoCase.Case2Short, SCardProtocol.Any)
            {
                CLA = CUSTOM_CLA,
                Instruction = InstructionCode.GetData,
                P1 = 0x00,
                P2 = 0x00
            };

            var response = _isoReader.Transmit(getDataCmd);
            return IsSuccess(response)
                    ? response.GetData() ?? new byte[0]
                    : null;
        }


        private static bool IsSuccess(Response response) =>
            (response.SW1 == (byte)SW1Code.Normal) &&
            (response.SW2 == 0x00);
    }
}
