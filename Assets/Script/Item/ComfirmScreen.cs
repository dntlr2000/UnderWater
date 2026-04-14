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
    public Button ComfirmButtonA;
    public Button ComfirmButtonB;
    public Scrollbar amountBar;

    // 델리게이트를 사용하여 확인 버튼이 눌렸을 때 실행할 함수를 외부에서 지정할 수 있도록 구현
    // 사용 예시 : 객체.onConfirmAction = this.ConfirmThrowItem;
    // 이렇게 하면 ConfirmThrowItem 함수가 onConfirmAction에 연결되고, 확인 버튼이 눌렸을 때 해당 함수가 실행됩니다.
    public Action onConfirmAction;
    public Action onConfirmAction2;

    public void ConstructComfirmScreen(int itemId, int itemPrice = 0)
    {
        itemNameText.text = ItemDatabase.Instance.getItemName(itemId);
        amount = 1;
        amountBar.value = 0;
        amountText.text = "1 / 10";
        price = itemPrice;
        if (itemPrice == 0)
            priceText.text = "";
        else
            priceText.text = "G " + price;
    }

    public void onScrollAmountChanged()
    {
        amount = Mathf.RoundToInt(amountBar.value * (amountBar.numberOfSteps - 1)) + 1;
        amountText.text = $"{amount} / 10";
        //priceText.text = "\\ " + (shopPrice[selectedID] * amount);
        if (price == 0)
            priceText.text = "";
        else
            priceText.text = "G " + price * amount;
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
