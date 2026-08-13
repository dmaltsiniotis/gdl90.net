using System;
using System.Buffers;

namespace GDL90 {
    /*
        The GDL 90 will always output an Ownship Report message once per second. The message
        uses the same format as the Traffic Report, with the Message ID set to the value 10 (See Table
        6). See §3.5.1 for specification of the Traffic Report field.

        The Ownship Report is output by the GDL 90 regardless of whether a valid GPS position fix is
        available. If the ownship GPS position fix is invalid, the Latitude, Longitude, and NIC fields in
        the Ownship Report all have the ZERO value.
    */
    public class OwnshipReport : TrafficReport {

        public OwnshipReport(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {

        }
    }
}