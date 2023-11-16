using System;

namespace GDL90 {
    internal class NotImplementedMessage : Message
    {
        public NotImplementedMessage(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
        }

        public override void PrintDebugInfo()
        {
            string debugInfo = string.Format("GDL90 Message type 0x{0:X2} ({1}) is not yet implemented.", (int)MessageId, MessageName);
            Console.WriteLine(debugInfo);
        }
    }

}