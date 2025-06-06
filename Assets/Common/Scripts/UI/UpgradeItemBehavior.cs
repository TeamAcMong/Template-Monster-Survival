using OctoberStudio.Audio;
using OctoberStudio.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OctoberStudio.Upgrades.UI
{
    public class UpgradeItemBehavior : MonoBehaviour
    {
        [Header("Info")]
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text levelLabel;

        [Header("Button")]
        [SerializeField] Button upgradeButton;
        [SerializeField] Sprite enabledButtonSprite;
        [SerializeField] Sprite disabledButtonSprite;

        [Space]
        [SerializeField] ScalingLabelBehavior costLabel;
        [SerializeField] GameObject upgradedLabel;

        public CurrencySave GoldCurrency { get; private set; }

        public UpgradeData Data { get; private set; }
        public int UpgradeLevelId { get; private set; }

        private void Start()
        {
            upgradeButton.onClick.AddListener(UpgradeButtonClick);
        }

        public void Init(UpgradeData data, int levelId)
        {
            if(GoldCurrency == null)
            {
                GoldCurrency = GameController.SaveManager.GetSave<CurrencySave>("gold");
                GoldCurrency.onGoldAmountChanged += OnGoldAmountChanged;
            }

            Data = data;
            UpgradeLevelId = levelId;

            RedrawVisuals();
        }

        private void RedrawVisuals()
        {
            if(UpgradeLevelId >= Data.LevelsCount - 1)
            {
                levelLabel.text = "Max Level";
            }
            else
            {
                levelLabel.text = $"LEVEL {UpgradeLevelId + 1}";
            }
            
            titleLabel.text = Data.Title;
            iconImage.sprite = Data.Icon;

            RedrawButton();
        }

        private void RedrawButton()
        {
            if (UpgradeLevelId >= Data.LevelsCount)
            {
                costLabel.gameObject.SetActive(false);
                upgradedLabel.gameObject.SetActive(true);

                upgradeButton.enabled = false;
                upgradeButton.image.sprite = disabledButtonSprite;
            }
            else
            {
                costLabel.gameObject.SetActive(true);
                upgradedLabel.gameObject.SetActive(false);

                var level = Data.GetLevel(UpgradeLevelId);
                costLabel.SetAmount(level.Cost);

                if (GoldCurrency.CanAfford(level.Cost))
                {
                    upgradeButton.enabled = true;
                    upgradeButton.image.sprite = enabledButtonSprite;
                } else
                {
                    upgradeButton.enabled = false;
                    upgradeButton.image.sprite = disabledButtonSprite;
                }
            }
        }

        private void UpgradeButtonClick()
        {
            var level = Data.GetLevel(UpgradeLevelId);

            GameController.UpgradesManager.IncrementUpgradeLevel(Data.UpgradeType);
            UpgradeLevelId++;
            GoldCurrency.Withdraw(level.Cost);

            RedrawVisuals();

            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
        }

        private void OnGoldAmountChanged(int amount)
        {
            RedrawButton();
        }

        private void OnDestroy()
        {
            if(GoldCurrency != null)
            {
                GoldCurrency.onGoldAmountChanged -= OnGoldAmountChanged;
            }
        }
    }
}