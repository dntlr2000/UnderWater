using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ComfirmScreen : MonoBehaviour
{
    public RawImage background;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI priceText;

    public int amount;
    public int price;
    private Func<int, int> totalPrice;
    private int maximumAmount = 10;
    public Button ComfirmButtonA;
    public Button ComfirmButtonB;
    public Scrollbar amountBar;

    // 델리게이트를 사용하여 확인 버튼이 눌렸을 때 실행할 함수를 외부에서 지정할 수 있도록 구현
    // 사용 예시 : 객체.onConfirmAction = this.ConfirmThrowItem;
    // 이렇게 하면 ConfirmThrowItem 함수가 onConfirmAction에 연결되고, 확인 버튼이 눌렸을 때 해당 함수가 실행됩니다.
    public Action onConfirmAction;
    public Action onConfirmAction2;

    // 기존 창고·버리기 호출의 단가 기반 확인창을 유지합니다.
    public void ConstructComfirmScreen(int itemId, int itemPrice = 0)
    {
        price = itemPrice;
        ConstructComfirmScreen(itemId, itemPrice == 0 ? null : n => itemPrice * n, 10);
    }

    // 구매·판매가 실제로 사용하는 수량별 계산기로 확인 금액을 표시합니다.
    public void ConstructComfirmScreen(int itemId, Func<int, int> quote, int maxAmount)
    {
        itemNameText.text = ItemDatabase.Instance.getItemName(itemId);
        maximumAmount = Math.Max(1, maxAmount);
        totalPrice = quote;
        amount = 1;
        amountBar.numberOfSteps = Math.Max(2, maximumAmount);
        amountBar.SetValueWithoutNotify(0);
        onScrollAmountChanged();
    }

    // 슬라이더 수량과 최종 합계는 같은 시점에 계산합니다.
    public void onScrollAmountChanged()
    {
        amount = Mathf.RoundToInt(amountBar.value * (maximumAmount - 1)) + 1;
        amountText.text = $"{amount} / {maximumAmount}";
        priceText.text = totalPrice == null ? "" : "G " + totalPrice(amount);
    }

    public void onClickExit()
    {
        gameObject.SetActive(false);
    }

    public void onClickComfirm()
    {
        // onConfirmAction에 연결된 함수가 있다면, 결정된 amount를 전달하며 실행합니다.
        onConfirmAction?.Invoke();

        // 확인을 눌렀으니 창을 닫아줍니다.
        onClickExit();
    }

    public void onClickComfirm2()
    {
        onConfirmAction2?.Invoke();
        onClickExit();
    }


}
