using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiniMES_Dashboard.Models
{
    public class EquipmentModel : INotifyPropertyChanged
    {
        private byte _nodeId;
        private byte _status;
        private byte _temperature;
        private ushort _rpm;
        private ushort _current;
        private byte _productionCount;

        public byte NodeId
        {
            get => _nodeId;
            set { _nodeId = value; OnPropertyChanged(); }
        }

        public byte Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public byte Temperature
        {
            get => _temperature;
            set { _temperature = value; OnPropertyChanged(); }
        }

        public ushort RPM
        {
            get => _rpm;
            set { _rpm = value; OnPropertyChanged(); }
        }

        public ushort Current
        {
            get => _current;
            set { _current = value; OnPropertyChanged(); }
        }

        public byte ProductionCount
        {
            get => _productionCount;
            set { _productionCount = value; OnPropertyChanged(); }
        }

        // 데이터 바인딩을 위한 필수 이벤트
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}