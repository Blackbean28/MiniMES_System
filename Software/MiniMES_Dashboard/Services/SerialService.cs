using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace MiniMES_Dashboard.Services
{
    public class SerialService
    {
        private SerialPort _serialPort;
        private List<byte> _buffer = new List<byte>(); // 스트림 파편화 방지용 버퍼

        // ViewModel로 파싱된 데이터를 전달하기 위한 델리게이트(Action)
        // (NodeID, Status, Temp, RPM, Current, Count)
        public Action<byte, byte, byte, ushort, ushort, byte> OnDataReceived;

        public SerialService()
        {
            _serialPort = new SerialPort();
            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        public bool Connect(string portName, int baudRate = 115200)
        {
            try
            {
                _serialPort.PortName = portName;
                _serialPort.BaudRate = baudRate;
                _serialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                // 실제 환경에서는 로그를 남겨야 합니다.
                Console.WriteLine($"Connection Error: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        // 하위 노드(설비)로 인터락(긴급 정지) 명령 하달
        public void SendInterlockCommand(byte targetNode)
        {
            if (_serialPort.IsOpen)
            {
                // STX(0x02) | TargetNode | Command(0xFF) | ... | ETX(0x03)
                byte[] commandPacket = { 0x02, targetNode, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03 };
                _serialPort.Write(commandPacket, 0, commandPacket.Length);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[1단계] 데이터 수신 이벤트 발생! 읽을 바이트 수: {_serialPort.BytesToRead}");
            int bytesToRead = _serialPort.BytesToRead;
            byte[] tempBuffer = new byte[bytesToRead];
            _serialPort.Read(tempBuffer, 0, bytesToRead);

            // 들어오는 데이터의 Hex을 확인합니다.
            System.Diagnostics.Debug.WriteLine($"[RAW DATA] {BitConverter.ToString(tempBuffer)}");

            // 1. 수신된 바이트를 내부 버퍼에 누적 (스트림 파편화 대응)
            _buffer.AddRange(tempBuffer);

            // 2. 버퍼에 최소 패킷 길이(11바이트) 이상 쌓였을 때 검사
            while (_buffer.Count >= 11)
            {
                if (_buffer[0] == 0x02) // 시작 바이트(STX) 확인
                {
                    if (_buffer[10] == 0x03) // 종료 바이트(ETX) 확인 (정상 패킷)
                    {
                        byte[] validPacket = _buffer.GetRange(0, 11).ToArray();
                        System.Diagnostics.Debug.WriteLine($"[2단계] 11바이트 완벽 파싱 성공! NodeID: {validPacket[1]}, Temp: {validPacket[3]}");
                        ParsePacket(validPacket);

                        // 처리한 패킷은 버퍼에서 삭제
                        _buffer.RemoveRange(0, 11);
                    }
                    else
                    {
                        // STX는 맞는데 끝이 ETX가 아님 -> 쓰레기 데이터이므로 STX 한 칸만 날리고 재검색
                        _buffer.RemoveAt(0);
                    }
                }
                else
                {
                    // 시작이 STX가 아니면 버림
                    _buffer.RemoveAt(0);
                }
            }
        }

        private void ParsePacket(byte[] packet)
        {
            byte nodeId = packet[1];
            byte status = packet[2];
            byte temp = packet[3];

            ushort rpm = BitConverter.ToUInt16(packet, 4);
            ushort current = BitConverter.ToUInt16(packet, 6);
            byte count = packet[8];

            try
            {
                // ViewModel로 데이터 전달
                OnDataReceived?.Invoke(nodeId, status, temp, rpm, current, count);

                // 🚩 [디버깅 3단계] UI 측(ViewModel)으로 에러 없이 넘어갔는지 확인
                System.Diagnostics.Debug.WriteLine($"[3단계] ViewModel로 데이터 전달 성공 (Node: {nodeId})");
            }
            catch (Exception ex)
            {
                // 🚩 [치명적 에러] 스레드 충돌이나 바인딩 에러 발생 시
                System.Diagnostics.Debug.WriteLine($"[치명적 에러] ViewModel 전달 실패: {ex.Message}");
            }
        }
    }
}