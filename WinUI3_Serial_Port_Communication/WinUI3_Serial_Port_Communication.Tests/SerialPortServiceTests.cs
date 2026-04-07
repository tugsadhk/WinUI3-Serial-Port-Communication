using System;
using System.IO.Ports;
using WinUI3_Serial_Port_Communication;
using Xunit;

namespace WinUI3_Serial_Port_Communication.Tests
{
    public class SerialPortServiceTests
    {
        // ------------------------------------------------------------------ //
        //  Initial state                                                      //
        // ------------------------------------------------------------------ //

        [Fact]
        public void IsConnected_InitiallyFalse()
        {
            using var svc = new SerialPortService();
            Assert.False(svc.IsConnected);
        }

        // ------------------------------------------------------------------ //
        //  Disconnect when not connected                                      //
        // ------------------------------------------------------------------ //

        [Fact]
        public void Disconnect_WhenNotConnected_DoesNotThrow()
        {
            using var svc = new SerialPortService();
            var ex = Record.Exception(() => svc.Disconnect());
            Assert.Null(ex);
        }

        // ------------------------------------------------------------------ //
        //  Write when not connected                                           //
        // ------------------------------------------------------------------ //

        [Fact]
        public void Write_WhenNotConnected_ThrowsInvalidOperationException()
        {
            using var svc = new SerialPortService();
            Assert.Throws<InvalidOperationException>(() => svc.Write(new byte[] { 0x41 }));
        }

        // ------------------------------------------------------------------ //
        //  Connect with invalid port → throws, IsConnected stays false       //
        // ------------------------------------------------------------------ //

        [Fact]
        public void Connect_InvalidPort_Throws_AndIsConnectedRemainsfalse()
        {
            using var svc = new SerialPortService();
            var cfg = new SerialPortConfig { PortName = "COM_INVALID_9999", BaudRate = 9600 };

            // SerialPort.Open() will throw for a non-existent port name
            Assert.ThrowsAny<Exception>(() => svc.Connect(cfg));
            Assert.False(svc.IsConnected);
        }

        // ------------------------------------------------------------------ //
        //  Dispose can be called multiple times safely                       //
        // ------------------------------------------------------------------ //

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var svc = new SerialPortService();
            svc.Dispose();
            var ex = Record.Exception(() => svc.Dispose());
            Assert.Null(ex);
        }
    }
}
