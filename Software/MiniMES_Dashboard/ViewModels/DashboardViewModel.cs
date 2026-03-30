using MiniMES_Dashboard.Models;
using MiniMES_Dashboard.Services;
using System.Windows;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.IO;
using System.Collections.ObjectModel;

namespace MiniMES_Dashboard.ViewModels
{
    public class DashboardViewModel
    {
        // 1. 화면에 바인딩 될 설비 데이터 모델 (1호기, 2호기)
        public EquipmentModel Node1 { get; set; }
        public EquipmentModel Node2 { get; set; }

        // 2. 백그라운드 통신 서비스
        private SerialService _serialService;
        
        // 3. UI 버튼에 바인딩될 ICommand 속성
        public ICommand StopNode1Command { get; }
        public ICommand StopNode2Command { get; }

        // 4. 차트에 바인딩될 시계열 데이터 컬렉션 (1호기, 2호기 온도)
        public ChartValues<double> Node1TempValues { get; set; }
        public ChartValues<double> Node2TempValues { get; set; }

        // 5. 상태 변화 감지를 위한 이전 상태 저장 변수
        private byte _prevNode1Status = 0;
        private byte _prevNode2Status = 0;
        // 6. DataGrid에 바인딩될 로그 리스트 (WPF 특화: 아이템이 추가되면 화면이 자동 갱신됨)
        public ObservableCollection<EventLogModel> EventLogs { get; set; }
        public DashboardViewModel()
        {
            // 모델 초기화
            Node1 = new EquipmentModel { NodeId = 1, Status = 0, Temperature = 0, RPM = 0, Current = 0, ProductionCount = 0 };
            Node2 = new EquipmentModel { NodeId = 2, Status = 0, Temperature = 0, RPM = 0, Current = 0, ProductionCount = 0 };

            // 통신 서비스 초기화 및 이벤트 구독
            _serialService = new SerialService();
            _serialService.OnDataReceived += HandleDataReceived;

            // 실제 테스트 시에는 본인의 COM 포트 번호로 변경해야 합니다.
            _serialService.Connect("COM3", 115200);
            
            // 3. 커맨드 초기화 (버튼이 눌렸을 때 실행할 동작 정의)
            StopNode1Command = new RelayCommand(param => ExecuteEmergencyStop(1));
            StopNode2Command = new RelayCommand(param => ExecuteEmergencyStop(2));

            // 4. 차트 컬렉션 초기화
            Node1TempValues = new ChartValues<double>();
            Node2TempValues = new ChartValues<double>();

            EventLogs = new ObservableCollection<EventLogModel>();
        }
        // 3. 실제 하드웨어로 제어 명령(CAN 브로드캐스트용 UART 패킷)을 내리는 메서드
        private void ExecuteEmergencyStop(byte targetNode)
        {
            // 통신 서비스를 통해 0xFF(정지 명령) 패킷 전송
            _serialService.SendInterlockCommand(targetNode);

            // 화면 UI의 상태(Status)를 2(Error)로 즉각 변경하여 시각적 피드백 제공
            if (targetNode == 1) Node1.Status = 2;
            else if (targetNode == 2) Node2.Status = 2;

            // 실제 프로젝트라면 이 시점에 DB나 CSV에 "수동 긴급 정지 발동" 이력을 로깅해야 합니다.
            Console.WriteLine($"[Interlock] {targetNode}호기 긴급 정지 명령 하달됨.");
        }
        // 통신 클래스에서 데이터가 파싱되어 넘어올 때마다 실행되는 콜백 함수
        private void HandleDataReceived(byte nodeId, byte status, byte temp, ushort rpm, ushort current, byte count)
        {
            // [매우 중요] SerialPort의 DataReceived는 백그라운드 스레드에서 실행됩니다.
            // UI에 바인딩된 Model을 수정하려면 반드시 UI 스레드(Dispatcher)를 경유해야 충돌(Cross-Thread)이 나지 않습니다.
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (nodeId == 1)
                {
                    // 상태 변화(Transition) 감지 로직
                    if (_prevNode1Status != status)
                    {
                        LogEvent(1, status, temp);
                        _prevNode1Status = status;
                    }
                    Node1.Status = status;
                    Node1.Temperature = temp;
                    Node1.RPM = rpm;
                    Node1.Current = current;
                    Node1.ProductionCount = count;
                    // 3. 차트 데이터 추가 및 메모리 관리 (FIFO)
                    Node1TempValues.Add(temp);
                    // 실시간 차트는 데이터가 무한히 쌓이면 메모리 누수(OOM)가 발생하므로 최근 50개만 유지
                    if (Node1TempValues.Count > 50) Node1TempValues.RemoveAt(0);
                }
                else if (nodeId == 2)
                {
                    if (_prevNode2Status != status)
                    {
                        LogEvent(2, status, temp);
                        _prevNode2Status = status;
                    }
                    Node2.Status = status;
                    Node2.Temperature = temp;
                    Node2.RPM = rpm;
                    Node2.Current = current;
                    Node2.ProductionCount = count;
                    Node2TempValues.Add(temp);
                    if (Node2TempValues.Count > 50) Node2TempValues.RemoveAt(0);
                }
            });
        }

        // 인터락(긴급 정지) 명령 하달 메서드 (추후 UI 버튼과 연결)
        public void TriggerEmergencyStop(byte targetNode)
        {
            _serialService.SendInterlockCommand(targetNode);
        }

        // 3. 로그 생성 및 CSV 저장 통합 함수
        private void LogEvent(byte nodeId, byte status, byte temp)
        {
            string statusStr = status == 0 ? "NORMAL" : (status == 1 ? "WARNING" : "ERROR/INTERLOCK");
            string message = status == 1 ? $"온도 상승 주의 ({temp}℃)" : (status == 2 ? $"한계 초과 강제 차단 ({temp}℃)" : "정상 가동 복귀");

            var newLog = new EventLogModel
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                NodeId = $"Node {nodeId}",
                Status = statusStr,
                Message = message
            };

            // 화면 DataGrid 리스트 맨 위에 추가
            EventLogs.Insert(0, newLog);
            if (EventLogs.Count > 100) EventLogs.RemoveAt(EventLogs.Count - 1); // 메모리 관리 (최근 100개 유지)

            // CSV 파일에 비동기적 기록
            LogDataToCSV(newLog);
        }

        // 4. CSV 일자별 분할 로깅 로직
        private void LogDataToCSV(EventLogModel log)
        {
            try
            {
                string dateString = DateTime.Now.ToString("yyyyMMdd");
                string filePath = $"Equipment_EventLog_{dateString}.csv";
                string logLine = $"{log.Timestamp},{log.NodeId},{log.Status},{log.Message}\n";

                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, "Timestamp,NodeID,Status,Message\n");
                }
                File.AppendAllText(filePath, logLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CSV File Write Error: {ex.Message}");
            }
        }
    }
}