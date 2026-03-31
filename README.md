# 🏭 Mini-MES: IT-OT 융합형 분산 제어 및 모니터링 시스템

## 1. Project Overview (프로젝트 개요)
본 프로젝트는 생산 라인의 하위 설비(OT) 데이터 수집부터 상위 대시보드(IT)의 모니터링 및 제어까지 아우르는 **통합 제조 실행 시스템(Mini-MES)의 MVP(Minimum Viable Product)**입니다. 
단일 컨트롤러에 의존하는 모놀리식(Monolithic) 제어의 한계를 극복하고자, **게이트웨이를 통한 라우팅과 엣지 노드의 분산 제어 아키텍처**를 설계하여 시스템의 실시간성(Real-time)과 신뢰성(Reliability)을 검증했습니다.

## 2. System Architecture (시스템 아키텍처)
*(여기에 Draw.io 등으로 그린 아키텍처 다이어그램 이미지를 삽입하세요)*

* **[IT Layer] PC Dashboard (C# WPF):** 실시간 데이터 시각화, 예지보전 트렌드 분석, 상태 전이 기반 로깅 및 긴급 제어 명령 하달.
* **[Gateway Layer] Master Node (STM32 F429 + FreeRTOS):** CAN-UART 이종 통신 간 비동기 라우팅, 병목 현상 완화를 위한 Queue 기반 버퍼링.
* **[Edge Layer] Equipment Node (STM32 F429):** 센서 데이터(Temp, RPM, Current) 취합 및 CAN 송신, 최고 우선순위 ISR 기반의 하드웨어 인터락(Fail-Safe) 구동.

---

## 3. Core Technology & Justification (핵심 기술 및 도입 당위성)

### 💻 IT (상위 시스템): C# WPF 기반 비동기 대시보드
* **ICommand 기반 UI-로직 분리 (Decoupling):**
  * **도입 배경:** 기존 Event Handler(Click) 방식은 UI 스레드가 멈추면 제어 명령도 마비되는 안전 결함이 존재함.
  * **당위성/효과:** MVVM 패턴의 `ICommand`를 적용하여 비즈니스 로직과 UI를 완벽히 분리, 화면 렌더링 부하와 무관하게 즉각적인 제어 패킷(Interlock)이 하달되는 산업용 표준 구조 확립.
* **이벤트 드리븐(Event-Driven) 로깅 및 메모리 최적화:**
  * **도입 배경:** 밀리초 단위로 쏟아지는 센서 데이터를 모두 파일(I/O)에 쓰면 병목 및 시스템 다운 발생.
  * **당위성/효과:** 이전 상태(Previous State)와 현재 상태를 비교하여 전이(Transition)가 일어난 시점에만 선별적으로 CSV에 기록하도록 최적화. LiveCharts 시계열 데이터는 FIFO 큐 버퍼링을 적용해 OOM(Out of Memory)을 원천 차단.

### ⚙️ OT (하위 시스템): STM32 & FreeRTOS 기반 하드웨어 제어망
* **통신 병목 해결을 위한 RTOS Task & Queue 설계:**
  * **도입 배경:** 고속의 CAN 통신(500kbps) 데이터를 저속의 UART(115.2kbps)로 변환 시, 폴링(Polling) 방식을 사용하면 데이터 유실(Data Loss)이 발생함.
  * **당위성/효과:** 수신은 하드웨어 ISR로 즉각 처리하여 큐(Queue)에 적재하고, UART 송신 전담 Task를 분리 구성함. 이를 통해 느린 전송 속도가 시스템 전체를 블로킹(Blocking)하지 않도록 비동기 파이프라인 구축.
* **결정론적 응답성(Deterministic)을 위한 Task Notification 및 하드웨어 필터링:**
  * **도입 배경:** 엣지 노드에서 올라오는 트래픽 폭주 상황에서도 상위의 '긴급 정지' 명령은 즉각 처리되어야 함.
  * **당위성/효과:** 수신된 긴급 제어 패킷은 무거운 Queue 대신 오버헤드가 가장 적은 `Task Notification`으로 즉시 컨텍스트 스위칭(Context Switching)함. CAN 제어 프레임 ID를 최우선 순위(`0x001`)로 할당하여, 물리적 버스 충돌 상황에서도 제어 명령이 1순위로 하달되는 Fail-Safe 시스템 구현.
* **단일 코드베이스(Single Codebase) 매크로 스위칭:**
  * **도입 배경:** 다수의 설비 노드 펌웨어를 개별 프로젝트로 관리하면 유지보수 비용이 급증함.
  * **당위성/효과:** C 전처리기 매크로(`#define NODE_ID`)를 활용하여 소스 코드 하나로 1호기, 2호기 펌웨어를 동적 생성할 수 있는 양산 친화적 형상 관리 적용.

---

## 4. Key Features (주요 기능)
1. **Real-time Predictive Maintenance:** LiveCharts를 활용한 온도/전류 트렌드 실시간 시각화 및 FIFO 메모리 관리.
2. **Hardware-Level Interlock:** 대시보드 비상 정지 명령 시, OS 스케줄링을 거치지 않고 하위 보드의 ISR에서 즉각 액추에이터 구동 차단.
3. **Asynchronous Traceability:** 시스템 상태 변화 시점에만 동작하는 비동기 이벤트 DataGrid 표출 및 일자별 CSV 로깅.

## 5. Environment & Tech Stack
* **Language:** C# (WPF, .NET), C (Firmware)
* **MCU/Hardware:** STM32F429I-DISC1, CAN Transceiver
* **OS / IDE:** FreeRTOS (CMSIS_V1), STM32CubeIDE, Visual Studio 2022
* **Communication:** CAN 2.0B (500kbps), UART (115200bps)
* **Version Control:** Git Monorepo (Hardware + Software 통합 관리)
