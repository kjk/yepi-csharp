using System;
using System.Diagnostics;

using Yepi;

namespace Yepi.Tests
{
    public static class UtilTests
    {
        public static void CleanAppVerTest()
        {
            Debug.Assert("1" == Utils.CleanAppVer("1"));
            Debug.Assert("1.0" == Utils.CleanAppVer("1."));
            Debug.Assert("1.0" == Utils.CleanAppVer("1.0"));

            Debug.Assert("0.1" == Utils.CleanAppVer("0.1"));
            Debug.Assert("0.1" == Utils.CleanAppVer("0.1."));
            Debug.Assert("0.1" == Utils.CleanAppVer("0.1.0"));
            Debug.Assert("0.1" == Utils.CleanAppVer("0.1.0."));
            Debug.Assert("0.1" == Utils.CleanAppVer("0.1.0.0"));

            Debug.Assert("1.8.3" == Utils.CleanAppVer("1.8.3"));
            Debug.Assert("1.8.3" == Utils.CleanAppVer("1.8.3."));
            Debug.Assert("1.8.3" == Utils.CleanAppVer("1.8.3.0"));
            Debug.Assert("0.1.10" == Utils.CleanAppVer("0.1.10"));
        }

        public static void ProgramVersionGreaterTests()
        {
            Debug.Assert(Utils.ProgramVersionGreater("1", "0.9"));
            Debug.Assert(Utils.ProgramVersionGreater("0.0.0.2", "0.0.0.1"));
            Debug.Assert(Utils.ProgramVersionGreater("1.0", "0.9"));
            Debug.Assert(Utils.ProgramVersionGreater("2.0.1", "2.0.0"));
            Debug.Assert(Utils.ProgramVersionGreater("2.0.1", "2.0"));
            Debug.Assert(Utils.ProgramVersionGreater("2.0.1", "2"));
            Debug.Assert(Utils.ProgramVersionGreater("0.9.1", "0.9.0"));
            Debug.Assert(Utils.ProgramVersionGreater("0.9.2", "0.9.1"));
            Debug.Assert(Utils.ProgramVersionGreater("0.9.11", "0.9.2"));
            Debug.Assert(Utils.ProgramVersionGreater("0.9.12", "0.9.11"));
            Debug.Assert(Utils.ProgramVersionGreater("0.10", "0.9"));
            Debug.Assert(Utils.ProgramVersionGreater("2.0", "2.0b35"));
            Debug.Assert(Utils.ProgramVersionGreater("1.10.3", "1.10.3b3"));
            Debug.Assert(Utils.ProgramVersionGreater("1.10.3", "1.10.3B3"));
            Debug.Assert(Utils.ProgramVersionGreater("88", "88a12"));
            Debug.Assert(Utils.ProgramVersionGreater("0.0.33", "0.0.33rc23"));
            Debug.Assert(Utils.ProgramVersionGreater("0.0.33", "0.0.33RC23"));
            Debug.Assert(Utils.ProgramVersionGreater("2.0b1", "2.0a2"));
        }

    }
}
