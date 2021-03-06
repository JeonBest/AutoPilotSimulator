# AutoPilotSimulator

자율주행 AI 시뮬레이터 - 졸업프로젝트

## 1. DrivingSimulator

#### (with. NWH Vehicle Physics2)

차로 중앙 유지, 차선 변경

### (1) Guide Pivot System

AI Driver 들의 차선인식과 추후 Navigation System을 위한 방향제어에 도움을 주는 시스템

![Guide Pivot System Image 1](./Images/GuidePivotSystem1.png) - Image.1 -

![Guide Pivot System Image 2](./Images/GuidePivotSystem2.png) - Image.2 -

##### 참고영상: (https://youtu.be/Sd1zf1EzL4s)

### Good Driver

- 일정한 속도로 주행
- 한번에 1차로씩 변경
- 끝차로에서 나들목으로 진출
- 1차로 주행시 후행차량이 빠르게 다가오면 2차로로 변경 (양보)
- 차로 변경 1.5초전 깜빡이
- 차로 변경시 변경하려는 차로에 차량이 있는지 확인 (있으면, 변경 미룸)

### Bad Driver

- 랜덤하게 브레이킹, 속도 랜덤하게 변경
- 한번에 2개이상 차로변경
- 끝차로가 아닌 차로에서 나들목으로 진출
- 후행차량에게 양보하지 않음
- 차로 변경시 깜빡이를 넣지 않거나 변경중 깜빡이
- 차로 변경시 변경하는 차로에 차량이 있는지 확인하지 않음

### Good, Bad Driver 공통점

- 차로변경 하지 않을 때는 현 차로 중앙을 유지
- 전방 차량을 인식하여 추돌사고를 회피하려고 함 (차선변경 혹은 감속)
