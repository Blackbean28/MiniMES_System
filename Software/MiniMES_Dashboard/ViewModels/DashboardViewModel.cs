using System.Windows;
using MiniMES_Dashboard.Models;
using MiniMES_Dashboard.Services;

namespace MiniMES_Dashboard.ViewModels
{
    public class DashboardViewModel
    {
        // 1. 화면에 바인딩 될 설비 데이터 모델 (1호기, 2호기)
        public EquipmentModel Node1 { get; set; }
        public EquipmentModel Node2 { get; set; }

        // 2. 백그라운드 통신 서비스
        private SerialService _serialService;

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
                    Node1.Status = status;
                    Node1.Temperature = temp;
                    Node1.RPM = rpm;
                    Node1.Current = current;
                    Node1.ProductionCount = count;
                }
                else if (nodeId == 2)
                {
                    Node2.Status = status;
                    Node2.Temperature = temp;
                    Node2.RPM = rpm;
                    Node2.Current = current;
                    Node2.ProductionCount = count;
                }
            });
        }

        // 인터락(긴급 정지) 명령 하달 메서드 (추후 UI 버튼과 연결)
        public void TriggerEmergencyStop(byte targetNode)
        {
            _serialService.SendInterlockCommand(targetNode);
        }
    }
}