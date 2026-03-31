# 🚀 Multi-Node Distributed Control System
## (Mini-MES Prototype)

> **STM32(FreeRTOS)와 C# WPF를 활용한 다중 노드 실시간 모니터링 및 원격 긴급 제어 시스템**

본 프로젝트는 산업 현장의 **제조 실행 시스템(MES)** 아키텍처를 모사하여, 게이트웨이를 중심으로 다중 하위 노드(설비)의 데이터를 실시간으로 수집하고 위급 상황 시 즉각적인 인터락을 수행하는 분산 제어 시스템입니다.

---

## 🛠 Tech Stack

### **Hardware & Firmware**
* **MCU:** STM32F429ZI (Gateway & Nodes)
* **OS:** FreeRTOS (Real-time Task Scheduling)
* **Language/IDE:** C (STM32CubeIDE), HAL Drivers
* **Communication:** CAN 2.0B (Internal Bus), UART (External/PC Link)

### **Software (Dashboard)**
* **Framework:** C# WPF (.NET)
* **Pattern:** MVVM (Model-View-ViewModel)
* **Library:** LiveCharts 

---

## 🏗 System Architecture

시스템은 Uplink(데이터 수집)와 Downlink(명령 하달)의 이종 통신 파이프라인으로 구성됩니다.

1.  **Nodes (1 & 2):** 500ms 주기로 센서 데이터(Temp, RPM, Current) 생성 및 CAN 브로드캐스트.
2.  **Gateway:** 다중 노드 CAN 데이터 병합 및 UART 프로토콜 변환(Bridge).
3.  **Dashboard:** UART 패킷 파싱, 실시간 차트 렌더링 및 CSV 데이터 로깅.

---

## 📑 Communication Protocol (11-Byte)

데이터 무결성을 위해 **STX/ETX 구조**의 사용자 정의 프로토콜을 설계하였습니다.

| Index | Field | Value/Description |
| :--- | :--- | :--- |
| 0 | **STX** | Start of Text (0x02) |
| 1 | **Node ID** | 0x01 (Node 1) / 0x02 (Node 2) |
| 2~9 | **Data/CMD** | Payload (Sensor Data or Control CMD) |
| 10 | **ETX** | End of Text (0x03) |

---

## 🚨 Key Troubleshooting (핵심 기술 역량)

단순 기능 구현을 넘어, 개발 과정에서 발생한 하드웨어 및 소프트웨어 제약 사항을 논리적으로 해결한 기록입니다.

### 1️⃣ In-band Signaling 충돌 방어 (State Machine Parser)
* **Issue:** 통신 시작 문자(STX: 0x02)와 2호기의 ID(0x02) 값이 겹치면서, 수신 버퍼 인덱스가 0으로 초기화되어 2호기 제어 명령이 무시되는 현상 발생.
* **Solution:** 단순 조건문이 아닌 **상태 머신(State Machine) 구조**를 도입. `rxIndex == 0`일 때만 STX를 판별하도록 설계하여, 데이터 영역의 0x02가 제어 문자로 오인되는 현상을 완벽히 방어.

### 2️⃣ RTOS Task-ISR 동기화 및 지연 최소화
* **Issue:** 고속 CAN 통신 시 데이터 병목으로 인한 큐 오버플로우 및 제어 지연 발생.
* **Solution:** `osMessageQ`의 요소 크기를 구조체(`Packet_t`) 단위로 정밀 할당하고, `portYIELD_FROM_ISR`을 통해 인터럽트 직후 즉각적인 Context Switching을 트리거하여 시스템 실시간성 확보.

### 3️⃣ 컴파일러 최적화 대응 및 메모리 동기화 (`volatile`)
* **Issue:** CAN 인터럽트(ISR)에서 변경한 긴급 정지 상태 변수가 메인 가동 Task에 즉시 반영되지 않아 장비 정지가 지연되는 현상.
* **Solution:** 컴파일러 최적화에 의한 레지스터 캐싱을 방지하기 위해 핵심 제어 변수에 **`volatile`** 키워드 적용. 메모리 직접 참조를 강제하여 인터럽트 상황에서의 제어 신뢰성 증명.

---

## 📈 주요 기능

* **Real-time Monitoring:** 하위 노드 2종의 가동 데이터를 LiveCharts로 시각화.
* **Remote Interlock:** 비상 상황 시 중앙 대시보드에서 10ms 이내의 속도로 개별 노드 전력 차단.
* **Data Traceability:** 모든 이벤트 및 가동 이력을 일자별 CSV 파일로 자동 로깅하여 추적성 확보.

---
