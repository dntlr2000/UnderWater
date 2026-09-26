# 모델 기반 아이템

원본 GLB는 `Assets/Resources/FBX/Items`에서 사용한다. 모델을 단순화하거나 새 월드 배치/자연 스폰은 추가하지 않는다. 인게임에서는 상점 → 공용 우편함 수령 → 인벤토리 사용/버리기 → 필드 재획득 경로를 이용한다.

| ID | 데이터 | 모델 | 구매 1개 | 직접 판매 1개 | 사용 효과 |
|---:|---|---|---:|---:|---|
| 9 | `item_seaweed` | `Seaweed_BAG.glb` | 4G | 1G | 재료, 사용해도 소모하지 않음 |
| 12 | `item_water` | `WaterBottle_BAG.glb` | 40G | 12G | 갈증 40 회복, 1개 소모 |
| 13 | `item_fish` | `SilverFish_BAG.glb` | 30G | 9G | 생선, 회복 효과가 없어 소모하지 않음 |
| 16 | `item_energy_bar` | `EnergyBar_BAG.glb` | 80G | 24G | 허기 30 회복, 1개 소모 |
| 18 | `item_gem` | `TealGem_BAG.glb` | 200G | 60G | 재료, 사용해도 소모하지 않음 |
| 20 | `item_cloth` | `Item_Cloth_BAG.glb` | 20G | 6G | 재료, 무게 0.2, 기준가 10G |

직접 판매 합계는 `floor(기준가 × 실제 판매 수량 × 0.6)`이다. 해초 10개는 12G이며, 단가를 먼저 반올림해 곱하지 않는다. 기존 SellBox 자동 판매의 기준가 100% 정책은 유지한다.

각 아이템은 다음 경로로 연결된다.

- UI: `Assets/Resources/Item/Item{ID}.png` — 1000×1000 RGBA, 개별 Sprite.
- 데이터: `Assets/Resources/Data/ItemData/item_*.asset`, 원본 표 `Data/TSV/03_Items.txt`.
- 필드: `Assets/Resources/FieldItem/Object{ID}.prefab` — 원본 GLB 자식, Collider/Rigidbody/FieldItem/Photon 구성.
- 상점: `Assets/Resources/Data/ShopCatalog.asset` — 기존 실제 상품 5종과 새 모델 연결 6종. 표시용 ID 0은 제외한다.

루트 `FBX/Item_Cloth_BAG.glb`는 원본으로 보존하며, 신규 연결은 `FBX/Items/Item_Cloth_BAG.glb`로 통일한다.

## 처리 순서

1. `ShopManager`가 카탈로그의 상품 ID와 수량으로 구매를 요청한다. 개인 돈은 아직 차감하지 않는다.
2. `ShopPurchaseService`가 고정 요청 ID를 만들어 방장에게 전달한다. 확인 중에는 같은 플레이어의 중복 구매와 돈 출고를 막는다.
3. 방장이 카탈로그 가격, 잔액, 우편함 전량 수용 여부를 검사한다. 공간이 부족하면 실패 결과만 기록하며 돈/아이템은 변경하지 않는다.
4. 성공하면 우편함 내용, 구매 비용, 거래 영수증을 동일한 공유 상태에 기록한다. 방에 확정된 기록을 받은 구매자만 비용을 한 번 반영한다. 저장 복원·재전송도 같은 영수증을 확인한다.
5. 우편함 수령은 기존 출고 기능을 사용한다. 필드 재획득은 수령 성공 영수증을 확인한 뒤 원본을 제거하고 수집 진행을 보고한다. 가득 찬 인벤토리는 원본 필드 아이템을 남긴다.
6. 물/에너지바는 인벤토리 소유자의 상태를 회복시킨다. 효과가 없는 재료와 생선은 소비되지 않는다.

상점 구매와 우편함 수령 자체는 필드 수집 퀘스트 진행으로 보고하지 않는다. UI/직접 판매는 일반 인벤토리 슬롯만 사용한다. 장착 슬롯은 직접 판매 목록에서 제외한다.

## 아이콘 재렌더링

Blender 5.0.1의 원본 glTF 임포터와 Cycles로 렌더한다. 모델/내장 재질 파일은 수정하지 않는다. 카메라·조명·투명 배경 설정은 `render_icons.py`에 들어 있다.

PowerShell 예시 — 실행별 고유 폴더를 만들고 작업 종료 시 해당 폴더만 정리한다.

```powershell
$itemRenderTemp = Join-Path 'E:\CodexTemp' ('item-icons-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $itemRenderTemp
$env:TEMP = $itemRenderTemp
$env:TMP = $itemRenderTemp
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --factory-startup --python 'E:\Unity\Underwater\Tools\Items\render_icons.py' -- --project 'E:\Unity\Underwater'
```

일부만 렌더하려면 끝에 `--ids 12 20`을 붙인다. 기본은 6종 모두, 128 samples다.

## Unity 재생성·검증

유효한 라이선스로 Unity 6000.0.69f1을 열고 Play Mode를 종료한 뒤 사용한다.

1. `Overflown > Items > Build Six Model Items`: 현재 PNG/TSV를 기준으로 대상 데이터, 필드 프리팹, 상점 연결을 재생성한다. 대상 6종 프리팹의 수동 편집은 이 과정에서 다시 생성된다. 기존 상품과 데이터 GUID는 유지한다.
2. `Overflown > Items > Validate Six Model Items`: 실제 임포트된 Sprite/GLB/재질/Photon 참조, 중복 ID, 거래 계산과 Unity JSON 복원을 검사한다. 사용자 씬과 저장 파일은 수정하지 않는다.
3. Play Mode에서 각 품목의 구매·우편함 수령·사용·버리기·재획득·판매를 확인한다. 두 번째 상점 페이지의 가격과 빈 슬롯 표시도 확인한다.
4. 두 클라이언트에서 우편함 가득 참, 동시 구매/동시 줍기, 확인 중 창 닫기, 응답 전 방장 변경, 저장/재접속, 수령 완료 후 늦은 입장을 확인한다. 실패한 거래의 차감·아이템 유실과 성공 거래의 중복 지급/차감이 없어야 한다.

배치 검증의 `-projectPath`, `-logFile` 및 임시 경로는 E 드라이브를 사용한다. 선택적으로 `ITEM_VALIDATION_OUTPUT=E:\CodexValidation\<실행ID>\evidence\unity-asset-checks.txt`를 지정하면 에디터 검증 결과를 파일로 남긴다.
